namespace RenameProjectEntities.Renamers;

/// <summary>
/// Attempts to rename project parameter names (and shared parameter names where possible).
/// </summary>
public sealed class ParameterNameRenamer : IEntityRenamer
{
    public string Category => "Parameter Names";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var bindingMap = document.ParameterBindings;
        var definitions = new HashSet<Definition>();

        var it = bindingMap.ForwardIterator();
        while (it.MoveNext())
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            if (it.Key is Definition definition)
                definitions.Add(definition);
        }

        foreach (var definition in definitions)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameDefinitionCore(definition, args, previewOnly);
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameDefinitionCore(Definition definition, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string current = definition.Name;
        if (string.IsNullOrEmpty(current))
            return null;

        var newName = MatchEvaluator.Replace(current, args);
        if (newName is null)
            return null;

        if (previewOnly)
            return new RenameResult { Category = "Parameter Names", EntityId = definition.GetHashCode().ToString(), FieldName = "Parameter Name", OldValue = current, NewValue = newName, Success = true };

        // Standard Revit API does not allow renaming bound parameter definitions.
        if (definition is ExternalDefinition)
        {
            return new RenameResult { Category = "Parameter Names", EntityId = definition.GetHashCode().ToString(), FieldName = "Parameter Name", OldValue = current, NewValue = newName, Success = false, Error = "Shared parameter names are read-only after binding via standard API" };
        }

        return new RenameResult { Category = "Parameter Names", EntityId = definition.GetHashCode().ToString(), FieldName = "Parameter Name", OldValue = current, NewValue = newName, Success = false, Error = "Parameter definition names are read-only via standard Revit API" };
    }
}
