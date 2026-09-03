namespace StreamBIMUploader;

public class StreamBIMUploaderArgs
{
    [PasswordField(
        Label = "Credential application id",
        ToolTip = "Select credentials stored in Windows Credential Manager for the specified application id.")]
    [Required(ErrorMessage = "Credential application id is required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [TextField(
        Label = "Project",
        ToolTip = "Select the StreamBIM project to upload files to. After changing this field, click Reload to refresh the file suggestions.",
        HelperText = "After selecting a project, click Reload before opening Files to upload.",
        CollectorType = typeof(StreamBIMProjectRootFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [FolderPickerField(
        Label = "Upload folder",
        ToolTip = "Select the local folder to upload files from.")]
    [Required(ErrorMessage = "Upload folder is required.")]
    public string UploadFolder { get; set; } = string.Empty;

    [ListField(
        Label = "Files to upload",
        ToolTip = "Enter one file or folder path relative to the upload folder per row. Wildcards with * and ? are supported. Type part of a path, then click Reload to refresh suggestions for that location.",
        HelperText = "This list updates only when you click Reload.",
        CollectorType = typeof(StreamBIMLocalFilesAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [MinLength(1, ErrorMessage = "Select at least one file or folder to upload.")]
    public List<string> Files { get; set; } = [];

    [TextField(
        Label = "Target folder",
        ToolTip = "Optional remote folder path inside the StreamBIM project to upload files into (e.g., 'Uploads/2024'). Leave empty to upload to the project root.")]
    public string TargetFolder { get; set; } = string.Empty;

    [BooleanField(
        Label = "Skip unchanged files",
        ToolTip = "If checked, files that already exist on StreamBIM with the same modified timestamp are skipped.")]
    public bool SkipUnchangedFiles { get; set; }
}
