using System.Text;
using System.Text.RegularExpressions;

namespace OpenWorksets;

public class OpenWorksetsCommand : IRevitExtension<OpenWorksetsArgs>
{
    private const string TemporaryViewNamePrefix = "Opening worksets...";

    public IExtensionResult Run(IRevitExtensionContext context, OpenWorksetsArgs args, CancellationToken cancellationToken)
    {
        var uiDocument = context.UIApplication.ActiveUIDocument;
        var document = uiDocument?.Document;
        if (uiDocument is null || document is null)
            return Result.Text.Failed("Revit has no active document. Open the model that contains the worksets and run the extension again.");

        if (!document.IsWorkshared)
            return Result.Text.Failed($"'{document.Title}' is not workshared, so it has no worksets. Run the extension on a workshared model.");

        if (args.WorksetIds.Count == 0 && args.RegexWorksets.Count == 0)
            return Result.Text.Failed("No worksets were requested. Select at least one workset, or add at least one name pattern, in the extension configuration.");

        List<Regex> patterns;
        try
        {
            patterns = args.RegexWorksets
                .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase))
                .ToList();
        }
        catch (ArgumentException exception)
        {
            return Result.Text.Failed($"A name pattern is not a valid regular expression: {exception.Message} Correct the entry in the Name Patterns field and run the extension again.");
        }

        var selection = SelectWorksets(document, args.WorksetIds, patterns);
        if (selection.ToOpen.Count == 0)
            return Result.Text.Failed(BuildNothingToOpenMessage(selection));

        var links = GetLinksToLoad(document, selection.ToOpen);

        if (context.IsDryRun)
            return Result.Markdown.Succeeded(BuildDryRunSummary(document, selection.ToOpen, links));

        cancellationToken.ThrowIfCancellationRequested();

        if (!TryOpenWorksets(uiDocument, document, selection.ToOpen, links, out var viewFailureReason))
            return Result.Text.Failed(BuildNoViewMessage(document, viewFailureReason));

        cancellationToken.ThrowIfCancellationRequested();

        var worksetRows = GetWorksetStatus(document, selection.ToOpen);
        var linkRows = LoadLinks(links, cancellationToken);

        return BuildResult(document, worksetRows, linkRows);
    }

    private static WorksetSelection SelectWorksets(Document document, List<string> selectedWorksetIds, List<Regex> patterns)
    {
        var userWorksets = GetUserWorksets(document);

        var toOpen = new List<WorksetInfo>();
        var alreadyOpen = new List<string>();
        var missing = new List<string>();

        foreach (var worksetId in selectedWorksetIds)
        {
            var workset = userWorksets.FirstOrDefault(candidate => GetKey(candidate.Id) == worksetId);
            if (workset is null)
            {
                missing.Add(worksetId);
                continue;
            }

            if (workset.IsOpen)
                alreadyOpen.Add(workset.Name);
            else
                toOpen.Add(new WorksetInfo(workset.Id, workset.Name));
        }

        if (patterns.Count > 0)
        {
            var selectedIds = new HashSet<string>(selectedWorksetIds);
            foreach (var workset in userWorksets)
            {
                if (selectedIds.Contains(GetKey(workset.Id)))
                    continue;

                if (!patterns.Any(pattern => pattern.IsMatch(workset.Name)))
                    continue;

                if (workset.IsOpen)
                    alreadyOpen.Add(workset.Name);
                else
                    toOpen.Add(new WorksetInfo(workset.Id, workset.Name));
            }
        }

        return new WorksetSelection(toOpen, alreadyOpen, missing);
    }

    private static IList<Workset> GetUserWorksets(Document document)
    {
        using var collector = new FilteredWorksetCollector(document);
        return collector
            .OfKind(WorksetKind.UserWorkset)
            .ToWorksets();
    }

    private static string GetKey(WorksetId worksetId) =>
        worksetId.IntegerValue.ToString(CultureInfo.InvariantCulture);

    private static string BuildNothingToOpenMessage(WorksetSelection selection)
    {
        var message = new StringBuilder("No closed worksets matched the configuration, so nothing was opened.");

        if (selection.AlreadyOpen.Count > 0)
            message.Append($" Already open: {string.Join(", ", selection.AlreadyOpen)}.");

        if (selection.Missing.Count > 0)
            message.Append($" The active document has no workset with id {string.Join(", ", selection.Missing)}; the configuration was most likely saved against a different model, so re-pick the worksets in the extension configuration.");

        message.Append(" Compare the Worksets selection and the name patterns against the Worksets dialog in Revit.");
        return message.ToString();
    }

    private static string BuildNoViewMessage(Document document, string? failureReason)
    {
        var message = new StringBuilder($"No temporary 3D view could be created in '{document.Title}', so no worksets were opened.");

        if (string.IsNullOrWhiteSpace(failureReason))
            message.Append(" The model has no 3D view family type to create one from.");
        else
            message.Append($" Revit reported: {failureReason}");

        message.Append(" Check that you can create a 3D view manually in this model, then run the extension again.");
        return message.ToString();
    }

    /// <summary>
    /// Forces Revit to open the requested worksets by creating a throw-away element in each of
    /// them and showing those elements in a temporary 3D view. Every model change happens inside
    /// a transaction group that is rolled back, so only the open/closed state of the worksets
    /// survives the operation.
    /// </summary>
    /// <returns><c>false</c> when no temporary 3D view could be created.</returns>
    private static bool TryOpenWorksets(UIDocument uiDocument, Document document, List<WorksetInfo> worksets, List<RevitLinkType> links, out string? viewFailureReason)
    {
        using var group = new TransactionGroup(document, "Open worksets");
        group.Start();

        MoveLinksToTemporaryWorkset(document, links);

        var view = CreateTemporaryView(document, out viewFailureReason);
        if (view is null)
        {
            group.RollBack();
            return false;
        }

        var categoryId = new ElementId(BuiltInCategory.OST_Site);
        var originalView = uiDocument.ActiveView;
        uiDocument.ActiveView = view;

        var elementIds = worksets
            .Select(workset => CreateElement(document, categoryId, workset.Id))
            .OfType<ElementId>()
            .ToList();

        if (elementIds.Count > 0)
            uiDocument.ShowElements(elementIds);

        uiDocument.ActiveView = originalView;

        group.RollBack();
        return true;
    }

    private static List<RevitLinkType> GetLinksToLoad(Document document, List<WorksetInfo> worksets)
    {
        var worksetIds = new HashSet<int>(worksets.Select(workset => workset.Id.IntegerValue));

        using var collector = new FilteredElementCollector(document);
        return collector
            .OfClass(typeof(RevitLinkType))
            .OfType<RevitLinkType>()
            .Where(link => worksetIds.Contains(link.WorksetId.IntegerValue))
            .ToList();
    }

    /// <summary>
    /// Moves the links that live in the worksets being opened onto a temporary workset, so that
    /// opening a workset does not also drag its linked models into the temporary view.
    /// </summary>
    private static void MoveLinksToTemporaryWorkset(Document document, List<RevitLinkType> links)
    {
        if (links.Count == 0)
            return;

        using var transaction = new Transaction(document, "Move links to temporary workset");
        transaction.Start();
        OpenWorksetsFailurePreprocessor.Attach(transaction);

        var temporaryWorkset = Workset.Create(document, Guid.NewGuid().ToString());
        using var collector = new FilteredElementCollector(document);
        var linkInstances = collector
            .OfClass(typeof(RevitLinkInstance))
            .OfType<RevitLinkInstance>()
            .ToLookup(instance => instance.GetTypeId());

        foreach (var link in links)
        {
            TrySetWorkset(link, temporaryWorkset.Id);
            foreach (var linkInstance in linkInstances[link.Id])
            {
                TrySetWorkset(linkInstance, temporaryWorkset.Id);
            }
        }

        transaction.Commit();
    }

    private static void TrySetWorkset(Element element, WorksetId worksetId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
        if (parameter is null || parameter.IsReadOnly || parameter.StorageType != StorageType.Integer)
            return;

        parameter.Set(worksetId.IntegerValue);
    }

    /// <summary>
    /// Creates a throw-away <see cref="DirectShape"/> owned by <paramref name="worksetId"/>.
    /// Showing that element in a view is what makes Revit open the workset.
    /// </summary>
    /// <returns><c>null</c> when Revit refused the change, for example because the workset is
    /// owned by another user.</returns>
    private static ElementId? CreateElement(Document document, ElementId categoryId, WorksetId worksetId)
    {
        var table = document.GetWorksetTable();
        var activeWorksetId = table.GetActiveWorksetId();
        table.SetActiveWorksetId(worksetId);

        try
        {
            using var transaction = new Transaction(document, "Create temporary element");
            transaction.Start();
            OpenWorksetsFailurePreprocessor.Attach(transaction);

            var element = DirectShape.CreateElement(document, categoryId);
            element.SetShape(ShapeBuilderUtils.CylinderBuilder());

            return transaction.Commit() == TransactionStatus.Committed ? element.Id : null;
        }
        finally
        {
            table.SetActiveWorksetId(activeWorksetId);
        }
    }

    /// <param name="failureReason">What Revit last reported when a view could not be created,
    /// or <c>null</c> when the document offers no 3D view type at all.</param>
    private static View3D? CreateTemporaryView(Document document, out string? failureReason)
    {
        failureReason = null;
        var name = GetUnusedViewName(document);

        // A view family type of the three-dimensional family is what is guaranteed to produce an
        // isometric view. The types of the existing 3D views are only a fallback: they can belong
        // to a view template or to a perspective view, which CreateIsometric may refuse.
        using var viewFamilyTypes = new FilteredElementCollector(document);
        var viewTypeIds = viewFamilyTypes
            .OfClass(typeof(ViewFamilyType))
            .OfType<ViewFamilyType>()
            .Where(viewFamilyType => viewFamilyType.ViewFamily == ViewFamily.ThreeDimensional)
            .Select(viewFamilyType => viewFamilyType.Id)
            .ToList();

        using var existingViews = new FilteredElementCollector(document);
        viewTypeIds.AddRange(existingViews
            .OfClass(typeof(View3D))
            .OfType<View3D>()
            .Where(view => !view.IsTemplate)
            .Select(view => view.GetTypeId()));

        foreach (var viewTypeId in viewTypeIds.Distinct())
        {
            var view = TryCreateView(document, viewTypeId, name, ref failureReason);
            if (view is not null)
                return view;
        }

        return null;
    }

    /// <summary>
    /// Revit refuses a view name that is already taken, and a model can easily contain a view
    /// left behind by an earlier task that names its temporary view the same way. Build a name
    /// no view in the document uses, so creating the temporary view cannot fail over its name.
    /// </summary>
    private static string GetUnusedViewName(Document document)
    {
        using var collector = new FilteredElementCollector(document);
        var usedNames = new HashSet<string>(
            collector
                .OfClass(typeof(View))
                .OfType<View>()
                .Select(view => view.Name),
            StringComparer.OrdinalIgnoreCase);

        string name;
        do
        {
            name = $"{TemporaryViewNamePrefix} {Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
        while (usedNames.Contains(name));

        return name;
    }

    private static View3D? TryCreateView(Document document, ElementId viewTypeId, string name, ref string? failureReason)
    {
        using var transaction = new Transaction(document, "Create temporary view");
        transaction.Start();
        OpenWorksetsFailurePreprocessor.Attach(transaction);

        View3D view;
        try
        {
            view = View3D.CreateIsometric(document, viewTypeId);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException exception)
        {
            // This view family type cannot produce an isometric view here; the caller tries the next.
            failureReason = exception.Message;
            transaction.RollBack();
            return null;
        }

        // The name only labels the view while it is on screen, so a name Revit will not accept
        // must not cost us the view. Keep the name Revit generated and carry on.
        try
        {
            view.Name = name;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
        }

        if (transaction.Commit() == TransactionStatus.Committed)
            return view;

        failureReason = "Revit rolled back the transaction that creates the temporary view.";
        return null;
    }

    private static List<StatusRow> GetWorksetStatus(Document document, List<WorksetInfo> worksets)
    {
        var current = GetUserWorksets(document).ToLookup(workset => workset.Id.IntegerValue);

        return worksets
            .Select(workset =>
            {
                var updated = current[workset.Id.IntegerValue].FirstOrDefault();
                if (updated is null)
                    return new StatusRow(workset.Name, "No longer present in the document", false);

                return updated.IsOpen
                    ? new StatusRow(workset.Name, "Opened", true)
                    : new StatusRow(workset.Name, "Still closed. Check in Revit that the workset is not owned by another user.", false);
            })
            .ToList();
    }

    private static List<StatusRow> LoadLinks(List<RevitLinkType> links, CancellationToken cancellationToken)
    {
        var rows = new List<StatusRow>();

        foreach (var link in links)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (link.IsNestedLink)
            {
                rows.Add(new StatusRow(link.Name, "Skipped, nested link. Reload the parent link instead.", true));
                continue;
            }

            if (link.GetLinkedFileStatus() == LinkedFileStatus.Loaded)
            {
                rows.Add(new StatusRow(link.Name, "Already loaded", true));
                continue;
            }

            try
            {
                var loadResult = link.Load();
                var succeeded = loadResult.LoadResult == LinkLoadResultType.LinkLoaded;
                rows.Add(new StatusRow(link.Name, loadResult.LoadResult.ToString(), succeeded));
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException exception)
            {
                rows.Add(new StatusRow(link.Name, $"Not loaded: {exception.Message}", false));
            }
        }

        return rows;
    }

    private static string BuildDryRunSummary(Document document, List<WorksetInfo> worksets, List<RevitLinkType> links)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"## Open worksets in '{document.Title}' (dry run)");
        markdown.AppendLine();
        markdown.AppendLine($"{worksets.Count} closed workset(s) would be opened and {links.Count} link(s) in those worksets would be reloaded. The document was not modified.");
        markdown.AppendLine();

        AppendTable(markdown, "Workset", worksets
            .Select(workset => new StatusRow(workset.Name, "Would be opened", true))
            .ToList());

        AppendTable(markdown, "Link", links
            .Select(link => new StatusRow(link.Name, link.IsNestedLink ? "Would be skipped, nested link" : "Would be reloaded", true))
            .ToList());

        return markdown.ToString();
    }

    private static IExtensionResult BuildResult(Document document, List<StatusRow> worksetRows, List<StatusRow> linkRows)
    {
        var opened = worksetRows.Count(row => row.Succeeded);

        var markdown = new StringBuilder();
        markdown.AppendLine($"## Open worksets in '{document.Title}'");
        markdown.AppendLine();
        markdown.AppendLine($"Opened {opened} of {worksetRows.Count} workset(s).");
        markdown.AppendLine();
        AppendTable(markdown, "Workset", worksetRows);
        AppendTable(markdown, "Link", linkRows);

        var summary = markdown.ToString();

        if (opened == 0)
            return Result.Markdown.Failed(summary);

        return worksetRows.Concat(linkRows).All(row => row.Succeeded)
            ? Result.Markdown.Succeeded(summary)
            : Result.Markdown.PartiallySucceeded(summary);
    }

    private static void AppendTable(StringBuilder markdown, string header, List<StatusRow> rows)
    {
        if (rows.Count == 0)
            return;

        markdown.AppendLine($"| {header} | Status |");
        markdown.AppendLine("| --- | --- |");
        foreach (var row in rows)
        {
            markdown.AppendLine($"| {row.Name} | {row.Status} |");
        }
        markdown.AppendLine();
    }

    private record WorksetInfo(WorksetId Id, string Name);

    private record WorksetSelection(List<WorksetInfo> ToOpen, List<string> AlreadyOpen, List<string> Missing);

    private record StatusRow(string Name, string Status, bool Succeeded);
}
