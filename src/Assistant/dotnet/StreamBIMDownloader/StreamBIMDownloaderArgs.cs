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
        ToolTip = "Select the StreamBIM project to browse files from.",
        CollectorType = typeof(StreamBIMProjectRootFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Download folder",
        ToolTip = "Select the local folder to download files to.")]
    [Required(ErrorMessage = "Download folder is required.")]
    public string DownloadFolder { get; set; } = string.Empty;

    [OptionsField(
        Label = "Files to download",
        ToolTip = "Paths inside the selected project. Wildcards with * and ? are supported.",
        CollectorType = typeof(StreamBIMFilesAndFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    public List<string> Files { get; set; } = [];

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist locally with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}
