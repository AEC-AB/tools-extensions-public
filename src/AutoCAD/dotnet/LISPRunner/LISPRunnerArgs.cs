namespace LISPRunner;

public enum LispRunnerMode
{
	SelectFile,
	Inline,
}

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class LISPRunnerArgs
{
	[ChoiceField(Label = "Mode")]
	public LispRunnerMode Mode { get; set; } = LispRunnerMode.SelectFile;

	[TextField(
		Label = "Inline Lisp Script",
		ToolTip = "Enter the Lisp script to execute directly in AutoCAD.",
		Hint = "Paste or type Lisp code",
		IsMultiline = true,
		MinLines = 10,
		MaxLines = 18,
		Visibility = $"{nameof(Mode)} == '{nameof(LispRunnerMode.Inline)}'")]
	public string InlineScript { get; set; } = string.Empty;

	[FilePickerField(
		Label = "Lisp Script",
		FileExtensions = ["*","lsp"],
		ToolTip = "Select the Lisp script file to load in AutoCAD.",
		Hint = "Select a Lisp script file",
		Visibility = $"{nameof(Mode)} == '{nameof(LispRunnerMode.SelectFile)}'")]
	public string LispScriptPath { get; set; } = string.Empty;
}
