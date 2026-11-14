using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
public class StreamBIMDownloaderCommand : IAssistantExtension<StreamBIMDownloaderArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.ApplicationName))
        {
            return Result.Text.Failed("Credential application id is required.");
        }

        if (string.IsNullOrWhiteSpace(args.ProjectValue))
        {
            return Result.Text.Failed("Project is required.");
        }

        if (args.Transfers.Targets.Count == 0 || args.Transfers.Targets.Any(target =>
            target.Source is null || string.IsNullOrWhiteSpace(target.Source.Value) || string.IsNullOrWhiteSpace(target.Destination)))
        {
            return Result.Text.Failed("Add at least one download row, with a remote source and local destination folder in every row.");
        }

        try
        {
            var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return Result.Text.Failed($"No stored credentials were found for '{args.ApplicationName}'.");
            }

            using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
            var result = await StreamBimDownloadService.DownloadAsync(args, client, cancellationToken);
            return StreamBimExtensionResultFactory.Create(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (StreamBimExceptionHelper.IsHandledFailure(exception))
        {
            return Result.Text.Failed(StreamBimExceptionHelper.GetInnermostMessage(exception));
        }
    }
}
