using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant.Collectors;
using CW.Assistant.Extensions.Contracts.Enums;
using CW.Assistant.Extensions.Contracts.Fields;
using FluentFTP;

namespace StreamBIMDownloader;

public class StreamBIMDownloaderArgs
{
    [PasswordField(
        Label = "Credential application id",
        ToolTip = "Select credentials stored in Windows Credential Manager for the specified application id.")]
    [Required(ErrorMessage = "Credential application id is required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [TextField(
        Label = "Root folder",
        Hint = "Project/Folder",
        ToolTip = "The root folder to start looking for files in. Example: Project/Folder")]
    [Required(ErrorMessage = "Root folder is required.")]
    public string RootFolder { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Download folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Download folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [IntegerField(
        Label = "Max depth",
        ToolTip = "Keep this as low as possible for faster loading. Increase it if you cannot find your files.")]
    [Range(1, 25, ErrorMessage = "Max depth must be between 1 and 25.")]
    public int MaxDepth { get; set; } = 2;

    [OptionsField(
        Label = "Files to download",
        ToolTip = "Relative paths under the root folder. Wildcards with * and ? are supported.",
        CollectorType = typeof(StreamBIMFilesAndFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    public List<string> Files { get; set; } = [];

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}

internal class StreamBIMFilesAndFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMDownloaderArgs>
{
    public async Task<Dictionary<string, string>> Get(StreamBIMDownloaderArgs args, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rootFolder = NormalizeRootFolder(args.RootFolder);
            var credentials = StreamBIMDownloaderCommand.TryGetUserCredentials(args.ApplicationName);
            if (credentials is null)
            {
                return result;
            }

            using var client = await StreamBIMDownloaderCommand.CreateAndConnectClientAsync(credentials, cancellationToken);
            await GetFilesAsync(client, result, rootFolder, 1, rootFolder, args.MaxDepth, cancellationToken);
        }
        catch
        {
            return result;
        }

        return result;
    }

    private static async Task GetFilesAsync(
        AsyncFtpClient client,
        Dictionary<string, string> result,
        string folderPath,
        int currentDepth,
        string rootFolder,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (folderPath.EndsWith("-revs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var listing = await client.GetListing(folderPath);
        foreach (var item in listing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.FullName.EndsWith("-revs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = GetRelativeName(rootFolder, item.FullName);
            if (item.Type == FtpObjectType.File)
            {
                result[name] = name;
                continue;
            }

            if (item.Type != FtpObjectType.Directory)
            {
                continue;
            }

            if (currentDepth < maxDepth)
            {
                await GetFilesAsync(client, result, item.FullName, currentDepth + 1, rootFolder, maxDepth, cancellationToken);
            }
            else
            {
                result[name] = name;
            }
        }
    }

    private static string NormalizeRootFolder(string? rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder))
        {
            return "/";
        }

        return rootFolder.StartsWith('/') ? rootFolder : "/" + rootFolder;
    }

    private static string GetRelativeName(string rootFolder, string fullName)
    {
        var name = fullName;
        if (name.StartsWith("/", StringComparison.Ordinal))
        {
            name = name[1..];
        }

        var trimmedRoot = rootFolder.TrimStart('/');
        if (name.StartsWith(trimmedRoot, StringComparison.OrdinalIgnoreCase))
        {
            name = name[trimmedRoot.Length..];
        }

        if (name.StartsWith("/", StringComparison.Ordinal))
        {
            name = name[1..];
        }

        return name;
    }
}