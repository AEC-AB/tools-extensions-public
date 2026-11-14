using System.Runtime.Versioning;
using CW.Assistant.Extensions.Contracts.Collectors;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    public Task<Dictionary<string, string>> GetAsync(StreamBIMDownloaderArgs args, CancellationToken cancellationToken) =>
        StreamBimProjectRootFolderCollector.CollectAsync(args.ApplicationName, cancellationToken);
}
