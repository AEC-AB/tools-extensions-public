namespace DWGExport.Enums;

public enum NamingOptions
{
    [Description("View Name Only")]
    ViewNameOnly,
    [Description("Model Name Only")]
    ModelNameOnly,
    [Description("Model Name And View Name")]
    ModelNameAndViewName,
    [Description("View Name And Model Name")]
    ViewNameAndModelName,
    [Description("Custom Naming Convention")]
    CustomNamingConvention
}