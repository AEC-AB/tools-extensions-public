using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMUploader;

internal static class StreamBimFileTransferService
{
    private static readonly TimeSpan DirectoryCreationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UploadAttemptTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CancellationCleanupTimeout = TimeSpan.FromSeconds(10);
    private const int RemoteDirectoryOperationAttempts = 3;

    internal static async Task<StreamBimSingleFileUploadResult> UploadFileAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string localFilePath,
        string remoteRelativePath,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            var remotePath = StreamBimPathHelper.CreateRemotePath(projectPath, args.TargetFolder, remoteRelativePath);
            diagnostics.Log($"Resolved remote path: '{remotePath}'.");

            if (StreamBimPathHelper.ContainsIgnoredFolder(remotePath))
            {
                diagnostics.Log($"Skipped ignored folder path: '{remotePath}'.");
                return StreamBimSingleFileUploadResult.Skipped(remotePath.TrimStart('/'));
            }

            var directoryFailure = await ValidateRemoteDirectoryAsync(
                client,
                remotePath,
                diagnostics,
                cancellationToken);
            if (directoryFailure is not null)
            {
                return StreamBimSingleFileUploadResult.Failed(remotePath.TrimStart('/'), directoryFailure);
            }

            var remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(remoteDirectory))
            {
                return StreamBimSingleFileUploadResult.Failed(
                    remotePath.TrimStart('/'),
                    $"Could not determine the remote directory for '{remotePath}'.");
            }

            var workingDirectoryFailure = await SetRemoteWorkingDirectoryAsync(
                client,
                remoteDirectory,
                diagnostics,
                cancellationToken);
            if (workingDirectoryFailure is not null)
            {
                return StreamBimSingleFileUploadResult.Failed(remotePath.TrimStart('/'), workingDirectoryFailure);
            }

            var remoteFileName = StreamBimPathHelper.GetLeafName(remotePath);
            diagnostics.Log(
                $"Starting upload without a remote file existence check in '{remoteDirectory}': '{remoteFileName}'. Using {FtpRemoteExists.NoCheck}.");

            var uploadStatus = await UploadFileWithRetriesAsync(
                client,
                localFilePath,
                remoteFileName,
                FtpRemoteExists.NoCheck,
                diagnostics,
                cancellationToken);
            diagnostics.Log($"Upload completed with status {uploadStatus}: '{remotePath}'.");
            if (uploadStatus == FtpStatus.Success)
            {
                var isVerified = await VerifyUploadedFileAsync(client, remotePath, diagnostics, cancellationToken);
                return isVerified
                    ? StreamBimSingleFileUploadResult.Uploaded(remotePath.TrimStart('/'))
                    : StreamBimSingleFileUploadResult.Failed(
                        remotePath.TrimStart('/'),
                        "StreamBIM reported a successful upload but the file could not be verified afterward.");
            }

            return uploadStatus == FtpStatus.Skipped
                ? StreamBimSingleFileUploadResult.Skipped(remotePath.TrimStart('/'))
                : StreamBimSingleFileUploadResult.Failed(remotePath.TrimStart('/'), "Failed to upload.");
        }
        catch (StreamBimFtpRecoveryException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (InvalidOperationException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (FtpException exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            throw;
        }
        catch (FtpException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (IOException exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            throw;
        }
        catch (IOException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (SocketException exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            throw;
        }
        catch (SocketException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (TimeoutException exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            throw;
        }
        catch (TimeoutException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (AuthenticationException exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            throw;
        }
        catch (AuthenticationException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
    }

    private static async Task<FtpStatus> UploadFileWithRetriesAsync(
        AsyncFtpClient client,
        string localPath,
        string remotePath,
        FtpRemoteExists remoteExistsMode,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                diagnostics.Log($"Starting FTP upload attempt {attempt + 1}: '{localPath}' -> '{remotePath}'.");
                using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var uploadTask = client.UploadFile(
                    localPath,
                    remotePath,
                    remoteExistsMode,
                    createRemoteDir: false,
                    token: timeoutCancellationTokenSource.Token);
                FtpStatus uploadStatus;
                try
                {
                    uploadStatus = await uploadTask.WaitAsync(UploadAttemptTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    timeoutCancellationTokenSource.Cancel();
                    if (!await WaitForCancellationAsync(uploadTask))
                    {
                        throw new StreamBimFtpRecoveryException($"FTP upload timed out and did not stop within {CancellationCleanupTimeout.TotalSeconds:0} seconds: '{remotePath}'.");
                    }

                    diagnostics.Log($"FTP upload timed out after {UploadAttemptTimeout.TotalMinutes:0} minutes: '{remotePath}'.");
                    throw new TimeoutException($"Upload timed out after {UploadAttemptTimeout.TotalMinutes:0} minutes: '{remotePath}'.");
                }

                if (uploadStatus == FtpStatus.Failed)
                {
                    diagnostics.Log($"FTP upload returned Failed: '{remotePath}'.");
                    continue;
                }

                var localModified = File.GetLastWriteTimeUtc(localPath);
                if (localModified != default)
                {
                    try
                    {
                        await client.SetModifiedTime(remotePath, localModified, cancellationToken);
                    }
                    catch
                    {
                        // Best-effort: ignore failures to set remote modified time
                    }
                }

                return uploadStatus;
            }
            catch (StreamBimFtpRecoveryException)
            {
                throw;
            }
            catch (TimeoutException exception)
            {
                diagnostics.Log($"FTP upload timed out: '{remotePath}'. {exception.Message}");
                throw;
            }
            catch (Exception exception) when (attempt < 2 &&
                (exception is UnauthorizedAccessException || StreamBimExceptionHelper.IsTransientFtpFailure(exception)))
            {
                diagnostics.Log(
                    $"FTP upload attempt {attempt + 1} failed for '{remotePath}' with {exception.GetType().Name}: {StreamBimExceptionHelper.GetInnermostMessage(exception)}. Retrying.");
                Trace.TraceWarning(
                    "Retrying upload for '{0}' after attempt {1} failed with {2}: {3}",
                    remotePath,
                    attempt + 1,
                    exception.GetType().Name,
                    StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
        }

        return FtpStatus.Failed;
    }

    private static async Task<string?> ValidateRemoteDirectoryAsync(
        AsyncFtpClient client,
        string remotePath,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(remoteDirectory))
        {
            return null;
        }

        diagnostics.Log($"Validating remote directory: '{remoteDirectory}'.");
        var directorySegments = remoteDirectory.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentDirectory = string.Empty;
        foreach (var directorySegment in directorySegments)
        {
            currentDirectory += "/" + directorySegment;
            if (await IsRemoteDirectoryListedAsync(client, currentDirectory, diagnostics, cancellationToken))
            {
                continue;
            }

            var failure = $"The target folder '{remoteDirectory}' does not exist because '{currentDirectory}' was not found. Create the folder in StreamBIM and try again.";
            diagnostics.Log(failure);
            return failure;
        }

        return null;
    }

    private static async Task<string?> SetRemoteWorkingDirectoryAsync(
        AsyncFtpClient client,
        string remoteDirectory,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < RemoteDirectoryOperationAttempts; attempt++)
        {
            diagnostics.Log($"Changing FTP working directory to: '{remoteDirectory}', attempt {attempt + 1}.");
            using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var setWorkingDirectoryTask = client.SetWorkingDirectory(remoteDirectory, timeoutCancellationTokenSource.Token);
            try
            {
                await setWorkingDirectoryTask.WaitAsync(DirectoryCreationTimeout, cancellationToken);
                diagnostics.Log($"FTP working directory set to: '{remoteDirectory}'.");
                return null;
            }
            catch (TimeoutException)
            {
                timeoutCancellationTokenSource.Cancel();
                if (!await WaitForCancellationAsync(setWorkingDirectoryTask))
                {
                    throw new StreamBimFtpRecoveryException($"Changing to the remote directory timed out and did not stop within {CancellationCleanupTimeout.TotalSeconds:0} seconds: '{remoteDirectory}'.");
                }

                var failure = $"Changing to the remote directory timed out after {DirectoryCreationTimeout.TotalSeconds:0} seconds: '{remoteDirectory}'.";
                diagnostics.Log(failure);
                return failure;
            }
            catch (FtpCommandException exception) when (attempt < RemoteDirectoryOperationAttempts - 1)
            {
                diagnostics.Log(
                    $"Changing to '{remoteDirectory}' was rejected on attempt {attempt + 1}: {StreamBimExceptionHelper.GetInnermostMessage(exception)}. Waiting for StreamBIM to expose the new folder.");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (FtpCommandException exception)
            {
                var failure = $"Changing to the remote directory failed: {StreamBimExceptionHelper.GetInnermostMessage(exception)}";
                diagnostics.Log(failure);
                return failure;
            }
        }

        return $"Changing to the remote directory failed after {RemoteDirectoryOperationAttempts} attempts: '{remoteDirectory}'.";
    }

    private static async Task<bool> IsRemoteDirectoryListedAsync(
        AsyncFtpClient client,
        string remoteDirectory,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var parentDirectory = Path.GetDirectoryName(remoteDirectory)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return false;
        }

        var directoryName = StreamBimPathHelper.GetLeafName(remoteDirectory);
        for (var attempt = 0; attempt < RemoteDirectoryOperationAttempts; attempt++)
        {
            diagnostics.Log($"Listing parent directory to confirm remote folder: '{parentDirectory}', attempt {attempt + 1}.");
            try
            {
                var entries = await client.GetListing(parentDirectory, cancellationToken);
                var isListed = entries.Any(item =>
                    item.Type == FtpObjectType.Directory &&
                    string.Equals(item.Name, directoryName, StringComparison.OrdinalIgnoreCase));
                diagnostics.Log(isListed
                    ? $"Remote directory is present in parent listing: '{remoteDirectory}'."
                    : $"Remote directory is not present in parent listing: '{remoteDirectory}'.");
                return isListed;
            }
            catch (FtpException exception) when (attempt < RemoteDirectoryOperationAttempts - 1)
            {
                diagnostics.Log(
                    $"Parent directory listing failed on attempt {attempt + 1}: {StreamBimExceptionHelper.GetInnermostMessage(exception)}. Reconnecting before retrying.");
                using var reconnectCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var reconnectTask = client.Connect(reConnect: true, token: reconnectCancellationTokenSource.Token);
                try
                {
                    await reconnectTask.WaitAsync(DirectoryCreationTimeout, cancellationToken);
                    diagnostics.Log("FTP reconnect completed before retrying the parent directory listing.");
                }
                catch (TimeoutException)
                {
                    reconnectCancellationTokenSource.Cancel();
                    if (!await WaitForCancellationAsync(reconnectTask))
                    {
                        throw new StreamBimFtpRecoveryException($"Reconnecting to StreamBIM timed out and did not stop within {CancellationCleanupTimeout.TotalSeconds:0} seconds while retrying the parent directory listing.");
                    }

                    var failure = $"Reconnecting to StreamBIM timed out after {DirectoryCreationTimeout.TotalSeconds:0} seconds while retrying the parent directory listing.";
                    diagnostics.Log(failure);
                    throw new TimeoutException(failure);
                }
            }
        }

        return false;
    }

    private static async Task<bool> WaitForCancellationAsync(Task operation)
    {
        var completedTask = await Task.WhenAny(operation, Task.Delay(CancellationCleanupTimeout));
        if (completedTask != operation)
        {
            return false;
        }

        _ = operation.Exception;
        return true;
    }

    private static async Task<bool> VerifyUploadedFileAsync(
        AsyncFtpClient client,
        string remotePath,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Log($"Verifying uploaded file, attempt {attempt + 1}: '{remotePath}'.");

            var remoteInfo = await client.GetObjectInfo(remotePath, token: cancellationToken);
            if (remoteInfo?.Type == FtpObjectType.File)
            {
                diagnostics.Log($"Object lookup verified uploaded file: '{remotePath}', size={remoteInfo.Size} bytes.");
                return await IsFileListedInRemoteDirectoryAsync(client, remotePath, diagnostics, cancellationToken);
            }

            diagnostics.Log($"Verification did not find a file at '{remotePath}'.");
            if (attempt < 2)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        return false;
    }

    private static async Task<bool> IsFileListedInRemoteDirectoryAsync(
        AsyncFtpClient client,
        string remotePath,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(remoteDirectory))
        {
            diagnostics.Log($"Cannot list a parent directory for '{remotePath}'.");
            return false;
        }

        var fileName = StreamBimPathHelper.GetLeafName(remotePath);
        diagnostics.Log($"Listing remote directory for verification: '{remoteDirectory}'.");
        var entries = await client.GetListing(remoteDirectory, cancellationToken);
        var entry = entries.FirstOrDefault(item =>
            item.Type == FtpObjectType.File &&
            string.Equals(item.Name, fileName, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            diagnostics.Log($"Remote directory listing did not contain '{fileName}'.");
            return false;
        }

        diagnostics.Log($"Remote directory listing verified file: name='{entry.Name}', size={entry.Size} bytes, path='{entry.FullName}'.");
        return true;
    }
}

internal sealed class StreamBimFtpRecoveryException(string message) : TimeoutException(message);
