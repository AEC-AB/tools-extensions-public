namespace OpenWorksets;

/// <summary>
/// Finds and loads the Revit links that belong to the worksets being opened.
/// </summary>
internal static class LinkLoader
{
    public sealed record LinkLoadOutcome(string Name, bool Loaded, string Detail);

    /// <summary>
    /// Returns link types whose type element is on one of the worksets. When
    /// <paramref name="includeLinkInstances"/> is set, link types with at least one instance on
    /// one of the worksets are also returned.
    /// </summary>
    public static List<RevitLinkType> FindLinks(Document document, IEnumerable<Workset> worksets, bool includeLinkInstances)
    {
        var worksetIds = new HashSet<int>(worksets.Select(workset => workset.Id.IntegerValue));

        var linkTypeIdsWithInstanceOnWorkset = includeLinkInstances
            ? new HashSet<ElementId>(new FilteredElementCollector(document)
                .OfClass(typeof(RevitLinkInstance))
                .Where(instance => worksetIds.Contains(instance.WorksetId.IntegerValue))
                .Select(instance => instance.GetTypeId()))
            : [];

        return new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkType))
            .Cast<RevitLinkType>()
            .Where(linkType => worksetIds.Contains(linkType.WorksetId.IntegerValue)
                || linkTypeIdsWithInstanceOnWorkset.Contains(linkType.Id))
            .OrderBy(linkType => linkType.Name)
            .ToList();
    }

    /// <summary>
    /// Loads every link type that is not loaded. Must be called outside any transaction.
    /// </summary>
    public static List<LinkLoadOutcome> LoadUnloaded(IEnumerable<RevitLinkType> linkTypes, CancellationToken cancellationToken)
    {
        var outcomes = new List<LinkLoadOutcome>();

        foreach (var linkType in linkTypes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (linkType.IsNestedLink)
            {
                outcomes.Add(new(linkType.Name, false, "Nested link. Reload the parent link instead."));
                continue;
            }

            var status = linkType.GetLinkedFileStatus();
            if (status == LinkedFileStatus.Loaded)
                continue;

            outcomes.Add(Load(linkType, status));
        }

        return outcomes;
    }

    private static LinkLoadOutcome Load(RevitLinkType linkType, LinkedFileStatus previousStatus)
    {
        try
        {
            var result = linkType.Load();
            var loaded = linkType.GetLinkedFileStatus() == LinkedFileStatus.Loaded;
            return new(linkType.Name, loaded, $"{previousStatus} → {result.LoadResult}");
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException exception)
        {
            return new(linkType.Name, false, $"{previousStatus} → not loaded: {exception.Message}");
        }
    }
}
