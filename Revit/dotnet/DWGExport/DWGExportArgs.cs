using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Fields.Revit;
using DWGExport.Enums;
using System.ComponentModel.DataAnnotations;

namespace DWGExport;

public class DWGExportArgs
{
    [FolderPickerField(
        Label = "Destination Folder",
        ToolTip = "Select the folder where the exported DWG file(s) will be saved.")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? DestinationDirectory { get; set; }

    [OptionsField(
        Label = "Choose What To Export",
        ToolTip = "Select an option on what to export.")]
    public ExportOptions ExportOption { get; set; } = ExportOptions.AllSheets;

    [FilterField(
        Label = "Set Filter Rules",
                ToolTip = """
                    Define filter rules to select views/sheets to export. Example: 'View Name' equals 'DWG_Export'.
                    If no rules are defined, all views and sheets will be selected.
                    """,
        UseActiveDocument = true,
        Categories = ["Views", "Sheets"],
        DisableCategorySelection = true,
        DisableModelSelection = true,
        Visibility = $"{nameof(ExportOption)} == 'CustomFilter'",
        Hint = "Click to add filter rules")]
    public FilteredElementCollector? CustomFilter { get; set; }

    [TextField(
        Label = "View/Sheet Set",
        ToolTip = "Select a view/sheet set or Enter the name of the view/sheet set.",
        CollectorType = typeof(ViewSetCollector),
        Visibility = $"{nameof(ExportOption)} == 'ViewSet'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? ViewSet { get; set; }

    [TextField(
        Label = "Regex Pattern",
        ToolTip = "Regular expression used to match view/sheet set names (e.g., '^DWG_.*').",
        Visibility = $"{nameof(ExportOption)} == 'UseRegexInViewSet'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? RegexPattern { get; set; }

    [OptionsField(
        Label = "Naming Options",
        ToolTip = """
        Choose how exported files are named. 
        'ViewNameOnly' uses the view name; other options can add prefix/suffix, separators, and an optional custom pattern.
        """)]
    public NamingOptions NamingOptions { get; set; } = NamingOptions.ViewNameOnly;

    [TextField(
        Label = "Separator in File Name",
        ToolTip = "Text used between name parts when building the file name (for example: '-' produces ViewName-Discipline).",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string SeparatorInFileName { get; set; } = "-";

    [TextField(
        Label = "Prefix in file name (Optional)",
        ToolTip = "Optional text added at the beginning of each exported DWG file name.",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? PrefixFileName { get; internal set; }

    [TextField(
        Label = "Suffix in file name (Optional)",
        ToolTip = "Optional text added at the end of each exported DWG file name (before the .dwg extension).",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? SuffixFileName { get; internal set; }

    [TextField(
        Label = "Custom Naming Convention",
        ToolTip = """
                Define a custom naming pattern for the DWG file. 
                Use place holders like {ViewName}, {ModelName}, or any View parameter name in braces (e.g., {Discipline}, {Status}). 
                Examples: '{ViewName}_{Phase}', 'DWG_{ModelName}_{Discipline}', 'Custom-{ViewName}-v1', '{Discipline}-{ViewName}'
                """, 
        Visibility = $"{nameof(NamingOptions)} == 'CustomNamingConvention'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? CustomNamingConvention { get; set; }

    [TextField(
        Label = "",
        IsMultiline = true)]
    public string? Separator { get; } = "\nConfigure Revit DWG Settings\n";

    [OptionsField(
        Label = "ACA Preference",
        ToolTip = "Choose how ACA (AutoCAD Architecture) objects are exported (as objects or as raw geometry).")]
    public ACAObjectPreference ACAPreference { get; internal set; } = ACAObjectPreference.Object;

    [OptionsField(
        Label = "Colors",
        ToolTip = "Select how colors are written to DWG (for example indexed colors vs. true colors).")]
    public ExportColorMode Colors { get; internal set; } = ExportColorMode.IndexColors;

    [OptionsField(
        Label = "Export Of Solids",
        ToolTip = "Choose how 3D solid geometry is exported (applies to 3D views).")]
    public SolidGeometry ExportOfSolids { get; internal set; } = SolidGeometry.Polymesh;

    [OptionsField(
        Label = "File Version",
        ToolTip = "Select the DWG/DXF file format version to export.")]
    public ACADVersion FileVersion { get; internal set; } = ACADVersion.R2013;

    [OptionsField(
        Label = "Export Unit",
        ToolTip = "Select the unit used when writing geometry to the DWG (use Default to follow project/export defaults).")]
    public ExportUnit Values { get; internal set; } = ExportUnit.Default;

    [OptionsField(
        Label = "Line Scaling ",
        ToolTip = "Select how line patterns are scaled (for example by view scale).")]
    public LineScaling LineScaling { get; internal set; } = LineScaling.ViewScale;

    [OptionsField(
        Label = "Export layer options",
        ToolTip = "Select how categories/graphics are mapped to DWG layers during export.")]
    public PropOverrideMode PropOverrideMode { get; internal set; } = PropOverrideMode.ByEntity;

    [OptionsField(
        Label = "Target Unit",
        ToolTip = "Select the target unit for the exported DWG. Use Default to keep the standard export behavior.")]
    public ExportUnit TargetUnit { get; internal set; } = ExportUnit.Default;

    [OptionsField(
        Label = "Text Treatment",
        ToolTip = "Select how Revit text is translated to DWG text entities (for example exact vs. approximated).")]
    public TextTreatment TextTreatment { get; internal set; } = TextTreatment.Exact;

    [BooleanField(
        Label = "Export Areas",
        ToolTip = "If enabled, area and room boundaries/geometry will be included in the DWG export.")]
    public bool ExportingAreas { get; internal set; }

    [BooleanField(
        Label = "Hide Reference Plane",
        ToolTip = "If enabled, reference planes will be hidden in the exported DWG.")]
    public bool HideReferencePlane { get; internal set; } = true;

    [BooleanField(
        Label = "Hide Scope Box",
        ToolTip = "If enabled, scope boxes will be hidden in the exported DWG.")]
    public bool HideScopeBox { get; internal set; }

    [BooleanField(
        Label = "Hide Unreferenced View Tags",
        ToolTip = "If enabled, view tags that are not referenced will be hidden in the exported DWG.")]
    public bool HideUnreferenceViewTags { get; internal set; } = true;

    [BooleanField(
        Label = "Merge Views",
        ToolTip = "If enabled, views are exported into a single DWG using external references (XRefs).")]
    public bool MergedViews { get; internal set; } = true;

    [BooleanField(
        Label = "Preserve Coincident Lines",
        ToolTip = "If enabled, coincident/overlapping lines will be preserved instead of being merged or removed.")]
    public bool PreserveCoincidentLines { get; internal set; }

    [BooleanField(
        Label = "Shared Coordinate",
        ToolTip = "If enabled, the export uses the shared coordinate origin; otherwise it uses the project internal origin.")]
    public bool SharedCoords { get; internal set; } = true;

    [BooleanField(
        Label = "Use Hatch Background Color",
        ToolTip = "If enabled, hatch patterns will be exported with their background color (if any).")]
    public bool UseHatchBackgroundColor { get; internal set; }

    [BooleanField(
        Label = "Mark Nonplot Layers",
        ToolTip = "If enabled, layers whose names contain the specified nonplot suffix will be marked as non-plot in the exported DWG.")]
    public bool MarkNonplotLayers { get; internal set; }

    [TextField(
        Label = "Nonplot Suffix",
        ToolTip = "If the MarkNonplotLayers attribute is set to true, all layers with names containing this suffix will be marked as non-plot. No action will be performed if the suffix is empty.",
        Visibility = nameof(MarkNonplotLayers))]
    public string? NonplotSuffix { get; set; }
}

internal class ViewSetCollector : IRevitAutoFillCollector<DWGExportArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, DWGExportArgs args)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;
            if (document is null)
                return result;

            var viewSets = new FilteredElementCollector(document)
                .OfClass(typeof(ViewSheetSet))
                .ToElements();

            foreach (var viewSet in viewSets)
            {
                // Use name as both key and value for UI display/selection
                result[viewSet.Name] = viewSet.Name;
            }
        }
        catch
        {
            // Intentionally ignore errors when auto-filling; UI can still function without these values.
        }

        return result;
    }
}