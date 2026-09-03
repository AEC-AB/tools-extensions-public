using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMUploader;

internal static class StreamBimFileTransferService
{
    internal static async Task<StreamBimSingleFileUploadResult> UploadFileAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string localFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var remotePath = StreamBimPathHelper.CreateRemotePath(projectPath, args.TargetFolder, args.UploadFolder, localFilePath);

            if (StreamBimPathHelper.ContainsIgnoredFolder(remotePath))
            {
                return StreamBimSingleFileUploadResult.Skipped(remotePath.TrimStart('/'));
            }

            if (args.SkipUnchangedFiles)
            {
                var remoteInfo = await client.GetObjectInfo(remotePath, token: cancellationToken);
                if (remoteInfo is not null &&
                    remoteInfo.Modified != default &&
                    StreamBimPathHelper.AreEquivalentTimestamps(remoteInfo.Modified, File.GetLastWriteTimeUtc(localFilePath)))
                {
                    return StreamBimSingleFileUploadResult.Skipped(remotePath.TrimStart('/'));
                }
            }

            var uploadStatus = await UploadFileWithRetriesAsync(client, localFilePath, remotePath, cancellationToken);
            return uploadStatus == FtpStatus.Failed
                ? StreamBimSingleFileUploadResult.Failed(remotePath.TrimStart('/'), "Failed to upload.")
                : StreamBimSingleFileUploadResult.Uploaded(remotePath.TrimStart('/'));
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
        catch (FtpException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (IOException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (SocketException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (TimeoutException exception)
        {
            return StreamBimSingleFileUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
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
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(remoteDirectory))
                {
                    await client.CreateDirectory(remoteDirectory, cancellationToken);
                }

                var uploadStatus = await client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, token: cancellationToken);
                if (uploadStatus == FtpStatus.Failed)
                {
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
            catch (Exception exception) when (attempt < 2 &&
                (exception is UnauthorizedAccessException || StreamBimExceptionHelper.IsTransientFtpFailure(exception)))
            {
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
}
