using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Browsing;
using CW.Assistant.Extensions.Contracts.Controls;
using CW.Assistant.Extensions.Contracts.Upgrade;

namespace StreamBIMDownloader;

[ArgsVersion(2)]
public class StreamBIMDownloaderArgs
{
    [PasswordField(
        Label = "StreamBIM Credentials",
        ToolTip = "Enter your StreamBIM credentials.")]
    [Required(ErrorMessage = "StreamBIM Credentials are required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [OnlineBrowserField(typeof(StreamBimProjectBrowser),
        SelectionKind = OnlineBrowserSelectionKind.Folders,
        FavoritesKey = "streambim.projects",
        ScopeProperties = [nameof(ApplicationName)],
        Label = "Project",
        ToolTip = "Select the StreamBIM project to browse files from.")]
    [Required(ErrorMessage = "Project is required.")]
    public OnlineBrowserSelection Project { get; set; } = new();

    [FolderPickerField(
        Label = "Destination folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Destination folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [OnlineBrowserField(typeof(StreamBimFilesAndFoldersBrowser),
        SelectionKind = OnlineBrowserSelectionKind.FilesAndFolders,
        FavoritesKey = "streambim.project-content",
        ScopeProperties = [nameof(Project) + ".Value", nameof(ApplicationName)],
        Label = "Files/Folders to download",
        ToolTip = "Enter a project-relative file or folder path, for example \"Planning/Electrical/design.pdf\". Folder downloads include subfolders. Wildcards with * and ? are supported.",
        HelperText = "Browse the project and select one or more files or folders.")]
    [MinLength(1, ErrorMessage = "Add at least one file or folder to download.")]
    public OnlineBrowserSelections Files { get; set; } = new();

    internal string ProjectValue => Project.SelectedItem?.Value ?? string.Empty;

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}

public sealed class StreamBIMDownloaderArgsV1
{
    public string ApplicationName { get; set; } = "StreamBIM";
    public string Project { get; set; } = string.Empty;
    public string DownloadFolder { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
    public bool SkipUnchangedFiles { get; set; }
}

public sealed class StreamBIMDownloaderArgsUpgradeV1ToV2 : IArgsUpgrade<StreamBIMDownloaderArgsV1, StreamBIMDownloaderArgs>
{
    public StreamBIMDownloaderArgs Upgrade(StreamBIMDownloaderArgsV1 previousVersion)
    {
        var applicationName = previousVersion.ApplicationName ?? "StreamBIM";
        var projectPath = previousVersion.Project ?? string.Empty;
        var projectItem = CreateSelection(projectPath, OnlineBrowserItemKind.Folder, applicationName);
        var fileItems = (previousVersion.Files ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => CreateSelection(path, OnlineBrowserItemKind.File, $"{applicationName}:{projectPath}"))
            .OfType<OnlineBrowserSelectionItem>()
            .ToArray();

        return new StreamBIMDownloaderArgs
        {
            ApplicationName = applicationName,
            Project = new OnlineBrowserSelection
            {
                SelectedItem = projectItem,
                Json = JsonSerializer.Serialize(projectItem),
            },
            DownloadFolder = previousVersion.DownloadFolder,
            Files = new OnlineBrowserSelections
            {
                Items = fileItems,
                Json = JsonSerializer.Serialize(fileItems),
            },
            SkipUnchangedFiles = previousVersion.SkipUnchangedFiles,
        };
    }

    private static OnlineBrowserSelectionItem? CreateSelection(string value, OnlineBrowserItemKind kind, string scope)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var name = value.TrimEnd('/', '\\').Split('/', '\\').LastOrDefault() ?? value;
        return new OnlineBrowserSelectionItem
        {
            Id = null,
            Value = value,
            Name = name,
            Kind = kind,
            Provider = OnlineBrowserProvider.Custom,
            Scope = scope,
        };
    }
}
