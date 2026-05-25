using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant.Collectors;
using FluentFTP;

namespace StreamBIMDownloader;

internal class StreamBIMProjectRootFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    public async Task<Dictionary<string, string>> Get(StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var credentials = StreamBIMDownloaderCommand.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return result;
            }

            using var client = await StreamBIMDownloaderCommand.CreateAndConnectClientAsync(credentials, cancellationToken);
            var listing = await client.GetListing("/", cancellationToken);
            foreach (var item in listing)
            {
                if (item.Type != FtpObjectType.Directory)
                {
                    continue;
                }

                var name = item.Name;
                if (string.IsNullOrWhiteSpace(name) || name.EndsWith("-revs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result[name] = name;
            }
        }
        catch
        {
            return result;
        }

        return result;
    }
}