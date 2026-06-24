using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMDownloader;

internal static class StreamBimDownloadService
{
    internal static async Task<StreamBimDownloadResult> DownloadAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        CancellationToken cancellationToken)
    {
        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var builder = new StreamBimDownloadOutcomeBuilder();

        foreach (var configuredFile in args.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Add(await DownloadConfiguredFileWithRetriesAsync(args, client, projectPath, configuredFile, cancellationToken));
        }

        return builder.BuildBatchResult();
    }

    private static async Task<StreamBimItemDownloadResult> DownloadConfiguredFileWithRetriesAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string configuredFile,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await DownloadConfiguredFileAsync(args, client, projectPath, configuredFile, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), exception.Message);
            }
            catch (Exception exception) when (attempt < 3 && StreamBimExceptionHelper.IsTransientFtpFailure(exception))
            {
                var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                await Task.Delay(delay, cancellationToken);
            }
            catch (FtpException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (IOException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (SocketException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (TimeoutException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (AuthenticationException exception)
            {
                return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
        }

        return StreamBimItemDownloadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), "Failed to download.");
    }

    private static async Task<StreamBimItemDownloadResult> DownloadConfiguredFileAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string configuredFile,
        CancellationToken cancellationToken)
    {
        var normalizedConfiguredFile = StreamBimPathHelper.NormalizeConfiguredFile(projectPath, configuredFile);
        var fullFilePath = StreamBimPathHelper.CombineFtpPath(projectPath, normalizedConfiguredFile);
        var displayPath = fullFilePath.TrimStart('/');

        if (StreamBimPathHelper.ContainsIgnoredFolder(fullFilePath))
        {
            return StreamBimItemDownloadResult.FromSingle(StreamBimSingleFileDownloadResult.Skipped(displayPath));
        }

        if (StreamBimPathHelper.ContainsWildcard(Path.GetFileName(normalizedConfiguredFile)))
        {
            return await DownloadFilesByWildcardAsync(args, client, projectPath, fullFilePath, cancellationToken);
        }

        var item = await client.GetObjectInfo(fullFilePath, token: cancellationToken)
            ?? await TryResolveItemFromParentListingAsync(client, fullFilePath, cancellationToken);
        if (item is null)
        {
            return StreamBimItemDownloadResult.Failed(displayPath, "File not found.");
        }

        if (item.Type == FtpObjectType.File)
        {
            return StreamBimItemDownloadResult.FromSingle(await StreamBimFileTransferService.DownloadFileAsync(args, client, projectPath, item, cancellationToken));
        }

        if (item.Type == FtpObjectType.Directory)
        {
            return await DownloadFilesByWildcardAsync(args, client, projectPath, fullFilePath + "/*", cancellationToken);
        }

        return StreamBimItemDownloadResult.Empty;
    }

    private static async Task<FtpListItem?> TryResolveItemFromParentListingAsync(
        AsyncFtpClient client,
        string fullFilePath,
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(fullFilePath)?.Replace('\\', '/');
        var fileName = Path.GetFileName(fullFilePath);
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var listing = await client.GetListing(folder, cancellationToken);
        foreach (var item in listing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(item.Name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private static async Task<StreamBimItemDownloadResult> DownloadFilesByWildcardAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string file,
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(file)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(folder))
        {
            return StreamBimItemDownloadResult.Empty;
        }

        var builder = new StreamBimDownloadOutcomeBuilder();
        var pattern = Path.GetFileName(file);
        await foreach (var itemInFolder in client.GetListingEnumerable(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemInFolder.Type != FtpObjectType.File || !StreamBimPathHelper.MatchesWildcard(itemInFolder.Name, pattern))
            {
                continue;
            }

            builder.Add(await StreamBimFileTransferService.DownloadFileAsync(args, client, projectPath, itemInFolder, cancellationToken));
        }

        return builder.BuildItemResult();
    }
}
