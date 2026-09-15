using System.Runtime.Versioning;
using CW.Assistant.Extensions.Assistant.Collectors;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
internal class StreamBIMProjectRootFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMUploaderArgs>
{
    public async Task<Dictionary<string, string>> Get(StreamBIMUploaderArgs args, CancellationToken cancellationToken)
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
