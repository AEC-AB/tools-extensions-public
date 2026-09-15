namespace DaluxRevitUpload;

internal sealed class CdpTimeoutDiagnostics
{
    private readonly DaluxAutomationConfig _config;
    private readonly Action<string> _log;

    public CdpTimeoutDiagnostics(DaluxAutomationConfig config, Action<string> log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public async Task LogAsync(CancellationToken cancellationToken)
    {
        DaluxDevToolsActivePortFile.LogState(_log);

        var candidatePorts = CdpPortInspector.FindWebViewCdpPorts(_config.RevitProcessId);
        if (candidatePorts.Count > 0)
        {
            await LogWebViewPortsAsync(candidatePorts, cancellationToken);
            return;
        }

        LogConfiguredPortFallback();
    }

    private async Task LogWebViewPortsAsync(
        IReadOnlyList<WebViewCdpPort> candidatePorts,
        CancellationToken cancellationToken)
    {
        _log($"[*] {candidatePorts.Count} msedgewebview2 listening port(s) found post-timeout:");
        foreach (var portInfo in candidatePorts)
        {
            var parentNote = portInfo.IsRevitDescendant ? "Revit descendant" : "parent chain not Revit";
            var probe = await CdpClient.ProbeVersionAsync(portInfo.Port, cancellationToken);
            if (!probe.CdpUp)
            {
                _log($"    port={portInfo.Port} pid={portInfo.Pid} ({parentNote}) — /json/version did not respond ({probe.Error ?? "no detail"}).");
                continue;
            }

            await LogTabsAsync(portInfo, parentNote, cancellationToken);
        }

        _log("[!] No CDP target containing 'dalux' was found on any msedgewebview2 process.");
        _log("[!] The Dalux popup may be running with AdditionalBrowserArguments that disable CDP,");
        _log("[!] or the Dalux addon explicitly overrides the env var. Next step: verify with");
        _log("[!]   Get-CimInstance Win32_Process | ? { $_.Name -eq 'msedgewebview2.exe' } |");
        _log("[!]   select ProcessId, ParentProcessId, CommandLine");
        _log("[!] that the popup's msedgewebview2.exe actually carries --remote-debugging-port=.");
    }

    private async Task LogTabsAsync(
        WebViewCdpPort portInfo,
        string parentNote,
        CancellationToken cancellationToken)
    {
        var tabs = await CdpClient.GetAllTabsAsync(portInfo.Port, cancellationToken);
        _log($"    port={portInfo.Port} pid={portInfo.Pid} ({parentNote}) — {tabs.Count} debuggable target(s):");
        for (int i = 0; i < tabs.Count; i++)
        {
            var t = tabs[i];
            var title = TrimForLog(t.Title, 120);
            var url = TrimForLog(t.Url, 200);
            _log($"        [{i}] title=\"{title}\" url=\"{url}\"");
        }
    }

    private void LogConfiguredPortFallback()
    {
        var portState = CdpPortInspector.ProbePortState(_config.DebuggingPort);
        _log($"[*] No msedgewebview2 listening ports found. Configured port {_config.DebuggingPort} status = {portState}");

        if (portState == CdpPortState.InUseByOther)
        {
            LogConfiguredPortOwner();
            _log("[!] Re-run with a different port via Advanced options -> CDP Debugging Port (e.g., 9223).");
            return;
        }

        _log("[!] No WebView2 is exposing CDP on any port.");
        _log("[!] Likely causes:");
        _log("[!]   (a) Revit was not fully restarted since the env var was registered.");
        _log("[!]   (b) The Dalux addon on this PC sets AdditionalBrowserArguments explicitly,");
        _log("[!]       overriding WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS.");
        _log("[!]   (c) WebView2 Runtime is outdated (see version logged in pre-flight).");
        _log("[!]   (d) Antivirus/Defender blocked the CDP port bind.");
    }

    private void LogConfiguredPortOwner()
    {
        var ownerPid = CdpPortInspector.FindPortOwnerPid(_config.DebuggingPort);
        if (ownerPid.HasValue)
        {
            try
            {
                using var owner = Process.GetProcessById((int)ownerPid.Value);
                _log($"[!] Port {_config.DebuggingPort} is held by {owner.ProcessName} (PID {ownerPid.Value}).");
            }
            catch
            {
                _log($"[!] Port {_config.DebuggingPort} is held by PID {ownerPid.Value} (process info unavailable).");
            }
        }
        else
        {
            _log($"[!] Port {_config.DebuggingPort} is held but its owner could not be identified.");
        }
    }

    private static string TrimForLog(string? value, int maxLength)
    {
        var cleaned = (value ?? "").Replace('\n', ' ').Replace('\r', ' ');
        return cleaned.Length > maxLength ? cleaned[..maxLength] + "..." : cleaned;
    }
}
