namespace IdsValidator;

public class IdsValidatorArgs
{
    [FilePickerField(Label = "IFC file", FileExtensions = ["ifc"], ToolTip = "Select one IFC model. No project-specific filename or entity restrictions apply.")]
    [Required(ErrorMessage = "Select an IFC file.")]
    public string IfcPath { get; set; } = "";
    [FilePickerField(Label = "IDS file", FileExtensions = ["ids"], ToolTip = "Select the IDS requirements document. The standard version is detected from its schema declaration.")]
    [Required(ErrorMessage = "Select an IDS file.")]
    public string IdsPath { get; set; } = "";
    [FolderPickerField(Label = "Report folder", ToolTip = "Select a writable folder for Markdown, JSON, HTML and BCF reports.")]
    [Required(ErrorMessage = "Select a report folder.")]
    public string OutputDirectory { get; set; } = "";
}
