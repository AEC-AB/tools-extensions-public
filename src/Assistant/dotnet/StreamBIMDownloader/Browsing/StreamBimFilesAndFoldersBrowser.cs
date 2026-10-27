using System.Runtime.Versioning;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimFilesAndFoldersBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
{
    public Task<OnlineBrowserLocation> GetLocationAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserItemReference reference,
        CancellationToken cancellationToken)
        => StreamBimContentBrowsing.GetLocationAsync(args.ApplicationName, args.ProjectValue, reference, cancellationToken);

    public Task<OnlineBrowserPage> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserPageRequest request,
        CancellationToken cancellationToken)
        => StreamBimContentBrowsing.GetChildrenAsync(args.ApplicationName, args.ProjectValue, request, cancellationToken);
}
