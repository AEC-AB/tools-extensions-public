namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames workset names.
/// </summary>
public sealed class WorksetRenamer : IEntityRenamer
{
    public string Category => "Worksets";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var worksets = new FilteredWorksetCollector(document)
            .OfKind(WorksetKind.UserWorkset);

        foreach (Workset workset in worksets)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var result = RenameWorksetCore(document, workset, args, previewOnly);
            if (result is not null)
                yield return result;
        }
    }

    private static RenameResult? RenameWorksetCore(Document document, Workset workset, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string current = workset.Name;
        if (string.IsNullOrEmpty(current))
            return null;

        var newName = MatchEvaluator.Replace(current, args);
        if (newName is null)
            return null;

        if (previewOnly)
            return new RenameResult { Category = "Worksets", EntityId = workset.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = true };

        try
        {
            WorksetTable.RenameWorkset(document, workset.Id, newName);
            return new RenameResult { Category = "Worksets", EntityId = workset.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = true };
        }
        catch (Exception ex)
        {
            return new RenameResult { Category = "Worksets", EntityId = workset.Id.ToString(), FieldName = "Name", OldValue = current, NewValue = newName, Success = false, Error = ex.Message };
        }
    }
}
