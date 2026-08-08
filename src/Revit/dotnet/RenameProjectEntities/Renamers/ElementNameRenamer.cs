namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames element and type names.
/// </summary>
public sealed class ElementNameRenamer : IEntityRenamer
{
    public string Category => "Element & Type Names";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var elements = new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .ToElements();

        CurrentLog.Step($"ElementNameRenamer: collected {elements.Count} instance elements");

        foreach (Element element in elements)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var result = RenameElementCore(element, args, previewOnly);
            if (result is not null)
                yield return result;
        }

        var types = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .ToElements();

        CurrentLog.Step($"ElementNameRenamer: collected {types.Count} type elements");

        foreach (ElementType elementType in types)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var result = RenameElementCore(elementType, args, previewOnly);
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameElementCore(Element element, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string className = element.GetType().Name;
        string elementId = element.Id.ToString();

        string? currentName = null;
        try { currentName = element.Name; }
        catch (Exception ex)
        {
            CurrentLog.Error($"ElementNameRenamer: Name read FAILED on [{className}] Id={elementId}: {ex.Message}");
            return null;
        }

        if (string.IsNullOrEmpty(currentName))
            return null;

        var newName = MatchEvaluator.Replace(currentName, args);
        if (newName is null)
            return null;

        if (previewOnly)
        {
            CurrentLog.Step($"ElementNameRenamer: PREVIEW [{className}] Id={elementId} '{currentName}' -> '{newName}'");
            return new RenameResult
            {
                Category = "Element & Type Names",
                EntityId = elementId,
                FieldName = "Name",
                OldValue = currentName!,
                NewValue = newName,
                Success = true
            };
        }

        try
        {
            element.Name = newName;
            return new RenameResult
            {
                Category = "Element & Type Names",
                EntityId = elementId,
                FieldName = "Name",
                OldValue = currentName!,
                NewValue = newName,
                Success = true
            };
        }
        catch (Exception ex)
        {
            CurrentLog.Error($"ElementNameRenamer: Name write FAILED on [{className}] Id={elementId} '{currentName}' -> '{newName}': {ex.Message}");
            return new RenameResult
            {
                Category = "Element & Type Names",
                EntityId = elementId,
                FieldName = "Name",
                OldValue = currentName!,
                NewValue = newName,
                Success = false,
                Error = ex.Message
            };
        }
    }
}
