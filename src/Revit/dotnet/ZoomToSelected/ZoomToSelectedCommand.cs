
namespace ZoomToSelected;

public class ZoomToSelectedCommand : IRevitExtension<ZoomToSelectedArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, ZoomToSelectedArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        
        if (document is null || !(context.UIApplication.ActiveUIDocument?.ActiveView is { } view))
            return Result.Empty.Succeeded();

        var uiView = GetUiView(context.UIApplication.ActiveUIDocument, view.Id);
        if (uiView is null)
            return Result.Empty.Succeeded();

        var visibleIds = new FilteredElementCollector(document, view.Id).ToElementIds().ToLookup(x => x.GetValue());
        if (!visibleIds.Any())
            return Result.Empty.Succeeded();

        var selectedElements = context.UIApplication.ActiveUIDocument.Selection.GetElementIds();

        if (!selectedElements.Any())
           return Result.Empty.Succeeded();

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        var maxZ = double.NegativeInfinity;
        var minZ = double.PositiveInfinity;

        foreach (var selectedElementId in selectedElements)
        {
            if (!visibleIds.Contains(selectedElementId.GetValue()))
                continue;

            using var element = document.GetElement(selectedElementId);
            var bb = element?.get_BoundingBox(view);
            if (bb is null)
                continue;

            var x = (bb.Max.X + bb.Min.X) / 2;
            if (x > maxX)
                maxX = x;
            if (x < minX)
                minX = x;

            var y = (bb.Max.Y + bb.Min.Y) / 2;
            if (y > maxY)
                maxY = y;
            if (y < minY)
                minY = y;

            var z = (bb.Max.Z + bb.Min.Z) / 2;
            if (z > maxZ)
                maxZ = z;
            if (z < minZ)
                minZ = z;

        }

        if (double.IsNegativeInfinity(maxX) || double.IsNegativeInfinity(maxY) || double.IsPositiveInfinity(minX) || double.IsNegativeInfinity(minY))
            return Result.Empty.Succeeded();
        var pMin = new XYZ(minX, minY, minZ);
        var pMax = new XYZ(maxX, maxY, maxZ);
        var tempDist = pMax.DistanceTo(pMin);

        if (tempDist * 0.10 > 3)
        {
            pMin = pMin.Add(new XYZ(tempDist * -1, tempDist * -1, tempDist * -1));
            pMax = pMax.Add(new XYZ(tempDist, tempDist, tempDist));

        }
        else
        {
            pMin = pMin.Add(new XYZ(-3, -3, -3));
            pMax = pMax.Add(new XYZ(3, 3, 3));
        }


        var dist = pMax.DistanceTo(pMin);
        if (dist < 10)
        {
            var vec = new XYZ(10 - dist, 10 - dist, 0);
            var transform = Transform.CreateTranslation(vec);
            pMax = transform.OfPoint(pMax);
            pMin = transform.Inverse.OfPoint(pMin);
        }

        uiView.ZoomAndCenterRectangle(pMin, pMax);
        return Result.Empty.Succeeded();

    }

    private static UIView? GetUiView(UIDocument uiDoc, ElementId viewId)
    {
        var openUiViews = uiDoc.GetOpenUIViews();
        return openUiViews.FirstOrDefault(uv => uv.ViewId.Equals(viewId));
    }   
}