using System.Text.RegularExpressions;

namespace OpenWorksets;

/// <summary>
/// The user worksets resolved from the configured names and name patterns.
/// </summary>
internal sealed class WorksetSelection
{
    private WorksetSelection(List<Workset> closed, List<Workset> alreadyOpen, List<string> notFound)
    {
        Closed = closed;
        AlreadyOpen = alreadyOpen;
        NotFound = notFound;
    }

    /// <summary>Closed worksets that should be opened.</summary>
    public IReadOnlyList<Workset> Closed { get; }

    /// <summary>Worksets selected by name that are already open.</summary>
    public IReadOnlyList<Workset> AlreadyOpen { get; }

    /// <summary>Configured workset names that do not exist in the document.</summary>
    public IReadOnlyList<string> NotFound { get; }

    public static WorksetSelection Resolve(Document document, IReadOnlyCollection<string> names, IReadOnlyCollection<Regex> patterns)
    {
        using var collector = new FilteredWorksetCollector(document);
        var userWorksets = collector
            .OfKind(WorksetKind.UserWorkset)
            .ToWorksets();

        var selected = new Dictionary<int, Workset>();
        foreach (var workset in userWorksets)
        {
            var selectedByName = names.Contains(workset.Name, StringComparer.Ordinal);
            var selectedByPattern = !workset.IsOpen && patterns.Any(pattern => pattern.IsMatch(workset.Name));
            if (selectedByName || selectedByPattern)
                selected[workset.Id.IntegerValue] = workset;
        }

        var existingNames = new HashSet<string>(userWorksets.Select(workset => workset.Name), StringComparer.Ordinal);

        return new WorksetSelection(
            closed: selected.Values.Where(workset => !workset.IsOpen).OrderBy(workset => workset.Name).ToList(),
            alreadyOpen: selected.Values.Where(workset => workset.IsOpen).OrderBy(workset => workset.Name).ToList(),
            notFound: names.Where(name => !existingNames.Contains(name)).Distinct().ToList());
    }
}
