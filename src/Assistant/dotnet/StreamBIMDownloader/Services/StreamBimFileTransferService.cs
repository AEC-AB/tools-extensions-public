using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMDownloader;

internal static class StreamBimFileTransferService
{
    internal static async Task<StreamBimSingleFileDownloadResult> DownloadFileAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        FtpListItem file,
        CancellationToken cancellationToken)
    {
        try
        {
            if (StreamBimPathHelper.ContainsIgnoredFolder(file.FullName))
            {
                return StreamBimSingleFileDownloadResult.Skipped(file.FullName);
            }

            var localPath = StreamBimPathHelper.CreateLocalPath(args.DownloadFolder, projectPath, file.FullName);

            if (args.SkipUnchangedFiles &&
                File.Exists(localPath) &&
                StreamBimPathHelper.AreEquivalentTimestamps(file.Modified, File.GetLastWriteTimeUtc(localPath)))
            {
                return StreamBimSingleFileDownloadResult.Skipped(file.FullName);
            }

            var downloadStatus = await DownloadFileWithRetriesAsync(client, file, localPath, cancellationToken);
            return downloadStatus == FtpStatus.Failed
                ? StreamBimSingleFileDownloadResult.Failed(file.FullName, "Failed to download.")
                : StreamBimSingleFileDownloadResult.Downloaded(file.FullName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (InvalidOperationException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (FtpException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (IOException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (SocketException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (TimeoutException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (AuthenticationException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(file.FullName, StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
    }

    private static async Task<FtpStatus> DownloadFileWithRetriesAsync(
        AsyncFtpClient client,
        FtpListItem item,
        string localPath,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                File.Delete(tempPath);

                var downloadStatus = await client.DownloadFile(tempPath, item.FullName, token: cancellationToken);
                if (downloadStatus == FtpStatus.Failed)
                {
                    continue;
                }

                var targetDirectory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Move(tempPath, localPath, true);
                tempPath = null;

                if (item.Created != default)
                {
                    File.SetCreationTimeUtc(localPath, item.Created);
                }

                if (item.Modified != default)
                {
                    File.SetLastWriteTimeUtc(localPath, item.Modified);
                }

                return downloadStatus;
            }
            catch (Exception exception) when (attempt < 2 &&
                (exception is UnauthorizedAccessException || StreamBimExceptionHelper.IsTransientFtpFailure(exception)))
            {
                Trace.TraceWarning(
                    "Retrying download for '{0}' after attempt {1} failed with {2}: {3}",
                    item.FullName,
                    attempt + 1,
                    exception.GetType().Name,
                    StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            finally
            {
                if (tempPath is not null && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        return FtpStatus.Failed;
    }
}