using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Enums;
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

    [OptionsField(
        CollectorType = typeof(StreamBimProjectCollector),
        CollectorSortOrder = SortOrder.SortByAscending,
        TriggerProperties = [nameof(ApplicationName)],
        Label = "Project",
        ToolTip = "Select the StreamBIM project to browse files from.")]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [OnlineBrowserCopyField(typeof(StreamBimFilesAndFoldersBrowser),
        DestinationKind = OnlineBrowserDestinationKind.Folder,
        SelectionKind = OnlineBrowserSelectionKind.FilesAndFolders,
        FavoritesKey = "streambim.project-content",
        ScopeProperties = [nameof(Project), nameof(ApplicationName)],
        Label = "Download project files",
        ToolTip = "Choose a remote file, folder, or wildcard path and a local destination folder for each row.",
        HelperText = "Each row downloads its source into its own local folder. Folder downloads include subfolders.")]
    [MinLength(1, ErrorMessage = "Add at least one file or folder to download.")]
    public OnlineBrowserCopy Transfers { get; set; } = new();

    internal string ProjectValue => Project ?? string.Empty;

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
        var paths = (previousVersion.Files ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
        if (paths.Count == 0 && !string.IsNullOrWhiteSpace(previousVersion.DownloadFolder))
            paths.Add(string.Empty);
        var targets = paths.Select(path => new OnlineBrowserCopyTarget
        {
            Source = new OnlineBrowserSelectionItem
            {
                Value = path,
                Name = path.TrimEnd('/', '\\').Split('/', '\\').LastOrDefault() ?? path,
                Kind = OnlineBrowserItemKind.File,
                Provider = OnlineBrowserProvider.Custom,
                Scope = $"{previousVersion.ApplicationName}:{previousVersion.Project}",
            },
            Destination = previousVersion.DownloadFolder,
        }).ToArray();

        return new StreamBIMDownloaderArgs
        {
            ApplicationName = previousVersion.ApplicationName,
            Project = previousVersion.Project,
            Transfers = new OnlineBrowserCopy { Targets = targets, Json = JsonSerializer.Serialize(targets) },
            SkipUnchangedFiles = previousVersion.SkipUnchangedFiles,
        };
    }
}
