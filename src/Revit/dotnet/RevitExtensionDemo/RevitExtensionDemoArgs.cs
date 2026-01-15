using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts.Fields.Revit;
using RevitExtensionDemo.Collectors;
using RevitExtensionDemo.Enums;
using System.ComponentModel.DataAnnotations;

namespace RevitExtensionDemo;

public class RevitExtensionDemoArgs
{
    [Authorization(Login.Autodesk)]
    [BaseUrl("https://developer.api.autodesk.com/")]
    public IExtensionHttpClient? AutodeskClient { get; set; }

    [TextField(
        Label = "TextBox with Revit AutoComplete",
        ToolTip = "TextBox control with Phases in active Revit file as auto complete sorted by ascending order.")]
    [RevitAutoFill(RevitAutoFillSource.Phases, SortOrder = SortOrder.SortByAscending)]
    public string? TextBoxWithAutoComplete { get; set; }

    [ListField(
        Label = "List of Sheet Numbers",
        ToolTip = "List of strings with autocomplete populated from Sheet Numbers in the active Revit document.",
        MaxHeight = 200)]
    [RevitAutoFill(RevitAutoFillSource.ByCustomFilter, RevitType = typeof(View), RevitBuiltInCategory = "OST_Sheets", WhereElementIsType = false, ParameterName = "Sheet Number")]
    public List<string> SheetNumbersList { get; set; } = [];

    [OptionsField(
        Label = "Revit Categories ListBox",
        ToolTip = "ListBox control with CompactMode displaying Revit categories as element IDs.",
        CompactMode = true)]
    [RevitAutoFill(RevitAutoFillSource.Categories)]
    public List<int> RevitCategories { get; set; } = [];

    [DictionaryField(
        Label = "Family and Type Dictionary",
        ToolTip = "Dictionary control populated with Revit family and type combinations sorted in descending order.")]
    [RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByDescending)]
    public Dictionary<string, string> FamilyTypeDictionary { get; set; } = [];

    [OptionsField(
        Label = "Element Id from Family and Type",
        ToolTip = "When the datatype is int and the control has an autofill source, you will get the ElementId of the selected element.")]
    [RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByAscending)]
    public int ElementId { get; set; }

    [OptionsField(
        Label = "Element UniqueId from Family and Type",
        ToolTip = "When the datatype is string, controltype is ComboBox and the control has an autofill source, you will get the UniqueId of the selected element.")]
    [RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByAscending)]
    public string? ElementUniqueId { get; set; }

    [FilterField(
        Label = "Filtered Element Collector",
        ToolTip = "Control to define a selection filter for elements in the active Revit document.",
        UseActiveDocument = true)]
    public FilteredElementCollector? FilterControl { get; set; }

    [FilterField(
        Label = "Filtered Element Collector with Selected Categories",
        ToolTip = "Control to define a selection filter for elements in the active Revit document, restricted to Walls and Doors.",
        UseActiveDocument = true,
        DisableCategorySelection = true,
        Categories = ["Walls", "Doors"])]
    public FilteredElementCollector? FilterControlWithSelectedCategories { get; set; }

    [FilterField(
        Label = "Filtered Element Collector for Multiple Documents",
        ToolTip = "Control to define a selection filter for elements in multiple Revit documents.")]
    public Dictionary<Document, FilteredElementCollector>? FilterControlMultipleDocuments { get; set; }

    [ElementSelectorField(
        Label = "Selected Element Id from Revit",
        ToolTip = "The ElementId of the currently selected element in Revit.")]
    public ElementId? SelectedElementId { get; set; }

    [ElementSelectorField(
        Label = "Selected Element Ids from Revit",
        ToolTip = "The ElementIds of the currently selected elements in Revit.")]
    public List<ElementId>? SelectedElementIds { get; set; }

    [OptionsField(
        Label = "Custom Revit AutoFill Collector",
        ToolTip = "ComboBox control with custom Revit-specific autofill data collector implementation.",
        CollectorType = typeof(CustomRevitAutoFillCollector))]
    public string? CustomRevitCollector { get; set; }

    [OptionsField(
        Label = "Custom AutoFill Collector",
        ToolTip = "ComboBox control with custom autofill data collector implementation.",
        CollectorType = typeof(CustomRevitAutoFillCollector),
        CollectorSortOrder = SortOrder.SortByAscending)]
    public string? CustomCollector { get; set; }

    [ValueCopyField(
        Label = "Value Copy from Revit Elements",
        ToolTip = """
        Control for copying values between Revit elements using a custom collector implementation.
        This control uses the Filtered Element Collector field for sources and
        the Filtered Element Collector with Selected Categories as targets.
        """,
        CollectorType = typeof(ValueCopyRevitCollector))]
    public ValueCopy? ValueCopy { get; set; }
}
