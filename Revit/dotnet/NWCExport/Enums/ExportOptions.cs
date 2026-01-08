using System.ComponentModel;

namespace NWCExport.Enums;

public enum ExportOptions
{
    [Description("Use Active View")]
    UseActiveView,
    [Description("All 3D Views in Model")]
    AllViews,
    [Description("Set Filter Rules")]
    CustomFilter,
    [Description("Select View Set")]
    ViewSet,
    [Description("Use Regex In View Set")]
    UseRegexInViewSet,
}