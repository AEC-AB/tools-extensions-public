using System.Runtime.Versioning;
using FluentFTP;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimFilesAndFoldersBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
{
    public async Task<IReadOnlyList<OnlineBrowserItem>> GetLocationAsync(
        StreamBIMDownloaderArgs args,
        string value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(args.Project))
        {
            throw new InvalidOperationException("Select a StreamBIM project before resolving a saved file.");
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");
        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var relativeValue = StreamBimPathHelper.NormalizeRelativePath(value);
        if (StreamBimPathHelper.ContainsIgnoredFolder(relativeValue))
        {
            throw new InvalidOperationException($"Saved StreamBIM path '{value}' points to an ignored folder.");
        }

        var segments = relativeValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var wildcardSegmentIndex = Array.FindIndex(segments, StreamBimPathHelper.ContainsWildcard);
        string relativeFolderPath;

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        if (wildcardSegmentIndex >= 0)
        {
            relativeFolderPath = string.Join("/", segments.Take(wildcardSegmentIndex));
            var folderInfo = await client.GetObjectInfo(
                StreamBimPathHelper.CombineFtpPath(projectPath, relativeFolderPath),
                token: cancellationToken);
            if (!string.IsNullOrEmpty(relativeFolderPath) && folderInfo?.Type != FtpObjectType.Directory)
            {
                throw new InvalidOperationException($"Saved StreamBIM path '{value}' could not resolve its containing folder.");
            }
        }
        else
        {
            var item = await client.GetObjectInfo(
                StreamBimPathHelper.CombineFtpPath(projectPath, relativeValue),
                token: cancellationToken);
            if (item is null)
            {
                throw new InvalidOperationException($"Saved StreamBIM item '{value}' was not found in project '{args.Project}'.");
            }

            relativeFolderPath = item.Type == FtpObjectType.Directory
                ? relativeValue
                : string.Join("/", segments.Take(Math.Max(segments.Length - 1, 0)));
            if (item.Type is not (FtpObjectType.File or FtpObjectType.Directory))
            {
                throw new InvalidOperationException($"Saved StreamBIM item '{value}' has an unsupported remote type.");
            }
        }

        var breadcrumb = new List<OnlineBrowserItem>();
        var cumulativePath = string.Empty;
        foreach (var segment in relativeFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            cumulativePath = StreamBimPathHelper.CombineRelativePath(cumulativePath, segment);
            breadcrumb.Add(new OnlineBrowserItem
            {
                Id = cumulativePath,
                Name = segment,
                Value = cumulativePath,
                Kind = OnlineBrowserItemKind.Folder,
            });
        }

        return breadcrumb;
    }

    public async Task<IReadOnlyList<OnlineBrowserItem>> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        string? parentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            throw new InvalidOperationException("Select a StreamBIM project before browsing its files.");
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");

        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var relativeParentPath = string.IsNullOrWhiteSpace(parentId)
            ? string.Empty
            : StreamBimPathHelper.NormalizeRelativePath(parentId);

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing(
            StreamBimPathHelper.CombineFtpPath(projectPath, relativeParentPath),
            cancellationToken);

        return listing
            .Where(item => item.Type is FtpObjectType.File or FtpObjectType.Directory)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Where(item => item.Type != FtpObjectType.Directory || !StreamBimPathHelper.IsIgnoredDirectoryName(item.Name))
            .OrderBy(item => item.Type == FtpObjectType.Directory ? 0 : 1)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item =>
            {
                var value = StreamBimPathHelper.CombineRelativePath(relativeParentPath, item.Name);
                return new OnlineBrowserItem
                {
                    Id = value,
                    Name = item.Name,
                    Value = value,
                    Kind = item.Type == FtpObjectType.Directory
                        ? OnlineBrowserItemKind.Folder
                        : OnlineBrowserItemKind.File,
                };
            })
            .ToArray();
    }
}
