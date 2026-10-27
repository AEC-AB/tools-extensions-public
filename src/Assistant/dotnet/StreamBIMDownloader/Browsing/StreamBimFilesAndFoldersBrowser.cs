using System.Runtime.Versioning;
using FluentFTP;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimFilesAndFoldersBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
{
    public async Task<OnlineBrowserLocation> GetLocationAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserItemReference reference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(reference.Value))
        {
            return new();
        }

        if (string.IsNullOrWhiteSpace(args.Project))
        {
            throw new InvalidOperationException("Select a StreamBIM project before resolving a saved file.");
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");
        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var relativeValue = StreamBimPathHelper.NormalizeRelativePath(reference.Value);
        if (string.IsNullOrWhiteSpace(relativeValue))
        {
            return new();
        }
        if (StreamBimPathHelper.ContainsIgnoredFolder(relativeValue))
        {
            throw new InvalidOperationException($"Saved StreamBIM path '{reference.Value}' points to an ignored folder.");
        }

        var segments = relativeValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var wildcardSegmentIndex = Array.FindIndex(segments, StreamBimPathHelper.ContainsWildcard);
        string relativeFolderPath;
        FtpObjectType? resolvedType = null;

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        if (wildcardSegmentIndex >= 0)
        {
            relativeFolderPath = string.Join("/", segments.Take(wildcardSegmentIndex));
            var folderInfo = await client.GetObjectInfo(
                StreamBimPathHelper.CombineFtpPath(projectPath, relativeFolderPath),
                token: cancellationToken);
            if (!string.IsNullOrEmpty(relativeFolderPath) && folderInfo?.Type != FtpObjectType.Directory)
            {
                throw new InvalidOperationException($"Saved StreamBIM path '{reference.Value}' could not resolve its containing folder.");
            }
        }
        else
        {
            var item = await client.GetObjectInfo(
                StreamBimPathHelper.CombineFtpPath(projectPath, relativeValue),
                token: cancellationToken);
            if (item is null)
            {
                throw new InvalidOperationException($"Saved StreamBIM item '{reference.Value}' was not found in project '{args.Project}'.");
            }

            resolvedType = item.Type;

            relativeFolderPath = item.Type == FtpObjectType.Directory
                ? relativeValue
                : string.Join("/", segments.Take(Math.Max(segments.Length - 1, 0)));
            if (item.Type is not (FtpObjectType.File or FtpObjectType.Directory))
            {
                throw new InvalidOperationException($"Saved StreamBIM item '{reference.Value}' has an unsupported remote type.");
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

        OnlineBrowserItem? resolvedItem = resolvedType is FtpObjectType.File or FtpObjectType.Directory
            ? new OnlineBrowserItem
            {
                Id = relativeValue,
                Name = segments.Last(),
                Value = relativeValue,
                Kind = resolvedType == FtpObjectType.Directory ? OnlineBrowserItemKind.Folder : OnlineBrowserItemKind.File,
            }
            : null;
        return new OnlineBrowserLocation { Item = resolvedItem, Folders = breadcrumb };
    }

    public async Task<OnlineBrowserPage> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserPageRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ContinuationToken))
        {
            throw new InvalidOperationException("StreamBIM file browsing does not support continuation tokens.");
        }
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            throw new InvalidOperationException("Select a StreamBIM project before browsing its files.");
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");

        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var relativeParentPath = string.IsNullOrWhiteSpace(request.ParentId)
            ? string.Empty
            : StreamBimPathHelper.NormalizeRelativePath(request.ParentId);

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing(
            StreamBimPathHelper.CombineFtpPath(projectPath, relativeParentPath),
            cancellationToken);

        var items = listing
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
        return new OnlineBrowserPage { Items = items };
    }
}
