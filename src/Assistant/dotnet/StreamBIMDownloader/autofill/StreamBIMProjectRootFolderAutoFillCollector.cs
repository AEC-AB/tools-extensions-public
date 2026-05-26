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
            var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                result["auth_message"] = "Enter username and password and click Reload to see available projects.";
                return result;
            }

            using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
            var listing = await client.GetListing("/", cancellationToken);
            foreach (var item in listing)
            {
                if (item.Type != FtpObjectType.Directory)
                {
                    continue;
                }

                var name = item.Name;
                if (string.IsNullOrWhiteSpace(name)
                    || StreamBimPathHelper.IsIgnoredDirectoryName(name))
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
        catch (System.Security.Authentication.AuthenticationException e)
        {
            result["auth_message"] = "Authentication failed. Please check your credentials.";
            result["auth_error_message"] = e.Message;
            return result;
        }
        catch (Exception exception) when (StreamBimExceptionHelper.IsTransientFtpFailure(exception))
        {
            return result;
        }

        return result;
    }
}