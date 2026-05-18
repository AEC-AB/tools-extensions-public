namespace DaluxCloudUpload;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class DaluxCloudUploadArgs
{
    [PasswordField(
        Label = "Dalux API Key",
        ToolTip = "Your Dalux API Identity key")]
    [Required(ErrorMessage = "Add organization name in the 'Name' field and Dalux API Key in the 'Password' field.")]
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

    [FilePickerField(
        Label = "File to Upload",
        Hint = "Select the file to upload",
        ToolTip = "Select the file you want to upload to Dalux")]
    [Required(ErrorMessage = "File to Upload is required.")]
    public string FilePath { get; set; } = string.Empty;

    [TextField(
        Label = "Destination Folder Path",
        ToolTip = "Folder path in Dalux (e.g., 'Files/C07_Geometry/C07_K07/Model')")]
    [Required(ErrorMessage = "Destination Folder Path is required.")]
    public string FolderPath { get; set; } = string.Empty;

    [DictionaryField(
        Label = "Metadata",
        ToolTip = "Optional key-value pairs to include as metadata with the uploaded file")]
    public Dictionary<string, string> MetaData { get; set; } = new();
}
