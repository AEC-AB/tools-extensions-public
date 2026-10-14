//-----------------------------------------------------------------------------
// RenameProjectEntitiesArgs.cs
//
// This file defines the input parameters for the RenameProjectEntities.
// The properties defined here become UI controls in the Assistant application
// when configuring and running this extension.
//
// DEVELOPER GUIDE:
// 1. Add properties with appropriate attributes to create UI controls
// 2. Use attribute decorations to customize the appearance and behavior
// 3. Set default values where appropriate
//-----------------------------------------------------------------------------

namespace RenameProjectEntities;

public enum SearchScope
{
    Everything,
    NamesOnly,
    ParameterValuesOnly,
    ParameterNamesOnly,
    Custom
}

public enum MatchMode
{
    Exact,
    Contains,
    StartsWith,
    EndsWith,
    Regex
}

/// <summary>
/// Represents the inputs to the RenameProjectEntities.
/// This class defines all parameters that can be configured by users through the UI.
/// Each property will be transformed into a corresponding UI control in the Assistant application.
/// </summary>
/// <remarks>
/// To add new inputs:
/// 1. Define a public property with getter and setter
/// 2. Add appropriate attributes like [Description], [Required], etc.
/// 3. Set a sensible default value if appropriate
/// 
/// Common attributes include:
/// - [TextField(Label = "Label")] - Sets the visible label in the UI
/// - [TextField(ToolTip = "Help text")] - Adds tooltip help text
/// - [Required] and [Range] - Adds value validation
/// - Visibility = nameof(ShowAdvanced) - Conditional visibility based on another field
/// - [ControlType(ControlType.ComboBox)] - Sets a specific control type
/// - [RevitAutoFill(RevitAutoFillSource.Categories)] - Adds Revit data auto-fill
/// - [ValueCopyCollector(typeof(ValueCopyRevitCollector))] - Enables value copy functionality
/// </remarks>
public class RenameProjectEntitiesArgs
{
    /// <summary>
    /// A basic text input example.
    /// </summary>
    /// <remarks>
    /// This is a simple example of a string property that creates a text input field.
    /// The [TextField] attribute sets the visible label in the UI and provides tooltip text.
    /// Validation attributes enforce allowed input ranges and required values.
    /// </remarks>
    [TextField(Label = "Find", ToolTip = "Find text to search for in project entities")]
    [Required(ErrorMessage = "Find text is required.")]
    public string Find { get; set; } = string.Empty;

    [TextField(Label = "Replace", ToolTip = "Replace text to substitute in project entities")]
    [Required(ErrorMessage = "Replace text is required.")]
    public string Replace { get; set; } = string.Empty;

    [OptionsField(Label = "Search Scope", ToolTip = "What category of entities to rename")]
    public SearchScope SearchScope { get; set; } = SearchScope.Everything;

    [OptionsField(Label = "Match Mode", ToolTip = "How the find text is matched")]
    public MatchMode MatchMode { get; set; } = MatchMode.Contains;

    [BooleanField(Label = "Match Case", ToolTip = "Perform a case-sensitive search")]
    public bool MatchCase { get; set; }

    [BooleanField(Label = "Use Regex", ToolTip = "Treat Find as a regular expression (disables Match Mode)")]
    public bool UseRegex { get; set; }

    [BooleanField(Label = "Preview Only", ToolTip = "Show what would be renamed without making changes")]
    public bool PreviewMode { get; set; }

    // --- Custom scope toggles (visible when SearchScope == Custom) ---

    [BooleanField(Label = "Element & Type Names", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeElementNames { get; set; } = true;

    [BooleanField(Label = "Parameter Values", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeParameterValues { get; set; } = true;

    [BooleanField(Label = "Parameter Names", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeParameterNames { get; set; } = true;

    [BooleanField(Label = "Views, Sheets & Schedules", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeViews { get; set; } = true;

    [BooleanField(Label = "Families & Family Symbols", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeFamilies { get; set; } = true;

    [BooleanField(Label = "Materials", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeMaterials { get; set; } = true;

    [BooleanField(Label = "Project Info", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeProjectInfo { get; set; } = true;

    [BooleanField(Label = "Levels, Grids & Reference Planes", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeLevelsGrids { get; set; } = true;

    [BooleanField(Label = "Worksets", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludeWorksets { get; set; } = true;

    [BooleanField(Label = "Phases", Visibility = $"{nameof(SearchScope)} == '{nameof(SearchScope.Custom)}'")]
    public bool IncludePhases { get; set; } = true;
}
