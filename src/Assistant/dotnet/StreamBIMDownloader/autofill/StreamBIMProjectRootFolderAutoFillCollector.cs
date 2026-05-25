using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using CW.Assistant.Extensions.Assistant.Collectors;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
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
                if (string.IsNullOrWhiteSpace(name)
                    || name.EndsWith("-revs", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "_backup", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result[name] = name;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FtpException)
        {
            return result;
        }
        catch (System.IO.IOException)
        {
            return result;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return result;
        }
        catch (TimeoutException)
        {
            return result;
        }
        catch (System.Security.Authentication.AuthenticationException e)
        {
            result["auth_message"] = "Authentication failed. Please check your credentials.";
            result["auth_error_message"] = e.Message;
            return result;
        }

        return result;
    }
}