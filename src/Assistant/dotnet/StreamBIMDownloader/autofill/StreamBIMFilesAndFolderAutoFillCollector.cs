using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using CW.Assistant.Extensions.Assistant.Collectors;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
internal class StreamBIMFilesAndFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    private const int MaxSuggestionDepth = 2;
    private const string NoResultsMessage = "No results found. The folder may be empty or not exist. Try changing the folder and clicking reload.";
    private const string SelectProjectMessage = "Select a project and click reload";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);
    private static readonly ConcurrentDictionary<string, FolderCacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Dictionary<string, string>> Get(StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            return CreateSelectProjectResult();
        }

        var projectPath = NormalizeProjectPath(args.Project);
        var lookupContexts = CreateLookupContexts(args.Files);

        try
        {
            var credentials = StreamBIMDownloaderCommand.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            using var client = await StreamBIMDownloaderCommand.CreateAndConnectClientAsync(credentials, cancellationToken);
            var folderListings = await GetFolderListingsAsync(client, args, projectPath, lookupContexts, cancellationToken);
            return BuildResult(folderListings);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FtpException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (System.IO.IOException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Net.Sockets.SocketException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (TimeoutException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Security.Authentication.AuthenticationException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static async Task<FolderListingResult[]> GetFolderListingsAsync(
        AsyncFtpClient client,
        StreamBIMDownloaderArgs args,
        string projectPath,
        IReadOnlyList<LookupContext> lookupContexts,
        CancellationToken cancellationToken)
    {
        var results = new List<FolderListingResult>(lookupContexts.Count);
        foreach (var lookupContext in lookupContexts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await GetFolderListingAsync(client, args, projectPath, lookupContext, cancellationToken));
        }

        return results.ToArray();
    }

    private static async Task<FolderListingResult> GetFolderListingAsync(
        AsyncFtpClient client,
        StreamBIMDownloaderArgs args,
        string projectPath,
        LookupContext lookupContext,
        CancellationToken cancellationToken)
    {
        var currentContext = lookupContext;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var items = await GetSubtreeItemsAsync(client, args, projectPath, currentContext.RelativeFolderPath, cancellationToken);
                return new FolderListingResult(currentContext.RelativeFolderPath, currentContext.NamePrefix, items);
            }
            catch (FtpException) when (TryFallbackToParentContext(currentContext, out var fallbackContext))
            {
                currentContext = fallbackContext;
            }
            catch (System.IO.IOException) when (TryFallbackToParentContext(currentContext, out var fallbackContext))
            {
                currentContext = fallbackContext;
            }
            catch (System.Net.Sockets.SocketException) when (TryFallbackToParentContext(currentContext, out var fallbackContext))
            {
                currentContext = fallbackContext;
            }
            catch (TimeoutException) when (TryFallbackToParentContext(currentContext, out var fallbackContext))
            {
                currentContext = fallbackContext;
            }
            catch (System.Security.Authentication.AuthenticationException) when (TryFallbackToParentContext(currentContext, out var fallbackContext))
            {
                currentContext = fallbackContext;
            }
        }
    }

    private static async Task<FolderItem[]> GetSubtreeItemsAsync(
        AsyncFtpClient client,
        StreamBIMDownloaderArgs args,
        string projectPath,
        string relativeFolderPath,
        CancellationToken cancellationToken)
    {
        var results = new List<FolderItem>();
        var foldersToVisit = new Queue<(string RelativeFolderPath, int Depth)>();
        foldersToVisit.Enqueue((relativeFolderPath, 0));

        while (foldersToVisit.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (currentFolderPath, depth) = foldersToVisit.Dequeue();
            var items = await GetFolderItemsAsync(client, args, projectPath, currentFolderPath, cancellationToken);

            foreach (var item in items)
            {
                var descendantPath = CombineRelativePath(currentFolderPath, item.Name);
                results.Add(new FolderItem(descendantPath, item.IsDirectory));

                if (item.IsDirectory && depth < MaxSuggestionDepth - 1)
                {
                    foldersToVisit.Enqueue((descendantPath, depth + 1));
                }
            }
        }

        return results.ToArray();
    }

    private static async Task<FolderItem[]> GetFolderItemsAsync(
        AsyncFtpClient client,
        StreamBIMDownloaderArgs args,
        string projectPath,
        string relativeFolderPath,
        CancellationToken cancellationToken)
    {
        var cacheKey = CreateCacheKey(args, projectPath, relativeFolderPath);
        if (TryGetCachedFolderListing(cacheKey, out var cachedItems))
        {
            return cachedItems;
        }

        var searchRoot = CombinePath(projectPath, relativeFolderPath);
        var listing = await client.GetListing(searchRoot, cancellationToken);
        var items = listing
            .Where(item => item.Type is FtpObjectType.File or FtpObjectType.Directory)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Where(item => item.Type != FtpObjectType.Directory
                || (!item.Name.EndsWith("-revs", StringComparison.OrdinalIgnoreCase)
                 && !item.Name.EndsWith("_backup", StringComparison.OrdinalIgnoreCase)))
            .Select(item => new FolderItem(item.Name, item.Type == FtpObjectType.Directory))
            .ToArray();

        StoreCachedFolderListing(cacheKey, items);
        return items;
    }

    private static Dictionary<string, string> BuildResult(IReadOnlyList<FolderListingResult> folderListings)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folderListing in folderListings)
        {
            foreach (var item in folderListing.Items)
            {
                var itemLeafName = GetLeafName(item.Name);
                if (!itemLeafName.StartsWith(folderListing.NamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suggestion = item.Name;
                if (item.IsDirectory)
                {
                    suggestion += "/";
                }

                if (string.IsNullOrWhiteSpace(suggestion))
                {
                    continue;
                }

                result[suggestion] = suggestion;
            }
        }

        if (result.Count == 0)
        {
            result[NoResultsMessage] = NoResultsMessage;
        }

        return result;
    }

    private static string GetLeafName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.TrimEnd('/');
        var lastSeparatorIndex = normalized.LastIndexOf('/');
        return lastSeparatorIndex < 0
            ? normalized
            : normalized[(lastSeparatorIndex + 1)..];
    }

    private static IReadOnlyList<LookupContext> CreateLookupContexts(IReadOnlyList<string> configuredFiles)
    {
        var contexts = new List<LookupContext>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var currentInput = configuredFiles
            .LastOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var currentContext = CreateLookupContext(currentInput);
        AddLookupContext(contexts, seen, currentContext);

        foreach (var configuredFile in configuredFiles)
        {
            var folderPath = GetFolderPath(configuredFile);
            AddLookupContext(contexts, seen, new LookupContext(folderPath, string.Empty));
        }

        if (contexts.Count == 0)
        {
            contexts.Add(new LookupContext(string.Empty, string.Empty));
        }

        return contexts;
    }

    private static LookupContext CreateLookupContext(string? configuredFile)
    {
        var currentInput = configuredFile?
            .Trim()
            .Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(currentInput))
        {
            return new LookupContext(string.Empty, string.Empty);
        }

        var normalized = currentInput.TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new LookupContext(string.Empty, string.Empty);
        }

        if (normalized.EndsWith("/", StringComparison.Ordinal))
        {
            return new LookupContext(normalized.TrimEnd('/'), string.Empty);
        }

        var lastSeparatorIndex = normalized.LastIndexOf('/');
        if (lastSeparatorIndex < 0)
        {
            return new LookupContext(string.Empty, StripWildcardSuffix(normalized));
        }

        var relativeFolderPath = normalized[..lastSeparatorIndex].Trim('/');
        var namePrefix = StripWildcardSuffix(normalized[(lastSeparatorIndex + 1)..]);
        return new LookupContext(relativeFolderPath, namePrefix);
    }

    private static void AddLookupContext(List<LookupContext> contexts, HashSet<string> seen, LookupContext context)
    {
        var key = $"{context.RelativeFolderPath}|{context.NamePrefix}";
        if (!seen.Add(key))
        {
            return;
        }

        contexts.Add(context);
    }

    private static string GetFolderPath(string? configuredFile)
    {
        var context = CreateLookupContext(configuredFile);
        if (!string.IsNullOrWhiteSpace(context.NamePrefix))
        {
            return context.RelativeFolderPath;
        }

        var normalized = configuredFile?
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/')
            .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return context.RelativeFolderPath;
        }

        return normalized;
    }

    private static bool TryFallbackToParentContext(LookupContext currentContext, out LookupContext fallbackContext)
    {
        fallbackContext = new LookupContext(string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(currentContext.RelativeFolderPath))
        {
            return false;
        }

        var lastSeparatorIndex = currentContext.RelativeFolderPath.LastIndexOf('/');
        var fallbackFolderPath = lastSeparatorIndex < 0
            ? string.Empty
            : currentContext.RelativeFolderPath[..lastSeparatorIndex];
        var fallbackPrefix = lastSeparatorIndex < 0
            ? currentContext.RelativeFolderPath
            : currentContext.RelativeFolderPath[(lastSeparatorIndex + 1)..];

        fallbackContext = new LookupContext(fallbackFolderPath, fallbackPrefix);
        return true;
    }

    private static string StripWildcardSuffix(string value)
    {
        var wildcardIndex = value.IndexOfAny(['*', '?']);
        if (wildcardIndex < 0)
        {
            return value;
        }

        return value[..wildcardIndex];
    }

    private static string CombinePath(string projectPath, string relativeFolderPath)
    {
        if (string.IsNullOrWhiteSpace(relativeFolderPath))
        {
            return projectPath;
        }

        return projectPath == "/"
            ? "/" + relativeFolderPath.Trim('/')
            : projectPath + "/" + relativeFolderPath.Trim('/');
    }

    private static string CombineRelativePath(string relativeFolderPath, string name)
    {
        if (string.IsNullOrWhiteSpace(relativeFolderPath))
        {
            return name;
        }

        return relativeFolderPath.Trim('/') + "/" + name;
    }

    private static string NormalizeProjectPath(string? project)
    {
        if (string.IsNullOrWhiteSpace(project) || project == "/")
        {
            return "/";
        }

        var normalized = project.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    private static string CreateCacheKey(StreamBIMDownloaderArgs args, string projectPath, string relativeFolderPath)
    {
        return $"{args.ApplicationName}|{projectPath}|{relativeFolderPath}";
    }

    private static bool TryGetCachedFolderListing(string cacheKey, out FolderItem[] items)
    {
        items = [];
        if (!Cache.TryGetValue(cacheKey, out var entry))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - entry.CreatedAtUtc > CacheLifetime)
        {
            Cache.TryRemove(cacheKey, out _);
            return false;
        }

        items = CloneItems(entry.Items);
        return true;
    }

    private static void StoreCachedFolderListing(string cacheKey, FolderItem[] items)
    {
        Cache[cacheKey] = new FolderCacheEntry(DateTimeOffset.UtcNow, CloneItems(items));
    }

    private static FolderItem[] CloneItems(FolderItem[] items)
    {
        return items.ToArray();
    }

    private static Dictionary<string, string> CreateSelectProjectResult()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SelectProjectMessage] = SelectProjectMessage,
        };
    }

    private sealed record LookupContext(string RelativeFolderPath, string NamePrefix);

    private sealed record FolderItem(string Name, bool IsDirectory);

    private sealed record FolderListingResult(string RelativeFolderPath, string NamePrefix, FolderItem[] Items);

    private sealed record FolderCacheEntry(DateTimeOffset CreatedAtUtc, FolderItem[] Items);
}