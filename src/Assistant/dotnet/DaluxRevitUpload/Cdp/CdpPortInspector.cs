namespace DaluxRevitUpload;

internal static class CdpPortInspector
{
    private static readonly HashSet<string> SystemCriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "svchost", "csrss", "winlogon", "lsass",
        "services", "smss", "wininit", "dwm", "explorer"
    };

    public static CdpPortState ProbePortState(int port)
    {
        System.Net.Sockets.TcpListener? listener = null;
        try
        {
            listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            return CdpPortState.Free;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return CdpPortState.InUseByOther;
        }
        catch
        {
            return CdpPortState.ProbeError;
        }
        finally
        {
            try { listener?.Stop(); } catch { }
        }
    }

    public static uint? FindPortOwnerPid(int port)
    {
        foreach (var entry in EnumerateListeningPorts())
        {
            if (entry.Port == port) return (uint)entry.Pid;
        }
        return null;
    }

    public static CdpPortOwnerKind ClassifyPortOwner(uint ownerPid, Process? ownerProc, int revitPid)
    {
        if (ownerPid == 0 || ownerPid == 4)
            return CdpPortOwnerKind.SystemCritical;

        if ((int)ownerPid == revitPid)
            return CdpPortOwnerKind.Expected;

        if (ownerProc != null && IsDescendantOf(ownerProc.Id, revitPid))
            return CdpPortOwnerKind.Expected;

        if (ownerProc != null && SystemCriticalProcessNames.Contains(ownerProc.ProcessName))
            return CdpPortOwnerKind.SystemCritical;

        return CdpPortOwnerKind.Foreign;
    }

    public static List<WebViewCdpPort> FindWebViewCdpPorts(int revitPid)
    {
        var result = new List<WebViewCdpPort>();
        var seen = new HashSet<int>();

        foreach (var entry in EnumerateListeningPorts())
        {
            if (seen.Contains(entry.Port)) continue;

            try
            {
                using var proc = Process.GetProcessById(entry.Pid);
                if (!string.Equals(proc.ProcessName, "msedgewebview2", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool descendant = IsDescendantOf(entry.Pid, revitPid);
                result.Add(new WebViewCdpPort(entry.Port, entry.Pid, descendant));
                seen.Add(entry.Port);
            }
            catch
            {
                // Process exited between enumeration and lookup; skip.
            }
        }

        return result;
    }

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
}
