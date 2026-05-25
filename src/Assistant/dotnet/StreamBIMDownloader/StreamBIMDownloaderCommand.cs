using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;
using FluentFTP;
using FluentFTP.Exceptions;
using Meziantou.Framework.Win32;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
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
        client.Config.RetryAttempts = 5;

        var profile = new FtpProfile
        {
            Host = "ftp.streambim.com",
            Encoding = Encoding.UTF8,
            Encryption = FtpEncryptionMode.Explicit,
            Protocols = System.Security.Authentication.SslProtocols.Tls12,
            Credentials = new NetworkCredential(credentials.UserName, credentials.Password),
        };

        Exception? lastException = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await client.Connect(profile);
                return client;
            }
            catch (FtpException exception) when (attempt < 2)
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
            catch (IOException exception) when (attempt < 2)
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
            catch (SocketException exception) when (attempt < 2)
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
            catch (TimeoutException exception) when (attempt < 2)
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
            catch (AuthenticationException exception) when (attempt < 2)
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to connect to StreamBIM.", lastException);
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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ArgumentException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), exception.Message));
                    break;
                }
                catch (FtpException) when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
                catch (IOException) when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
                catch (SocketException) when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TimeoutException) when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
                catch (AuthenticationException) when (attempt < 3)
                {
                    var delay = (int)Math.Pow(attempt + 1, 2) * 1000;
                    await Task.Delay(delay, cancellationToken);
                }
                catch (FtpException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), GetInnermostException(exception).Message));
                    break;
                }
                catch (IOException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), GetInnermostException(exception).Message));
                    break;
                }
                catch (SocketException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), GetInnermostException(exception).Message));
                    break;
                }
                catch (TimeoutException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), GetInnermostException(exception).Message));
                    break;
                }
                catch (AuthenticationException exception)
                {
                    result.FailedFiles.Add(new FailedFile(CreateDisplayPath(projectPath, file), GetInnermostException(exception).Message));
                    break;
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
            await DownloadFilesByWildcardAsync(args, client, result, projectPath, fullFilePath, cancellationToken);
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
            await DownloadSingleFileAsync(args, client, result, projectPath, item, cancellationToken);
        }
        else if (item.Type == FtpObjectType.Directory)
        {
            await DownloadFilesByWildcardAsync(args, client, result, projectPath, fullFilePath + "/*", cancellationToken);
        }
    }

    private static async Task DownloadFilesByWildcardAsync(
        StreamBIMDownloaderArgs args,
        AsyncFtpClient client,
        StreamBimDownloadResult result,
        string projectPath,
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
                await DownloadSingleFileAsync(args, client, result, projectPath, itemInFolder, cancellationToken);
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
        string projectPath,
        FtpListItem file,
        CancellationToken cancellationToken)
    {
        try
        {
            var localPath = CreateLocalPath(args.DownloadFolder, projectPath, file.FullName);

            if (args.SkipUnchangedFiles && File.Exists(localPath) && AreEquivalentTimestamps(file.Modified, File.GetLastWriteTimeUtc(localPath)))
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (InvalidOperationException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (FtpException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (IOException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (UnauthorizedAccessException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (SocketException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (TimeoutException exception)
        {
            result.FailedFiles.Add(new FailedFile(file.FullName, GetInnermostException(exception).Message));
        }
        catch (AuthenticationException exception)
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

                var targetDirectory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
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
            catch (FtpException) when (attempt < 2)
            {
            }
            catch (IOException) when (attempt < 2)
            {
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
            }
            catch (SocketException) when (attempt < 2)
            {
            }
            catch (TimeoutException) when (attempt < 2)
            {
            }
            catch (AuthenticationException) when (attempt < 2)
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

    [SupportedOSPlatform("windows")]
    internal static UserCredentials? TryGetUserCredentials(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var credentials = CredentialManager.ReadCredential(applicationName);
            if (credentials is null ||
                string.IsNullOrWhiteSpace(credentials.UserName) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                return null;
            }

            return new UserCredentials(credentials.UserName, credentials.Password);
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
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

        var normalized = NormalizeRelativePath(configuredFile.Trim().TrimStart('/').TrimEnd('/'));
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

    private static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var segments = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment =>
            {
                if (segment == "." || segment == "..")
                {
                    throw new ArgumentException("Path segments cannot contain '.' or '..'.");
                }

                return segment;
            });

        return string.Join("/", segments);
    }

    private static string CreateDisplayPath(string projectPath, string configuredFile)
    {
        var safeProjectPath = NormalizeProjectPath(projectPath);
        var normalizedConfiguredFile = configuredFile.Trim().Replace('\\', '/').TrimStart('/');
        return CombineFtpPath(safeProjectPath, normalizedConfiguredFile).TrimStart('/');
    }

    private static string CreateLocalPath(string downloadFolder, string projectPath, string remotePath)
    {
        var projectPrefix = NormalizeProjectPath(projectPath).TrimEnd('/');
        var normalizedRemotePath = remotePath.Replace('\\', '/');

        var relativeRemotePath = projectPrefix.Length == 0 || projectPrefix == "/"
            ? normalizedRemotePath.TrimStart('/')
            : normalizedRemotePath.StartsWith(projectPrefix + "/", StringComparison.OrdinalIgnoreCase)
                ? normalizedRemotePath[(projectPrefix.Length + 1)..]
                : normalizedRemotePath.TrimStart('/');

        var relativePath = NormalizeRelativePath(relativeRemotePath);
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var localPath = downloadFolder;
        foreach (var segment in segments)
        {
            localPath = Path.Combine(localPath, segment);
        }

        var downloadRoot = Path.GetFullPath(downloadFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullLocalPath = Path.GetFullPath(localPath);
        if (!fullLocalPath.StartsWith(downloadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullLocalPath, downloadRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved download path escapes the selected download folder.");
        }

        return fullLocalPath;
    }

    private static bool AreEquivalentTimestamps(DateTime remoteTimestamp, DateTime localTimestamp)
    {
        if (remoteTimestamp == default || localTimestamp == default)
        {
            return false;
        }

        return (remoteTimestamp - localTimestamp).Duration() <= TimeSpan.FromSeconds(2);
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