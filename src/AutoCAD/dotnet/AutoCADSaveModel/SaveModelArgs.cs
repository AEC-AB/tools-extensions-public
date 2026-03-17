namespace SaveModel;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class SaveModelArgs
{
    [Description("Save with new name"), ControlData(ToolTip = "Check this to save with a new name")]
    public bool SaveWithNewName { get; set; }

    [Description("Save path"), ControlData(ToolTip = "The path to save the file to. This is only used if Save with new name is checked")]
    [ControlType(ControlType.Save)]
    [FileExtension("dwg")]
    [FileExtension("*")]
    public string? SavePath { get; set; }
}