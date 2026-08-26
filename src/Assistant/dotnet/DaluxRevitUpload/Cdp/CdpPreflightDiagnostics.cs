namespace DaluxRevitUpload;

internal sealed class CdpPreflightDiagnostics
{
    private readonly DaluxAutomationConfig _config;
    private readonly Action<string> _log;

    public CdpPreflightDiagnostics(DaluxAutomationConfig config, Action<string> log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Runs cheap checks before clicking Upload so later CDP failures have useful context.
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        _log("[*] Pre-flight diagnostics:");
        LogEnvironmentState();

        var revitStart = TryReadRevitStartTime();
        LogRevitEnvironmentTiming(revitStart);

        if (_config.DebuggingPort == 0)
        {
            _log("    Ephemeral port mode — skipping bind probe and auto-free");
            return true;
        }

        var portState = CdpPortInspector.ProbePortState(_config.DebuggingPort);
        _log($"    Port {_config.DebuggingPort} status = {portState}");

        if (portState == CdpPortState.InUseByOther &&
            !await TryAutoFreeCdpPortAsync(revitStart, cancellationToken))
        {
            return false;
        }

        await LogVersionProbeAsync(cancellationToken);
        return true;
    }

    private void LogEnvironmentState()
    {
        var envValue = Environment.GetEnvironmentVariable(
            "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            EnvironmentVariableTarget.User);
        _log($"    WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS (user) = \"{envValue ?? "<null>"}\"");

        var wv2Version = WindowsRegistryDiagnostics.ReadWebView2RuntimeVersion();
        _log($"    WebView2 Runtime version = {wv2Version ?? "<not installed or not found>"}");
    }

    private DateTime? TryReadRevitStartTime()
    {
        try
        {
            return Process.GetProcessById(_config.RevitProcessId).StartTime;
        }
        catch
        {
            // Process lookup failure is handled separately in RevitDaluxRibbonAutomation.
            return null;
        }
    }

    private void LogRevitEnvironmentTiming(DateTime? revitStart)
    {
        var envWriteTime = WindowsRegistryDiagnostics.ReadUserEnvironmentLastWriteTime();
        _log($"    Revit (PID {_config.RevitProcessId}) start time = {revitStart?.ToString("s") ?? "<unknown>"}");
        _log($"    Env var registry last-write    = {envWriteTime?.ToString("s") ?? "<unknown>"}");

        if (revitStart.HasValue && envWriteTime.HasValue && revitStart.Value < envWriteTime.Value)
        {
            _log("[!] Revit was started BEFORE the env var was registered. It will not have inherited");
            _log("[!] the --remote-debugging-port flag. Close Revit fully and relaunch it, then retry.");
        }
    }

    private async Task LogVersionProbeAsync(CancellationToken cancellationToken)
    {
        var versionProbe = await CdpClient.ProbeVersionAsync(_config.DebuggingPort, cancellationToken);
        if (versionProbe.CdpUp)
        {
            var trimmed = (versionProbe.VersionJson ?? "").Replace('\n', ' ').Replace('\r', ' ');
            if (trimmed.Length > 300) trimmed = trimmed[..300] + "...";
            _log($"    /json/version responded: {trimmed}");
        }
        else
        {
            _log($"    /json/version did not respond ({versionProbe.Error})");
        }
    }

    private async Task<bool> TryAutoFreeCdpPortAsync(DateTime? revitStart, CancellationToken cancellationToken)
    {
        var ownerPid = CdpPortInspector.FindPortOwnerPid(_config.DebuggingPort);
        if (ownerPid == null)
        {
            _log($"    Port {_config.DebuggingPort} owner PID = <unknown>");
            return true;
        }

        Process? ownerProc = null;
        try { ownerProc = Process.GetProcessById((int)ownerPid.Value); }
        catch { /* process exited between GetExtendedTcpTable and now */ }

        var ownerName = ownerProc?.ProcessName ?? "<unknown>";
        _log($"    Port {_config.DebuggingPort} owner = {ownerName} (PID {ownerPid})");

        var kind = CdpPortInspector.ClassifyPortOwner(ownerPid.Value, ownerProc, _config.RevitProcessId);
        return kind switch
        {
            CdpPortOwnerKind.Expected => LogExpectedOwner(),
            CdpPortOwnerKind.SystemCritical => LogSystemCriticalOwner(ownerName),
            CdpPortOwnerKind.Foreign => await HandleForeignOwnerAsync(ownerProc, ownerPid.Value, ownerName, revitStart, cancellationToken),
            _ => true
        };
    }

    private bool LogExpectedOwner()
    {
        _log("    Owner is Revit or a child of Revit — expected. Proceeding.");
        return true;
    }

    private bool LogSystemCriticalOwner(string ownerName)
    {
        _log($"[!] Port {_config.DebuggingPort} is held by a system-critical process ({ownerName}).");
        _log("[!] Cannot safely terminate. Change the CDP port in Advanced options and retry.");
        return false;
    }

    private async Task<bool> HandleForeignOwnerAsync(
        Process? ownerProc,
        uint ownerPid,
        string ownerName,
        DateTime? revitStart,
        CancellationToken cancellationToken)
    {
        if (!_config.AutoFreeCdpPort)
        {
            _log($"[!] Port {_config.DebuggingPort} is held by foreign process {ownerName} (PID {ownerPid}).");
            _log("[!] Auto-free CDP port is disabled. Terminate it manually or change the port in Advanced.");
            return false;
        }

        DateTime? ownerStart = null;
        try { ownerStart = ownerProc?.StartTime; } catch { }

        _log($"[*] Port {_config.DebuggingPort} is held by foreign process {ownerName} (PID {ownerPid}). Terminating...");
        try
        {
            ownerProc!.Kill();
            ownerProc.WaitForExit(2000);
        }
        catch (Exception ex)
        {
            _log($"[!] Failed to terminate {ownerName} (PID {ownerPid}): {ex.Message}");
            _log("[!] Close it manually, or change the CDP port in Advanced and retry.");
            return false;
        }

        await Task.Delay(500, cancellationToken);

        var postState = CdpPortInspector.ProbePortState(_config.DebuggingPort);
        if (postState != CdpPortState.Free)
        {
            _log($"[!] Port {_config.DebuggingPort} is still held after termination. Abort.");
            return false;
        }

        _log($"[+] Port {_config.DebuggingPort} is now free.");
        if (ownerStart.HasValue && revitStart.HasValue && ownerStart.Value < revitStart.Value)
        {
            _log("[*] Heads up: the offender was running before Revit started. If the next");
            _log("    attempt still can't see the Dalux popup, restart Revit fully and retry.");
        }

        return true;
    }
}
