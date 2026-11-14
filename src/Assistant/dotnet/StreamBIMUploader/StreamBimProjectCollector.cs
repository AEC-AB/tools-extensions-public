using System.Runtime.Versioning;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
public sealed class StreamBimProjectCollector : IAsyncAutoFillCollector<StreamBIMUploaderArgs>
{
    public Task<Dictionary<string, string>> GetAsync(StreamBIMUploaderArgs args, CancellationToken cancellationToken) =>
        StreamBimProjectRootFolderCollector.CollectAsync(args.ApplicationName, cancellationToken);
}
