namespace DaluxRevitUpload;

internal sealed class DaluxRemoteDebuggingEnvironment
{
    private readonly DaluxAutomationConfig _config;
    private readonly Action<string> _log;

    public DaluxRemoteDebuggingEnvironment(DaluxAutomationConfig config, Action<string> log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public bool EnsureRemoteDebuggingEnvVar()
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
                _log($"[*] Env var contained --remote-debugging-port={currentPort}. Rewrote to =0 for per-UDF ephemeral ports.");
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
            _log($"[*] WebView2 remote debugging persisted to user environment ({mode})");
        }
        else
        {
            desiredValue = existingValue;
            var configuredMode = _config.DebuggingPort == 0 ? "port=0, per-UDF ephemeral" : $"port={_config.DebuggingPort}";
            _log($"[*] WebView2 remote debugging already in user environment ({configuredMode})");
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
            _log("[!] Could not patch the running Revit process's environment block.");
            _log("[!] Close Revit COMPLETELY, relaunch it, then re-run this extension.");
            _log("[!] The current Revit session will not see the new setting — aborting this run.");
            return false;
        }

        _log("[!] Process injection failed but user env was already correct — continuing on the");
        _log("    assumption that Revit inherited the flag at launch.");
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
            _log("[!] Env-var injection requires a 64-bit Assistant process. Skipping.");
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
            _log($"[!] OpenProcess(Revit PID {revitPid}) failed with Win32 error {err}.");
            if (err == 5) // ERROR_ACCESS_DENIED
                _log("    Likely cause: Revit is running elevated and Assistant is not (or vice-versa).");
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
                _log("[!] GetModuleHandle(kernel32.dll) returned null. Cannot inject.");
                return false;
            }
            IntPtr pSetEnv = GetProcAddress(hKernel32, "SetEnvironmentVariableW");
            if (pSetEnv == IntPtr.Zero)
            {
                _log("[!] GetProcAddress(SetEnvironmentVariableW) failed. Cannot inject.");
                return false;
            }

            byte[] nameBytes  = System.Text.Encoding.Unicode.GetBytes(name  + "\0");
            byte[] valueBytes = System.Text.Encoding.Unicode.GetBytes(value + "\0");
            uint dataSize = (uint)(nameBytes.Length + valueBytes.Length);

            pData = VirtualAllocEx(hProc, IntPtr.Zero, dataSize, MEM_COMMIT_RESERVE, PAGE_READWRITE);
            if (pData == IntPtr.Zero)
            {
                _log($"[!] VirtualAllocEx (data) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            IntPtr pName  = pData;
            IntPtr pValue = new IntPtr(pData.ToInt64() + nameBytes.Length);

            if (!WriteProcessMemory(hProc, pName,  nameBytes,  (uint)nameBytes.Length,  out _) ||
                !WriteProcessMemory(hProc, pValue, valueBytes, (uint)valueBytes.Length, out _))
            {
                _log($"[!] WriteProcessMemory (data) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
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
                _log($"[!] VirtualAllocEx (code) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            if (!WriteProcessMemory(hProc, pCode, shellcode, (uint)shellcode.Length, out _))
            {
                _log($"[!] WriteProcessMemory (code) failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }

            // Tighten permissions on the code page before executing — some EDR products
            // flag RWX pages but tolerate RX.
            VirtualProtectEx(hProc, pCode, (uint)shellcode.Length, PAGE_EXECUTE_READ, out _);

            hThread = CreateRemoteThread(hProc, IntPtr.Zero, 0, pCode, IntPtr.Zero, 0, out _);
            if (hThread == IntPtr.Zero)
            {
                var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                _log($"[!] CreateRemoteThread failed: {err}. Likely AV/EDR blocking remote-thread creation.");
                return false;
            }

            var wait = WaitForSingleObject(hThread, 5000);
            if (wait != WAIT_OBJECT_0)
            {
                _log($"[!] Remote thread did not finish within 5s (WaitForSingleObject={wait}).");
                return false;
            }

            if (!GetExitCodeThread(hThread, out uint exitCode))
            {
                _log($"[!] GetExitCodeThread failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return false;
            }
            if (exitCode == 0)
            {
                _log("[!] SetEnvironmentVariableW returned FALSE inside Revit.");
                return false;
            }

            _log($"[+] Patched Revit (PID {revitPid}) env block — next WebView2 spawn will inherit the flag.");
            return true;
        }
        catch (Exception ex)
        {
            _log($"[!] Env-var injection threw: {ex.GetType().Name}: {ex.Message}");
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

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}

