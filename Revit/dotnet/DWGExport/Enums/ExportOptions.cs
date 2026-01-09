namespace DWGExport.Enums;

public enum ExportOptions
{
    [Description("Active View/Sheet")]
    ActiveView,
    [Description("All Views in Model")]
    AllViews,
    [Description("All Sheets in Model")]
    AllSheets,
    [Description("Set Filter Rules")]
    CustomFilter,
    [Description("Select View/Sheet Set")]
    ViewSet,
    [Description("Use Regex In View Set")]
    UseRegexInViewSet,   
}