namespace ReLoadRevitLinkFrom.Utils;

internal class WorksetUtils
{
    public static void OpenWorkset(Document document, Workset workset, CancellationToken? cancellationToken)
    {
        var application = new UIApplication(document.Application);

        using var group = new TransactionGroup(document, "Open worksets");
        group.Start();

        var originalView = application.ActiveUIDocument.ActiveView;

        try
        {
            cancellationToken?.ThrowIfCancellationRequested();

            var categoryId = new ElementId(BuiltInCategory.OST_Site);
            var view = CreateView(document, categoryId);

            application.ActiveUIDocument.ActiveView = view;

            var elementIds = new List<ElementId>();

            cancellationToken?.ThrowIfCancellationRequested();

            var elementId = CreateTemporaryElement(document, categoryId, workset.Id);
            elementIds.Add(elementId);

            cancellationToken?.ThrowIfCancellationRequested();

            application.ActiveUIDocument.ShowElements(elementIds);
        }
        finally
        {
            application.ActiveUIDocument.ActiveView = originalView;
            group.RollBack();
        }
    }

    private static ElementId CreateTemporaryElement(Document document, ElementId categoryId, WorksetId worksetId)
    {
        var table = document.GetWorksetTable();
        var activeWorksetId = table.GetActiveWorksetId();
        table.SetActiveWorksetId(worksetId);
        try
        {
            var transaction = new Transaction(document, "Create element");
            transaction.Start();

            var element = DirectShape.CreateElement(document, categoryId);
            var builder = ShapeBuilderUtils.CylinderBuilder();
            element.SetShape(builder);

            transaction.Commit();

            return element.Id;
        }
        finally
        {
            table.SetActiveWorksetId(activeWorksetId);
        }
    }

    private static View CreateView(Document document, ElementId categoryId)
    {
        // View3D also covers walkthroughs and other non-isometric views whose type is not a ThreeDimensional
        // view family type, so only view types from the ThreeDimensional family are valid for CreateIsometric.
        var viewTypeIds = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .OfType<ViewFamilyType>()
            .Where(x => x.ViewFamily == ViewFamily.ThreeDimensional)
            .Select(x => x.Id)
            .ToList();

        var view = TryCreateView(document, viewTypeIds, categoryId);

        if (view is null)
            throw new InvalidOperationException("Unable to create a temporary 3D view to open the link workset. Make sure the model has at least one 3D view type.");

        return view;
    }

    private static View? TryCreateView(Document document, IEnumerable<ElementId> viewTypeIds, ElementId categoryIdToShow)
    {
        foreach (var viewTypeId in viewTypeIds)
        {
            using var transaction = new Transaction(document, "Create temp view");
            transaction.Start();
            try
            {
                var view = CreateTempView(document, viewTypeId, categoryIdToShow, "Opening worksets...");
                transaction.Commit();
                return view;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException argumentException)
            {
                if (argumentException.Message.StartsWith("Name must be unique", StringComparison.OrdinalIgnoreCase))
                {
                    var randomNumber = new Random().Next(1000);
                    var view = CreateTempView(document, viewTypeId, categoryIdToShow, $"Opening worksets... {randomNumber}");
                    transaction.Commit();
                    return view;
                }

                transaction.RollBack();
            }
        }

        return null;
    }

    private static View3D CreateTempView(Document document, ElementId viewTypeId, ElementId categoryIdToShow, string viewName)
    {
        var view = View3D.CreateIsometric(document, viewTypeId);
        view.Name = viewName;

        if (view.ViewTemplateId != ElementId.InvalidElementId)
            view.ViewTemplateId = ElementId.InvalidElementId;

        if (view.GetCategoryHidden(categoryIdToShow))
            view.SetCategoryHidden(categoryIdToShow, false);

        return view;
    }
}
