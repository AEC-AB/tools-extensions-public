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

    [OptionsField(
        CollectorType = typeof(StreamBimProjectCollector),
        CollectorSortOrder = SortOrder.SortByAscending,
        TriggerProperties = [nameof(ApplicationName)],
        Label = "Project",
        ToolTip = "Select the StreamBIM project to upload files to.")]
    [Required(ErrorMessage = "Project is required.")]
    public string Project { get; set; } = string.Empty;

    [OnlineBrowserCopyField(typeof(StreamBimTargetFolderBrowser),
        Direction = OnlineBrowserCopyDirection.LocalToRemote,
        AllowEmptyDestination = true,
        SelectionKind = OnlineBrowserSelectionKind.Folders,
        FavoritesKey = "streambim.project-content",
        ScopeProperties = [nameof(Project), nameof(ApplicationName)],
        Label = "Upload project files",
        ToolTip = "Choose a local source file and a StreamBIM target folder for each row.",
        HelperText = "Leave a target folder empty to upload that row to the project root.")]
    [MinLength(1, ErrorMessage = "Add at least one file to upload.")]
    public OnlineBrowserCopy Transfers { get; set; } = new();

    internal string ProjectValue => Project ?? string.Empty;

    [BooleanField(
        Label = "Verbose diagnostics",
        ToolTip = "Writes a detailed per-file upload log for troubleshooting. Enable only when needed.")]
    public bool VerboseDiagnostics { get; set; }
}
