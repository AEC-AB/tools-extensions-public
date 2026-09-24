
namespace OpenWorksets;

[ArgsVersion(2)]
public class OpenWorksetsArgs
{
    [OptionsField(
        Label = "Worksets",
        ToolTip = "Select the closed worksets to open in the active Revit document. Worksets that are already open are skipped.",
        CollectorType = typeof(WorksetCollector),
        CollectorSortOrder = SortOrder.SortByAscending,
        CompactMode = true)]
    public List<string> WorksetIds { get; set; } = [];

    [ListField(
        Label = "Name Patterns",
        ToolTip = """
        Regular expressions matched against workset names, for example '^LINK' to open every workset whose name starts with 'LINK'.
        Matching is case-insensitive. Combine this with the Worksets field to open both named and pattern-matched worksets.
        """,
        Hint = "For example: ^LINK",
        MaxHeight = 200)]
    public List<string> RegexWorksets { get; set; } = [];
}

/// <summary>
/// Populates the Worksets field with the user worksets of the active document.
/// The workset id is the stored key and the workset name is the display value,
/// which matches how the field was persisted before <see cref="OpenWorksetsArgs"/> version 2.
/// </summary>
internal class WorksetCollector : IRevitAutoFillCollector<OpenWorksetsArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, OpenWorksetsArgs args)
    {
        var result = new Dictionary<string, string>();

        var document = uiApplication.ActiveUIDocument?.Document;
        if (document is null || !document.IsWorkshared)
            return result;

        using var collector = new FilteredWorksetCollector(document);
        var worksets = collector
            .OfKind(WorksetKind.UserWorkset)
            .ToWorksets();

        foreach (var workset in worksets)
        {
            result[workset.Id.IntegerValue.ToString(CultureInfo.InvariantCulture)] = workset.Name;
        }

        return result;
    }
}
