# SEC-011: StreamBIM diagnostics logging of file paths and FTP operations

| | |
|---|---|
| Severity | Low - File paths and FTP operation details written to a local temp directory log file. Information disclosure with limited scope: local file on the running machine, opt-in via a verbose flag, no credentials or personal data in logs. |
| Status | Open |
| Category | Logging |
| Location | `src/Assistant/dotnet/StreamBIMUploader/Services/StreamBimUploadDiagnostics.cs` (all), `StreamBimUploadService.cs` (lines 25, 30, 68, 71, 98), `StreamBimFileTransferService.cs` (lines 29, 33, 66-67, 76, 167, 188, 202, 231, 238-240, 246, 263, 303, 308, 312, 331, 336, 344, 348) |
| First seen | run 20260926-215844 at commit 4658b6b |
| Last verified | run 20260926-215844 |
| Introduced | Unknown - this is a diagnostics/logging framework, likely present since inception. |

## What

The `StreamBimUploadDiagnostics` class writes verbose diagnostic messages to a log file in the system temp directory (`%TEMP%\StreamBIMUploader\upload-<timestamp>.log`) whenever the `VerboseDiagnostics` flag is set. The log captures detailed file paths (full local paths and full remote FTP paths), FTP operation details (connection mode, upload status, directory listing results), exception messages from FluentFTP, and file sizes. The log file path itself is returned to the caller via the upload result's `Diagnostics` list. No credentials, API keys, or personal data are written to the logs.

## Evidence

**Diagnostics class** (`src/Assistant/dotnet/StreamBIMUploader/Services/StreamBimUploadDiagnostics.cs`):

- Line 29: `var directory = Path.Combine(Path.GetTempPath(), LogDirectoryName);` - logs written to temp directory
- Line 32: `var fileName = $"upload-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.log";` - timestamped log file
- Line 45: `var entry = $"{DateTimeOffset.UtcNow:ToString("O", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}";` - all messages logged verbatim
- Line 50: `File.AppendAllText(LogPath, entry);` - no rotation, no access control

**Upload service calls** (`src/Assistant/dotnet/StreamBIMUploader/Services/StreamBimUploadService.cs`):

- Line 25: `diagnostics.Log($"Using FTP data connection mode {client.Config.DataConnectionType}.");`
- Line 30: `diagnostics.Log($"Selected file: '{localFilePath}'.");`
- Line 68: `diagnostics.Log($"Preparing upload: local='{localFilePath}', remote-relative='{remoteRelativePath}', attempt={attempt + 1}.");`
- Line 98: `diagnostics.Log($"Transient FTP failure for '{localFilePath}': {StreamBimExceptionHelper.GetInnermostMessage(exception)}. Retrying.");`

**File transfer service calls** (`src/Assistant/dotnet/StreamBIMUploader/Services/StreamBimFileTransferService.cs`):

- Line 29: `diagnostics.Log($"Resolved remote path: '{remotePath}'.");`
- Line 67: logs `remoteDirectory` and `remoteFileName`
- Line 76: `diagnostics.Log($"Upload completed with status {uploadStatus}: '{remotePath}'.");`
- Line 167: `diagnostics.Log($"Starting FTP upload attempt {attempt + 1}: '{localPath}' -> '{remotePath}'.");`
- Line 308: `diagnostics.Log($"Object lookup verified uploaded file: '{remotePath}', size={remoteInfo.Size} bytes.");`
- Line 348: `diagnostics.Log($"Remote directory listing verified file: name='{entry.Name}', size={entry.Size} bytes, path='{entry.FullName}'.");`

**Exception helper** (`src/Assistant/dotnet/StreamBim/Services/StreamBimExceptionHelper.cs`):

- Line 21-23: `GetInnermostMessage` returns `exception.InnerException.Message` - raw FTP exception messages from the server are logged verbatim.

## Impact

Anyone with file system access to the machine running the uploader can read the log files and learn:
- Full directory structure and file names being uploaded (including project paths, customer file names, folder hierarchies)
- FTP remote paths and folder structure on the StreamBIM server
- FTP error messages that could reveal server software, version, or internal configuration details
- File sizes which could leak information about data volume
- Upload timing patterns

The log file is placed in the system temp directory with no file-level access control beyond the standard temp directory permissions. Log files are never rotated or cleaned up.

Because this is opt-in via `args.VerboseDiagnostics`, a default setting that enables it would increase the impact significantly.

## What a fix involves

1. **Restrict what is logged**: Replace full file paths with relative paths or anonymized names where the path reveals project structure. Log file counts or hashes instead of full paths and names.
2. **Filter exception messages**: Do not log raw FTP exception messages. Log a canned message like "FTP operation failed: {error category}" instead of the server-returned message that could leak infrastructure details.
3. **Log rotation and cleanup**: Implement log file rotation (max age, max size) and cleanup on upload completion or at application exit.
4. **Consider a standard logging framework**: Replace direct file appends with a structured logging library that supports log levels, rotation, and redaction out of the box.
5. **Review default value**: Ensure `VerboseDiagnostics` defaults to `false` in production configurations so verbose logging is only enabled when needed for troubleshooting.

## References

- SEC-001: StreamBIM credential storage and FTP transfer security (same subsystem)
- SEC-002: Dalux API key handling (similar API service area)
