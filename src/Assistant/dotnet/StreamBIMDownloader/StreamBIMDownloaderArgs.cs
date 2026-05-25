using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CW.Assistant.Extensions.Contracts.Enums;
using CW.Assistant.Extensions.Contracts.Fields;

namespace StreamBIMDownloader;

public class StreamBIMDownloaderArgs
{
    [PasswordField(
        Label = "Credential application id",
        ToolTip = "Select credentials stored in Windows Credential Manager for the specified application id.")]
    [Required(ErrorMessage = "Credential application id is required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [TextField(
        Label = "Project",
        ToolTip = "Select the StreamBIM project to browse files from. After changing this field, click Reload to refresh the file suggestions.",
        HelperText = "After selecting a project, click Reload before opening Files to download.",
        CollectorType = typeof(StreamBIMProjectRootFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Download folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Download folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [ListField(
        Label = "Files to download",
        ToolTip = "Enter one project-relative file or folder path per row. Wildcards with * and ? are supported. Type part of a path, then click Reload to refresh suggestions for that location.",
        HelperText = "This list updates only when you click Reload. If the first suggestion tells you to select a folder ending with /, choose that folder and click Reload again to browse deeper.",
        CollectorType = typeof(StreamBIMFilesAndFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    public List<string> Files { get; set; } = [];

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}
