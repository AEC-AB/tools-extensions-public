using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Fields.Revit;
using PrintPDF.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrintPDF;

public class PrintPDFArgs
{
    [FolderPickerField(
        Label = "Destination Folder",
        ToolTip = "Choose the folder where the exported PDF file(s) will be written.")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? DestinationDirectory { get; set; }

    [OptionsField(
        Label = "Choose What To Print",
        ToolTip = "Select an option on what to print")]
    public ExportOptions ExportOption { get; set; } = ExportOptions.SheetSet;   

    [FilterField(
        Label = "Set Filter Rules",
        ToolTip = """
                    Define filter rules to select sheets to print. Example: 'Sheet Name' equals 'PDF_Export'.
                    If no rules are defined, all sheets will be selected.
                    """,
        UseActiveDocument = true,
        Categories = ["Sheets"],
        DisableCategorySelection = true,
        DisableModelSelection = true,
        Visibility = $"{nameof(ExportOption)} == 'CustomFilter'",
        Hint = "Click to add filter rules")]
    public FilteredElementCollector? CustomFilter { get; set; }

    [TextField(
        Label = "Sheet Set",
        ToolTip = "Select a sheet set or Enter the name of the sheet set",
        CollectorType = typeof(ViewSetCollector),
        Visibility = $"{nameof(ExportOption)} == 'SheetSet'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? ViewSet { get; set; }

#if R2025_OR_GREATER
    [TextField(
        Label = "Sheet Collection",
        ToolTip = "Select a sheet collection or Enter the name of the sheet collection",
        CollectorType = typeof(ViewCollectionCollector),
        Visibility = $"{nameof(ExportOption)} == 'SheetCollection'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? ViewCollection { get; set; }
#endif
    [TextField(
        Label = "Regex Pattern",
        ToolTip = "Regex pattern to search in sheet set/collection",
        Visibility = $"{nameof(ExportOption)} == 'UseRegexInSheetSet' || {nameof(ExportOption)} == 'UseRegexInSheetCollections'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? RegexPattern { get; set; }

    [OptionsField(
        Label = "Naming Options",
        ToolTip = "Choose how PDF files are named: 'SheetNameOnly' uses the sheet name; other options combine prefix/suffix/separator and optional custom pattern",
        CollectorSortOrder = SortOrder.None)]
    public NamingOptions NamingOptions { get; set; } = NamingOptions.SheetNameOnly;

    [TextField(
        Label = "Separator in file Name",
        ToolTip = "Separator used between file name parts",
        Visibility =$"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string SeparatorInFileName { get; set; } = "-";

    [TextField(
        Label = "Prefix in file name (Optional)",
        ToolTip = "Text added before the file name",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? PrefixFileName { get; internal set; }

    [TextField(
        Label = "Suffix in file name (Optional)",
        ToolTip = "Text added after the file name",
        Visibility = $"{nameof(NamingOptions)} != 'CustomNamingConvention'")]
    public string? SuffixFileName { get; internal set; }

    [TextField(
        Label = "Custom Naming Convention",
        ToolTip = """
                Define a custom naming pattern for the PDF file. 
                Use place holders like {SheetName}, {ModelName}, or any View parameter name in braces (e.g., {Discipline}, {Status}). 
                Examples: '{SheetName}_{Phase}', 'PDF_{ModelName}_{Discipline}', 'Custom-{SheetName}-v1', '{Discipline}-{SheetName}'
                """,      
        Visibility = $"{nameof(NamingOptions)} == 'CustomNamingConvention'")]
    [Required(ErrorMessage = "Value can not be empty")]
    public string? CustomNamingConvention { get; set; }

    [TextField(
        Label = "",
        IsMultiline = true)]
    public string? Separator { get; } = "\nConfigure Revit Print Settings\n";

    [BooleanField(
        Label ="Combine",
        ToolTip = "Export to a single PDF instead of one per sheet/view")]
    public bool Combine { get; set; }

    [BooleanField(
        Label = "Use sheet set name as PDF Name",
        ToolTip ="Use the selected sheet set name for the combined PDF",
         Visibility = $"{nameof(Combine)} && {nameof(ExportOption)} == 'SheetSet'")]
    public bool UseSheetSetNameAsPdfName { get; set; }

    [TextField(
        Label = "Combined File Name (Optional)",
        ToolTip = "File name to use for the combined PDF",
        Visibility = $"{nameof(Combine)} && {nameof(UseSheetSetNameAsPdfName)} == 'false'")]
    public string? CombinedFileName { get; set; }

    [BooleanField(
        Label = "Always Use Raster",
        ToolTip = "Force raster processing for all content")]
    public bool AlwaysUseRaster { get; set; } = false;

    [OptionsField(
        Label = "Color Depth Type",
        ToolTip = "Choose black/white, grayscale, or color")]
    public ColorDepthType ColorDepthType { get; set; } = ColorDepthType.Color;

    [OptionsField(
        Label = "PDF Export Quality Type",
        ToolTip = "Select the PDF export DPI")]
    public PDFExportQualityType PDFExportQualityType { get; set; } = PDFExportQualityType.DPI144;

    [BooleanField(
        Label = "Hide Crop Boundaries",
        ToolTip = "Hide crop boundaries in the exported PDF")]
    public bool HideCropBoundaries { get; set; } = true;

    [BooleanField(
        Label = "Hide Reference Plane",
        ToolTip = "Hide reference planes in the exported PDF")]
    public bool HideReferencePlane { get; set; } = true;

    [BooleanField(
        Label = "Hide Scope Boxes",
        ToolTip = "Hide scope boxes in the exported PDF")]
    public bool HideScopeBoxes { get; set; } = true;

    [BooleanField(
        Label = "Hide Unreferenced View Tags",
        ToolTip = "Hide unreferenced view tags in the exported PDF")]
    public bool HideUnreferencedViewTags { get; set; } = true;

    [BooleanField(
        Label = "Mask Coincident Lines",
        ToolTip = "Mask coincident lines to improve clarity")]
    public bool MaskCoincidentLines { get; set; } = true;

    [BooleanField(
        Label = "Replace Halftone With Thin Lines",
        ToolTip = "Replace halftone with thin lines for better visibility")]
    public bool ReplaceHalftoneWithThinLines { get; set; } = true;

    [BooleanField(
        Label = "View Links In Blue",
        ToolTip = "Set view links to be displayed in blue color")]
    public bool ViewLinksInBlue { get; set; }

    [DoubleField(
        Label = "Origin Offset X",
        ToolTip = "Offset between left sides of pdf content and paper")]
    public double OriginOffsetX { get; set; }

    [DoubleField(
        Label = "Origin Offset Y",
        ToolTip = "Offset between bottom sides of pdf content and paper")]
    public double OriginOffsetY { get; set; }

    [IntegerField(
        Label = "Zoom Percentage",
        ToolTip = "Percentage of the zoom for the view")]
    public int ZoomPercentage { get; set; }

    [OptionsField(
        Label = "Paper Format",
        ToolTip = "Paper format to be used for export")]
    public ExportPaperFormat ExportPaperFormat { get; set; } = ExportPaperFormat.Default;

    [OptionsField(
        Label = "Page Orientation Type",
        ToolTip = "Paper orientation of either portrait, landscape or auto")]
    public PageOrientationType PageOrientationType { get; set; } = PageOrientationType.Landscape;

    [OptionsField(
        Label = "Paper Placement Type",
        ToolTip = "Paper placement of either center or offset from corner")]
    public PaperPlacementType PaperPlacementType { get; set; } = PaperPlacementType.Center;

    [OptionsField(
        Label = "Raster Quality Type",
        ToolTip = "Select raster output DPI")]
    public RasterQualityType RasterQualityType { get; set; } = RasterQualityType.High;

    [OptionsField(
        Label = "Zoom Type",
        ToolTip = "Zoom type of either fit to page or on a specific percentage")]
    public ZoomType ZoomType { get; set; } = ZoomType.FitToPage;

    [BooleanField(
        Label = "Open View On Export",
        ToolTip = "Open the view when exporting the PDF")]
    public bool OpenViewOnExport { get; set; }
}

#if R2025_OR_GREATER
internal class ViewCollectionCollector : IRevitAutoFillCollector<PrintPDFArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, PrintPDFArgs args)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;

            if (document is null)
                return result;

            var viewCollections = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SheetCollections)
                            .WhereElementIsNotElementType().ToElements();

            foreach (var viewCollection in viewCollections)
            {
                result.Add(viewCollection.Name, viewCollection.Name);
            }
        }
        catch
        {
            // ignore
        }

        return result;
    }
}
#endif

internal class ViewSetCollector : IRevitAutoFillCollector<PrintPDFArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, PrintPDFArgs args)
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