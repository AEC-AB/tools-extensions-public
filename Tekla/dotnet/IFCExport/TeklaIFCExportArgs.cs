using CW.Assistant.Extensions.Contracts.Fields;

namespace TeklaIFCExport;

/// <summary>
/// Represents the inputs to the Tekla IFC Export extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class TeklaIFCExportArgs
{
    [FilePickerField(
        Label = "IFC Export Config File",
        ToolTip = "Path to the IFC export configuration XML file",
        Hint = "Select a IFC export config file",
        FileExtensions = ["xml","*"])]
    public string? ExportConfigFilePath { get; set; }

    [SaveFileField(
        Label = "Output File Override",
        Hint = "Optional: Override the output file path from the config",
        ToolTip = "Optional: Specify a custom output IFC file path",
        FileExtensions = ["ifc","*"])]
    public string? FilePathOverride { get; set; }

    [TextField(
        Label = "Base Point Name",
        ToolTip = "The base point to use for export (or 'Model Origin')")]
    public string BasePointName { get; set; } = "Model Origin";
}