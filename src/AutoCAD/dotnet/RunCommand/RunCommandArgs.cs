using CW.Assistant.Extensions.Contracts.Fields;
namespace RunCommand;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class RunCommandArgs
{
   [TextField(
        Label = "Commands",
        IsMultiline = true,
        MinLines = 3,
        MaxLines = 10)]
    public string? Commands { get; set; }
}