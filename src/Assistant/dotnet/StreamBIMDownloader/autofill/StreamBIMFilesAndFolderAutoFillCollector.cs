using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant.Collectors;
using FluentFTP;

namespace StreamBIMDownloader;

internal class StreamBIMFilesAndFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    private const string SelectProjectMessage = "Select a project and click reload";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Dictionary<string, string>> Get(StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            return CreateSelectProjectResult();
        }

        var projectPath = NormalizeProjectPath(args.Project);
        var cacheKey = CreateCacheKey(args, projectPath);
        if (TryGetCachedResult(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var credentials = StreamBIMDownloaderCommand.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return result;
            }

            using var client = await StreamBIMDownloaderCommand.CreateAndConnectClientAsync(credentials, cancellationToken);
            await GetFilesAsync(client, result, projectPath, cancellationToken);
            StoreCachedResult(cacheKey, result);
        }
        catch
        {
            return result;
        }

        return result;
    }

    private static async Task GetFilesAsync(AsyncFtpClient client, Dictionary<string, string> result, string projectPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var listing = await client.GetListing(projectPath, FtpListOption.Recursive, cancellationToken);
        foreach (var item in listing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ContainsRevisionFolder(item.FullName))
            {
                continue;
            }

            var name = GetNameFromFullName(projectPath, item);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (item.Type == FtpObjectType.File)
            {
                if (!name.Contains('/'))
                {
                    continue;
                }

                result[name] = name;
            }
            else if (item.Type == FtpObjectType.Directory)
            {
                result[name] = name;
            }
        }
    }

    private static string GetNameFromFullName(string projectPath, FtpListItem item)
    {
        var name = item.FullName;
        if (name.StartsWith("/", StringComparison.Ordinal))
        {
            name = name[1..];
        }

        if (name.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            name = name[projectPath.Length..];
        }

        if (name.StartsWith("/", StringComparison.Ordinal))
        {
            name = name[1..];
        }

        return name;
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

    private static string CreateCacheKey(StreamBIMDownloaderArgs args, string projectPath)
    {
        return $"{args.ApplicationName}|{projectPath}";
    }

    private static bool ContainsRevisionFolder(string fullName)
    {
        return fullName
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.EndsWith("-revs", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetCachedResult(string cacheKey, out Dictionary<string, string> result)
    {
        result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Cache.TryGetValue(cacheKey, out var entry))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - entry.CreatedAtUtc > CacheLifetime)
        {
            Cache.TryRemove(cacheKey, out _);
            return false;
        }

        result = CloneResult(entry.Items);
        return true;
    }

    private static void StoreCachedResult(string cacheKey, Dictionary<string, string> result)
    {
        Cache[cacheKey] = new CacheEntry(DateTimeOffset.UtcNow, CloneResult(result));
    }

    private static Dictionary<string, string> CloneResult(Dictionary<string, string> result)
    {
        return new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CreateSelectProjectResult()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SelectProjectMessage] = SelectProjectMessage,
        };
    }

    private sealed record CacheEntry(DateTimeOffset CreatedAtUtc, Dictionary<string, string> Items);
}