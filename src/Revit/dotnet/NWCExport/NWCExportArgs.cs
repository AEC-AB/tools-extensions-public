using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Fields.Revit;
using NWCExport.Enums;
using System.ComponentModel.DataAnnotations;

namespace NWCExport;

public class NWCExportArgs
{
    [FolderPickerField(
        Label = "Destination Folder",
        ToolTip = "Select the folder where the exported NWC file(s) will be written.")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? DestinationDirectory { get; set; }

    [OptionsField(
        Label = "Choose What To Export",
        ToolTip = "Select an option on what to export.")]
    public ExportOptions ExportOption { get; set; } = ExportOptions.UseActiveView;

    [FilterField(
        Label = "Set Filter Rules",
        ToolTip = """
                    Define filter rules to select views to export. Example: 'View Name' equals 'NWC_Export'.
                    If no rules are defined, all views will be selected.
                    """,
        UseActiveDocument = true,
        Categories = ["Views"],
        DisableCategorySelection = true,
        DisableModelSelection = true,
        Visibility = $"{nameof(ExportOption)} == 'CustomFilter'",
        Hint = "Click to add filter rules")]
    public FilteredElementCollector? ViewFilterControl { get; set; }

    [TextField(
        Label = "View Set",
        ToolTip = "Select a view set or Enter the name of the view set.",
        CollectorType = typeof(ViewSetCollector),
        Visibility = $"{nameof(ExportOption)} == 'ViewSet'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? ViewSet { get; set; }

    [TextField(
        Label = "Regex Pattern",
        ToolTip = "Regular expression used to match view set names (e.g., '^NWC_.*').",
        Visibility = $"{nameof(ExportOption)} == 'UseRegexInViewSet'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? RegexPattern { get; set; }

    [OptionsField(
        Label = "Naming Options",
        ToolTip = """
                Choose how exported files are named. 
                'ViewNameOnly' uses the view name; other options can combine prefix/suffix/separator and an optional custom pattern.     
                """)]
    public NamingOptions NamingOptions { get; set; } = NamingOptions.ViewNameOnly;

    [TextField(
        Label = "Separator in File Name",
        ToolTip = "Text inserted between name parts when building the file name (e.g., '-' produces 'ViewName-Discipline').",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string SeparatorInFileName { get; set; } = "-";

    [TextField(
        Label = "Prefix in file name (Optional)",
        ToolTip = "Optional text added at the beginning of the exported file name.",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? PrefixFileName { get; set; }

    [TextField(
        Label = "Suffix in file name (Optional)",
        ToolTip = "Optional text added at the end of the exported file name (before the extension).",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? SuffixFileName { get; set; }

    [TextField(
        Label = "Custom Naming Convention",
        ToolTip = """
                Define a custom naming pattern for the NWC file. 
                Use place holders like {ViewName}, {ModelName}, or any View parameter name in braces (e.g., {Discipline}, {Status}). 
                Examples: '{ViewName}_{Phase}', 'NWC_{ModelName}_{Discipline}', 'Custom-{ViewName}-v1', '{Discipline}-{ViewName}'
                """,
        Visibility = $"{nameof(NamingOptions)} == 'CustomNamingConvention'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? CustomNamingConvention { get; set; }

    [TextField(
        Label = "",
        IsMultiline = true)]
    public string? Separator { get; } = "\nConfigure Revit NWC Settings\n";

    [OptionsField(
        Label = "Parameters",
        ToolTip = "Controls how Revit parameters are exported to Navisworks (parameter conversion mode).")]
    public NavisworksParameters Parameters { get; internal set; } = NavisworksParameters.All;

    [OptionsField(
        Label = "Navisworks Coordinates",
        ToolTip = "Select the coordinate system used for export (e.g., Shared, Project Internal).")]
    public NavisworksCoordinates Coordinate { get; internal set; } = NavisworksCoordinates.Shared;

    [BooleanField(
        Label = "Convert Element Properties",
        ToolTip = "Include element properties in the exported NWC.")]
    public bool ConvertElementProperties { get; internal set; } = true;

    [BooleanField(
        Label = "Find Missing Materials",
        ToolTip = "Attempt to resolve materials that would otherwise be missing from the export.")]
    public bool FindMissingMaterials { get; internal set; }

    [BooleanField(
        Label = "Divide File Into Levels",
        ToolTip = "Split the export into levels.")]
    public bool DivideFileIntoLevels { get; internal set; }

    [BooleanField(
        Label = "Export ElementIds",
        ToolTip = "Include Revit element IDs in the exported NWC.")]
    public bool ExportElementIds { get; internal set; } = true;

    [BooleanField(
        Label = "Export Links",
        ToolTip = "Include linked Revit models found in the main model.")]
    public bool ExportLinks { get; internal set; }

    [BooleanField(
        Label = "Export Parts",
        ToolTip = "Include Revit Parts in the export.")]
    public bool ExportParts { get; internal set; }

    [BooleanField(
        Label = "Export Room As Attribute",
        ToolTip = "Export room data as a single shared room attribute per element.")]
    public bool ExportRoomAsAttribute { get; internal set; }

    [BooleanField(
        Label = "Export Room Geometry",
        ToolTip = "Include room geometry in the exported NWC.")]
    public bool ExportRoomGeometry { get; internal set; }

    [BooleanField(
        Label = "Export Urls",
        ToolTip = "Export URL parameters.")]
    public bool ExportUrls { get; internal set; }

#if R2020_OR_GREATER
    [DoubleField(
        Label = "Faceting Factor",
        ToolTip = "Controls curved-geometry tessellation. Higher values create smoother curves but increase file size and export time (valid range: 0.1 to 10.0).")]
    public double FacetingFactor { get; set; } = 1;

    [BooleanField(
        Label = "Convert Lights",
        ToolTip = "Include light elements in the export.")]
    public bool ConvertLights { get; set; }

    [BooleanField(
        Label = "Convert Linked CAD Formats",
        ToolTip = "Include linked CAD files in the export.")]
    public bool ConvertLinkedCADFormats { get; set; }
#endif

}


internal class ViewSetCollector : IRevitAutoFillCollector<NWCExportArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, NWCExportArgs args)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;

            if (document is null)
                return result;

            var viewSets = new FilteredElementCollector(document).OfClass(typeof(ViewSheetSet)).ToElements();

            foreach (var viewSet in viewSets)
            {
                result.Add(viewSet.Name, viewSet.Name);
            }
        }
        catch
        {
            // ignore
        }
        return result;
    }
}