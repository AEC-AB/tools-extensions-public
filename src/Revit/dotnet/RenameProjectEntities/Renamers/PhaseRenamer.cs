namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames phase names.
/// </summary>
public sealed class PhaseRenamer : IEntityRenamer
{
    public string Category => "Phases";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var phases = new FilteredElementCollector(document)
            .OfClass(typeof(Phase))
            .ToElements();

        foreach (Phase phase in phases)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            var result = RenamePhaseCore(phase, args, previewOnly);
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenamePhaseCore(Phase phase, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string? current = null;
        try { current = phase.Name; } catch { return null; }
        if (string.IsNullOrEmpty(current)) return null;

        var newName = MatchEvaluator.Replace(current, args);
        if (newName is null) return null;

        if (previewOnly)
            return new RenameResult { Category = "Phases", EntityId = phase.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = true };

        try
        {
            phase.Name = newName;
            return new RenameResult { Category = "Phases", EntityId = phase.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = true };
        }
        catch (Exception ex)
        {
            return new RenameResult { Category = "Phases", EntityId = phase.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = false, Error = ex.Message };
        }
    }
}
