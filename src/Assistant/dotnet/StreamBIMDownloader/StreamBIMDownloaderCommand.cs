using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;
using FluentFTP;
using Meziantou.Framework.Win32;

namespace StreamBIMDownloader;

public class StreamBIMDownloaderCommand : IAssistantExtension<StreamBIMDownloaderArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.DownloadFolder))
        {
            return Result.Text.Failed("Download folder is required.");
        }

        if (args.Files.Count == 0)
        {
            return Result.Text.Failed("Select at least one file or folder to download.");
        }

        try
        {
            Directory.CreateDirectory(args.DownloadFolder);

            var credentials = TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return Result.Text.Failed($"No stored credentials were found for '{args.ApplicationName}'.");
            }

            using var client = await CreateAndConnectClientAsync(credentials, cancellationToken);
            var result = await DownloadFilesAsync(args, client, cancellationToken);
            var message = ComposeMessageFromResult(result);
            return CreateExtensionResult(result, message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Text.Failed(GetInnermostException(exception).Message);
        }
    }

    private static TextExtensionResult CreateExtensionResult(StreamBimDownloadResult result, string message)
    {
        var successfulCount = result.DownloadedFiles.Count + result.SkippedFiles.Count;
        var failedCount = result.FailedFiles.Count;
        var total = successfulCount + failedCount;

        if (total == 0)
        {
            return Result.Text.Failed("No files found.");
        }

        if (successfulCount > 0 && failedCount == 0)
        {
            return Result.Text.Succeeded(message);
        }

        if (successfulCount > 0)
        {
            return Result.Text.PartiallySucceeded(message);
        }

        return Result.Text.Failed(message);
    }

    private static string ComposeMessageFromResult(StreamBimDownloadResult result)
    {
        var message = $"Downloaded {result.DownloadedFiles.Count} files";
        if (result.DownloadedFiles.Count > 0)
        {
            message += $"\n\n{string.Join("\n", result.DownloadedFiles)}";
        }

        if (result.SkippedFiles.Count > 0)
        {
            message += $"\n\nSkipped {result.SkippedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.SkippedFiles)}";
        }

        if (result.FailedFiles.Count > 0)
        {
            message += $"\n\nFailed to download {result.FailedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.FailedFiles.Select(x => x.FileName + ": " + x.ErrorMessage))}";
        }

        return message;
    }

    internal static async Task<AsyncFtpClient> CreateAndConnectClientAsync(UserCredentials credentials, CancellationToken cancellationToken)
    {
        var client = new AsyncFtpClient();
        client.Config.ValidateAnyCertificate = true;
        client.Config.RetryAttempts = 5;

        var profile = new FtpProfile
        {
            Host = "ftp.o.rendra.io",
            Encoding = Encoding.UTF8,
            Encryption = FtpEncryptionMode.Explicit,
            Protocols = System.Security.Authentication.SslProtocols.Tls12,
            Credentials = new NetworkCredential(credentials.UserName, credentials.Password),
        };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await client.Connect(profile);
                return client;
            }
            catch when (attempt < 2)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to connect to StreamBIM.");
    }

    private static async Task<StreamBimDownloadResult> DownloadFilesAsync(StreamBIMDownloaderArgs args, AsyncFtpClient client, CancellationToken cancellationToken)
    {
        var result = new StreamBimDownloadResult();
        var projectPath = NormalizeProjectPath(args.Project);

        foreach (var file in args.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    await DownloadConfiguredFile(args, client, result, projectPath, file, cancellationToken);
                    break;
                }
                catch when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        return result;
    }

    private static async Task DownloadConfiguredFile(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        StreamBimDownloadResult result,
        string projectPath,
        string configuredFile,
        CancellationToken cancellationToken)
    {
        var normalizedConfiguredFile = NormalizeConfiguredFile(projectPath, configuredFile);
        var fullFilePath = CombineFtpPath(projectPath, normalizedConfiguredFile);

        var displayPath = fullFilePath.TrimStart('/');

        var fileName = Path.GetFileName(normalizedConfiguredFile);
        if (ContainsWildcard(fileName))
        {
            await DownloadFilesByWildcardAsync(args, client, result, fullFilePath, cancellationToken);
            return;
        }

        var item = await client.GetObjectInfo(fullFilePath);
        if (item is null)
        {
            result.FailedFiles.Add(new FailedFile(displayPath, "File not found."));
            return;
        }

        if (item.Type == FtpObjectType.File)
        {
            await DownloadSingleFileAsync(args, client, result, item, cancellationToken);
        }
        else if (item.Type == FtpObjectType.Directory)
        {
            await DownloadFilesByWildcardAsync(args, client, result, fullFilePath + "/*", cancellationToken);
        }
    }

    private static async Task DownloadFilesByWildcardAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        StreamBimDownloadResult result,
        string file,
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(file)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        var pattern = Path.GetFileName(file);
        await foreach (var itemInFolder in client.GetListingEnumerable(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemInFolder.Type != FtpObjectType.File)
            {
                continue;
            }

            if (MatchesWildcard(itemInFolder.Name, pattern))
            {
                await DownloadSingleFileAsync(args, client, result, itemInFolder, cancellationToken);
            }
        }
    }

    private static bool MatchesWildcard(string fileName, string fileNameWithWildcard)
    {
        var regex = "^" + Regex.Escape(fileNameWithWildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static async Task DownloadSingleFileAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        StreamBimDownloadResult result,
        FtpListItem file,
        CancellationToken cancellationToken)
    {
        var localPath = Path.Combine(args.DownloadFolder, file.Name);
        try
        {
            if (args.SkipUnchangedFiles && File.Exists(localPath) && file.Modified == File.GetLastWriteTimeUtc(localPath))
            {
                result.SkippedFiles.Add(file.FullName);
                return;
            }

            var downloadStatus = await DownloadFileWithRetries(client, file, localPath, cancellationToken);
            if (downloadStatus == FtpStatus.Failed)
            {
                result.FailedFiles.Add(new FailedFile(file.FullName, "Failed to download."));
            }
            else
            {
                result.DownloadedFiles.Add(file.FullName);
            }
        }
        catch (Exception exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
    }

    private static async Task<FtpStatus> DownloadFileWithRetries(AsyncFtpClient client, FtpListItem item, string localPath, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                File.Delete(tempPath);

                var downloadStatus = await client.DownloadFile(tempPath, item.FullName);
                if (downloadStatus == FtpStatus.Failed)
                {
                    continue;
                }

                File.Move(tempPath, localPath, true);
                tempPath = null;

                if (item.Created != default)
                {
                    File.SetCreationTimeUtc(localPath, item.Created);
                }

                if (item.Modified != default)
                {
                    File.SetLastWriteTimeUtc(localPath, item.Modified);
                }

                return downloadStatus;
            }
            catch when (attempt < 2)
            {
            }
            finally
            {
                if (tempPath is not null && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        return FtpStatus.Failed;
    }

    internal static UserCredentials? TryGetUserCredentials(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return null;
        }

        try
        {
            var credentials = CredentialManager.ReadCredential(applicationName);
            if (credentials is null || string.IsNullOrWhiteSpace(credentials.UserName))
            {
                return null;
            }

            return new UserCredentials(credentials.UserName, credentials.Password);
        }
        catch
        {
            return null;
        }
    }

    private static Exception GetInnermostException(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    private static bool ContainsWildcard(string? fileName)
    {
        return !string.IsNullOrEmpty(fileName) && (fileName.Contains('*') || fileName.Contains('?'));
    }

    private static string NormalizeProjectPath(string? project)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            return "/";
        }

        var normalized = project.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    private static string NormalizeConfiguredFile(string projectPath, string? configuredFile)
    {
        if (string.IsNullOrWhiteSpace(configuredFile))
        {
            return string.Empty;
        }

        var normalized = configuredFile.Trim().TrimStart('/').TrimEnd('/');
        var trimmedProjectPath = projectPath.Trim('/');
        if (!string.IsNullOrEmpty(trimmedProjectPath) &&
            normalized.StartsWith(trimmedProjectPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[(trimmedProjectPath.Length + 1)..];
        }
        else if (string.Equals(normalized, trimmedProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            normalized = string.Empty;
        }

        return normalized;
    }

    private static string CombineFtpPath(string basePath, string? relativePath)
    {
        var normalizedBasePath = string.IsNullOrWhiteSpace(basePath)
            ? "/"
            : "/" + basePath.Trim('/');

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return normalizedBasePath;
        }

        return normalizedBasePath == "/"
            ? "/" + relativePath.Trim('/')
            : normalizedBasePath + "/" + relativePath.Trim('/');
    }
}

internal sealed record UserCredentials(string UserName, string Password);

internal sealed record FailedFile(string FileName, string ErrorMessage);

internal sealed class StreamBimDownloadResult
{
    public List<string> DownloadedFiles { get; } = [];

    public List<FailedFile> FailedFiles { get; } = [];

    public List<string> SkippedFiles { get; } = [];
}