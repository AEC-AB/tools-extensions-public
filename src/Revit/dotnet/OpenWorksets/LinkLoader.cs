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

        var linkTypeIdsWithInstanceOnWorkset = new HashSet<ElementId>();
        if (includeLinkInstances)
        {
            using var instanceCollector = new FilteredElementCollector(document);
            linkTypeIdsWithInstanceOnWorkset.UnionWith(instanceCollector
                .OfClass(typeof(RevitLinkInstance))
                .Where(instance => worksetIds.Contains(instance.WorksetId.IntegerValue))
                .Select(instance => instance.GetTypeId()));
        }

        using var linkTypeCollector = new FilteredElementCollector(document);
        return linkTypeCollector
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
    /// <remarks>
    /// Revit cannot load a link type whose type element is on a closed workset. That happens
    /// when a link is selected through an instance on an opened workset while its type is on
    /// another workset that stays closed. Such links are reported, not loaded, because opening
    /// a workset the user did not select would change more of the model than was asked for.
    /// </remarks>
    public static List<LinkLoadOutcome> LoadUnloaded(Document document, IEnumerable<RevitLinkType> linkTypes, CancellationToken cancellationToken)
    {
        var worksetTable = document.GetWorksetTable();
        var outcomes = new List<LinkLoadOutcome>();

        foreach (var linkType in linkTypes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var status = linkType.GetLinkedFileStatus();
            if (status == LinkedFileStatus.Loaded)
                continue;

            if (linkType.IsNestedLink)
            {
                outcomes.Add(new(linkType.Name, false, "Nested link. Reload the parent link instead."));
                continue;
            }

            var typeWorkset = worksetTable.GetWorkset(linkType.WorksetId);
            if (!typeWorkset.IsOpen)
            {
                outcomes.Add(new(linkType.Name, false, $"The link type is on the closed workset '{typeWorkset.Name}'. Add that workset to the selection and run the extension again."));
                continue;
            }

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
        catch (Autodesk.Revit.Exceptions.FileAccessException exception)
        {
            return new(linkType.Name, false, $"{previousStatus} → not loaded, the linked file cannot be accessed: {exception.Message} Check that you have permission to open the file.");
        }
    }
}
