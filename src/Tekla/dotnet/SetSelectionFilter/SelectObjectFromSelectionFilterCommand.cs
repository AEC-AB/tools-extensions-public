using System.Collections;

namespace SelectObjectFromSelectionFilter;

public class SelectObjectFromSelectionFilterCommand : ITeklaExtension<SelectObjectFromSelectionFilterArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, SelectObjectFromSelectionFilterArgs args, CancellationToken token)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (string.IsNullOrWhiteSpace(args.FilterName))
        {
            return Result.Text.Failed("Selection filter name is required.");
        }

        var model = new Model();
        if (!model.GetConnectionStatus())
        {
            return Result.Text.Failed("Tekla Structures has no active model.");
        }

        token.ThrowIfCancellationRequested();

        var filterEnumerator = model.GetModelObjectSelector().GetObjectsByFilterName(args.FilterName);
        var selectedObjects = new ArrayList();
        while (filterEnumerator.MoveNext())
        {
            token.ThrowIfCancellationRequested();

            if (filterEnumerator.Current is ModelObject modelObject)
            {
                selectedObjects.Add(modelObject);
            }
        }

        if (selectedObjects.Count == 0)
        {
            return Result.Text.Failed($"Filter '{args.FilterName}' did not match any objects.");
        }

        var modelObjectSelector = new Tekla.Structures.Model.UI.ModelObjectSelector();
        modelObjectSelector.Select(selectedObjects);
        model.CommitChanges();

        return Result.Text.Succeeded($"Selected {selectedObjects.Count} objects with filter '{args.FilterName}'.");
    }
}
