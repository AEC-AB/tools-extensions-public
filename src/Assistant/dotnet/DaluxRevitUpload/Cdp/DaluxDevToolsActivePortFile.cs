namespace DaluxRevitUpload;

internal static class DaluxDevToolsActivePortFile
{
    private static string Path =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DaluxWebView2", "Default", "EBWebView", "DevToolsActivePort");

    public static int? TryReadPort()
    {
        try
        {
            if (!File.Exists(Path)) return null;

            // FileShare.ReadWrite lets us read even while Chromium is actively writing.
            using var stream = new System.IO.FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
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

    public static void LogState(Action<string> log)
    {
        try
        {
            if (File.Exists(Path))
            {
                var lastWrite = File.GetLastWriteTime(Path);
                log($"[*] DevToolsActivePort file present: {Path}");
                log($"    last-write = {lastWrite:s}");
                LogContents(log);
                return;
            }

            log($"[*] DevToolsActivePort file NOT present at {Path}");
            var parentDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DaluxWebView2");
            if (Directory.Exists(parentDir))
                log($"    parent dir exists: {parentDir}");
            else
                log($"    parent dir DOES NOT exist: {parentDir} — Dalux WebView2 may not have spawned at all");
        }
        catch (Exception ex)
        {
            log($"[!] DevToolsActivePort probe failed: {ex.Message}");
        }
    }

    private static void LogContents(Action<string> log)
    {
        try
        {
            using var stream = new System.IO.FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var line1 = reader.ReadLine();
            var line2 = reader.ReadLine();
            log($"    line 1 (port)   = \"{line1}\"");
            log($"    line 2 (target) = \"{line2}\"");
        }
        catch (Exception ex)
        {
            log($"    could not read contents: {ex.Message}");
        }
    }
}
