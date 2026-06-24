using System.IO;
using System.Runtime.Versioning;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
public class StreamBIMUploaderCommand : IAssistantExtension<StreamBIMUploaderArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, StreamBIMUploaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.ApplicationName))
        {
            return Result.Text.Failed("Credential application id is required.");
        }

        if (string.IsNullOrWhiteSpace(args.Project))
        {
            return Result.Text.Failed("Project is required.");
        }

        if (string.IsNullOrWhiteSpace(args.UploadFolder))
        {
            return Result.Text.Failed("Upload folder is required.");
        }

        if (!Directory.Exists(args.UploadFolder))
        {
            return Result.Text.Failed($"Upload folder does not exist: '{args.UploadFolder}'.");
        }

        if (args.Files.Count == 0)
        {
            return Result.Text.Failed("Select at least one file or folder to upload.");
        }

        try
        {
            var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return Result.Text.Failed($"No stored credentials were found for '{args.ApplicationName}'.");
            }

            using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
            var result = await StreamBimUploadService.UploadAsync(args, client, cancellationToken);
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
