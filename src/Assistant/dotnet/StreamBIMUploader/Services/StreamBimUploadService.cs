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

        foreach (var configuredFile in args.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Add(await UploadConfiguredFileWithRetriesAsync(args, client, projectPath, configuredFile, cancellationToken));
        }

        return builder.BuildBatchResult();
    }

    private static async Task<StreamBimItemUploadResult> UploadConfiguredFileWithRetriesAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string configuredFile,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await UploadConfiguredFileAsync(args, client, projectPath, configuredFile, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), exception.Message);
            }
            catch (Exception exception) when (attempt < 3 && StreamBimExceptionHelper.IsTransientFtpFailure(exception))
            {
                var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                await Task.Delay(delay, cancellationToken);
            }
            catch (FtpException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (IOException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (SocketException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (TimeoutException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
            catch (AuthenticationException exception)
            {
                return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), StreamBimExceptionHelper.GetInnermostMessage(exception));
            }
        }

        return StreamBimItemUploadResult.Failed(StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile), "Failed to upload.");
    }

    private static async Task<StreamBimItemUploadResult> UploadConfiguredFileAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string configuredFile,
        CancellationToken cancellationToken)
    {
        var normalizedConfiguredFile = StreamBimPathHelper.NormalizeRelativePath(configuredFile.Trim().TrimStart('/').TrimEnd('/'));
        var displayPath = StreamBimPathHelper.CreateDisplayPath(projectPath, configuredFile);

        if (StreamBimPathHelper.ContainsIgnoredFolder(normalizedConfiguredFile))
        {
            return StreamBimItemUploadResult.FromSingle(StreamBimSingleFileUploadResult.Skipped(displayPath));
        }

        var localPath = Path.Combine(args.UploadFolder, normalizedConfiguredFile.Replace('/', Path.DirectorySeparatorChar));

        if (Directory.Exists(localPath))
        {
            return await UploadFilesByWildcardAsync(args, client, projectPath, localPath + Path.DirectorySeparatorChar + "*", cancellationToken);
        }

        if (StreamBimPathHelper.ContainsWildcard(Path.GetFileName(normalizedConfiguredFile)))
        {
            return await UploadFilesByWildcardAsync(args, client, projectPath, localPath, cancellationToken);
        }

        if (!File.Exists(localPath))
        {
            return StreamBimItemUploadResult.Failed(displayPath, "File not found.");
        }

        return StreamBimItemUploadResult.FromSingle(
            await StreamBimFileTransferService.UploadFileAsync(args, client, projectPath, localPath, cancellationToken));
    }

    private static async Task<StreamBimItemUploadResult> UploadFilesByWildcardAsync(
        StreamBIMUploaderArgs args,
        AsyncFtpClient client,
        string projectPath,
        string localPath,
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(localPath);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return StreamBimItemUploadResult.Empty;
        }

        var pattern = Path.GetFileName(localPath);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = "*";
        }

        var builder = new StreamBimUploadOutcomeBuilder();
        var files = Directory.EnumerateFiles(folder, pattern, SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Add(await StreamBimFileTransferService.UploadFileAsync(args, client, projectPath, file, cancellationToken));
        }

        return builder.BuildItemResult();
    }
}
