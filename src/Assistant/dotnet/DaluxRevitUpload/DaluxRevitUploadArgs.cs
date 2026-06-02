namespace DaluxRevitUpload;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class DaluxRevitUploadArgs
{
    [TextField(Label = "Revit Process ID", ToolTip = "Process ID of the target Revit instance. Set by the 'revitprocessid' variable from the Python script.")]
    [Required(ErrorMessage = "Revit Process ID is required. Please check if the Python script is set before the extension")]
    public string RevitProcessId { get; set; } = "${{ revitprocessid }}";

    [TextField(Label = "Target Filename", ToolTip = "The exact filename to process (e.g., I90_BBH_A6_B72_K07_M00_F2_N001)")]
    [Required(ErrorMessage = "Target Filename is required.")]
    public string TargetFilename { get; set; } = string.Empty;

    [DoubleField(Label = "Revision Increment", ToolTip = "Optional. If the Revision column is found in Dalux, its current value will be incremented by this amount. Set to 0 to skip.")]
    public double RevisionIncrement { get; set; } = 0.1;

    [BooleanField(Label = "Trigger upload", ToolTip = "When enabled, clicks the Upload button and waits for the Done confirmation after all fields are filled.")]
    public bool TriggerUpload { get; set; } = false;

    [DictionaryField(Label = "Dalux Fields", ToolTip = "Map column header names to their target values. Field type is detected automatically. For date picker columns, enter any recognizable date (e.g., 10 Jun 2026, 10/06/2026, 2026-06-10).")]
    public Dictionary<string, string> ColumnFields { get; set; } = new Dictionary<string, string>();
}
