namespace DaluxCloudDownload;

public class DaluxCloudDownloadArgs
{
    [PasswordField(
        Label = "Dalux API Key",
        ToolTip = "Add organization name in the 'Name' field and Dalux API Key in the 'Password' field.")]
    [Required(ErrorMessage = "Dalux API Key is required.")]
    public string ApiKey { get; set; } = "Dalux API Key";

    [TextField(
        Label = "API Base URL",
        ToolTip = "Base URL for Dalux API (provided by Dalux support)")]
    [Required(ErrorMessage = "API Base URL is required.")]
    public string BaseUrl { get; set; } = "https://node1.field.dalux.com/service/api";

    [TextField(
        Label = "Project Name",
        ToolTip = "The Dalux project name to query")]
    [Required(ErrorMessage = "Project Name is required.")]
    public string ProjectName { get; set; } = string.Empty;

    [ListField(
        Label = "Download Files/Folders",
        ToolTip = "Dalux file or folder paths. For files: 'Files/Path/file.ifc'. For folders: 'Files/Path' (all files and subfolders are downloaded with structure preserved)")]
    [Required(ErrorMessage = "At least one Dalux File or Folder Path is required.")]
    public List<string> FilePaths { get; set; } = new ();

    [FolderPickerField(
        Label = "Output Folder",
        Hint = "Destination folder path",
        ToolTip = "The local folder where downloaded files and folders will be saved")]
    [Required(ErrorMessage = "Output Folder is required.")]
    public string OutputFolder { get; set; } = string.Empty;
}
