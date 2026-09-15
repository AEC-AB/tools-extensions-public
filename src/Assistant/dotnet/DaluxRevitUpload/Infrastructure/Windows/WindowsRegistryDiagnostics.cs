namespace DaluxRevitUpload;

internal static class WindowsRegistryDiagnostics
{
    public static string? ReadWebView2RuntimeVersion()
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

    /// <summary>
    /// HKCU\Environment holds user-scope env vars. The last-write time of the key
    /// is the best proxy available for determining whether Revit started before
    /// WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS was updated.
    /// </summary>
    public static DateTime? ReadUserEnvironmentLastWriteTime()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment");
            if (key == null) return null;

            const uint KEY_QUERY_VALUE = 0x0001;
            IntPtr hKey = IntPtr.Zero;
            try
            {
                int rc = RegOpenKeyExW(
                    new IntPtr(unchecked((int)0x80000001)),
                    "Environment",
                    0,
                    KEY_QUERY_VALUE,
                    out hKey);
                if (rc != 0) return null;

                var ftLastWrite = new System.Runtime.InteropServices.ComTypes.FILETIME();
                rc = RegQueryInfoKeyW(
                    hKey,
                    null,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    ref ftLastWrite);
                if (rc != 0) return null;

                long fileTime = ((long)ftLastWrite.dwHighDateTime << 32) | (uint)ftLastWrite.dwLowDateTime;
                return DateTime.FromFileTime(fileTime);
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
}
