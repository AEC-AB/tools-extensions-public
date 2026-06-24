using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        var resolution = await ResolveFtpPathAsync(client, fullFilePath, cancellationToken);
        var item = await client.GetObjectInfo(fullFilePath, token: cancellationToken)
            ?? await TryResolveItemFromParentListingAsync(client, fullFilePath, cancellationToken)
            ?? resolution.Item;
        if (item is null)
        {
            return StreamBimItemDownloadResult.Failed(displayPath, CreatePathNotFoundMessage(resolution, false));
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
        var folderResolution = await ResolveFtpPathAsync(client, folder, cancellationToken);
        if (folderResolution.Item?.Type != FtpObjectType.Directory)
        {
            return StreamBimItemDownloadResult.Failed(file.TrimStart('/'), CreatePathNotFoundMessage(folderResolution, true));
        }

        var matchedAny = false;
        var listing = await client.GetListing(GetResolvedItemPath(folderResolution.ValidParentPath, folderResolution.Item), cancellationToken);
        foreach (var itemInFolder in listing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemInFolder.Type != FtpObjectType.File || !StreamBimPathHelper.MatchesWildcard(itemInFolder.Name, pattern))
            {
                continue;
            }

            matchedAny = true;
            builder.Add(await StreamBimFileTransferService.DownloadFileAsync(args, client, projectPath, itemInFolder, cancellationToken));
        }

        if (!matchedAny)
        {
            var similarFiles = GetSimilarNames(pattern, listing.Where(item => item.Type == FtpObjectType.File));
            return StreamBimItemDownloadResult.Failed(file.TrimStart('/'), CreateWildcardNotFoundMessage(pattern, folder, similarFiles));
        }

        return builder.BuildItemResult();
    }

    private static async Task<FtpPathResolution> ResolveFtpPathAsync(
        AsyncFtpClient client,
        string fullFilePath,
        CancellationToken cancellationToken)
    {
        var segments = fullFilePath
            .Replace('\\', '/')
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return new FtpPathResolution(null, "/", null, false, []);
        }

        var currentFolder = "/";
        FtpListItem? matchedItem = null;
        for (var index = 0; index < segments.Length; index++)
        {
            var listing = await client.GetListing(currentFolder, cancellationToken);
            matchedItem = null;
            foreach (var item in listing)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(item.Name, segments[index], StringComparison.OrdinalIgnoreCase))
                {
                    matchedItem = item;
                    break;
                }
            }

            if (matchedItem is null)
            {
                var missingSegmentShouldBeFolder = index < segments.Length - 1;
                return new FtpPathResolution(
                    null,
                    currentFolder,
                    segments[index],
                    missingSegmentShouldBeFolder,
                    GetSimilarNames(segments[index], listing, missingSegmentShouldBeFolder ? FtpObjectType.Directory : null));
            }

            if (index == segments.Length - 1)
            {
                return new FtpPathResolution(matchedItem, currentFolder, null, false, []);
            }

            if (matchedItem.Type != FtpObjectType.Directory)
            {
                return new FtpPathResolution(null, currentFolder, segments[index], true, []);
            }

            currentFolder = GetResolvedItemPath(currentFolder, matchedItem);
        }

        return new FtpPathResolution(matchedItem, currentFolder, null, false, []);
    }

    private static string GetResolvedItemPath(string parentFolder, FtpListItem item)
    {
        var normalizedFullName = item.FullName?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(normalizedFullName) &&
            normalizedFullName != "/" &&
            string.Equals(StreamBimPathHelper.GetLeafName(normalizedFullName), item.Name, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullName;
        }

        return StreamBimPathHelper.CombineFtpPath(parentFolder, item.Name);
    }

    private static string CreatePathNotFoundMessage(FtpPathResolution resolution, bool expectedFolder)
    {
        if (resolution.MissingSegment is null)
        {
            return expectedFolder ? "Folder not found." : "File not found.";
        }

        var itemKind = expectedFolder || resolution.MissingSegmentShouldBeFolder ? "folder" : "file or folder";
        var validPath = string.IsNullOrWhiteSpace(resolution.ValidParentPath.TrimStart('/')) ? "/" : resolution.ValidParentPath.TrimStart('/');
        var message = $"Missing {itemKind} '{resolution.MissingSegment}'. Path is valid through '{validPath}'.";
        if (resolution.SimilarNames.Count > 0)
        {
            message += $" Similar names: {string.Join(", ", resolution.SimilarNames)}.";
        }

        return message;
    }

    private static string CreateWildcardNotFoundMessage(string pattern, string folder, IReadOnlyList<string> similarFiles)
    {
        var message = $"No files matched wildcard '{pattern}'. Folder exists: '{folder.TrimStart('/')}'.";
        if (similarFiles.Count > 0)
        {
            message += $" Similar files: {string.Join(", ", similarFiles)}.";
        }

        return message;
    }

    private static IReadOnlyList<string> GetSimilarNames(string expectedName, IEnumerable<FtpListItem> items, FtpObjectType? itemType = null)
    {
        return items
            .Where(item => itemType is null || item.Type == itemType)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => GetSimilarityScore(expectedName, name))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
    }

    private static int GetSimilarityScore(string expectedName, string candidateName)
    {
        if (candidateName.StartsWith(expectedName, StringComparison.OrdinalIgnoreCase) ||
            expectedName.StartsWith(candidateName, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (candidateName.Contains(expectedName, StringComparison.OrdinalIgnoreCase) ||
            expectedName.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return GetLevenshteinDistance(expectedName, candidateName);
    }

    private static int GetLevenshteinDistance(string left, string right)
    {
        var distances = new int[left.Length + 1, right.Length + 1];
        for (var leftIndex = 0; leftIndex <= left.Length; leftIndex++)
        {
            distances[leftIndex, 0] = leftIndex;
        }

        for (var rightIndex = 0; rightIndex <= right.Length; rightIndex++)
        {
            distances[0, rightIndex] = rightIndex;
        }

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var cost = char.ToUpperInvariant(left[leftIndex - 1]) == char.ToUpperInvariant(right[rightIndex - 1]) ? 0 : 1;
                distances[leftIndex, rightIndex] = Math.Min(
                    Math.Min(distances[leftIndex - 1, rightIndex] + 1, distances[leftIndex, rightIndex - 1] + 1),
                    distances[leftIndex - 1, rightIndex - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private sealed record FtpPathResolution(
        FtpListItem? Item,
        string ValidParentPath,
        string? MissingSegment,
        bool MissingSegmentShouldBeFolder,
        IReadOnlyList<string> SimilarNames);
}
