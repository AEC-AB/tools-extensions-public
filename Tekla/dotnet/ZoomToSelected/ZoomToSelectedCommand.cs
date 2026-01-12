using Tekla.Structures;
using Tekla.Structures.Drawing;
using Tekla.Structures.Drawing.UI;

namespace ZoomToSelected;

public class ZoomToSelectedCommand : ITeklaExtension<ZoomToSelectedArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, ZoomToSelectedArgs args, CancellationToken cancellationToken)
    {

        var hiddenDrawingObjectsId = Show();

        Tekla.Structures.ModelInternal.Operation.dotStartAction("ZoomToSelected", string.Empty);

        Hide(hiddenDrawingObjectsId);

        // Return a result with the message
        return Result.Text.Succeeded("Zoomed to selected objects");
    }


    // Method will unhide any selected objects in the drawing and return a list of identifiers of the hidden objects
    private List<Identifier> Show()
    {
        DrawingHandler drawingHandler = new DrawingHandler();
        DrawingObjectSelector dos = drawingHandler.GetDrawingObjectSelector();
        var selectedDrawingObjectEnumerator = dos.GetSelected();

        var hiddenDrawingObjectsId = new List<Identifier>();
        while (selectedDrawingObjectEnumerator.MoveNext())
        {
            var drawingObject = selectedDrawingObjectEnumerator.Current;
            if (drawingObject is not Tekla.Structures.Drawing.ModelObject currentObject)
                continue;

            if (currentObject.Hideable.IsHidden)
            {
                hiddenDrawingObjectsId.Add(currentObject.ModelIdentifier);
                currentObject.Hideable.ShowInDrawing();
                currentObject.Modify();
            }
        }
        return hiddenDrawingObjectsId;
    }

    // Method will hide any selected objects in the drawing based on the list of identifiers
    private void Hide(List<Identifier> identifiers)
    {
        DrawingHandler drawingHandler = new DrawingHandler();
        DrawingObjectSelector dos = drawingHandler.GetDrawingObjectSelector();
        var selectedDrawingObjectEnumerator = dos.GetSelected();

        while (selectedDrawingObjectEnumerator.MoveNext())
        {
            var drawingObject = selectedDrawingObjectEnumerator.Current;
            if (drawingObject is not Tekla.Structures.Drawing.ModelObject currentObject)
                continue;

            if (identifiers.Contains(currentObject.ModelIdentifier))
            {
                currentObject.Hideable.HideFromDrawing();
                currentObject.Modify();
            }
        }
    }
}