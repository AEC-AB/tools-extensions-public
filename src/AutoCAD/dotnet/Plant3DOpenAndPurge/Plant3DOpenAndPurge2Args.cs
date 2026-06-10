namespace Plant3DOpenAndPurge2;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class Plant3DOpenAndPurge2Args
{
    [FilePickerField(
    Label = "Browse for File input",
    ToolTip = "Open file dialog control",
    Hint = "Select a XML project file",
    FileExtensions = ["xml", "*"])]
    public string? ProjectPath { get; set; }


    [TextField(Label = "Text input", ToolTip = "Sample tooltip")]
    [Required(ErrorMessage = "Text input is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Text input must be between 3 and 100 characters.")]
    public string TextInput { get; set; } = "Default input";

    [BooleanField(Label = "Show advanced options")]
    public bool ShowAdvanced { get; set; }

    [IntegerField(Label = "Retry count", ToolTip = "Number of retries for transient failures", MinimumValue = 0, MaximumValue = 10, Visibility = nameof(ShowAdvanced))]
    [Range(0, 10, ErrorMessage = "Retry count must be between 0 and 10.")]
    public int RetryCount { get; set; } = 3;
}
