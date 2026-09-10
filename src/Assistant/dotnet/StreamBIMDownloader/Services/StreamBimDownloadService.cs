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
            return await DownloadFilesByWildcardAsync(args, client, fullFilePath, null, cancellationToken);
        }

        var item = await client.GetObjectInfo(fullFilePath, token: cancellationToken);
        if (item is null)
        {
            return StreamBimItemDownloadResult.Failed(displayPath, "File not found.");
        }

        if (item.Type == FtpObjectType.File)
        {
            return StreamBimItemDownloadResult.FromSingle(await StreamBimFileTransferService.DownloadFileAsync(
                args,
                client,
                item,
                StreamBimPathHelper.GetLeafName(item.FullName),
                cancellationToken));
        }

        if (item.Type == FtpObjectType.Directory)
        {
            return await DownloadFilesByWildcardAsync(args, client, fullFilePath + "/*", fullFilePath, cancellationToken);
        }

        return StreamBimItemDownloadResult.Empty;
    }

    private static async Task<StreamBimItemDownloadResult> DownloadFilesByWildcardAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        string file,
        string? directoryRoot,
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(file)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(folder))
        {
            return StreamBimItemDownloadResult.Failed(file.TrimStart('/'), "No matching files found.");
        }

        var builder = new StreamBimDownloadOutcomeBuilder();
        var pattern = Path.GetFileName(file);
        var filesFound = 0;
        var foldersToVisit = new Queue<string>();
        foldersToVisit.Enqueue(folder);

        while (foldersToVisit.Count > 0)
        {
            var currentFolder = foldersToVisit.Dequeue();
            await foreach (var itemInFolder in client.GetListingEnumerable(currentFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (directoryRoot is not null && itemInFolder.Type == FtpObjectType.Directory)
                {
                    if (!StreamBimPathHelper.ContainsIgnoredFolder(itemInFolder.FullName))
                    {
                        foldersToVisit.Enqueue(itemInFolder.FullName);
                    }

                    continue;
                }

                if (itemInFolder.Type != FtpObjectType.File || !StreamBimPathHelper.MatchesWildcard(itemInFolder.Name, pattern))
                {
                    continue;
                }

                filesFound++;
                var localRelativePath = directoryRoot is null
                    ? StreamBimPathHelper.GetLeafName(itemInFolder.FullName)
                    : GetRelativePath(directoryRoot, itemInFolder.FullName);
                builder.Add(await StreamBimFileTransferService.DownloadFileAsync(
                    args,
                    client,
                    itemInFolder,
                    localRelativePath,
                    cancellationToken));
            }
        }

        return filesFound == 0
            ? StreamBimItemDownloadResult.Failed(file.TrimStart('/'), "No matching files found.")
            : builder.BuildItemResult();
    }

    private static string GetRelativePath(string directoryRoot, string remoteFilePath)
    {
        var normalizedRoot = directoryRoot.TrimEnd('/');
        var normalizedFilePath = remoteFilePath.Replace('\\', '/');
        if (!normalizedFilePath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Remote file is outside the selected folder.");
        }

        return normalizedFilePath[(normalizedRoot.Length + 1)..];
    }
}
