namespace DaluxRevitUpload;

internal sealed class CdpEndpointDiscovery
{
    private readonly DaluxAutomationConfig _config;
    private readonly Action<string> _log;
    private readonly CdpPreflightDiagnostics _preflightDiagnostics;
    private readonly CdpTimeoutDiagnostics _timeoutDiagnostics;

    public CdpEndpointDiscovery(DaluxAutomationConfig config, Action<string> log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _preflightDiagnostics = new CdpPreflightDiagnostics(_config, _log);
        _timeoutDiagnostics = new CdpTimeoutDiagnostics(_config, _log);
    }

    /// <summary>
    /// Scans all WebView2 processes descended from Revit for a CDP endpoint hosting a
    /// tab whose URL or title contains "dalux". Polls every 2 seconds for up to
    /// <paramref name="window"/>. Logs every new/changed/removed tab per port so
    /// unusual WebView2 layouts are visible even when no Dalux match is found.
    ///
    /// On each poll tick, first tries the DevToolsActivePort fast-path: reads
    /// %LocalAppData%\DaluxWebView2\Default\EBWebView\DevToolsActivePort (written by
    /// Chromium when the Dalux popup spawns), probes that port's /json, and returns
    /// the first tab with a WebSocket URL — bypassing tab-title matching because
    /// everything at that UDF is Dalux by definition.
    ///
    /// Returns (port, WebSocket URL) the instant a Dalux tab surfaces, or null when
    /// the window expires without a match — in which case callers should invoke
    /// <see cref="CdpTimeoutDiagnostics"/> for a full per-port dump.
    /// </summary>
    public async Task<(int Port, string WebSocketUrl)?> FindDaluxEndpointAnywhereAsync(TimeSpan window, CancellationToken cancellationToken)
    {
        _log($"[*] Scanning DevToolsActivePort + all WebView2 CDP endpoints every 2s for up to {(int)window.TotalSeconds}s...");
        var deadline = DateTime.UtcNow + window;

        // Keyed on (port, tab.Id) -> last known (title, url)
        var known = new Dictionary<(int Port, string Id), (string Title, string Url)>();
        var knownPorts = new HashSet<int>();
        bool firstSnapshot = true;
        int? lastLoggedDevToolsPort = null;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fast-path: Dalux's own UDF wrote its bound port. Prefer this over port
            // enumeration because it names the Dalux browser process directly, with
            // no title/URL matching and no race with sibling WebView2 processes.
            var activePort = DaluxDevToolsActivePortFile.TryReadPort();
            if (activePort.HasValue)
            {
                if (lastLoggedDevToolsPort != activePort.Value)
                {
                    _log($"    [+] Dalux DevToolsActivePort found at port={activePort.Value}");
                    lastLoggedDevToolsPort = activePort.Value;
                }

                var probe = await CdpClient.ProbeVersionAsync(activePort.Value, cancellationToken);
                if (probe.CdpUp)
                {
                    var tabs = await CdpClient.GetAllTabsAsync(activePort.Value, cancellationToken);
                    var daluxTab = tabs.FirstOrDefault(t => !string.IsNullOrEmpty(t.WebSocketDebuggerUrl));
                    if (daluxTab != null)
                    {
                        var matchedTitle = (daluxTab.Title ?? "").Replace('\n', ' ').Replace('\r', ' ');
                        if (matchedTitle.Length > 120) matchedTitle = matchedTitle[..120] + "...";
                        _log($"[+] Dalux tab matched via DevToolsActivePort on port {activePort.Value}: \"{matchedTitle}\"");
                        return (activePort.Value, daluxTab.WebSocketDebuggerUrl);
                    }
                }
                // File present but CDP/tabs not ready yet — fall through to the broader
                // scan and we'll retry the fast-path on the next tick.
            }

            var candidatePorts = CdpPortInspector.FindWebViewCdpPorts(_config.RevitProcessId);
            var currentKeys = new HashSet<(int Port, string Id)>();
            var currentPorts = new HashSet<int>();

            foreach (var (port, pid, isRevitDescendant) in candidatePorts)
            {
                currentPorts.Add(port);
                if (!knownPorts.Contains(port))
                {
                    var marker = firstSnapshot ? "baseline" : "NEW-PORT";
                    var parentNote = isRevitDescendant ? "Revit descendant" : "parent chain not Revit";
                    _log($"    [{marker}] port={port} pid={pid} ({parentNote})");
                    knownPorts.Add(port);
                }

                var probe = await CdpClient.ProbeVersionAsync(port, cancellationToken);
                if (!probe.CdpUp) continue;

                var tabs = await CdpClient.GetAllTabsAsync(port, cancellationToken);
                foreach (var t in tabs)
                {
                    if (string.IsNullOrEmpty(t.Id)) continue;
                    var key = (port, t.Id);
                    currentKeys.Add(key);

                    var title = (t.Title ?? "").Replace('\n', ' ').Replace('\r', ' ');
                    var url = (t.Url ?? "").Replace('\n', ' ').Replace('\r', ' ');
                    var titleLc = title.ToLowerInvariant();
                    var urlLc = url.ToLowerInvariant();
                    bool isDalux = titleLc.Contains("dalux") || urlLc.Contains("dalux");

                    if (title.Length > 120) title = title[..120] + "...";
                    if (url.Length > 200) url = url[..200] + "...";

                    if (!known.TryGetValue(key, out var prev))
                    {
                        var marker = firstSnapshot ? "baseline" : "NEW";
                        _log($"    [{marker}] port={port} title=\"{title}\" url=\"{url}\"");
                        known[key] = (title, url);
                    }
                    else if (prev.Title != title || prev.Url != url)
                    {
                        _log($"    [changed] port={port} title=\"{title}\" url=\"{url}\"");
                        known[key] = (title, url);
                    }

                    if (isDalux && !string.IsNullOrEmpty(t.WebSocketDebuggerUrl))
                    {
                        _log($"[+] Dalux tab matched on port {port}: \"{title}\"");
                        return (port, t.WebSocketDebuggerUrl);
                    }
                }
            }

            foreach (var goneKey in known.Keys.Where(k => !currentKeys.Contains(k)).ToList())
            {
                var prev = known[goneKey];
                _log($"    [gone] port={goneKey.Port} title=\"{prev.Title}\" url=\"{prev.Url}\"");
                known.Remove(goneKey);
            }
            foreach (var gonePort in knownPorts.Where(p => !currentPorts.Contains(p)).ToList())
            {
                _log($"    [gone-port] port={gonePort}");
                knownPorts.Remove(gonePort);
            }

            firstSnapshot = false;
            await Task.Delay(2000, cancellationToken);
        }

        _log("[!] Timed out waiting for a Dalux tab on any Revit-descended WebView2 CDP endpoint.");
        await _timeoutDiagnostics.LogAsync(cancellationToken);
        return null;
    }

    /// <summary>
    /// Runs pre-flight diagnostics before clicking Upload.
    /// </summary>
    public Task<bool> RunPreflightDiagnosticsAsync(CancellationToken cancellationToken)
    {
        return _preflightDiagnostics.RunAsync(cancellationToken);
    }
}

