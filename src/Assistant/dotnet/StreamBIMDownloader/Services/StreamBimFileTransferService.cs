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
        string resolvedRemotePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var displayPath = resolvedRemotePath.TrimStart('/');
            if (StreamBimPathHelper.ContainsIgnoredFolder(resolvedRemotePath))
            {
                return StreamBimSingleFileDownloadResult.Skipped(displayPath);
            }

            var localPath = StreamBimPathHelper.CreateLocalPath(args.DownloadFolder, projectPath, resolvedRemotePath);

            if (args.SkipUnchangedFiles &&
                File.Exists(localPath) &&
                StreamBimPathHelper.AreEquivalentTimestamps(file.Modified, File.GetLastWriteTimeUtc(localPath)))
            {
                return StreamBimSingleFileDownloadResult.Skipped(displayPath);
            }

            var primaryRemotePath = string.IsNullOrWhiteSpace(file.FullName)
                ? resolvedRemotePath
                : file.FullName.Replace('\\', '/');
            var primaryStatus = await DownloadFileWithRetriesAsync(client, file, localPath, primaryRemotePath, cancellationToken);
            if (primaryStatus != FtpStatus.Failed)
            {
                return StreamBimSingleFileDownloadResult.Downloaded(displayPath);
            }

            if (StreamBimPathHelper.EqualsNormalized(primaryRemotePath, resolvedRemotePath))
            {
                return StreamBimSingleFileDownloadResult.Failed(displayPath, "Failed to download.");
            }

            var fallbackStatus = await DownloadFileWithRetriesAsync(client, file, localPath, resolvedRemotePath, cancellationToken);
            if (fallbackStatus == FtpStatus.Failed)
            {
                return StreamBimSingleFileDownloadResult.Failed(
                    displayPath,
                    $"Failed to download using item.FullName '{primaryRemotePath}' and resolved path '{resolvedRemotePath}'.");
            }

            return StreamBimSingleFileDownloadResult.Downloaded(
                displayPath,
                $"Downloaded '{displayPath}' using resolved path fallback after item.FullName failed. item.FullName='{primaryRemotePath}', resolvedPath='{resolvedRemotePath}'.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (InvalidOperationException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (FtpException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (IOException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (SocketException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (TimeoutException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
        catch (AuthenticationException exception)
        {
            return StreamBimSingleFileDownloadResult.Failed(resolvedRemotePath.TrimStart('/'), StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
    }

    private static async Task<FtpStatus> DownloadFileWithRetriesAsync(
        AsyncFtpClient client,
        FtpListItem item,
        string localPath,
        string remotePath,
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

                var downloadStatus = await client.DownloadFile(tempPath, remotePath, token: cancellationToken);
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
            catch (Exception exception) when (exception is UnauthorizedAccessException || StreamBimExceptionHelper.IsTransientFtpFailure(exception))
            {
                Trace.TraceWarning(
                    "Download for '{0}' failed on attempt {1} with {2}: {3}",
                    remotePath,
                    attempt + 1,
                    exception.GetType().Name,
                    StreamBimExceptionHelper.GetInnermostMessage(exception));

                if (attempt == 2)
                {
                    return FtpStatus.Failed;
                }
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
