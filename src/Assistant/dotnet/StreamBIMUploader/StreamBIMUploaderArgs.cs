using CW.Assistant.Extensions.Contracts.Browsing;
using CW.Assistant.Extensions.Contracts.Controls;
using CW.Assistant.Extensions.Contracts.Upgrade;

namespace StreamBIMUploader;

[ArgsVersion(2)]
public class StreamBIMUploaderArgs
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
        ToolTip = "Browse StreamBIM projects and select the project to upload files to.")]
    [Required(ErrorMessage = "Project is required.")]
    public OnlineBrowserSelection Project { get; set; } = new();

    [FilePickerField(
        Label = "Files to upload",
        ToolTip = "Select one or more local files to upload.")]
    public List<string> Files { get; set; } = [];

    [OnlineBrowserField(typeof(StreamBimTargetFolderBrowser),
        SelectionKind = OnlineBrowserSelectionKind.Folders,
        FavoritesKey = "streambim.project-content",
        ScopeProperties = [nameof(Project) + ".Value", nameof(ApplicationName)],
        Label = "Target folder",
        ToolTip = "Remote folder path inside the StreamBIM project to upload files into (e.g., 'Uploads/2024'). Leave empty to upload to the project root.",
        HelperText = "Browse for a destination folder, or clear the path to use the project root.")]
    public OnlineBrowserSelection TargetFolder { get; set; } = new();

    internal string ProjectValue => Project?.SelectedItem?.Value ?? string.Empty;
    internal string TargetFolderValue => TargetFolder?.SelectedItem?.Value ?? string.Empty;

    [BooleanField(
        Label = "Verbose diagnostics",
        ToolTip = "Writes a detailed per-file upload log for troubleshooting. Enable only when needed.")]
    public bool VerboseDiagnostics { get; set; }
}
