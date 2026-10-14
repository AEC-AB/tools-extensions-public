namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames material names.
/// </summary>
public sealed class MaterialRenamer : IEntityRenamer
{
    public string Category => "Materials";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var materials = new FilteredElementCollector(document)
            .OfClass(typeof(Material))
            .ToElements();

        foreach (Material material in materials)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameMaterialCore(material, args, previewOnly);
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameMaterialCore(Material material, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string? currentName = null;
        try { currentName = material.Name; } catch { return null; }
        if (string.IsNullOrEmpty(currentName)) return null;

        var newName = MatchEvaluator.Replace(currentName, args);
        if (newName is null) return null;

        if (previewOnly)
            return new RenameResult { Category = "Materials", EntityId = material.Id.ToString(), FieldName = "Name", OldValue = currentName, NewValue = newName, Success = true };

        try
        {
            material.Name = newName;
            return new RenameResult { Category = "Materials", EntityId = material.Id.ToString(), FieldName = "Name", OldValue = currentName, NewValue = newName, Success = true };
        }
        catch (Exception ex)
        {
            return new RenameResult { Category = "Materials", EntityId = material.Id.ToString(), FieldName = "Name", OldValue = currentName, NewValue = newName, Success = false, Error = ex.Message };
        }
    }
}
