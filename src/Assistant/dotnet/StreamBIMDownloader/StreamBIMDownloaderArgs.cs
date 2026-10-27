using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace StreamBIMDownloader;

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
    public string Project { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Destination folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Destination folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [OnlineBrowserField(typeof(StreamBimFilesAndFoldersBrowser),
        SelectionKind = OnlineBrowserSelectionKind.FilesAndFolders,
        FavoritesKey = "streambim.project-content",
        ScopeProperties = [nameof(Project), nameof(ApplicationName)],
        Label = "Files/Folders to download",
        ToolTip = "Enter a project-relative file or folder path, for example \"Planning/Electrical/design.pdf\". Folder downloads include subfolders. Wildcards with * and ? are supported.",
        HelperText = "Browse the project and select one or more files or folders.")]
    [MinLength(1, ErrorMessage = "Add at least one file or folder to download.")]
    public List<string> Files { get; set; } = [];

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}
