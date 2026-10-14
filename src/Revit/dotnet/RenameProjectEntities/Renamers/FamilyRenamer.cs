namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames family and family symbol names.
/// </summary>
public sealed class FamilyRenamer : IEntityRenamer
{
    public string Category => "Families";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        // Family.Name is read-only in Revit API; only rename FamilySymbols (Family Types).
        var symbols = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .ToElements();

        foreach (FamilySymbol symbol in symbols)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameNameCore(symbol, args, previewOnly, "Family Symbol Name");
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameNameCore(Element element, RenameProjectEntitiesArgs args, bool previewOnly, string fieldName)
    {
        string? currentName = null;
        try { currentName = element.Name; } catch { return null; }
        if (string.IsNullOrEmpty(currentName)) return null;

        var newName = MatchEvaluator.Replace(currentName, args);
        if (newName is null) return null;

        if (previewOnly)
            return new RenameResult { Category = "Families", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = currentName, NewValue = newName, Success = true };

        try
        {
            element.Name = newName;
            return new RenameResult { Category = "Families", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = currentName, NewValue = newName, Success = true };
        }
        catch (Exception ex)
        {
            return new RenameResult { Category = "Families", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = currentName, NewValue = newName, Success = false, Error = ex.Message };
        }
    }
}
