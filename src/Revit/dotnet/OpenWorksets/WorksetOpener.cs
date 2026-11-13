namespace OpenWorksets;

/// <summary>
/// Opens closed worksets through the Revit UI.
/// </summary>
/// <remarks>
/// The Revit API has no call that opens a workset in an already open document. Revit opens a
/// closed workset when it is asked to show an element on it, so for each workset a small
/// marker element is created on that workset and shown in a temporary 3D view. All model
/// changes run inside a transaction group that is rolled back, so the markers, the temporary
/// view and any temporary workset never reach the model. Only the opened worksets remain.
/// </remarks>
internal static class WorksetOpener
{
    private static readonly ElementId MarkerCategoryId = new(BuiltInCategory.OST_Site);
    private const double MarkerSize = 1.0;

    public enum Outcome
    {
        Completed,
        NoThreeDimensionalViewType,
        Cancelled,
    }

    /// <param name="linksToIsolate">
    /// Link types on the worksets being opened. They and their instances are moved to a
    /// temporary workset while the worksets open so Revit does not load them as part of that
    /// step. Links are loaded afterwards only when the user enables link reloading, so each
    /// load result can be reported.
    /// </param>
    public static Outcome Open(
        UIDocument uiDocument,
        IReadOnlyCollection<Workset> worksets,
        IReadOnlyCollection<RevitLinkType> linksToIsolate,
        CancellationToken cancellationToken)
    {
        var document = uiDocument.Document;

        var viewTypeId = FindThreeDimensionalViewType(document);
        if (viewTypeId is null)
            return Outcome.NoThreeDimensionalViewType;

        using var group = new TransactionGroup(document, "Open worksets");
        group.Start();

        if (linksToIsolate.Count > 0)
            MoveLinksToTemporaryWorkset(document, linksToIsolate);

        var view = CreateTemporaryView(document, viewTypeId);
        var originalView = uiDocument.ActiveView;
        var markerIds = new List<ElementId>();

        try
        {
            uiDocument.ActiveView = view;

            foreach (var workset in worksets)
            {
                if (cancellationToken.IsCancellationRequested)
                    return Outcome.Cancelled;

                markerIds.Add(CreateMarker(document, workset.Id));
            }

            uiDocument.ShowElements(markerIds);
        }
        finally
        {
            uiDocument.ActiveView = originalView;
            group.RollBack();
        }

        return Outcome.Completed;
    }

    private static ElementId? FindThreeDimensionalViewType(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(viewType => viewType.ViewFamily == ViewFamily.ThreeDimensional)
            ?.Id;
    }

    private static View3D CreateTemporaryView(Document document, ElementId viewTypeId)
    {
        using var transaction = new Transaction(document, "Create temporary 3D view");
        transaction.Start();

        var view = View3D.CreateIsometric(document, viewTypeId);

        // A default view template on the view type could hide the marker category.
        view.ViewTemplateId = ElementId.InvalidElementId;

        transaction.Commit();
        return view;
    }

    private static void MoveLinksToTemporaryWorkset(Document document, IReadOnlyCollection<RevitLinkType> links)
    {
        using var transaction = new Transaction(document, "Isolate links");
        transaction.Start();

        var temporaryWorkset = Workset.Create(document, $"OpenWorksets-{Guid.NewGuid():N}");
        var linkTypeIds = new HashSet<ElementId>(links.Select(link => link.Id));

        foreach (var link in links)
            SetWorkset(link, temporaryWorkset.Id);

        var instances = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Where(instance => linkTypeIds.Contains(instance.GetTypeId()));

        foreach (var instance in instances)
            SetWorkset(instance, temporaryWorkset.Id);

        transaction.Commit();
    }

    private static void SetWorkset(Element element, WorksetId worksetId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
        if (parameter is { IsReadOnly: false })
            parameter.Set(worksetId.IntegerValue);
    }

    private static ElementId CreateMarker(Document document, WorksetId worksetId)
    {
        var worksetTable = document.GetWorksetTable();
        var activeWorksetId = worksetTable.GetActiveWorksetId();
        worksetTable.SetActiveWorksetId(worksetId);

        try
        {
            using var transaction = new Transaction(document, "Create workset marker");
            transaction.Start();

            var marker = DirectShape.CreateElement(document, MarkerCategoryId);
            marker.SetShape([CreateMarkerSolid()]);

            transaction.Commit();
            return marker.Id;
        }
        finally
        {
            worksetTable.SetActiveWorksetId(activeWorksetId);
        }
    }

    private static Solid CreateMarkerSolid()
    {
        var corners = new[]
        {
            XYZ.Zero,
            new XYZ(MarkerSize, 0, 0),
            new XYZ(MarkerSize, MarkerSize, 0),
            new XYZ(0, MarkerSize, 0),
        };

        var profile = new CurveLoop();
        for (var i = 0; i < corners.Length; i++)
            profile.Append(Line.CreateBound(corners[i], corners[(i + 1) % corners.Length]));

        return GeometryCreationUtilities.CreateExtrusionGeometry([profile], XYZ.BasisZ, MarkerSize);
    }
}
