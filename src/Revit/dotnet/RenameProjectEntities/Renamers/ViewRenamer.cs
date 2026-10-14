namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames views, sheets, and schedules.
/// </summary>
public sealed class ViewRenamer : IEntityRenamer
{
    public string Category => "Views & Sheets";

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var views = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .WhereElementIsNotElementType()
            .ToElements();

        foreach (View view in views)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var nameResult = RenameElementCore(view, args, previewOnly, "View Name");
            if (nameResult is not null)
                yield return nameResult;

            if (view is ViewSheet sheet)
            {
                var numberResult = RenameSheetNumberCore(sheet, args, previewOnly);
                if (numberResult is not null)
                    yield return numberResult;
            }
        }
    }

    private static RenameResult? RenameElementCore(Element element, RenameProjectEntitiesArgs args, bool previewOnly, string fieldName)
    {
        string? currentName = null;
        try { currentName = element.Name; } catch { return null; }
        if (string.IsNullOrEmpty(currentName)) return null;

        var newName = MatchEvaluator.Replace(currentName, args);
        if (newName is null) return null;

        if (previewOnly)
            return Result(element, fieldName, currentName, newName, true, null);

        try
        {
            element.Name = newName;
            return Result(element, fieldName, currentName, newName, true, null);
        }
        catch (Exception ex)
        {
            return Result(element, fieldName, currentName, newName, false, ex.Message);
        }
    }

    private static RenameResult? RenameSheetNumberCore(ViewSheet sheet, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        var param = sheet.get_Parameter(BuiltInParameter.SHEET_NUMBER);
        if (param is null) return null;

        string current = param.AsString() ?? string.Empty;
        if (string.IsNullOrEmpty(current)) return null;

        var newValue = MatchEvaluator.Replace(current, args);
        if (newValue is null) return null;

        if (previewOnly)
            return Result(sheet, "Sheet Number", current, newValue, true, null);

        try
        {
            if (!param.IsReadOnly)
                param.Set(newValue);
            return Result(sheet, "Sheet Number", current, newValue, true, null);
        }
        catch (Exception ex)
        {
            return Result(sheet, "Sheet Number", current, newValue, false, ex.Message);
        }
    }

    private static RenameResult Result(Element element, string field, string oldVal, string newVal, bool success, string? error) =>
        new RenameResult
        {
            Category = "Views & Sheets",
            EntityId = element.Id.ToString(),
            FieldName = field,
            OldValue = oldVal,
            NewValue = newVal,
            Success = success,
            Error = error
        };
}
