namespace StreamBIMUploader;

public class StreamBIMUploaderArgs
{
    [PasswordField(
        Label = "StreamBIM Credentials",
        ToolTip = "Enter your StreamBIM credentials.")]
    [Required(ErrorMessage = "StreamBIM Credentials are required.")]
    public string ApplicationName { get; set; } = "StreamBIM";

    [TextField(
        Label = "Project",
        ToolTip = "Select the StreamBIM project to upload files to. After changing this field, click Reload to refresh the file suggestions.",
        CollectorType = typeof(StreamBIMProjectRootFolderAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [FilePickerField(
        Label = "Files to upload",
        ToolTip = "Select one or more local files to upload.")]
    public List<string> Files { get; set; } = [];

    [TextField(
        Label = "Target folder",
        ToolTip = "Optional remote folder path inside the StreamBIM project to upload files into (e.g., 'Uploads/2024'). Leave empty to upload to the project root.")]
    public string TargetFolder { get; set; } = string.Empty;

    [BooleanField(
        Label = "Verbose diagnostics",
        ToolTip = "Writes a detailed per-file upload log for troubleshooting. Enable only when needed.")]
    public bool VerboseDiagnostics { get; set; }
}
