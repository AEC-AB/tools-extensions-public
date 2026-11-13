namespace OpenWorksets;

public class OpenWorksetsArgs
{
    [OptionsField(
        Label = "Worksets",
        ToolTip = "User worksets to open, listed from the active Revit model. Worksets that are already open are skipped.",
        CollectorType = typeof(UserWorksetCollector),
        CollectorSortOrder = SortOrder.SortByAscending,
        CompactMode = true)]
    public List<string> WorksetNames { get; set; } = [];

    [ListField(
        Label = "Workset name patterns",
        ToolTip = "Regular expressions matched against closed workset names (case-insensitive). Example: '^LINK' opens every closed workset whose name starts with 'LINK'.")]
    public List<string> WorksetNamePatterns { get; set; } = [];

    [BooleanField(
        Label = "Reload unloaded links",
        ToolTip = "Load Revit links placed on the opened worksets that are still unloaded after the worksets open, including links unloaded with 'Unload' or 'Unload for me'. When off, no links are loaded.")]
    public bool ReloadUnloadedLinks { get; set; }
}

internal class UserWorksetCollector : IRevitAutoFillCollector<OpenWorksetsArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, OpenWorksetsArgs args)
    {
        var document = uiApplication.ActiveUIDocument?.Document;
        if (document is null || !document.IsWorkshared)
            return [];

        using var collector = new FilteredWorksetCollector(document);
        return collector
            .OfKind(WorksetKind.UserWorkset)
            .ToWorksets()
            .Select(workset => workset.Name)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(name => name, name => name);
    }
}
