namespace PrintPDF.Enums;

public enum ExportOptions
{
    [Description("Active Sheet")]
    ActiveView,
    [Description("All Sheets in Model")]
    AllSheets,
    [Description("Set Filter Rules")]
    CustomFilter,
    [Description("Select Sheet Set")]
    SheetSet,    
    [Description("Use Regex in Sheet Set")]
    UseRegexInSheetSet,
#if R2025_OR_GREATER
    [Description("Select Sheet Collection")]
    SheetCollection,
    [Description("Use Regex in Sheet Collections")]
    UseRegexInSheetCollections
#endif
}
