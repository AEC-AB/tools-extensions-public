using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;

namespace StreamBIMUploader;

internal sealed class StreamBimUploadDiagnostics
{
    private const string LogDirectoryName = "StreamBIMUploader";
    private readonly bool isEnabled;
    private readonly object syncRoot = new();

    private StreamBimUploadDiagnostics(bool isEnabled, string? logPath)
    {
        this.isEnabled = isEnabled;
        LogPath = logPath;
    }

    internal string? LogPath { get; }

    internal static StreamBimUploadDiagnostics Create(bool isEnabled)
    {
        if (!isEnabled)
        {
            return new StreamBimUploadDiagnostics(false, null);
        }

        var directory = Path.Combine(Path.GetTempPath(), LogDirectoryName);
        Directory.CreateDirectory(directory);

        var fileName = $"upload-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.log";
        var diagnostics = new StreamBimUploadDiagnostics(true, Path.Combine(directory, fileName));
        diagnostics.Log("Upload run started.");
        return diagnostics;
    }

    internal void Log(string message)
    {
        if (!isEnabled || LogPath is null)
        {
            return;
        }

        var entry = $"{DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}";
        try
        {
            lock (syncRoot)
            {
                File.AppendAllText(LogPath, entry);
            }
        }
        catch (IOException exception)
        {
            Trace.TraceWarning("[StreamBIMUploader] Unable to write diagnostics: {0}", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning("[StreamBIMUploader] Unable to write diagnostics: {0}", exception.Message);
        }
        catch (SecurityException exception)
        {
            Trace.TraceWarning("[StreamBIMUploader] Unable to write diagnostics: {0}", exception.Message);
        }
    }
}
