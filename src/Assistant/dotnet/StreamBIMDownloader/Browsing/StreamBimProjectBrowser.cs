using System.Runtime.Versioning;
using FluentFTP;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
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

        var projects = await GetChildrenAsync(args, null, cancellationToken);
        var project = projects.FirstOrDefault(item => item.Value == value)
            ?? throw new InvalidOperationException($"Saved StreamBIM project '{value}' was not found.");
        return [project];
    }

    public async Task<IReadOnlyList<OnlineBrowserItem>> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        string? parentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            return [];
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing("/", cancellationToken);

        return listing
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
            })
            .ToArray();
    }
}
