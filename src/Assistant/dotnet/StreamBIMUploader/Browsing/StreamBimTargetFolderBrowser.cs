using System.Runtime.Versioning;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimTargetFolderBrowser : IOnlineBrowser<StreamBIMUploaderArgs>
{
    public Task<OnlineBrowserPage> GetChildrenAsync(StreamBIMUploaderArgs args, OnlineBrowserPageRequest request, CancellationToken cancellationToken) =>
        StreamBimContentBrowsing.GetChildrenAsync(args.ApplicationName, args.ProjectValue, request, cancellationToken);

    public Task<OnlineBrowserLocation> GetLocationAsync(StreamBIMUploaderArgs args, OnlineBrowserItemReference reference, CancellationToken cancellationToken) =>
        StreamBimContentBrowsing.GetLocationAsync(args.ApplicationName, args.ProjectValue, reference, cancellationToken);
}
