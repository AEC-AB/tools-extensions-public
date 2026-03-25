
using System.ComponentModel.DataAnnotations;

namespace LoadFamily;

public class LoadFamilyArgs
{
    [OptionsField(
        Label = "Load Mode",
        ToolTip = "Choose whether to select specific family files or load all families from a directory.")]
    public LoadMode Mode { get; set; } = LoadMode.SelectFiles;

    [FilePickerField(
        Label = "Family Files",
        ToolTip = "Select one or more .rfa family files to load into the active document.",
        Hint = "Select .rfa files",
        FileExtensions = ["rfa", "*"],
        Visibility = $"{nameof(Mode)} == 'SelectFiles'")]
    public List<string> FamilyPaths { get; set; } = [];

    [FolderPickerField(
        Label = "Family Directory",
        ToolTip = "Select a directory containing .rfa family files to load.",
        Hint = "Select a folder",
        Visibility = $"{nameof(Mode)} == 'LoadFromDirectory'")]
    public string? FamilyDirectory { get; set; }

    [BooleanField(
        Label = "Include Sub-directories",
        ToolTip = "If enabled, family files in sub-directories will also be loaded.",
        Visibility = $"{nameof(Mode)} == 'LoadFromDirectory'")]
    public bool IncludeSubDirectories { get; set; }

    [BooleanField(
        Label = "Overwrite if Found",
        ToolTip = "If the family already exists in the document, overwrite it.")]
    public bool OverwriteIfFound { get; set; }

    [BooleanField(
        Label = "Overwrite if in Use",
        ToolTip = "If the family is currently in use, also overwrite it. Only applies when 'Overwrite if Found' is enabled.")]
    public bool OverwriteIfInUse { get; set; }

    [BooleanField(
        Label = "Overwrite Parameter Values",
        ToolTip = "If enabled, parameter values will be overwritten when reloading an existing family.")]
    public bool OverwriteParameterValues { get; set; }
}