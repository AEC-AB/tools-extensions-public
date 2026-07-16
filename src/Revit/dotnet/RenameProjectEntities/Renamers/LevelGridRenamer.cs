namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames level, grid, and reference plane names.
/// </summary>
public sealed class LevelGridRenamer : IEntityRenamer
{
    public string Category => "Levels, Grids & Ref Planes";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var levels = new FilteredElementCollector(document).OfClass(typeof(Level)).ToElements();
        foreach (Level level in levels)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameElementCore(level, args, previewOnly, "Level Name");
            if (result is not null)
                yield return result;
        }

        var grids = new FilteredElementCollector(document).OfClass(typeof(Grid)).ToElements();
        foreach (Grid grid in grids)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameElementCore(grid, args, previewOnly, "Grid Name");
            if (result is not null)
                yield return result;
        }

        var refPlanes = new FilteredElementCollector(document).OfClass(typeof(ReferencePlane)).ToElements();
        foreach (ReferencePlane rp in refPlanes)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenameElementCore(rp, args, previewOnly, "Reference Plane Name");
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameElementCore(Element element, RenameProjectEntitiesArgs args, bool previewOnly, string fieldName)
    {
        string? current = null;
        try { current = element.Name; } catch { return null; }
        if (string.IsNullOrEmpty(current)) return null;

        var newName = MatchEvaluator.Replace(current, args);
        if (newName is null) return null;

        if (previewOnly)
            return new RenameResult { Category = "Levels, Grids & Ref Planes", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = current, NewValue = newName, Success = true };

        try
        {
            element.Name = newName;
            return new RenameResult { Category = "Levels, Grids & Ref Planes", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = current, NewValue = newName, Success = true };
        }
        catch (Exception ex)
        {
            return new RenameResult { Category = "Levels, Grids & Ref Planes", EntityId = element.Id.ToString(), FieldName = fieldName, OldValue = current, NewValue = newName, Success = false, Error = ex.Message };
        }
    }
}
