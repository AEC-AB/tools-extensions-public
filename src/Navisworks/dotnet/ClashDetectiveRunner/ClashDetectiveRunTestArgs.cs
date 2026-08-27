namespace ClashDetectiveRunTest;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class ClashDetectiveRunTestArgs
{
    [OptionsField(
        Label = "Clash test name",
        ToolTip = "Select a clash test from the active model when available.",
        CollectorType = typeof(ClashTestNameCollector),
        CollectorSortOrder = SortOrder.SortByAscending,
        Visibility = $"{nameof(RunAllTests)} == false")]
    public string? TestName { get; set; }

    [TextField(
        Label = "Manual clash test name",
        ToolTip = "Use this when there is no active document while configuring the task, or when the dropdown is empty.",
        Hint = "Exact test name",
        Visibility = $"{nameof(RunAllTests)} == false")]
    public string? ManualTestName { get; set; }

    [BooleanField(Label = "Run all tests")]
    public bool RunAllTests { get; set; }
}
