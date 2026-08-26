//-------------------------------------------------------------------------------
// DaluxAutomationService.cs
//
// Main service class that orchestrates the Dalux headless automation.
// Implements the complete automation workflow with all 4 steps from Python.
//
//--------------------------------------------------------------------------- 

namespace DaluxRevitUpload;

/// <summary>
/// Orchestrates the Dalux upload workflow across Revit UI, CDP discovery, and browser automation.
/// </summary>
public class DaluxAutomationService : IDisposable
{
    private readonly DaluxAutomationConfig _config;
    private readonly CdpClient _cdpClient;
    private readonly DaluxRemoteDebuggingEnvironment _remoteDebuggingEnvironment;
    private readonly RevitDaluxRibbonAutomation _ribbonAutomation;
    private readonly CdpEndpointDiscovery _endpointDiscovery;
    private readonly List<string> _auditLog = new();

    public DaluxAutomationService(DaluxAutomationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cdpClient = new CdpClient(_config.WebSocketTimeout);
        _remoteDebuggingEnvironment = new DaluxRemoteDebuggingEnvironment(_config, LogMessage);
        _ribbonAutomation = new RevitDaluxRibbonAutomation(_config, LogMessage);
        _endpointDiscovery = new CdpEndpointDiscovery(_config, LogMessage);
    }

    public IReadOnlyList<string> AuditLog => _auditLog.AsReadOnly();

