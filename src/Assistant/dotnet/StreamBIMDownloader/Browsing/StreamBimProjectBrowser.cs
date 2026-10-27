using System.Runtime.Versioning;
using FluentFTP;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
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

        var projects = await GetChildrenAsync(args, new(), cancellationToken);
        var project = projects.Items.FirstOrDefault(item => item.Value == reference.Value)
            ?? throw new InvalidOperationException($"Saved StreamBIM project '{reference.Value}' was not found.");
        return new OnlineBrowserLocation { Item = project, Folders = [project] };
    }

    public async Task<OnlineBrowserPage> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserPageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(request.ContinuationToken))
        {
            throw new InvalidOperationException("StreamBIM project browsing does not support continuation tokens.");
        }
        if (!string.IsNullOrWhiteSpace(request.ParentId))
        {
            return new();
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing("/", cancellationToken);

        var items = listing
            .Where(item => item.Type == FtpObjectType.Directory)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Where(item => !StreamBimPathHelper.IsIgnoredDirectoryName(item.Name))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new OnlineBrowserItem
            {
                Id = item.Name,
                Name = item.Name,
                Value = item.Name,
                Kind = OnlineBrowserItemKind.Folder,
                Provider = OnlineBrowserProvider.Custom,
                Scope = args.ApplicationName,
            })
            .ToArray();
        return new OnlineBrowserPage { Items = items };
    }
}
