using CW.Assistant.Extensions.Contracts.Fields;

namespace SelectObjectFromSelectionFilter;

public class SelectObjectFromSelectionFilterArgs
{
     [TextField(
        Label = "Selection filter name",
        ToolTip = "Selection filter name from Tekla (Shortcut in Tekla: Ctrl + G).",
        CollectorType = typeof(SelectionFilterCollector))]
    public string FilterName { get; set; } = string.Empty;
}
