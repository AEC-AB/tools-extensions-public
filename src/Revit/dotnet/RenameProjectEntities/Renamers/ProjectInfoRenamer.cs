namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames ProjectInfo parameter values.
/// </summary>
public sealed class ProjectInfoRenamer : IEntityRenamer
{
    public string Category => "Project Info";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var projectInfo = document.ProjectInformation;
        if (projectInfo is null)
            yield break;

        foreach (Parameter parameter in projectInfo.Parameters)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (parameter.StorageType != StorageType.String)
                continue;

            string? currentValue = parameter.AsString() ?? parameter.AsValueString();
            if (string.IsNullOrEmpty(currentValue))
                continue;

            var newValue = MatchEvaluator.Replace(currentValue, args);
            if (newValue is null)
                continue;

            if (previewOnly)
            {
                yield return new RenameResult { Category = "Project Info", EntityId = projectInfo.Id.ToString(), FieldName = parameter.Definition.Name, OldValue = currentValue, NewValue = newValue, Success = true };
                continue;
            }

            RenameResult? result = null;
            try
            {
                if (!parameter.IsReadOnly)
                    parameter.Set(newValue);
                result = new RenameResult { Category = "Project Info", EntityId = projectInfo.Id.ToString(), FieldName = parameter.Definition.Name, OldValue = currentValue, NewValue = newValue, Success = true };
            }
            catch (Exception ex)
            {
                result = new RenameResult { Category = "Project Info", EntityId = projectInfo.Id.ToString(), FieldName = parameter.Definition.Name, OldValue = currentValue, NewValue = newValue, Success = false, Error = ex.Message };
            }

            if (result is not null)
                yield return result;
        }
    }
}