    public async Task<bool> RunAutomationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_remoteDebuggingEnvironment.EnsureRemoteDebuggingEnvVar())
                return false;

            // Pre-flight diagnostics: collect state of the environment before clicking anything.
            // If a foreign process is holding the CDP port and we fail to free it, abort before
            // wasting the 2-minute popup wait.
            var preflightOk = await _endpointDiscovery.RunPreflightDiagnosticsAsync(cancellationToken);
            if (!preflightOk)
                return false;

            // Click the Dalux tab and Upload button in Revit's native ribbon
            var clicked = await _ribbonAutomation.ClickUploadAsync(cancellationToken);
            if (!clicked)
                return false;

            // Scan all Revit-descended WebView2 CDP ports for up to 2 minutes and return
            // as soon as a Dalux tab surfaces on any of them. Logs every new/changed/gone
            // tab across ports so unusual WebView2 layouts are visible even when no match
            // is found. Replaces the old fixed-port wait — the Dalux popup on some PCs
            // binds a different port than the one the env var requested.
            var endpoint = await _endpointDiscovery.FindDaluxEndpointAnywhereAsync(TimeSpan.FromMinutes(2), cancellationToken);
            if (endpoint == null)
                return false;
            var (endpointPort, wsUrl) = endpoint.Value;
            LogMessage($"[+] Dalux popup CDP endpoint found on port {endpointPort}");

            LogMessage("[*] Connecting via WebSocket...");
            int wsAttempts = 0;
            while (true)
            {
                try
                {
                    await _cdpClient.ConnectAsync(wsUrl!, cancellationToken);
                    LogMessage("[+] WebSocket connected!");
                    break;
                }
                catch (Exception ex) when (wsAttempts < 10 && IsTransientWebSocketConnectFailure(ex))
                {
                    wsAttempts++;
                    LogMessage($"[*] WebView not ready yet, retrying ({wsAttempts}/10)...");
                    await Task.Delay(500, cancellationToken);
                    // Re-fetch the WebSocket URL in case it changed between retries
                    wsUrl = await CdpClient.GetWebSocketUrlAsync(endpointPort, cancellationToken) ?? wsUrl;
                    _cdpClient.ResetConnection();
                }
            }

            int ctxRetries = 0;
            while (true)
            {
                try
                {
                    await ExecuteAutomationLogicAsync(cancellationToken);
                    await DebugPageStructureAsync(cancellationToken);
                    break;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("-32000") || ex.Message.Contains("CDP-EARLY-CLOSE"))
                {
                    if (ctxRetries >= _config.RetryCount)
                    {
                        LogMessage($"[!] CDP context lost and retry limit reached ({_config.RetryCount} attempt(s) exhausted).");
                        LogMessage($"[!] Last error: {ex.Message}");
                        LogMessage("[!] This means the Dalux popup navigated away immediately after every WebSocket connection.");
                        LogMessage("[!] Possible fixes:");
                        LogMessage("[!]   • Increase Retry Count in Advanced options (current: " + _config.RetryCount + ")");
                        LogMessage("[!]   • Check whether another automation session is running simultaneously");
                        LogMessage("[!]   • Verify the Dalux plugin version is compatible with this extension");
                        return false;
                    }
                    ctxRetries++;
                    LogMessage($"[*] Page navigated or closed before script completed — waiting and reconnecting (attempt {ctxRetries}/{_config.RetryCount})...");
                    await Task.Delay(2000, cancellationToken);

                    await _cdpClient.CloseAsync();
                    _cdpClient.ResetConnection();

                    // The Dalux popup often re-spawns as a different WebView2 process during init,
                    // which means the previous port may now be dead. Probe it first; if it's gone,
                    // rediscover the endpoint from scratch (DevToolsActivePort + WebView2 port scan).
                    var oldPortProbe = await CdpClient.ProbeVersionAsync(endpointPort, cancellationToken);
                    if (!oldPortProbe.CdpUp)
                    {
                        LogMessage($"[*] Previous CDP port {endpointPort} is dead — rediscovering Dalux endpoint (up to 30s)...");
                        var rediscovered = await _endpointDiscovery.FindDaluxEndpointAnywhereAsync(TimeSpan.FromSeconds(30), cancellationToken);
                        if (rediscovered == null)
                        {
                            LogMessage("[!] Could not rediscover the Dalux popup CDP endpoint after it tore down.");
                            return false;
                        }
                        (endpointPort, wsUrl) = rediscovered.Value;
                        LogMessage($"[+] Rediscovered Dalux CDP endpoint on port {endpointPort}");
                    }
                    else
                    {
                        // Port is still alive — same process, just refresh the WS URL in case the
                        // tab Id changed.
                        wsUrl = await CdpClient.GetWebSocketUrlAsync(endpointPort, cancellationToken) ?? wsUrl;
                    }

                    // Reconnect, retrying past any 500 until the new page is ready
                    int reAttempts = 0;
                    while (true)
                    {
                        try
                        {
                            await _cdpClient.ConnectAsync(wsUrl!, cancellationToken);
                            LogMessage("[+] Reconnected after page navigation.");
                            break;
                        }
                        catch (Exception re) when (reAttempts < 10 && IsTransientWebSocketConnectFailure(re))
                        {
                            reAttempts++;
                            await Task.Delay(500, cancellationToken);
                            wsUrl = await CdpClient.GetWebSocketUrlAsync(endpointPort, cancellationToken) ?? wsUrl;
                            _cdpClient.ResetConnection();
                        }
                    }
                }
            }

            LogMessage("[+] Automation completed successfully.");
            return true;
        }
        catch (OperationCanceledException)
        {
            LogMessage("[!] Automation was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            LogMessage($"[!] Error: {ex.Message}");
            return false;
        }
        finally
        {
            await _cdpClient.CloseAsync();
        }
    }

    private async Task ExecuteAutomationLogicAsync(CancellationToken cancellationToken)
    {
        LogMessage($"\n[*] Processing files, target: '{_config.TargetFilename}'");

        var jsScript = DaluxAutomationScriptBuilder.Generate(_config);
        var evaluateStartedAt = DateTime.UtcNow;

        try
        {
            var result = await _cdpClient.EvaluateAsync(jsScript, awaitPromise: true, cancellationToken);

            if (result.TryGetProperty("exceptionDetails", out var exceptionDetails))
            {
                // Try to surface a human-readable message before falling back to the raw JSON blob.
                // CDP exceptionDetails shape: { text, exception: { description, value }, lineNumber, columnNumber }
                string userMessage;
                if (exceptionDetails.TryGetProperty("exception", out var exc) &&
                    exc.TryGetProperty("description", out var desc) &&
                    desc.GetString() is { Length: > 0 } descStr)
                {
                    userMessage = descStr;
                }
                else if (exceptionDetails.TryGetProperty("text", out var text) &&
                         text.GetString() is { Length: > 0 } textStr)
                {
                    userMessage = textStr;
                }
                else
                {
                    userMessage = exceptionDetails.GetRawText();
                }

                var line   = exceptionDetails.TryGetProperty("lineNumber",   out var ln) ? ln.GetInt32() : -1;
                var column = exceptionDetails.TryGetProperty("columnNumber",  out var col) ? col.GetInt32() : -1;
                var location = line >= 0 ? $" (line {line}, col {column})" : string.Empty;

                LogMessage($"\n[!] JavaScript threw an unhandled exception{location}: {userMessage}");
                LogMessage("[!] Full CDP exceptionDetails (for debugging):");
                LogMessage(exceptionDetails.GetRawText());
                throw new InvalidOperationException(
                    $"JavaScript threw an unhandled exception{location}: {userMessage}");
            }
            else if (result.TryGetProperty("result", out var resultElement))
            {
                if (resultElement.TryGetProperty("value", out var valueElement))
                {
                    var output = valueElement.GetString() ?? "[No output]";
                    LogMessage(output);
                }
            }
        }
        catch (Exception ex) when (ex.Message.Contains("WebSocket") && ex.Message.Contains("closed"))
        {
            // Previously this catch silently reported success on ANY WebSocket close during
            // Runtime.evaluate, on the assumption it always meant "popup closed after upload
            // completed". That masked the much more common failure where the Dalux popup tears
            // down the CDP context shortly after we connect (popup still navigating), so the JS
            // never ran. Distinguish the two by elapsed time AND whether an action button was
            // actually requested:
            //   • close before the script could plausibly finish → throw EARLY-CLOSE so the
            //     outer retry loop reconnects (same handling as CDP -32000).
            //   • close after a long run with TriggerUpload=true → treat as the legitimate
            //     post-upload teardown.
            var elapsed = DateTime.UtcNow - evaluateStartedAt;
            var actionRequested = !string.IsNullOrEmpty(_config.ActionButtonText);
            if (actionRequested && elapsed >= TimeSpan.FromSeconds(30))
            {
                LogMessage($"[+] Popup closed after {elapsed.TotalSeconds:F0}s — assuming post-upload teardown.");
            }
            else
            {
                LogMessage($"[!] WebSocket closed after only {elapsed.TotalSeconds:F1}s — Dalux popup tore down the CDP context before the automation script could complete.");
                LogMessage($"[!] Underlying error: {ex.Message}");
                throw new InvalidOperationException(
                    $"CDP-EARLY-CLOSE: Dalux popup closed/navigated before the automation script could complete (after {elapsed.TotalSeconds:F1}s). " +
                    "This usually means the WebView2 popup was still initialising when we connected and the page navigation killed the CDP context.",
                    ex);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[!] JavaScript execution error: {ex.Message}");
            throw;
        }

        await Task.Delay(1000, cancellationToken);
    }

    private async Task DebugPageStructureAsync(CancellationToken cancellationToken)
    {
        var js = "(function() { return 'URL: ' + document.URL.substring(0,80) + ' | Elems: ' + document.querySelectorAll('*').length + ' | Checkboxes: ' + document.querySelectorAll('input[type=\"checkbox\"]').length + ' | Rows: ' + document.querySelectorAll('tr, [role=\"row\"]').length + ' | iframes: ' + document.querySelectorAll('iframe').length; })();";
        try
        {
            var result = await _cdpClient.EvaluateAsync(js, awaitPromise: false, cancellationToken);
            if (result.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("value", out var valueElement))
            {
                var output = valueElement.GetString() ?? "";
                LogMessage($"\n[DEBUG] {output}");
            }
        }
        catch { }
    }

    private void LogMessage(string message)
    {
        _auditLog.Add(message);
        System.Diagnostics.Debug.WriteLine(message);
    }

    private static bool IsTransientWebSocketConnectFailure(Exception ex)
    {
        return ex is TimeoutException ||
               ex.Message.Contains("500") ||
               ex.Message.Contains("status code");
    }

    public string GetAuditLogAsString()
    {
        return string.Join("\n", _auditLog);
    }

    public void Dispose()
    {
        _cdpClient.Dispose();
    }
}
