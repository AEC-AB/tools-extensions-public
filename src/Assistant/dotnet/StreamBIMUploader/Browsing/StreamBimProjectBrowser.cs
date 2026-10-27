using System.Runtime.Versioning;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectBrowser : IOnlineBrowser<StreamBIMUploaderArgs>
{
    public Task<OnlineBrowserPage> GetChildrenAsync(StreamBIMUploaderArgs args, OnlineBrowserPageRequest request, CancellationToken cancellationToken) =>
        StreamBimProjectBrowsing.GetChildrenAsync(args.ApplicationName, request, cancellationToken);

    public Task<OnlineBrowserLocation> GetLocationAsync(StreamBIMUploaderArgs args, OnlineBrowserItemReference reference, CancellationToken cancellationToken) =>
        StreamBimProjectBrowsing.GetLocationAsync(args.ApplicationName, reference, cancellationToken);
}
