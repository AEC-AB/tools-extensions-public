using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CW.Assistant.Extensions.Contracts.Enums;
using CW.Assistant.Extensions.Contracts.Fields;

namespace StreamBIMDownloader;

public class StreamBIMDownloaderArgs
{
    [PasswordField(
        Label = "StreamBIM Credentials",
        ToolTip = "Enter your StreamBIM credentials.")]
    [Required(ErrorMessage = "StreamBIM Credentials are required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [TextField(
        Label = "Project",
        ToolTip = "Select the StreamBIM project to browse files from. After changing this field, click Reload to refresh the file suggestions.",
        CollectorType = typeof(StreamBIMProjectRootFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Destination folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Destination folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [ListField(
        Label = "Files/Folders to download",
        ToolTip = "Enter a project-relative file or folder path, for example \"Planning/Electrical/design.pdf\". Folder downloads include subfolders. Wildcards with * and ? are supported.",
        HelperText = "Enter part of a file or folder path and click Reload to see matching StreamBIM suggestions.",
        CollectorType = typeof(StreamBIMFilesAndFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [MinLength(1, ErrorMessage = "Add at least one file or folder to download.")]
    public List<string> Files { get; set; } = [];

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}
