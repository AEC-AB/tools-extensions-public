//-------------------------------------------------------------------------------
// DaluxAutomationService.cs
//
// Main service class that orchestrates the Dalux headless automation.
// Implements the complete automation workflow with all 4 steps from Python.
//
//--------------------------------------------------------------------------- 

namespace DaluxRevitUpload;

/// <summary>
/// Manages the complete Dalux automation workflow
/// </summary>
public class DaluxAutomationService : IDisposable
{
    // P/Invoke for locked-screen-compatible window interaction
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT pt);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP   = 0x0202;

    private readonly DaluxAutomationConfig _config;
    private readonly CdpClient _cdpClient;
    private List<string> _auditLog = new();

    public DaluxAutomationService(DaluxAutomationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cdpClient = new CdpClient(_config.WebSocketTimeout);
    }

    public IReadOnlyList<string> AuditLog => _auditLog.AsReadOnly();

    public async Task<bool> RunAutomationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!EnsureRemoteDebuggingEnvVar())
                return false;

            // Pre-flight diagnostics: collect state of the environment before clicking anything.
            // If a foreign process is holding the CDP port and we fail to free it, abort before
            // wasting the 2-minute popup wait.
            var preflightOk = await RunPreflightDiagnosticsAsync(cancellationToken);
            if (!preflightOk)
                return false;

            // Click the Dalux tab and Upload button in Revit's native ribbon
            var clicked = await ClickRevitDaluxUploadAsync(cancellationToken);
            if (!clicked)
                return false;

            // Scan all Revit-descended WebView2 CDP ports for up to 2 minutes and return
            // as soon as a Dalux tab surfaces on any of them. Logs every new/changed/gone
            // tab across ports so unusual WebView2 layouts are visible even when no match
            // is found. Replaces the old fixed-port wait — the Dalux popup on some PCs
            // binds a different port than the one the env var requested.
            var endpoint = await FindDaluxEndpointAnywhereAsync(TimeSpan.FromMinutes(2), cancellationToken);
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
                catch (Exception ex) when (wsAttempts < 10 && (ex.Message.Contains("500") || ex.Message.Contains("status code")))
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
                        var rediscovered = await FindDaluxEndpointAnywhereAsync(TimeSpan.FromSeconds(30), cancellationToken);
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
                        catch (Exception re) when (reAttempts < 10 && (re.Message.Contains("500") || re.Message.Contains("status code")))
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

    /// <summary>
    /// Ensures WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS carries the expected
    /// --remote-debugging-port flag both in the User scope (for future Revit
    /// launches) AND in the currently running Revit process's environment block
    /// (so the Dalux WebView2 popup spawned this session inherits it).
    ///
    /// Process-block injection uses VirtualAllocEx + a tiny x64 shellcode stub +
    /// CreateRemoteThread to call kernel32!SetEnvironmentVariableW inside Revit.
    /// This removes the historical "first run writes env, second run uses it"
    /// double-launch UX. Falls back to the legacy "restart Revit" abort only when
    /// injection itself fails (e.g., AV blocks remote-thread creation, or
    /// Assistant is non-elevated while Revit is elevated).
    /// </summary>
    private bool EnsureRemoteDebuggingEnvVar()
    {
        const string envName = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";
        var existingValue = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User);
        var requiredFlag = $"--remote-debugging-port={_config.DebuggingPort}";

        // Legacy migration: existing env var has --remote-debugging-port=9222 but we now want =0.
        // Rewrite the flag in place to preserve any other arguments the user has added.
        if (_config.DebuggingPort == 0
            && !string.IsNullOrEmpty(existingValue)
            && System.Text.RegularExpressions.Regex.IsMatch(existingValue, @"--remote-debugging-port=\d+"))
        {
            var currentFlagMatch = System.Text.RegularExpressions.Regex.Match(existingValue, @"--remote-debugging-port=(\d+)");
            var currentPort = currentFlagMatch.Groups[1].Value;
            if (currentPort != "0")
            {
                var rewritten = System.Text.RegularExpressions.Regex.Replace(
                    existingValue,
                    @"--remote-debugging-port=\d+",
                    "--remote-debugging-port=0");
                Environment.SetEnvironmentVariable(envName, rewritten, EnvironmentVariableTarget.User);
                LogMessage($"[*] Env var contained --remote-debugging-port={currentPort}. Rewrote to =0 for per-UDF ephemeral ports.");
                existingValue = rewritten;
            }
        }

        string desiredValue;
        bool userScopeChanged = false;
        if (string.IsNullOrEmpty(existingValue) || !existingValue.Contains(requiredFlag))
        {
            desiredValue = string.IsNullOrEmpty(existingValue) ? requiredFlag : existingValue + " " + requiredFlag;
            Environment.SetEnvironmentVariable(envName, desiredValue, EnvironmentVariableTarget.User);
            userScopeChanged = true;
            var mode = _config.DebuggingPort == 0 ? "per-UDF ephemeral" : $"fixed port {_config.DebuggingPort}";
            LogMessage($"[*] WebView2 remote debugging persisted to user environment ({mode})");
        }
        else
        {
            desiredValue = existingValue;
            var configuredMode = _config.DebuggingPort == 0 ? "port=0, per-UDF ephemeral" : $"port={_config.DebuggingPort}";
            LogMessage($"[*] WebView2 remote debugging already in user environment ({configuredMode})");
        }

        // Patch the running Revit process so its next WebView2 spawn inherits the
        // flag without requiring a restart. Idempotent: if Revit's env block already
        // has the same value, SetEnvironmentVariableW is a no-op.
        if (TryInjectEnvVarIntoRevit(_config.RevitProcessId, envName, desiredValue))
            return true;

        // Injection failed. If user-scope was already correct before we touched it,
        // Revit *might* have inherited the flag at launch — let the run continue and
        // fail loudly later if not. If we just wrote the user var, the current Revit
        // session definitely won't see it, so abort with the restart message.
        if (userScopeChanged)
        {
            LogMessage("[!] Could not patch the running Revit process's environment block.");
            LogMessage("[!] Close Revit COMPLETELY, relaunch it, then re-run this extension.");
            LogMessage("[!] The current Revit session will not see the new setting — aborting this run.");
            return false;
        }

        LogMessage("[!] Process injection failed but user env was already correct — continuing on the");
        LogMessage("    assumption that Revit inherited the flag at launch.");
        return true;
    }

    /// <summary>
    /// Sets an environment variable inside the running Revit process by injecting a
    /// small x64 shellcode that calls kernel32!SetEnvironmentVariableW(name, value).
    /// kernel32.dll is mapped at the same virtual address in every process per boot,
    /// so the local GetProcAddress result is valid in Revit too.
    ///
    /// Returns false when OpenProcess is denied (elevation mismatch / SACL), when any
    /// VirtualAllocEx / WriteProcessMemory / CreateRemoteThread call fails (typically
    /// AV blocking), or when the remote SetEnvironmentVariableW itself returns 0.
    /// Callers should treat false as "fall back to restart-Revit UX".
    /// </summary>
    private bool TryInjectEnvVarIntoRevit(int revitPid, string name, string value)
    {
        if (!Environment.Is64BitProcess)
        {
            LogMessage("[!] Env-var injection requires a 64-bit Assistant process. Skipping.");
            return false;
        }

        const uint PROCESS_CREATE_THREAD     = 0x0002;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_OPERATION      = 0x0008;
        const uint PROCESS_VM_WRITE          = 0x0020;
        const uint PROCESS_VM_READ           = 0x0010;
        const uint MEM_COMMIT_RESERVE        = 0x3000;
        const uint MEM_RELEASE               = 0x8000;
        const uint PAGE_READWRITE            = 0x04;
        const uint PAGE_EXECUTE_READ         = 0x20;
        const uint PAGE_EXECUTE_READWRITE    = 0x40;
        const uint WAIT_OBJECT_0             = 0x0;

        var access = PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION
                     | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ;

        IntPtr hProc = OpenProcess(access, false, revitPid);
        if (hProc == IntPtr.Zero)
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            LogMessage($"[!] OpenProcess(Revit PID {revitPid}) failed with Win32 error {err}.");
            if (err == 5) // ERROR_ACCESS_DENIED
                LogMessage("    Likely cause: Revit is running elevated and Assistant is not (or vice-versa).");
            return false;
        }

        IntPtr pData = IntPtr.Zero;
        IntPtr pCode = IntPtr.Zero;
        IntPtr hThread = IntPtr.Zero;
        try
        {
            IntPtr hKernel32 = GetModuleHandle("kernel32.dll");
            if (hKernel32 == IntPtr.Zero)
            {
                LogMessage("[!] GetModuleHandle(kernel32.dll) returned null. Cannot inject.");
                return false;
            }
            IntPtr pSetEnv = GetProcAddress(hKernel32, "SetEnvironmentVariableW");
            if (pSetEnv == IntPtr.Zero)
            {
                LogMessage("[!] GetProcAddress(SetEnvironmentVariableW) failed. Cannot inject.");
                return false;
            }

            byte[] nameBytes  = System.Text.Encoding.Unicode.GetBytes(name  + "\0");
            byte[] valueBytes = System.Text.Encoding.Unicode.GetBytes(value + "\0");
            uint dataSize = (uint)(nameBytes.Length + valueBytes.Length);

            pData = VirtualAllocEx(hProc, IntPtr.Zero, dataSize, MEM_COMMIT_RESERVE, PAGE_READWRITE);
            if (pData == IntPtr.Zero)
            {
                LogMessage($"[!] VirtualAllocEx (data) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            IntPtr pName  = pData;
            IntPtr pValue = new IntPtr(pData.ToInt64() + nameBytes.Length);

            if (!WriteProcessMemory(hProc, pName,  nameBytes,  (uint)nameBytes.Length,  out _) ||
                !WriteProcessMemory(hProc, pValue, valueBytes, (uint)valueBytes.Length, out _))
            {
                LogMessage($"[!] WriteProcessMemory (data) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }

            // x64 shellcode:
            //   48 83 EC 28              sub  rsp, 0x28        ; shadow space + alignment
            //   48 B9 <imm64 pName>      mov  rcx, pName
            //   48 BA <imm64 pValue>     mov  rdx, pValue
            //   48 B8 <imm64 pSetEnv>    mov  rax, SetEnvironmentVariableW
            //   FF D0                    call rax
            //   48 83 C4 28              add  rsp, 0x28
            //   C3                       ret
            byte[] shellcode = new byte[4 + 10 + 10 + 10 + 2 + 4 + 1];
            int o = 0;
            shellcode[o++] = 0x48; shellcode[o++] = 0x83; shellcode[o++] = 0xEC; shellcode[o++] = 0x28;
            shellcode[o++] = 0x48; shellcode[o++] = 0xB9;
            Buffer.BlockCopy(BitConverter.GetBytes(pName.ToInt64()),   0, shellcode, o, 8); o += 8;
            shellcode[o++] = 0x48; shellcode[o++] = 0xBA;
            Buffer.BlockCopy(BitConverter.GetBytes(pValue.ToInt64()),  0, shellcode, o, 8); o += 8;
            shellcode[o++] = 0x48; shellcode[o++] = 0xB8;
            Buffer.BlockCopy(BitConverter.GetBytes(pSetEnv.ToInt64()), 0, shellcode, o, 8); o += 8;
            shellcode[o++] = 0xFF; shellcode[o++] = 0xD0;
            shellcode[o++] = 0x48; shellcode[o++] = 0x83; shellcode[o++] = 0xC4; shellcode[o++] = 0x28;
            shellcode[o++] = 0xC3;

            pCode = VirtualAllocEx(hProc, IntPtr.Zero, (uint)shellcode.Length, MEM_COMMIT_RESERVE, PAGE_EXECUTE_READWRITE);
            if (pCode == IntPtr.Zero)
            {
                LogMessage($"[!] VirtualAllocEx (code) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            if (!WriteProcessMemory(hProc, pCode, shellcode, (uint)shellcode.Length, out _))
            {
                LogMessage($"[!] WriteProcessMemory (code) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }

            // Tighten permissions on the code page before executing — some EDR products
            // flag RWX pages but tolerate RX.
            VirtualProtectEx(hProc, pCode, (uint)shellcode.Length, PAGE_EXECUTE_READ, out _);

            hThread = CreateRemoteThread(hProc, IntPtr.Zero, 0, pCode, IntPtr.Zero, 0, out _);
            if (hThread == IntPtr.Zero)
            {
                var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                LogMessage($"[!] CreateRemoteThread failed: {err}. Likely AV/EDR blocking remote-thread creation.");
                return false;
            }

            var wait = WaitForSingleObject(hThread, 5000);
            if (wait != WAIT_OBJECT_0)
            {
                LogMessage($"[!] Remote thread did not finish within 5s (WaitForSingleObject={wait}).");
                return false;
            }

            if (!GetExitCodeThread(hThread, out uint exitCode))
            {
                LogMessage($"[!] GetExitCodeThread failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            if (exitCode == 0)
            {
                LogMessage("[!] SetEnvironmentVariableW returned FALSE inside Revit.");
                return false;
            }

            LogMessage($"[+] Patched Revit (PID {revitPid}) env block — next WebView2 spawn will inherit the flag.");
            return true;
        }
        catch (Exception ex)
        {
            LogMessage($"[!] Env-var injection threw: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
        finally
        {
            if (hThread != IntPtr.Zero) CloseHandle(hThread);
            if (pCode   != IntPtr.Zero) VirtualFreeEx(hProc, pCode, 0, MEM_RELEASE);
            if (pData   != IntPtr.Zero) VirtualFreeEx(hProc, pData, 0, MEM_RELEASE);
            CloseHandle(hProc);
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    /// <summary>
    /// Uses Windows UI Automation to click the Dalux tab in Revit's ribbon,
    /// then click the Upload button to trigger the Dalux popup.
    /// </summary>
    private async Task<bool> ClickRevitDaluxUploadAsync(CancellationToken cancellationToken)
    {
        LogMessage("[*] Looking for Revit process...");
        Process revitProc;
        try
        {
            revitProc = Process.GetProcessById(_config.RevitProcessId);
        }
        catch
        {
            LogMessage($"[!] Revit process with ID {_config.RevitProcessId} not found");
            return false;
        }

        LogMessage("[*] Finding Revit main window via UI Automation...");
        var mainHwnd = revitProc.MainWindowHandle;
        if (mainHwnd == IntPtr.Zero)
        {
            LogMessage("[!] Revit main window handle not found");
            return false;
        }

        // FromHandle bypasses the desktop root — works even when the screen is locked
        var revitWindow = AutomationElement.FromHandle(mainHwnd);
        if (revitWindow == null)
        {
            LogMessage("[!] Could not create AutomationElement from Revit window");
            return false;
        }

        // Find and click the Dalux ribbon tab
        LogMessage("[*] Looking for Dalux tab in ribbon...");
        var daluxTab = revitWindow.FindFirst(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Dalux", PropertyConditionFlags.IgnoreCase)));

        if (daluxTab == null)
        {
            // Broader search: any element whose name is exactly "Dalux"
            daluxTab = revitWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Dalux", PropertyConditionFlags.IgnoreCase));
        }

        if (daluxTab == null)
        {
            LogMessage("[!] Dalux tab not found in Revit ribbon");
            return false;
        }

        LogMessage("[+] Found Dalux tab, clicking...");
        if (daluxTab.TryGetCurrentPattern(InvokePattern.Pattern, out var tabPattern))
            ((InvokePattern)tabPattern).Invoke();
        else
            PostMessageClick(mainHwnd, daluxTab);

        LogMessage("[*] Waiting for Dalux ribbon panel to load...");
        await Task.Delay(1500, cancellationToken);

        // Find and click the Upload button in the Dalux ribbon panel.
        // We search broadly because the Dalux plugin's exact label/control-type varies
        // across versions and Revit locales:
        //   - Some versions ship a Button literally named "Upload".
        //   - Others ship a SplitButton named "Upload Model" / "Upload to Dalux".
        //   - Localized Revit installs may translate the label.
        // Strategy: match any element whose Name contains "upload" (case-insensitive),
        // regardless of ControlType. Retry for several seconds while the ribbon paints.
        LogMessage("[*] Looking for Upload button in Dalux ribbon...");
        AutomationElement? uploadBtn = null;
        int attempts = 0;
        const int maxAttempts = 10;
        while (uploadBtn == null && attempts < maxAttempts)
        {
            // Pass 1: exact "Upload" Button (the original happy path — fastest match).
            uploadBtn = revitWindow.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Upload", PropertyConditionFlags.IgnoreCase)));

            // Pass 2: any control whose Name contains "upload" (substring, case-insensitive),
            // restricted to clickable types so we don't grab a static text label.
            if (uploadBtn == null)
            {
                var candidates = revitWindow.FindAll(TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.SplitButton),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.MenuItem),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.ListItem)));
                foreach (AutomationElement c in candidates)
                {
                    var name = c.Current.Name ?? string.Empty;
                    if (name.IndexOf("upload", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        uploadBtn = c;
                        if (attempts > 0)
                            LogMessage($"[*] Matched broader candidate: ControlType={c.Current.ControlType.ProgrammaticName}, Name=\"{name}\"");
                        break;
                    }
                }
            }

            if (uploadBtn == null)
            {
                attempts++;
                await Task.Delay(500, cancellationToken);
            }
        }

        if (uploadBtn == null)
        {
            LogMessage("[!] Upload button not found in Dalux ribbon panel");
            DumpRibbonCandidatesForDiagnostics(revitWindow);
            return false;
        }

        LogMessage("[+] Found Upload button, clicking to open Dalux popup...");
        if (uploadBtn.TryGetCurrentPattern(InvokePattern.Pattern, out var btnPattern))
            ((InvokePattern)btnPattern).Invoke();
        else
            PostMessageClick(mainHwnd, uploadBtn);

        return true;
    }

    /// <summary>
    /// Sends WM_LBUTTONDOWN/UP directly to the window's message queue using client-area
    /// coordinates calculated from the element's screen bounding rect.
    /// Works even when the screen is locked because PostMessage bypasses the active desktop.
    /// </summary>
    private void PostMessageClick(IntPtr hwnd, AutomationElement element)
    {
        var rect = element.Current.BoundingRectangle;
        var pt = new POINT
        {
            X = (int)(rect.Left + rect.Width  / 2),
            Y = (int)(rect.Top  + rect.Height / 2)
        };
        ScreenToClient(hwnd, ref pt);
        var lParam = new IntPtr((pt.Y << 16) | (pt.X & 0xFFFF));
        PostMessage(hwnd, WM_LBUTTONDOWN, new IntPtr(0x0001), lParam);
        PostMessage(hwnd, WM_LBUTTONUP,   IntPtr.Zero,        lParam);
    }


    /// <summary>
    /// Dumps a snapshot of the Dalux ribbon's clickable controls so we can diagnose
    /// "Upload button not found" failures remotely. The exact label varies across
    /// Dalux plugin versions and Revit locales; the dump lets a remote user paste the
    /// log back and we can add the correct match without another round-trip.
    /// </summary>
    private void DumpRibbonCandidatesForDiagnostics(AutomationElement revitWindow)
    {
        try
        {
            LogMessage("[DEBUG] Dumping clickable ribbon controls so we can see what's actually labelled.");
            LogMessage("[DEBUG] Send these lines back so the Upload-button matcher can be updated for your Revit/Dalux version.");

            var clickable = revitWindow.FindAll(TreeScope.Descendants,
                new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.SplitButton),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.MenuItem),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.ListItem)));

            int total = clickable.Count;
            int shown = 0;
            const int maxShown = 60;
            foreach (AutomationElement c in clickable)
            {
                var name = c.Current.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var lower = name.ToLowerInvariant();
                bool relevant =
                    lower.Contains("upload") ||
                    lower.Contains("dalux")  ||
                    lower.Contains("send")   ||
                    lower.Contains("publish");

                if (!relevant) continue;

                LogMessage($"[DEBUG]   ControlType={c.Current.ControlType.ProgrammaticName} Name=\"{name}\" AutomationId=\"{c.Current.AutomationId}\"");
                if (++shown >= maxShown) break;
            }

            if (shown == 0)
                LogMessage($"[DEBUG]   No controls matched upload/dalux/send/publish (scanned {total} clickable elements). Dalux ribbon panel may not be loaded \u2014 try opening the Dalux tab manually and rerunning.");
            else
                LogMessage($"[DEBUG]   (showed {shown} of {total} clickable elements; only those whose Name contains upload/dalux/send/publish)");
        }
        catch (Exception ex)
        {
            LogMessage($"[DEBUG] Failed to enumerate ribbon controls: {ex.Message}");
        }
    }
    /// <summary>
    /// Path to the Dalux WebView2 UserDataFolder's DevToolsActivePort file. Chromium
    /// writes this file when --remote-debugging-port is present (including =0 for an
    /// ephemeral port). Line 1 is the port the browser bound; line 2 is the
    /// /devtools/browser/{uuid} target. This file is the definitive source for the
    /// Dalux popup's CDP port regardless of what Revit internal / Teams did with 9222.
    /// </summary>
    private static string DaluxDevToolsActivePortPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DaluxWebView2", "Default", "EBWebView", "DevToolsActivePort");

    /// <summary>
    /// Reads line 1 of the Dalux WebView2 DevToolsActivePort file and parses it as
    /// the CDP port. Returns null when the file is absent, mid-write, or malformed —
    /// callers should treat null as "not ready yet" and retry.
    /// </summary>
    private static int? ReadDaluxDevToolsActivePort()
    {
        try
        {
            var path = DaluxDevToolsActivePortPath;
            if (!File.Exists(path)) return null;

            // FileShare.ReadWrite lets us read even while Chromium is actively writing.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var firstLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(firstLine)) return null;
            return int.TryParse(firstLine.Trim(), out var port) && port > 0 ? port : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
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
    /// <see cref="LogCdpTimeoutDiagnosticsAsync"/> for a full per-port dump.
    /// </summary>
    private async Task<(int Port, string WebSocketUrl)?> FindDaluxEndpointAnywhereAsync(TimeSpan window, CancellationToken cancellationToken)
    {
        LogMessage($"[*] Scanning DevToolsActivePort + all WebView2 CDP endpoints every 2s for up to {(int)window.TotalSeconds}s...");
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
            var activePort = ReadDaluxDevToolsActivePort();
            if (activePort.HasValue)
            {
                if (lastLoggedDevToolsPort != activePort.Value)
                {
                    LogMessage($"    [+] Dalux DevToolsActivePort found at port={activePort.Value}");
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
                        LogMessage($"[+] Dalux tab matched via DevToolsActivePort on port {activePort.Value}: \"{matchedTitle}\"");
                        return (activePort.Value, daluxTab.WebSocketDebuggerUrl);
                    }
                }
                // File present but CDP/tabs not ready yet — fall through to the broader
                // scan and we'll retry the fast-path on the next tick.
            }

            var candidatePorts = FindWebViewCdpPorts();
            var currentKeys = new HashSet<(int Port, string Id)>();
            var currentPorts = new HashSet<int>();

            foreach (var (port, pid, isRevitDescendant) in candidatePorts)
            {
                currentPorts.Add(port);
                if (!knownPorts.Contains(port))
                {
                    var marker = firstSnapshot ? "baseline" : "NEW-PORT";
                    var parentNote = isRevitDescendant ? "Revit descendant" : "parent chain not Revit";
                    LogMessage($"    [{marker}] port={port} pid={pid} ({parentNote})");
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
                        LogMessage($"    [{marker}] port={port} title=\"{title}\" url=\"{url}\"");
                        known[key] = (title, url);
                    }
                    else if (prev.Title != title || prev.Url != url)
                    {
                        LogMessage($"    [changed] port={port} title=\"{title}\" url=\"{url}\"");
                        known[key] = (title, url);
                    }

                    if (isDalux && !string.IsNullOrEmpty(t.WebSocketDebuggerUrl))
                    {
                        LogMessage($"[+] Dalux tab matched on port {port}: \"{title}\"");
                        return (port, t.WebSocketDebuggerUrl);
                    }
                }
            }

            foreach (var goneKey in known.Keys.Where(k => !currentKeys.Contains(k)).ToList())
            {
                var prev = known[goneKey];
                LogMessage($"    [gone] port={goneKey.Port} title=\"{prev.Title}\" url=\"{prev.Url}\"");
                known.Remove(goneKey);
            }
            foreach (var gonePort in knownPorts.Where(p => !currentPorts.Contains(p)).ToList())
            {
                LogMessage($"    [gone-port] port={gonePort}");
                knownPorts.Remove(gonePort);
            }

            firstSnapshot = false;
            await Task.Delay(2000, cancellationToken);
        }

        LogMessage("[!] Timed out waiting for a Dalux tab on any Revit-descended WebView2 CDP endpoint.");
        await LogCdpTimeoutDiagnosticsAsync(cancellationToken);
        return null;
    }

    /// <summary>
    /// Runs pre-flight diagnostics before clicking Upload. Cheap checks that help
    /// distinguish failure modes when the CDP connect times out later.
    /// Returns false when a foreign process holds the CDP port and we couldn't free it
    /// (or policy forbids freeing) — the overall run should abort in that case.
    /// </summary>
    private async Task<bool> RunPreflightDiagnosticsAsync(CancellationToken cancellationToken)
    {
        LogMessage("[*] Pre-flight diagnostics:");

        // Effective env var value as read back from User scope.
        var envValue = Environment.GetEnvironmentVariable(
            "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            EnvironmentVariableTarget.User);
        LogMessage($"    WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS (user) = \"{envValue ?? "<null>"}\"");

        // WebView2 Evergreen Runtime version from registry.
        var wv2Version = ReadWebView2RuntimeVersion();
        LogMessage($"    WebView2 Runtime version = {wv2Version ?? "<not installed or not found>"}");

        // Revit process start time vs env-var last-write time.
        DateTime? revitStart = null;
        try
        {
            revitStart = Process.GetProcessById(_config.RevitProcessId).StartTime;
        }
        catch
        {
            // Process lookup failure is handled separately in ClickRevitDaluxUploadAsync.
        }
        var envWriteTime = ReadEnvVarLastWriteTime();
        LogMessage($"    Revit (PID {_config.RevitProcessId}) start time = {revitStart?.ToString("s") ?? "<unknown>"}");
        LogMessage($"    Env var registry last-write    = {envWriteTime?.ToString("s") ?? "<unknown>"}");
        if (revitStart.HasValue && envWriteTime.HasValue && revitStart.Value < envWriteTime.Value)
        {
            LogMessage("[!] Revit was started BEFORE the env var was registered. It will not have inherited");
            LogMessage("[!] the --remote-debugging-port flag. Close Revit fully and relaunch it, then retry.");
        }

        // Ephemeral port mode: each WebView2 process binds its own port and writes it
        // to {UDF}/DevToolsActivePort. There's no fixed port to probe or free.
        if (_config.DebuggingPort == 0)
        {
            LogMessage("    Ephemeral port mode — skipping bind probe and auto-free");
            return true;
        }

        // Port state: bind probe.
        var portState = ProbePortState(_config.DebuggingPort);
        LogMessage($"    Port {_config.DebuggingPort} status = {portState}");

        // If the port is occupied, identify the owner and auto-free if it's foreign.
        if (portState == PortState.InUseByOther)
        {
            if (!await TryAutoFreeCdpPortAsync(revitStart, cancellationToken))
                return false;
        }

        // /json/version probe reflects the state AFTER any auto-free.
        var versionProbe = await CdpClient.ProbeVersionAsync(_config.DebuggingPort, cancellationToken);
        if (versionProbe.CdpUp)
        {
            var trimmed = (versionProbe.VersionJson ?? "").Replace('\n', ' ').Replace('\r', ' ');
            if (trimmed.Length > 300) trimmed = trimmed[..300] + "...";
            LogMessage($"    /json/version responded: {trimmed}");
        }
        else
        {
            LogMessage($"    /json/version did not respond ({versionProbe.Error})");
        }

        return true;
    }

    /// <summary>
    /// When the CDP port is occupied, identify the owning process and — unless it's Revit
    /// itself, a child of Revit, or a system-critical process — terminate it so Revit's
    /// WebView2 can bind the port on its next popup. Returns true when it's safe to
    /// continue (either expected owner, or foreign owner successfully killed, or opt-out).
    /// </summary>
    private async Task<bool> TryAutoFreeCdpPortAsync(DateTime? revitStart, CancellationToken cancellationToken)
    {
        var ownerPid = FindPortOwnerPid(_config.DebuggingPort);
        if (ownerPid == null)
        {
            // Port reports "in use" but no listener found — possibly TIME_WAIT or a brief
            // race. Nothing actionable; let the /json probes below show the real state.
            LogMessage($"    Port {_config.DebuggingPort} owner PID = <unknown>");
            return true;
        }

        Process? ownerProc = null;
        try { ownerProc = Process.GetProcessById((int)ownerPid.Value); }
        catch { /* process exited between GetExtendedTcpTable and now */ }

        var ownerName = ownerProc?.ProcessName ?? "<unknown>";
        LogMessage($"    Port {_config.DebuggingPort} owner = {ownerName} (PID {ownerPid})");

        var kind = ClassifyPortOwner(ownerPid.Value, ownerProc, _config.RevitProcessId);
        switch (kind)
        {
            case OwnerKind.Expected:
                LogMessage("    Owner is Revit or a child of Revit — expected. Proceeding.");
                return true;

            case OwnerKind.SystemCritical:
                LogMessage($"[!] Port {_config.DebuggingPort} is held by a system-critical process ({ownerName}).");
                LogMessage("[!] Cannot safely terminate. Change the CDP port in Advanced options and retry.");
                return false;

            case OwnerKind.Foreign:
                if (!_config.AutoFreeCdpPort)
                {
                    LogMessage($"[!] Port {_config.DebuggingPort} is held by foreign process {ownerName} (PID {ownerPid}).");
                    LogMessage("[!] Auto-free CDP port is disabled. Terminate it manually or change the port in Advanced.");
                    return false;
                }

                DateTime? ownerStart = null;
                try { ownerStart = ownerProc?.StartTime; } catch { }

                LogMessage($"[*] Port {_config.DebuggingPort} is held by foreign process {ownerName} (PID {ownerPid}). Terminating...");
                try
                {
                    ownerProc!.Kill();
                    ownerProc.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    LogMessage($"[!] Failed to terminate {ownerName} (PID {ownerPid}): {ex.Message}");
                    LogMessage("[!] Close it manually, or change the CDP port in Advanced and retry.");
                    return false;
                }

                await Task.Delay(500, cancellationToken);

                var postState = ProbePortState(_config.DebuggingPort);
                if (postState == PortState.Free)
                {
                    LogMessage($"[+] Port {_config.DebuggingPort} is now free.");
                    if (ownerStart.HasValue && revitStart.HasValue && ownerStart.Value < revitStart.Value)
                    {
                        LogMessage("[*] Heads up: the offender was running before Revit started. If the next");
                        LogMessage("    attempt still can't see the Dalux popup, restart Revit fully and retry.");
                    }
                    return true;
                }

                LogMessage($"[!] Port {_config.DebuggingPort} is still held after termination. Abort.");
                return false;
        }

        return true;
    }

    private enum OwnerKind { Expected, SystemCritical, Foreign }

    private static readonly HashSet<string> SystemCriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "svchost", "csrss", "winlogon", "lsass",
        "services", "smss", "wininit", "dwm", "explorer"
    };

    private static OwnerKind ClassifyPortOwner(uint ownerPid, Process? ownerProc, int revitPid)
    {
        if (ownerPid == 0 || ownerPid == 4)
            return OwnerKind.SystemCritical;

        if ((int)ownerPid == revitPid)
            return OwnerKind.Expected;

        if (ownerProc != null && IsDescendantOf(ownerProc.Id, revitPid))
            return OwnerKind.Expected;

        if (ownerProc != null && SystemCriticalProcessNames.Contains(ownerProc.ProcessName))
            return OwnerKind.SystemCritical;

        return OwnerKind.Foreign;
    }

    /// <summary>
    /// Returns true if <paramref name="childPid"/> is a descendant of <paramref name="ancestorPid"/>
    /// in the process tree. Uses Toolhelp32 to snapshot parent PIDs.
    /// </summary>
    private static bool IsDescendantOf(int childPid, int ancestorPid)
    {
        if (ancestorPid <= 0) return false;

        var parents = new Dictionary<int, int>();
        IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snap == new IntPtr(-1)) return false;
        try
        {
            var pe = new PROCESSENTRY32 { dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<PROCESSENTRY32>() };
            if (!Process32First(snap, ref pe)) return false;
            do
            {
                parents[(int)pe.th32ProcessID] = (int)pe.th32ParentProcessID;
            } while (Process32Next(snap, ref pe));
        }
        finally
        {
            CloseHandle(snap);
        }

        var cur = childPid;
        for (int i = 0; i < 32; i++)
        {
            if (!parents.TryGetValue(cur, out var parent)) return false;
            if (parent == ancestorPid) return true;
            if (parent == 0 || parent == cur) return false;
            cur = parent;
        }
        return false;
    }

    /// <summary>
    /// Enumerates localhost listening ports owned by any msedgewebview2.exe process
    /// and notes whether the process appears to descend from the target Revit PID.
    /// The descendant check is informational only — WebView2 browser processes are
    /// frequently spawned under a broker (RuntimeBroker, dllhost) rather than as
    /// direct children of the hosting app, so filtering on it hides real candidates.
    /// </summary>
    private List<(int Port, int Pid, bool IsRevitDescendant)> FindWebViewCdpPorts()
    {
        var result = new List<(int Port, int Pid, bool IsRevitDescendant)>();
        var seen = new HashSet<int>();

        foreach (var entry in EnumerateListeningPorts())
        {
            if (seen.Contains(entry.Port)) continue;

            try
            {
                using var proc = Process.GetProcessById(entry.Pid);
                if (!string.Equals(proc.ProcessName, "msedgewebview2", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool descendant = IsDescendantOf(entry.Pid, _config.RevitProcessId);
                result.Add((entry.Port, entry.Pid, descendant));
                seen.Add(entry.Port);
            }
            catch
            {
                // Process exited between enumeration and lookup; skip.
            }
        }

        return result;
    }

    /// <summary>
    /// Uses GetExtendedTcpTable to enumerate all IPv4 loopback TCP LISTEN sockets.
    /// Returns a list of (local port, owning PID) tuples.
    /// </summary>
    private static List<(int Port, int Pid)> EnumerateListeningPorts()
    {
        const int AF_INET = 2;
        const int TCP_TABLE_OWNER_PID_LISTENER = 3;
        const int NO_ERROR = 0;
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        var result = new List<(int Port, int Pid)>();

        uint size = 0;
        int rc = GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0);
        if (rc != NO_ERROR && rc != ERROR_INSUFFICIENT_BUFFER) return result;

        IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal((int)size);
        try
        {
            rc = GetExtendedTcpTable(buffer, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0);
            if (rc != NO_ERROR) return result;

            uint count = (uint)System.Runtime.InteropServices.Marshal.ReadInt32(buffer);
            int rowSize = System.Runtime.InteropServices.Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
            IntPtr rowPtr = buffer + sizeof(uint);
            for (uint i = 0; i < count; i++)
            {
                var row = System.Runtime.InteropServices.Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                // local port is stored big-endian in the low 16 bits of a 32-bit field
                int localPort = (int)(((row.localPort & 0xFF) << 8) | ((row.localPort >> 8) & 0xFF));
                result.Add((localPort, (int)row.owningPid));
                rowPtr += rowSize;
            }
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }
        return result;
    }

    /// <summary>
    /// Finds the PID listening on <paramref name="port"/> on IPv4 loopback.
    /// Returns null if nothing is listening on that port.
    /// </summary>
    private static uint? FindPortOwnerPid(int port)
    {
        foreach (var entry in EnumerateListeningPorts())
        {
            if (entry.Port == port) return (uint)entry.Pid;
        }
        return null;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public uint localPort;
        public uint remoteAddr;
        public uint remotePort;
        public uint owningPid;
    }

    [System.Runtime.InteropServices.DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref uint pdwSize,
        bool bOrder,
        int ulAf,
        int tableClass,
        uint reserved);

    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Logs extra diagnostics when the multi-port wait can't find a Dalux tab. Dumps
    /// every WebView2 CDP endpoint's /json (with Revit-descendant status noted), then
    /// the configured port's owner as a final fallback when no msedgewebview2 is
    /// listening anywhere.
    /// </summary>
    private async Task LogCdpTimeoutDiagnosticsAsync(CancellationToken cancellationToken)
    {
        // DevToolsActivePort is the definitive evidence that the Dalux WebView2 process
        // started with --remote-debugging-port. Its absence means the popup either
        // didn't spawn or didn't inherit the env var.
        LogDevToolsActivePortFileState();

        var candidatePorts = FindWebViewCdpPorts();

        if (candidatePorts.Count > 0)
        {
            LogMessage($"[*] {candidatePorts.Count} msedgewebview2 listening port(s) found post-timeout:");
            foreach (var (port, pid, isRevitDescendant) in candidatePorts)
            {
                var parentNote = isRevitDescendant ? "Revit descendant" : "parent chain not Revit";
                var probe = await CdpClient.ProbeVersionAsync(port, cancellationToken);
                if (!probe.CdpUp)
                {
                    LogMessage($"    port={port} pid={pid} ({parentNote}) — /json/version did not respond ({probe.Error ?? "no detail"}).");
                    continue;
                }

                var tabs = await CdpClient.GetAllTabsAsync(port, cancellationToken);
                LogMessage($"    port={port} pid={pid} ({parentNote}) — {tabs.Count} debuggable target(s):");
                for (int i = 0; i < tabs.Count; i++)
                {
                    var t = tabs[i];
                    var title = (t.Title ?? "").Replace('\n', ' ').Replace('\r', ' ');
                    var url = (t.Url ?? "").Replace('\n', ' ').Replace('\r', ' ');
                    if (title.Length > 120) title = title[..120] + "...";
                    if (url.Length > 200) url = url[..200] + "...";
                    LogMessage($"        [{i}] title=\"{title}\" url=\"{url}\"");
                }
            }

            LogMessage("[!] No CDP target containing 'dalux' was found on any msedgewebview2 process.");
            LogMessage("[!] The Dalux popup may be running with AdditionalBrowserArguments that disable CDP,");
            LogMessage("[!] or the Dalux addon explicitly overrides the env var. Next step: verify with");
            LogMessage("[!]   Get-CimInstance Win32_Process | ? { $_.Name -eq 'msedgewebview2.exe' } |");
            LogMessage("[!]   select ProcessId, ParentProcessId, CommandLine");
            LogMessage("[!] that the popup's msedgewebview2.exe actually carries --remote-debugging-port=.");
            return;
        }

        // No msedgewebview2 is listening on any localhost port. Fall back to the old
        // single-port failure-mode diagnosis and, if the configured port is busy, log
        // exactly who's holding it so we can see whether it's a stray that keeps
        // respawning after our auto-free kill.
        var portState = ProbePortState(_config.DebuggingPort);
        LogMessage($"[*] No msedgewebview2 listening ports found. Configured port {_config.DebuggingPort} status = {portState}");

        if (portState == PortState.InUseByOther)
        {
            var ownerPid = FindPortOwnerPid(_config.DebuggingPort);
            if (ownerPid.HasValue)
            {
                try
                {
                    using var owner = Process.GetProcessById((int)ownerPid.Value);
                    LogMessage($"[!] Port {_config.DebuggingPort} is held by {owner.ProcessName} (PID {ownerPid.Value}).");
                }
                catch
                {
                    LogMessage($"[!] Port {_config.DebuggingPort} is held by PID {ownerPid.Value} (process info unavailable).");
                }
            }
            else
            {
                LogMessage($"[!] Port {_config.DebuggingPort} is held but its owner could not be identified.");
            }
            LogMessage("[!] Re-run with a different port via Advanced options -> CDP Debugging Port (e.g., 9223).");
        }
        else
        {
            LogMessage("[!] No WebView2 is exposing CDP on any port.");
            LogMessage("[!] Likely causes:");
            LogMessage("[!]   (a) Revit was not fully restarted since the env var was registered.");
            LogMessage("[!]   (b) The Dalux addon on this PC sets AdditionalBrowserArguments explicitly,");
            LogMessage("[!]       overriding WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS.");
            LogMessage("[!]   (c) WebView2 Runtime is outdated (see version logged in pre-flight).");
            LogMessage("[!]   (d) Antivirus/Defender blocked the CDP port bind.");
        }
    }

    /// <summary>
    /// Logs the state of the Dalux DevToolsActivePort file: presence, contents (port and
    /// /devtools/browser/{uuid} lines), last-write timestamp, and parent UDF dir presence.
    /// Lets the user see at a glance whether Dalux's WebView2 even started with CDP.
    /// </summary>
    private void LogDevToolsActivePortFileState()
    {
        var path = DaluxDevToolsActivePortPath;
        try
        {
            if (File.Exists(path))
            {
                var lastWrite = File.GetLastWriteTime(path);
                LogMessage($"[*] DevToolsActivePort file present: {path}");
                LogMessage($"    last-write = {lastWrite:s}");
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    var line1 = reader.ReadLine();
                    var line2 = reader.ReadLine();
                    LogMessage($"    line 1 (port)   = \"{line1}\"");
                    LogMessage($"    line 2 (target) = \"{line2}\"");
                }
                catch (Exception ex)
                {
                    LogMessage($"    could not read contents: {ex.Message}");
                }
            }
            else
            {
                LogMessage($"[*] DevToolsActivePort file NOT present at {path}");
                var parentDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DaluxWebView2");
                if (Directory.Exists(parentDir))
                    LogMessage($"    parent dir exists: {parentDir}");
                else
                    LogMessage($"    parent dir DOES NOT exist: {parentDir} — Dalux WebView2 may not have spawned at all");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[!] DevToolsActivePort probe failed: {ex.Message}");
        }
    }

    private enum PortState { Free, InUseByOther, ProbeError }

    private static PortState ProbePortState(int port)
    {
        System.Net.Sockets.TcpListener? listener = null;
        try
        {
            listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            return PortState.Free;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return PortState.InUseByOther;
        }
        catch
        {
            return PortState.ProbeError;
        }
        finally
        {
            try { listener?.Stop(); } catch { }
        }
    }

    private static string? ReadWebView2RuntimeVersion()
    {
        // Evergreen runtime GUID: F3017226-FE2A-4295-8BDF-00C3A9A7E4C5
        string[] candidates =
        {
            @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
            @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
        };
        foreach (var path in candidates)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                var pv = key?.GetValue("pv") as string;
                if (!string.IsNullOrEmpty(pv)) return pv;
            }
            catch { }
        }
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}");
            var pv = key?.GetValue("pv") as string;
            if (!string.IsNullOrEmpty(pv)) return pv;
        }
        catch { }
        return null;
    }

    private static DateTime? ReadEnvVarLastWriteTime()
    {
        // HKCU\Environment holds user-scope env vars. The last-write time of the KEY
        // (not the individual value) is the best proxy .NET exposes without P/Invoke —
        // accurate to the most recent write to any user env var, which is good enough
        // for "did Revit start before or after we wrote this".
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment");
            if (key == null) return null;
            const uint KEY_QUERY_VALUE = 0x0001;
            IntPtr hKey = IntPtr.Zero;
            try
            {
                int rc = RegOpenKeyExW(new IntPtr(unchecked((int)0x80000001)), "Environment", 0, KEY_QUERY_VALUE, out hKey);
                if (rc != 0) return null;
                var ftLastWrite = new System.Runtime.InteropServices.ComTypes.FILETIME();
                rc = RegQueryInfoKeyW(hKey, null, IntPtr.Zero, IntPtr.Zero,
                    out _, out _, out _, out _, out _, out _, out _, ref ftLastWrite);
                if (rc != 0) return null;
                long ft = ((long)ftLastWrite.dwHighDateTime << 32) | (uint)ftLastWrite.dwLowDateTime;
                return DateTime.FromFileTime(ft);
            }
            finally
            {
                if (hKey != IntPtr.Zero) RegCloseKey(hKey);
            }
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int RegOpenKeyExW(IntPtr hKey, string lpSubKey, int ulOptions, uint samDesired, out IntPtr phkResult);

    [System.Runtime.InteropServices.DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);

    [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int RegQueryInfoKeyW(
        IntPtr hKey,
        System.Text.StringBuilder? lpClass,
        IntPtr lpcchClass,
        IntPtr lpReserved,
        out uint lpcSubKeys,
        out uint lpcbMaxSubKeyLen,
        out uint lpcbMaxClassLen,
        out uint lpcValues,
        out uint lpcbMaxValueNameLen,
        out uint lpcbMaxValueLen,
        out uint lpcbSecurityDescriptor,
        ref System.Runtime.InteropServices.ComTypes.FILETIME lpftLastWriteTime);

    private async Task ExecuteAutomationLogicAsync(CancellationToken cancellationToken)
    {
        LogMessage($"\n[*] Processing files, target: '{_config.TargetFilename}'");

        var jsScript = GenerateMainAutomationScript();
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

    /// <summary>
    /// Generates the complete JavaScript automation script implementing all 4 steps
    /// </summary>
    private string GenerateMainAutomationScript()
    {
        // Serialize all configuration values to JSON
        var targetJson = JsonSerializer.Serialize(_config.TargetFilename);
        var revisionIncrementStr = _config.RevisionIncrement.ToString();
        var columnConfigJson = JsonSerializer.Serialize(_config.ColumnFields);
        var actionButtonJson = JsonSerializer.Serialize(_config.ActionButtonText);

        var lines = new List<string>
        {
            "(async function() {",
            "    const results = [];",
            "    try {",
            $"        const target = {targetJson}.toLowerCase();",
            $"        const incrementValue = parseFloat(\"{revisionIncrementStr}\");",
            $"        const columnConfig = {columnConfigJson};",
            $"        const actionButtonText = {actionButtonJson};",
            "",
            "        // --- WAIT FOR POPUP TO FULLY LOAD ---",
            "        results.push('\\n--- WAITING FOR DALUX POPUP TO LOAD ---');",
            "        try {",
            "            let loadAttempts = 0;",
            "            const maxLoadAttempts = 1200; // 10 minutes (1200 × 500ms)",
            "            const countRows = () => {",
            "                let n = document.querySelectorAll('tr, [role=\"row\"]').length;",
            "                if (n === 0) {",
            "                    for (const f of document.querySelectorAll('iframe')) {",
            "                        try { n += f.contentDocument.querySelectorAll('tr, [role=\"row\"]').length; } catch(e) {}",
            "                    }",
            "                }",
            "                return n;",
            "            };",
            "            const countCbs = () => {",
            "                let n = document.querySelectorAll('input[type=\"checkbox\"], input[data-cy=\"checkbox-input-field\"]').length;",
            "                if (n === 0) {",
            "                    for (const f of document.querySelectorAll('iframe')) {",
            "                        try { n += f.contentDocument.querySelectorAll('input[type=\"checkbox\"], input[data-cy=\"checkbox-input-field\"]').length; } catch(e) {}",
            "                    }",
            "                }",
            "                return n;",
            "            };",
            "            while (loadAttempts < maxLoadAttempts) {",
            "                const rows = countRows();",
            "                const checkboxes = countCbs();",
            "                if (rows > 1 || checkboxes > 0) {",
            "                    results.push('[+] Popup fully loaded (' + rows + ' rows, ' + checkboxes + ' checkboxes)');",
            "                    break;",
            "                }",
            "                await new Promise(r => setTimeout(r, 500));",
            "                loadAttempts++;",
            "                // Every 2 seconds: fire resize + force virtual scroll viewports to non-zero height",
            "                if (loadAttempts % 4 === 0) {",
            "                    window.dispatchEvent(new Event('resize'));",
            "                    document.querySelectorAll('cdk-virtual-scroll-viewport, [class*=\"virtual-scroll\"]').forEach(vp => {",
            "                        if (!vp.getBoundingClientRect().height)",
            "                            vp.style.setProperty('height', '600px', 'important');",
            "                    });",
            "                }",
            "            }",
            "            if (loadAttempts >= maxLoadAttempts) {",
            "                const diagTitle   = document.title;",
            "                const diagUrl     = document.URL.substring(0, 100);",
            "                const diagReady   = document.readyState;",
            "                const diagElems   = document.querySelectorAll('*').length;",
            "                const diagIframes = document.querySelectorAll('iframe').length;",
            "                results.push('[DEBUG] Title: ' + diagTitle);",
            "                results.push('[DEBUG] URL: ' + diagUrl);",
            "                results.push('[DEBUG] ReadyState: ' + diagReady + ' | Elements: ' + diagElems + ' | iframes: ' + diagIframes);",
            "                if (diagIframes > 0) results.push('[DEBUG] rows in iframes: ' + countRows() + ' | checkboxes in iframes: ' + countCbs());",
            "                results.push('[!] Timed out (10 min) waiting for popup to load — no rows or checkboxes found. Aborting.');",
            "                return results.join('\\n');",
            "            }",
            "        } catch(e) {",
            "            results.push('[!] Error waiting for popup load: ' + e.message);",
            "        }",
            "",
            "        // --- STEP 0: RESETTING ALL SELECTIONS ---",
            "        results.push('\\n--- STEP 0: RESETTING SELECTIONS ---');",
            "        try {",
            "            let allCbs = Array.from(document.querySelectorAll('input[data-cy=\"checkbox-input-field\"]'));",
            "            if (allCbs.length === 0) {",
            "                allCbs = Array.from(document.querySelectorAll('input[type=\"checkbox\"]')).filter(cb => {",
            "                    if (cb.closest('mat-slide-toggle, .toggle, .switch, dlx-toggle')) return false;",
            "                    let rect = cb.getBoundingClientRect();",
            "                    if (rect.left > window.innerWidth * 0.7 && rect.left > 0) return false;",
            "                    return true;",
            "                });",
            "            }",
            "            let counterEls = Array.from(document.querySelectorAll('*')).filter(el => {",
            "                let txt = (el.innerText || '').trim();",
            "                let r = el.getBoundingClientRect();",
            "                return /^[0-9]+\\s*\\/\\s*[0-9]+$/.test(txt) && r.width > 0 && el.children.length === 0;",
            "            });",
            "            if (counterEls.length > 0 && allCbs.length > 0) {",
            "                let counterEl = counterEls[0];",
            "                let masterCb = allCbs[0];",
            "                let clickTarget = masterCb.closest('.checkbox')  || masterCb.closest('div') || masterCb;",
            "                let getSelectedCount = () => parseInt((counterEl.innerText || '0').split('/')[0].trim());",
            "                results.push('[*] Initial selection state: ' + counterEl.innerText);",
            "                let toggleCount = 0;",
            "                while (getSelectedCount() > 0 && toggleCount < 4) {",
            "                    results.push('[*] Clearing selections... (Click ' + (toggleCount + 1) + ')');",
            "                    ['pointerdown','mousedown','pointerup','mouseup','click'].forEach(evt => {",
            "                        clickTarget.dispatchEvent(new MouseEvent(evt, {bubbles: true, composed: true}));",
            "                    });",
            "                    masterCb.click();",
            "                    await new Promise(r => setTimeout(r, 600));",
            "                    toggleCount++;",
            "                }",
            "                results.push('[+] Master selection reset. Current state: ' + counterEl.innerText);",
            "            } else {",
            "                results.push('[*] Master counter not found. Proceeding.');",
            "            }",
            "        } catch (e) {",
            "            results.push('[!] Step 0 error: ' + e.message);",
            "        }",
            "",
            "        // --- STEP 1: FINDING TARGET FILE ---",
            "        const selectors = ['tr', '[role=\"row\"]', '[role=\"listitem\"]'];",
            "        let rows = [];",
            "        for (const sel of selectors) {",
            "            rows = Array.from(document.querySelectorAll(sel));",
            "            if (rows.length > 0) break;",
            "        }",
            "        results.push('\\n--- STEP 1: CHECKING TARGET FILE ---');",
            "        results.push('Found ' + rows.length + ' rows');",
            "        let targetRow = null;",
            "        for (const row of rows) {",
            "            const text = row.textContent ? row.textContent.trim().toLowerCase() : '';",
            "            if (text.includes(target)) {",
            "                results.push('[+] FOUND TARGET: ' + target);",
            "                targetRow = row;",
            "                let cb = row.querySelector('input[type=\"checkbox\"]');",
            "                if (cb && !cb.checked) cb.click();",
            "                break;",
            "            }",
            "        }",
            "        if (!targetRow) {",
            "            results.push('[!] TARGET NOT FOUND');",
            "            return results.join('\\n');",
            "        }",
            "",
            "        // --- STEP 2: EXTRACTING & UPDATING METADATA ---",
            "        results.push('\\n--- STEP 2: EXTRACTING & UPDATING METADATA ---');",
            "        const extractedData = {};",
            "        let revisionUpdated = false;",
            "        let revisionOldVal = '';",
            "        let revisionNewVal = '';",
            "        let columnsUpdated = {};",
            "        for (let key of Object.keys(columnConfig)) columnsUpdated[key] = false;",
            "        let allDetectedHeaders = new Set();",
            "",
            "        const getHeaderText = (el) => {",
            "            if (!el) return '';",
            "            let title = el.getAttribute('title') || el.getAttribute('data-tooltip') || el.getAttribute('mat-tooltip');",
            "            if (title && title.trim().length > 0) return title.trim();",
            "            let text = String(el.innerText || el.textContent || '');",
            "            text = text.replace(String.fromCharCode(10), ' ').replace(String.fromCharCode(13), ' ');",
            "            return text.trim();",
            "        };",
            "",
            "        const typeText = async (el, text) => {",
            "            el.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});",
            "            el.focus();",
            "            if (el.select) el.select();",
            "            await new Promise(r => setTimeout(r, 50));",
            "            let nativeSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value');",
            "            if (!nativeSetter) nativeSetter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value');",
            "            if (nativeSetter && nativeSetter.set) nativeSetter.set.call(el, '');",
            "            else el.value = '';",
            "            el.dispatchEvent(new Event('input', { bubbles: true, composed: true }));",
            "            await new Promise(r => setTimeout(r, 50));",
            "            for (let i = 0; i < text.length; i++) {",
            "                let char = text[i];",
            "                if (nativeSetter && nativeSetter.set) nativeSetter.set.call(el, el.value + char);",
            "                else el.value += char;",
            "                el.dispatchEvent(new KeyboardEvent('keydown', { key: char, bubbles: true, composed: true }));",
            "                el.dispatchEvent(new Event('input', { bubbles: true, composed: true }));",
            "                el.dispatchEvent(new KeyboardEvent('keyup', { key: char, bubbles: true, composed: true }));",
            "                await new Promise(r => setTimeout(r, 30));",
            "            }",
            "            await new Promise(r => setTimeout(r, 400));",
            "            el.dispatchEvent(new Event('change', { bubbles: true, composed: true }));",
            "            el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, composed: true }));",
            "            el.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, composed: true }));",
            "            el.blur();",
            "            el.dispatchEvent(new Event('focusout', { bubbles: true, composed: true }));",
            "            await new Promise(r => setTimeout(r, 100));",
            "            let safeSpot = document.querySelector('.dlx-datagrid-header-cell') || document.body;",
            "            safeSpot.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));",
            "            safeSpot.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));",
            "            safeSpot.click();",
            "            await new Promise(r => setTimeout(r, 200));",
            "        };",
            "",
            "        const handleDropdown = async (cell, targetValue, headerName) => {",
            "            results.push('[*] Processing Dropdown: ' + headerName + ' -> Target: ' + targetValue);",
            "            cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});",
            "            cell.click();",
            "            cell.dispatchEvent(new MouseEvent('dblclick', {bubbles: true, composed: true}));",
            "            await new Promise(r => setTimeout(r, 400));",
            "            let cRect = cell.getBoundingClientRect();",
            "            let hitX = cRect.right - 15;",
            "            let hitY = cRect.top + (cRect.height / 2);",
            "            results.push('[*] Dropdown pointer: cellRect={left:' + Math.round(cRect.left) + ',top:' + Math.round(cRect.top) + ',w:' + Math.round(cRect.width) + ',h:' + Math.round(cRect.height) + '} hitX=' + Math.round(hitX) + ' hitY=' + Math.round(hitY));",
            "            let arrowTarget = document.elementFromPoint(hitX, hitY) || cell;",
            "            results.push('[*] arrowTarget: ' + (arrowTarget === cell ? 'cell (elementFromPoint returned null)' : arrowTarget.tagName + '.' + (arrowTarget.className||'').split(' ').slice(0,2).join('.')));",
            "            ['pointerdown', 'mousedown', 'pointerup', 'mouseup', 'click'].forEach(evt => {",
            "                arrowTarget.dispatchEvent(new MouseEvent(evt, {",
            "                    bubbles: true, composed: true, view: window, clientX: hitX, clientY: hitY",
            "                }));",
            "            });",
            "            let focusTarget = document.activeElement || cell;",
            "            focusTarget.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', altKey: true, bubbles: true, composed: true }));",
            "            await new Promise(r => setTimeout(r, 1000));",
            "            let success = false;",
            "            const optionSelectors = '[role=\"option\"], .mat-option, .ng-option, .dropdown-item, li.dlx-list-item, div.option, .dlx-dropdown-item, li span';",
            "            const options = Array.from(document.querySelectorAll(optionSelectors));",
            "            results.push('[*] Options in DOM after Alt+Down: ' + options.length + (options.length > 0 ? ' | first: \"' + (options[0].innerText||options[0].textContent||'').trim().substring(0,30) + '\"' : ''));",
            "            results.push('[*] Searching ' + options.length + ' options for: \"' + targetValue + '\" | available: ' + options.slice(0,8).map(o => '\"' + (o.innerText||o.textContent||'').trim().substring(0,20) + '\"').join(', '));",
            "            let targetOption = options.find(opt => {",
            "                let text = (opt.innerText || opt.textContent || '').trim().toLowerCase();",
            "                return text === targetValue.toLowerCase() || text.includes(targetValue.toLowerCase()) || targetValue.toLowerCase().includes(text) && text.length > 2;",
            "            });",
            "            if (targetOption && targetOption.getBoundingClientRect().width > 0) {",
            "                results.push('[*] Target option \\'' + targetValue + '\\' found directly. Clicking it...');",
            "                targetOption.scrollIntoView({behavior: 'instant', block: 'center'});",
            "                targetOption.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));",
            "                targetOption.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));",
            "                targetOption.click();",
            "                success = true;",
            "            }",
            "            if (!success) {",
            "                let searchInput = null;",
            "                let activeEl = document.activeElement;",
            "                if (activeEl && activeEl.tagName === 'INPUT' && activeEl.type !== 'checkbox' && !cell.contains(activeEl)) {",
            "                    searchInput = activeEl;",
            "                } else {",
            "                    let overlayInputs = Array.from(document.querySelectorAll('body > *:last-child input, .cdk-overlay-container input, .mat-select-panel input, .dropdown-menu input, .dlx-dropdown input, input[placeholder*=\"earch\"]'))",
            "                        .filter(i => i.getBoundingClientRect().width > 0 && !cell.contains(i));",
            "                    if (overlayInputs.length > 0) searchInput = overlayInputs[0];",
            "                }",
            "                if (searchInput) {",
            "                    results.push('[*] Typing \\'' + targetValue + '\\' into search bar...');",
            "                    await typeText(searchInput, targetValue);",
            "                    success = true;",
            "                } else {",
            "                    results.push('[!] Could NOT safely identify an overlay for ' + headerName);",
            "                }",
            "            }",
            "            let safeSpot = document.querySelector('.dlx-datagrid-header-cell') || document.body;",
            "            safeSpot.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));",
            "            safeSpot.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));",
            "            safeSpot.click();",
            "            await new Promise(r => setTimeout(r, 300));",
            "            return success;",
            "        };",
            "",
            "        const handleDatePicker = async (cell, targetDateStr, headerName) => {",
            "            try {",
            "                // STEP 1: TRIGGER THE DATE PICKER",
            "                cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});",
            "                let triggerEl = cell.querySelector('[data-cy=\"datepicker-input-box\"], [class*=\"calendar\"], [class*=\"chevron\"]');",
            "                if (!triggerEl) triggerEl = cell.querySelector('button, [role=\"button\"]');",
            "                if (!triggerEl) triggerEl = cell;",
            "                ",
            "                // Dispatch full pointer event sequence for complex UI frameworks",
            "                triggerEl.dispatchEvent(new PointerEvent('pointerdown', {bubbles: true, composed: true}));",
            "                triggerEl.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));",
            "                triggerEl.dispatchEvent(new PointerEvent('pointerup', {bubbles: true, composed: true}));",
            "                triggerEl.dispatchEvent(new MouseEvent('mouseup', {bubbles: true}));",
            "                triggerEl.dispatchEvent(new MouseEvent('click', {bubbles: true}));",
            "                await new Promise(r => setTimeout(r, 500));",
            "                ",
            "                // STEP 2: LOCATE THE CALENDAR OVERLAY",
            "                let calendar = document.querySelector('dlx-date-calender, [data-cy=\"calendar-header\"], .dlx-date-picker');",
            "                if (!calendar) {",
            "                    calendar = document.querySelector('.cdk-overlay-pane [data-cy=\"calendar-header\"]');",
            "                }",
            "                if (!calendar) {",
            "                    for (let overlay of document.querySelectorAll('.cdk-overlay-pane')) {",
            "                        if (overlay.textContent.includes('Sun') || overlay.textContent.includes('Mon')) {",
            "                            calendar = overlay;",
            "                            break;",
            "                        }",
            "                    }",
            "                }",
            "                if (!calendar) {",
            "                    results.push('[!] Could not update ' + headerName + ' - date picker not found');",
            "                    return false;",
            "                }",
            "                ",
            "                // STEP 3: PARSE TARGET DATE",
            "                const monthNames = {jan:1,feb:2,mar:3,apr:4,may:5,jun:6,jul:7,aug:8,sep:9,oct:10,nov:11,dec:12};",
            "                let day, month, year;",
            "                let parts = targetDateStr.split('-');",
            "                if (parts.length < 3) parts = targetDateStr.split('/');",
            "                if (parts.length < 3) {",
            "                    let textMatch = targetDateStr.match(/(\\d{1,2})\\s+([A-Za-z]+)\\s+(\\d{4})/);",
            "                    if (textMatch) {",
            "                        day = parseInt(textMatch[1]);",
            "                        month = monthNames[textMatch[2].toLowerCase().substring(0,3)] || parseInt(textMatch[2]);",
            "                        year = parseInt(textMatch[3]);",
            "                    } else {",
            "                        results.push('[!] Invalid date format: ' + targetDateStr);",
            "                        return false;",
            "                    }",
            "                } else {",
            "                    day = parseInt(parts[0]);",
            "                    let monthPart = parts[1].toLowerCase();",
            "                    month = monthNames[monthPart.substring(0,3)] || parseInt(monthPart);",
            "                    year = parseInt(parts[2]);",
            "                }",
            "                if (year < 100) year += 2000;",
            "                const targetTotalMonths = (year * 12) + month;",
            "                ",
            "                // STEP 4: NAVIGATE TO TARGET MONTH/YEAR",
            "                let maxNav = 24;",
            "                let currentNav = 0;",
            "                while (currentNav < maxNav) {",
            "                    let headerEl = calendar.querySelector('[data-cy=\"calendar-header\"], [class*=\"header\"]');",
            "                    let currentText = '';",
            "                    for (let wait = 0; wait < 10; wait++) {",
            "                        currentText = headerEl ? (headerEl.innerText || headerEl.textContent || '') : '';",
            "                        if (currentText.match(/\\d{4}/) || currentText.includes('2026') || currentText.includes('2025')) break;",
            "                        await new Promise(r => setTimeout(r, 200));",
            "                    }",
            "                    ",
            "                    let match = currentText.match(/(\\d{4})|(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)/gi);",
            "                    let currentMonth = 1, currentYear = 2026;",
            "                    if (match) {",
            "                        for (let m of match) {",
            "                            if (/\\d{4}/.test(m)) currentYear = parseInt(m);",
            "                            else currentMonth = Math.max(currentMonth, Object.keys(monthNames).indexOf(m.toLowerCase().substring(0,3)) + 1);",
            "                        }",
            "                    }",
            "                    const currentTotalMonths = (currentYear * 12) + currentMonth;",
            "                    ",
            "                    if (currentTotalMonths === targetTotalMonths) break;",
            "                    ",
            "                    let navBtn = null;",
            "                    if (currentTotalMonths < targetTotalMonths) {",
            "                        navBtn = calendar.querySelector('[data-cy=\"date-next-month-btn\"]');",
            "                    } else {",
            "                        navBtn = calendar.querySelector('[data-cy=\"date-prev-month-btn\"]');",
            "                    }",
            "                    if (!navBtn) navBtn = calendar.querySelectorAll('button')[currentTotalMonths < targetTotalMonths ? 1 : 0];",
            "                    if (!navBtn) break;",
            "                    navBtn.click();",
            "                    await new Promise(r => setTimeout(r, 300));",
            "                    currentNav++;",
            "                }",
            "                ",
            "                // STEP 5: SELECT THE CORRECT DAY",
            "                let dayElements = Array.from(calendar.querySelectorAll('button, [role=\"button\"], td, div, span'));",
            "                dayElements = dayElements.filter(el => {",
            "                    if (el.offsetHeight === 0) return false;",
            "                    let text = el.textContent.trim();",
            "                    if (text !== String(day)) return false;",
            "                    let style = window.getComputedStyle(el);",
            "                    if (parseFloat(style.opacity || '1') !== 1) return false;",
            "                    if (el.className.includes('muted') || el.className.includes('disabled')) return false;",
            "                    return true;",
            "                });",
            "                ",
            "                if (dayElements.length > 1) {",
            "                    let minLeft = Math.min(...dayElements.map(el => el.getBoundingClientRect().left));",
            "                    let tolerance = 5;",
            "                    dayElements = dayElements.filter(el => el.getBoundingClientRect().left > minLeft + tolerance);",
            "                }",
            "                ",
            "                if (dayElements.length === 0) {",
            "                    let allElements = calendar.querySelectorAll('*');",
            "                    for (let el of allElements) {",
            "                        if (el.childNodes.length === 1 && el.childNodes[0].nodeType === 3) {",
            "                            let text = el.textContent.trim();",
            "                            if (text === String(day) && el.offsetHeight > 0) {",
            "                                let style = window.getComputedStyle(el);",
            "                                if (parseFloat(style.opacity || '1') === 1) {",
            "                                    dayElements = [el];",
            "                                    break;",
            "                                }",
            "                            }",
            "                        }",
            "                    }",
            "                }",
            "                ",
            "                let dayBtn = dayElements[0];",
            "                if (dayBtn) {",
            "                    dayBtn.scrollIntoView({behavior: 'instant', block: 'nearest'});",
            "                    await new Promise(r => setTimeout(r, 100));",
            "                    dayBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));",
            "                    dayBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true}));",
            "                    dayBtn.dispatchEvent(new MouseEvent('click', {bubbles: true}));",
            "                    await new Promise(r => setTimeout(r, 500));",
            "                    results.push('[+] Updated ' + headerName + ' to ' + targetDateStr);",
            "                    return true;",
            "                } else {",
            "                    results.push('[!] Could not find day ' + day + ' in calendar for ' + headerName);",
            "                    return false;",
            "                }",
            "            } catch(e) {",
            "                results.push('[!] Date picker error for ' + headerName + ': ' + e.message);",
            "                return false;",
            "            }",
            "        };",
            "",
            "        // Helper: get visible non-checkbox cells from the target row.",
            "        // Uses textContent (not innerText) so the row is found even when off-screen or vertically clipped.",
            "        // Does NOT filter the row by width — the row may be below the grid's visible area after moving to last position.",
            "        const getVisibleCells = () => {",
            "            let cells = Array.from(targetRow.querySelectorAll('td, th, [role=\"gridcell\"], [class*=\"cell\"]'));",
            "            if (cells.length === 0) cells = Array.from(targetRow.children);",
            "            let visible = cells.filter(c => c && c.nodeType === 1 && c.getBoundingClientRect().width > 0 && !c.querySelector('input[type=\"checkbox\"]'));",
            "            // Exclude frozen/pinned cells that live outside the scroll container —",
            "            // they have fixed viewport positions and corrupt horizontal position matching.",
            "            if (scrollerContainer) visible = visible.filter(c => scrollerContainer.contains(c));",
            "            return visible;",
            "        };",
            "        // Helper: detect cell type and update it",
            "        const updateCell = async (cell, targetValue, key) => {",
            "            let cR = cell.getBoundingClientRect();",
            "            let cTxt = (cell.innerText || cell.textContent || '').trim().replace(/\\s+/g, ' ').substring(0, 40);",
            "            let isDatePicker = !!cell.querySelector('[data-cy=\"datepicker-input-box\"], dlx-date-calender, dlx-date-picker, [class*=\"date-picker\"], [class*=\"datepicker\"], [class*=\"calendar\"]');",
            "            let isLikelyDropdown = !isDatePicker && !!cell.querySelector('[role=\"combobox\"], [role=\"listbox\"], mat-select, dlx-dropdown, dlx-select, select, [class*=\"dropdown\"], [class*=\"dlx-select\"], [class*=\"mat-select\"]');",
            "            results.push('[*] updateCell: ' + key + ' | cellRect={left:' + Math.round(cR.left) + ',top:' + Math.round(cR.top) + ',w:' + Math.round(cR.width) + '} | existingText=\"' + cTxt + '\" | type=' + (isDatePicker ? 'datePicker' : isLikelyDropdown ? 'dropdown' : 'unknown'));",
            "            if (isDatePicker) {",
            "                results.push('[*] Date picker detected for: ' + key);",
            "                await handleDatePicker(cell, targetValue, key);",
            "                results.push('[+] Updated ' + key + ' to ' + targetValue + ' (date)');",
            "                return true;",
            "            } else if (isLikelyDropdown) {",
            "                results.push('[*] Dropdown detected for: ' + key);",
            "                let ok = await handleDropdown(cell, targetValue, key);",
            "                if (ok) results.push('[+] Updated ' + key + ' to ' + targetValue + ' (dropdown)');",
            "                else results.push('[!] Dropdown failed for: ' + key);",
            "                return ok;",
            "            } else {",
            "                let inputEl = cell.querySelector('input:not([type=\"checkbox\"]), textarea');",
            "                if (inputEl && inputEl.getBoundingClientRect().width > 0) {",
            "                    await typeText(inputEl, targetValue);",
            "                    results.push('[+] Updated ' + key + ' to ' + targetValue + ' (text)');",
            "                    return true;",
            "                } else {",
            "                    cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});",
            "                    cell.click();",
            "                    await new Promise(r => setTimeout(r, 300));",
            "                    cell.dispatchEvent(new MouseEvent('dblclick', {bubbles: true}));",
            "                    await new Promise(r => setTimeout(r, 400));",
            "                    inputEl = cell.querySelector('input:not([type=\"checkbox\"]), textarea') || document.activeElement;",
            "                    if (inputEl && (inputEl.tagName === 'INPUT' || inputEl.tagName === 'TEXTAREA') && inputEl.getBoundingClientRect().width > 0) {",
            "                        await typeText(inputEl, targetValue);",
            "                        results.push('[+] Updated ' + key + ' to ' + targetValue + ' (text)');",
            "                        return true;",
            "                    } else {",
            "                        let ok = await handleDropdown(cell, targetValue, key);",
            "                        if (ok) results.push('[+] Updated ' + key + ' to ' + targetValue + ' (dropdown)');",
            "                        else results.push('[!] No text input or dropdown found for: ' + key);",
            "                        return ok;",
            "                    }",
            "                }",
            "            }",
            "        };",
            "        // ── SCROLL CONTAINER DETECTION ──",
            "        let scrollerContainer = targetRow.closest('.cdk-virtual-scroll-viewport, [class*=\"datagrid-body\"], [class*=\"grid-viewport\"], [class*=\"scroll-container\"]');",
            "        if (!scrollerContainer || scrollerContainer.scrollWidth <= scrollerContainer.clientWidth) {",
            "            let p = targetRow.parentElement;",
            "            while (p && p !== document.body) {",
            "                if (p.scrollWidth > p.clientWidth + 10) { scrollerContainer = p; break; }",
            "                p = p.parentElement;",
            "            }",
            "        }",
            "        let scrollerContainerRect = scrollerContainer ? scrollerContainer.getBoundingClientRect() : { left: 0, width: 800 };",
            "        let maxScrollLeft = scrollerContainer ? scrollerContainer.scrollWidth - scrollerContainer.clientWidth : 0;",
            "        const doScroll = (el, pos) => {",
            "            el.scrollLeft = pos;",
            "            if (el.scrollTo) el.scrollTo({ left: pos, behavior: 'instant' });",
            "            el.dispatchEvent(new Event('scroll', { bubbles: true }));",
            "            window.dispatchEvent(new Event('scroll'));",
            "        };",
            "        // ── COLLECT ALL COLUMN HEADERS (sticky — always in DOM) ──",
            "        let allGridHeaders = Array.from(document.querySelectorAll('th, [role=\"columnheader\"], [class*=\"header-cell\"], .dlx-datagrid-header-cell'));",
            "        allGridHeaders.filter(h => h.getBoundingClientRect().width > 0).forEach(h => { let t = getHeaderText(h); if (t) allDetectedHeaders.add(t); });",
            "        results.push('[*] Scroll container: ' + (scrollerContainer ? 'found (maxScroll=' + maxScrollLeft + 'px)' : 'none'));",
            "        // ── KEY-CENTRIC HELPER: scroll a column into view, return its data cell ──",
            "        const scrollToColumn = async (headerEl) => {",
            "            // Headers are NOT sticky — they scroll with content. Add currentScrollLeft to get the",
            "            // header's original (scroll=0) position, which is what naturalOffset must be based on.",
            "            let hLeft = headerEl.getBoundingClientRect().left + (scrollerContainer ? scrollerContainer.scrollLeft : 0);",
            "            if (!scrollerContainer) {",
            "                return getVisibleCells().find(c => Math.abs(c.getBoundingClientRect().left - hLeft) < 30) || null;",
            "            }",
            "            // Inner helper: scroll to bottom vertically, find fresh row, center it",
            "            const reAcquireRow = async () => {",
            "                results.push('[*] Re-acquiring target row (vertically scrolling to bottom)...');",
            "                doScroll(scrollerContainer, 0);",
            "                let vertCandidates = [];",
            "                for (let el of Array.from(document.querySelectorAll('.cdk-virtual-scroll-viewport, [class*=\"datagrid\"], [class*=\"grid-body\"], [class*=\"scroll\"], [class*=\"table\"]'))) {",
            "                    if (el.scrollHeight > el.clientHeight + 5) vertCandidates.push(el);",
            "                }",
            "                let vsP = scrollerContainer ? scrollerContainer.parentElement : null;",
            "                while (vsP && vsP !== document.body) {",
            "                    if (vsP.scrollHeight > vsP.clientHeight + 5 && !vertCandidates.includes(vsP)) vertCandidates.push(vsP);",
            "                    vsP = vsP.parentElement;",
            "                }",
            "                results.push('[*] Vert containers found: ' + vertCandidates.map(vc => vc.tagName + '(scrollTop=' + vc.scrollTop + ',scrollH=' + vc.scrollHeight + ')').join(', '));",
            "                for (let vc of vertCandidates) { vc.scrollTop = vc.scrollHeight; }",
            "                window.scrollTo(0, document.body.scrollHeight);",
            "                await new Promise(r => setTimeout(r, 1000));",
            "                for (let vc of vertCandidates) results.push('[*] After vertScroll: ' + vc.tagName + ' scrollTop=' + vc.scrollTop);",
            "                let freshRow = Array.from(document.querySelectorAll('tr, [role=\"row\"], [role=\"listitem\"]'))",
            "                    .find(r => r.textContent.toLowerCase().includes(target));",
            "                if (freshRow) {",
            "                    targetRow = freshRow;",
            "                    freshRow.scrollIntoView({ behavior: 'instant', block: 'center' });",
            "                    await new Promise(r => setTimeout(r, 300));",
            "                    let fr = freshRow.getBoundingClientRect();",
            "                    // Layout reflows after vertical scroll (scrollbar appearing shifts positions).",
            "                    // Recompute scrollerContainerRect so all subsequent naturalOffset calculations are accurate.",
            "                    let prevLeft = scrollerContainerRect.left;",
            "                    scrollerContainerRect = scrollerContainer.getBoundingClientRect();",
            "                    results.push('[*] Row re-acquired and centered: rect={top:' + Math.round(fr.top) + ',h:' + Math.round(fr.height) + '} | scrollerContainer.left: ' + Math.round(prevLeft) + ' → ' + Math.round(scrollerContainerRect.left));",
            "                    // ONE-TIME DIAGNOSTIC: dump all cells in the row at scroll=0 to understand the DOM structure",
            "                    doScroll(scrollerContainer, 0);",
            "                    await new Promise(r => setTimeout(r, 500));",
            "                    let diagCells = Array.from(freshRow.querySelectorAll('*')).filter(c => c.getBoundingClientRect().width > 0 && c.getBoundingClientRect().height > 0 && !c.querySelector('[role=\"gridcell\"], td, th'));",
            "                    let seen = new Set();",
            "                    diagCells = diagCells.filter(c => { let k = Math.round(c.getBoundingClientRect().left); if (seen.has(k)) return false; seen.add(k); return true; });",
            "                    results.push('[DIAG] Row cells at scroll=0 (one per unique left):');",
            "                    diagCells.slice(0, 30).forEach(c => {",
            "                        let r = c.getBoundingClientRect();",
            "                        let cls = (typeof c.className === 'string' ? c.className : (c.getAttribute && c.getAttribute('class') || '')).split(' ').slice(0,2).join('.');",
            "                        let txt = (c.innerText||c.textContent||'').trim().replace(/\\s+/g,' ').substring(0,25);",
            "                        let inSc = scrollerContainer.contains(c);",
            "                        results.push('[DIAG]  ' + c.tagName + '.' + cls + ' left=' + Math.round(r.left) + ' w=' + Math.round(r.width) + ' inScroller=' + inSc + ' text=\"' + txt + '\"');",
            "                    });",
            "                    results.push('[DIAG] Header positions:');",
            "                    allGridHeaders.filter(h => h.getBoundingClientRect().width > 0).forEach(h => {",
            "                        let r = h.getBoundingClientRect();",
            "                        results.push('[DIAG]  header \"' + getHeaderText(h).substring(0,20) + '\" left=' + Math.round(r.left) + ' w=' + Math.round(r.width));",
            "                    });",
            "                } else {",
            "                    results.push('[!] Row still not found after vertical scroll');",
            "                }",
            "            };",
            "            // Pre-scroll: re-acquire if already detached",
            "            if (!document.body.contains(targetRow)) await reAcquireRow();",
            "            let naturalOffset = hLeft - scrollerContainerRect.left;",
            "            let targetPos = Math.max(0, Math.min(Math.round(naturalOffset - scrollerContainer.clientWidth / 2), maxScrollLeft));",
            "            doScroll(scrollerContainer, targetPos);",
            "            await new Promise(r => setTimeout(r, 700));",
            "            let cells = getVisibleCells();",
            "            // Post-scroll: re-acquire if row became detached DURING the wait (Angular re-render)",
            "            if (cells.length === 0 && !document.body.contains(targetRow)) {",
            "                await reAcquireRow();",
            "                doScroll(scrollerContainer, targetPos);",
            "                await new Promise(r => setTimeout(r, 700));",
            "                cells = getVisibleCells();",
            "            }",
            "            let actualScroll = scrollerContainer.scrollLeft;",
            "            let expectedLeft = scrollerContainerRect.left + naturalOffset - actualScroll;",
            "            let rowY = targetRow.getBoundingClientRect().top + targetRow.getBoundingClientRect().height / 2;",
            "            // PRIMARY: elementFromPoint — finds the actual rendered element regardless of CSS class.",
            "            // This handles frozen/sticky cells that don't match our querySelectorAll selectors.",
            "            let efpCell = null;",
            "            {",
            "                let el = document.elementFromPoint(expectedLeft, rowY);",
            "                let walker = el;",
            "                while (walker && walker !== document.body) {",
            "                    if (walker.matches && walker.matches('td, th, [role=\"gridcell\"], [class*=\"cell\"]') && targetRow.contains(walker)) { efpCell = walker; break; }",
            "                    walker = walker.parentElement;",
            "                }",
            "                if (!efpCell && el && targetRow.contains(el)) efpCell = el;",
            "                let efpTxt = efpCell ? (efpCell.innerText || efpCell.textContent || '').trim().replace(/\\s+/g, ' ').substring(0, 30) : 'null';",
            "                results.push('[*] scrollToColumn: ' + getHeaderText(headerEl) + ' targetPos=' + targetPos + ' actualScroll=' + Math.round(actualScroll) + ' expectedX=' + Math.round(expectedLeft) + ' rowY=' + Math.round(rowY) + ' | EFP: ' + (el ? el.tagName + '.' + (el.className||'').split(' ')[0] : 'null') + ' inRow=' + !!efpCell + ' text=\"' + efpTxt + '\"');",
            "            }",
            "            // FALLBACK: position matching among querySelectorAll cells",
            "            let best = null, bestDist = Infinity;",
            "            for (let c of cells) { let d = Math.abs(c.getBoundingClientRect().left - expectedLeft); if (d < bestDist) { bestDist = d; best = c; } }",
            "            if (best && bestDist < 50) {",
            "                let bestTxt = (best.innerText || best.textContent || '').trim().replace(/\\s+/g, ' ').substring(0, 30);",
            "                results.push('[*]   POS fallback: bestDist=' + Math.round(bestDist) + ' text=\"' + bestTxt + '\"');",
            "            }",
            "            // Use EFP result if valid, otherwise pos-match if close enough",
            "            let finalCell = efpCell || (bestDist < scrollerContainer.clientWidth / 2 + 50 ? best : null);",
            "            return finalCell;",
            "        };",
            "        // ── UPDATE REVISION ──",
            "        if (incrementValue > 0) {",
            "            let revisionHeader = allGridHeaders.find(h => { let t = getHeaderText(h).toLowerCase().trim(); return (t.includes('revision') || t.startsWith('rev')) && !t.includes('date'); });",
            "            if (revisionHeader) {",
            "                let revCell = await scrollToColumn(revisionHeader);",
            "                if (revCell) {",
            "                    let existingInput = revCell.querySelector('input:not([type=\"checkbox\"]), select, textarea');",
            "                    let cellValue = existingInput ? (existingInput.value || '').trim() : (revCell.innerText || revCell.textContent || '').trim();",
            "                    if (cellValue.toLowerCase() === 'select date') cellValue = '';",
            "                    if (cellValue) {",
            "                        let numericStr = cellValue.replace(/[^0-9.]/g, '');",
            "                        if (numericStr) {",
            "                            let floatVal = parseFloat(numericStr);",
            "                            let calculatedVal = floatVal + incrementValue;",
            "                            let finalStringVal = calculatedVal.toFixed(2).toString();",
            "                            let newVal = cellValue.replace(numericStr, finalStringVal);",
            "                            if (!existingInput || existingInput.getBoundingClientRect().width === 0) {",
            "                                revCell.scrollIntoView({ behavior: 'instant', block: 'center', inline: 'center' });",
            "                                revCell.click();",
            "                                await new Promise(r => setTimeout(r, 300));",
            "                                existingInput = revCell.querySelector('input:not([type=\"checkbox\"]), textarea') || document.activeElement;",
            "                            }",
            "                            if (existingInput && (existingInput.tagName === 'INPUT' || existingInput.tagName === 'TEXTAREA')) {",
            "                                await typeText(existingInput, newVal);",
            "                                revisionUpdated = true; revisionOldVal = numericStr; revisionNewVal = finalStringVal;",
            "                            } else { results.push('[!] Revision input not accessible after click'); }",
            "                        }",
            "                    }",
            "                } else { results.push('[!] Revision cell not visible after scroll'); }",
            "            } else { results.push('[!] Revision header not found in grid'); }",
            "        }",
            "        // ── UPDATE COLUMN FIELDS (key-centric) ──",
            "        for (let key of Object.keys(columnConfig)) {",
            "            let matchingHeader = allGridHeaders.find(h => { let t = getHeaderText(h).toLowerCase().trim(); return t === key.toLowerCase() || t.includes(key.toLowerCase()); });",
            "            if (!matchingHeader) { results.push('[!] Header not found for: ' + key); continue; }",
            "            let cell = await scrollToColumn(matchingHeader);",
            "            if (!cell) { results.push('[!] Cell not found in viewport for: ' + key); continue; }",
            "            let updated = await updateCell(cell, columnConfig[key], key);",
            "            if (updated) columnsUpdated[key] = true;",
            "        }",
            "        if (scrollerContainer) doScroll(scrollerContainer, 0);",
            "        results.push('[+] Step 2 completed: Metadata analysis & updates done');",
            "",
            "        // --- STEP 3: ACTION BUTTON ---",
            "        if (actionButtonText) {",
            "            results.push('\\n--- STEP 3: ACTION BUTTON ---');",
            "            results.push('[*] Locating action button: ' + actionButtonText);",
            "            let actionBtn = null;",
            "            let allButtons = Array.from(document.querySelectorAll('button, [role=\"button\"], input[type=\"button\"], input[type=\"submit\"], a[class*=\"button\"]'));",
            "            for (let btn of allButtons) {",
            "                let btnText = (btn.innerText || btn.textContent || btn.getAttribute('aria-label') || btn.getAttribute('title') || '').trim().toLowerCase();",
            "                if (btnText.includes(actionButtonText.toLowerCase())) {",
            "                    actionBtn = btn;",
            "                    break;",
            "                }",
            "            }",
            "            if (actionBtn) {",
            "                results.push('[+] Found action button, clicking...');",
            "                actionBtn.scrollIntoView({behavior: 'smooth', block: 'center', inline: 'center'});",
            "                await new Promise(r => setTimeout(r, 300));",
            "                actionBtn.focus();",
            "                await new Promise(r => setTimeout(r, 100));",
            "                actionBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));",
            "                actionBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));",
            "                actionBtn.click();",
            "                await new Promise(r => setTimeout(r, 1000));",
            "                results.push('[+] Action button clicked');",
            "                if (actionButtonText.toLowerCase().includes('upload')) {",
            "                    results.push('[*] Upload initiated. Waiting for completion...');",
            "                    const waitForDoneButton = async () => {",
            "                        const maxWait = 12 * 60 * 60 * 1000;",
            "                        const checkInterval = 5000;",
            "                        const startTime = Date.now();",
            "                        while (Date.now() - startTime < maxWait) {",
            "                            let allElems = Array.from(document.querySelectorAll('button, [role=\"button\"], input[type=\"button\"], a[class*=\"button\"]'));",
            "                            let doneBtn = allElems.find(el => {",
            "                                let text = (el.innerText || el.textContent || el.getAttribute('aria-label') || el.getAttribute('title') || '').trim().toLowerCase();",
            "                                return text === 'done' || text === 'ok' || text.includes('done') && text.length < 20;",
            "                            });",
            "                            if (doneBtn) {",
            "                                results.push('[+] Done button found after ' + Math.round((Date.now() - startTime) / 1000) + 's');",
            "                                doneBtn.scrollIntoView({behavior: 'smooth', block: 'center', inline: 'center'});",
            "                                await new Promise(r => setTimeout(r, 300));",
            "                                doneBtn.focus();",
            "                                await new Promise(r => setTimeout(r, 100));",
            "                                doneBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));",
            "                                doneBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));",
            "                                doneBtn.click();",
            "                                results.push('[+] Done button clicked - Upload complete');",
            "                                return true;",
            "                            }",
            "                            await new Promise(r => setTimeout(r, checkInterval));",
            "                        }",
            "                        results.push('[!] Timeout: Done button not found within 12 hours');",
            "                        return false;",
            "                    };",
            "                    await waitForDoneButton();",
            "                }",
            "            } else {",
            "                results.push('[!] Action button not found: ' + actionButtonText);",
            "            }",
            "        }",
            "        // === FINAL SUMMARY ===",
            "        results.push('\\n=== AUTOMATION SUMMARY ===');",
            "        results.push('File : ' + target);",
            "        results.push('Headers detected across all scroll positions: ' + Array.from(allDetectedHeaders).join(' | '));",
            "        results.push('');",
            "        results.push('Metadata updates:');",
            "        if (incrementValue > 0) {",
            "            if (revisionUpdated)",
            "                results.push('  [+] Revision          : ' + revisionOldVal + ' → ' + revisionNewVal);",
            "            else",
            "                results.push('  [!] Revision          : NOT updated (column not found or no value)');",
            "        }",
            "        for (let key of Object.keys(columnConfig)) {",
            "            if (columnsUpdated[key])",
            "                results.push('  [+] ' + key + ' : ' + columnConfig[key]);",
            "            else",
            "                results.push('  [!] ' + key + ' : NOT updated (column not found)');",
            "        }",
            "        results.push('');",
            "        results.push('Steps completed:');",
            "        results.push('  [+] Popup loaded & file found');",
            "        results.push('  [+] Metadata updated');",
            "        if (actionButtonText)",
            "            results.push('  [+] Action button clicked: ' + actionButtonText);",
            "        results.push('\\n[+] Automation completed successfully.');",
            "",
            "        return results.join('\\n');",
            "    } catch (error) {",
            "        results.push('\\n[!] FATAL ERROR: ' + error.message);",
            "        return results.join('\\n');",
            "    }",
            "})();"
        };

        return string.Join("\n", lines);
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

    public string GetAuditLogAsString()
    {
        return string.Join("\n", _auditLog);
    }

    public void Dispose()
    {
        _cdpClient?.Dispose();
    }
}
