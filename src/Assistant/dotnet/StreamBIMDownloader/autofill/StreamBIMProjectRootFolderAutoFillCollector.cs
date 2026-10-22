using System.Runtime.Versioning;
using CW.Assistant.Extensions.Assistant.Collectors;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
internal class StreamBIMProjectRootFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    public async Task<Dictionary<string, string>> Get(StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        try
        {
            return await StreamBimProjectRootFolderCollector.CollectAsync(args.ApplicationName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}
