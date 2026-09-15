using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMUploader;

internal static class StreamBimUploadService
{
    internal static async Task<StreamBimUploadResult> UploadAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        CancellationToken cancellationToken)
    {
        var projectPath = StreamBimPathHelper.NormalizeProjectPath(args.Project);
        var builder = new StreamBimUploadOutcomeBuilder();
        var diagnostics = StreamBimUploadDiagnostics.Create(args.VerboseDiagnostics);
        client.Config.DataConnectionType = FtpDataConnectionType.PASVEX;
        if (diagnostics.LogPath is not null)
        {
            builder.AddDiagnostic($"Detailed upload log: {diagnostics.LogPath}");
        }

        diagnostics.Log($"Using FTP data connection mode {client.Config.DataConnectionType}.");

        foreach (var localFilePath in args.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Log($"Selected file: '{localFilePath}'.");
            try
            {
                builder.Add(await UploadFileWithRetriesAsync(
                    args,
                    client,
                    projectPath,
                    localFilePath,
                    Path.GetFileName(localFilePath),
                    diagnostics,
                    cancellationToken));
            }
            catch (StreamBimFtpRecoveryException exception)
            {
                builder.Add(StreamBimItemUploadResult.Failed(localFilePath, exception.Message));
                builder.AddDiagnostic("The FTP client was not reused after a timed-out operation that did not stop during cleanup.");
                break;
            }
        }

        return builder.BuildBatchResult();
    }

    private static async Task<StreamBimItemUploadResult> UploadFileWithRetriesAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string localFilePath,
        string remoteRelativePath,
        StreamBimUploadDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                diagnostics.Log($"Preparing upload: local='{localFilePath}', remote-relative='{remoteRelativePath}', attempt={attempt + 1}.");
                if (!File.Exists(localFilePath))
                {
                    diagnostics.Log($"File not found: '{localFilePath}'.");
                    return StreamBimItemUploadResult.Failed(localFilePath, "File not found.");
                }

                return StreamBimItemUploadResult.FromSingle(await StreamBimFileTransferService.UploadFileAsync(
                    args,
                    client,
                    projectPath,
                    localFilePath,
                    remoteRelativePath,
                    diagnostics,
                    cancellationToken));
            }
            catch (StreamBimFtpRecoveryException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, exception.Message);
            }
            catch (Exception exception) when (attempt < 3 && StreamBimExceptionHelper.IsTransientFtpFailure(exception))
            {
                diagnostics.Log($"Transient FTP failure for '{localFilePath}': {StreamBimExceptionHelper.GetInnermostMessage(exception)}. Retrying.");
                var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                await Task.Delay(delay, cancellationToken);
            }
            catch (FtpException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (IOException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (SocketException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (TimeoutException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (AuthenticationException exception)
            {
                return StreamBimItemUploadResult.Failed(localFilePath, StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
        }

        return StreamBimItemUploadResult.Failed(localFilePath, "Failed to upload.");
    }

}
