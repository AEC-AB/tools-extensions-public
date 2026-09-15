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
        var candidates = StreamBimPathHelper.CreateConfiguredFileCandidates(projectPath, configuredFile);
        var configuredPath = StreamBimPathHelper.CombineFtpPath(projectPath, candidates[0]);

        if (StreamBimPathHelper.ContainsIgnoredFolder(configuredPath))
        {
            return StreamBimItemDownloadResult.FromSingle(StreamBimSingleFileDownloadResult.Skipped(configuredPath.TrimStart('/')));
        }

        ConfiguredFileResolution? closestMiss = null;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await ResolveConfiguredFileAsync(client, projectPath, candidate, cancellationToken);
            if (resolved.Item is not { } item)
            {
                closestMiss = SelectClosestMiss(closestMiss, resolved);
                continue;
            }

            return await DownloadResolvedItemAsync(args, client, resolved, item, cancellationToken);
        }

        return closestMiss is null
            ? StreamBimItemDownloadResult.Failed(configuredPath.TrimStart('/'), "File not found.")
            : StreamBimItemDownloadResult.Failed(
                closestMiss.FullPath.TrimStart('/'),
                CreatePathNotFoundMessage(closestMiss.Resolution, closestMiss.IsWildcard));
    }

    private static async Task<ConfiguredFileResolution> ResolveConfiguredFileAsync(
        AsyncFtpClient client,
        string projectPath,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var fullPath = StreamBimPathHelper.CombineFtpPath(projectPath, relativePath);
        var isWildcard = StreamBimPathHelper.ContainsWildcard(Path.GetFileName(relativePath));
        var lookupPath = isWildcard
            ? Path.GetDirectoryName(fullPath)?.Replace('\\', '/') ?? string.Empty
            : fullPath;

        if (string.IsNullOrWhiteSpace(lookupPath))
        {
            return new ConfiguredFileResolution(fullPath, isWildcard, null, new FtpPathResolution(null, "/", null, isWildcard, []));
        }

        var resolution = await ResolveFtpPathAsync(client, lookupPath, cancellationToken);
        var item = resolution.Item;
        if (item is null && !isWildcard && !resolution.MissingSegmentShouldBeFolder)
        {
            item = await client.GetObjectInfo(fullPath, token: cancellationToken)
                ?? await TryResolveItemFromParentListingAsync(client, fullPath, cancellationToken);
        }

        if (isWildcard && item?.Type != FtpObjectType.Directory)
        {
            item = null;
        }

        return new ConfiguredFileResolution(fullPath, isWildcard, item, resolution);
    }

    private static ConfiguredFileResolution SelectClosestMiss(
        ConfiguredFileResolution? currentMiss,
        ConfiguredFileResolution candidateMiss) =>
        currentMiss is null || GetValidPathDepth(candidateMiss.Resolution) > GetValidPathDepth(currentMiss.Resolution)
            ? candidateMiss
            : currentMiss;

    private static int GetValidPathDepth(FtpPathResolution resolution) =>
        resolution.ValidParentPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

    private static async Task<StreamBimItemDownloadResult> DownloadResolvedItemAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        ConfiguredFileResolution resolved,
        FtpListItem item,
        CancellationToken cancellationToken)
    {
        if (resolved.IsWildcard)
        {
            return await DownloadFilesByWildcardAsync(args, client, resolved.FullPath, null, cancellationToken);
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
            return await DownloadFilesByWildcardAsync(args, client, resolved.FullPath + "/*", resolved.FullPath, cancellationToken);
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
            await foreach (var itemInFolder in client.GetListingEnumerable(
                currentFolder,
                cancellationToken,
                cancellationToken))
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
            matchedItem = listing.FirstOrDefault(item =>
                string.Equals(item.Name, segments[index], StringComparison.OrdinalIgnoreCase));

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

    private static string GetResolvedItemPath(string parentFolder, FtpListItem item) =>
        string.IsNullOrWhiteSpace(item.FullName)
            ? StreamBimPathHelper.CombineFtpPath(parentFolder, item.Name)
            : item.FullName.Replace('\\', '/');

    private static string CreatePathNotFoundMessage(FtpPathResolution resolution, bool expectedFolder)
    {
        if (resolution.MissingSegment is null)
        {
            return expectedFolder ? "Folder not found." : "File not found.";
        }

        var itemKind = expectedFolder || resolution.MissingSegmentShouldBeFolder ? "folder" : "file or folder";
        var validPath = string.IsNullOrWhiteSpace(resolution.ValidParentPath.TrimStart('/')) ? "/" : resolution.ValidParentPath.TrimStart('/');
        var message = $"Missing {itemKind} '{resolution.MissingSegment}'. Path is valid through '{validPath}'.";
        return resolution.SimilarNames.Count > 0
            ? message + $" Similar names: {string.Join(", ", resolution.SimilarNames)}."
            : message;
    }

    private static IReadOnlyList<string> GetSimilarNames(string expectedName, IEnumerable<FtpListItem> items, FtpObjectType? itemType = null) =>
        items
            .Where(item => itemType is null || item.Type == itemType)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => GetSimilarityScore(expectedName, name))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

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

    private sealed record ConfiguredFileResolution(
        string FullPath,
        bool IsWildcard,
        FtpListItem? Item,
        FtpPathResolution Resolution);

    private sealed record FtpPathResolution(
        FtpListItem? Item,
        string ValidParentPath,
        string? MissingSegment,
        bool MissingSegmentShouldBeFolder,
        IReadOnlyList<string> SimilarNames);
}
