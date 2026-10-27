using System.Runtime.Versioning;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectBrowser : IOnlineBrowser<StreamBIMDownloaderArgs>
{
    public Task<OnlineBrowserLocation> GetLocationAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserItemReference reference,
        CancellationToken cancellationToken)
        => StreamBimProjectBrowsing.GetLocationAsync(args.ApplicationName, reference, cancellationToken);

    public Task<OnlineBrowserPage> GetChildrenAsync(
        StreamBIMDownloaderArgs args,
        OnlineBrowserPageRequest request,
        CancellationToken cancellationToken)
        => StreamBimProjectBrowsing.GetChildrenAsync(args.ApplicationName, request, cancellationToken);
}
