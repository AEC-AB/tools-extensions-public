namespace RenameProjectEntities.Renamers;

/// <summary>
/// Renames string parameter values across all elements.
/// </summary>
public sealed class ParameterValueRenamer : IEntityRenamer
{
    public string Category => "Parameter Values";

    // Categories known to crash or hang when enumerating parameters.
    private static readonly HashSet<int> _excludedCategories =
    [
        (int)BuiltInCategory.OST_Matchline,
        (int)BuiltInCategory.OST_Cameras,
        (int)BuiltInCategory.OST_DuctSystem,
        (int)BuiltInCategory.OST_PipingSystem,
        (int)BuiltInCategory.OST_ElectricalCircuit,
        (int)BuiltInCategory.OST_SectionBox,
        (int)BuiltInCategory.OST_Viewers,
        (int)BuiltInCategory.OST_Levels,           // handled by LevelGridRenamer
        (int)BuiltInCategory.OST_Grids,            // handled by LevelGridRenamer
    ];

    public IEnumerable<RenameResult> Rename(Document document, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        var elements = new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .ToElements();

        CurrentLog.Step($"ParameterValueRenamer: collected {elements.Count} instance elements");
        foreach (var result in RenameElements(elements, args, previewOnly, cancellationToken))
            yield return result;

        var types = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .ToElements();

        CurrentLog.Step($"ParameterValueRenamer: collected {types.Count} type elements");
        foreach (var result in RenameElements(types, args, previewOnly, cancellationToken))
            yield return result;
    }

    private static IEnumerable<RenameResult> RenameElements(ICollection<Element> elements, RenameProjectEntitiesArgs args, bool previewOnly, CancellationToken cancellationToken)
    {
        foreach (Element element in elements)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            string className = element.GetType().Name;
            string elementId = element.Id.ToString();

            bool skipCategory = false;
            try
            {
                if (element.Category is Category cat && _excludedCategories.Contains(
#if R2026_OR_GREATER
                    (int)cat.Id.Value
#else
                    cat.Id.IntegerValue
#endif
                    ))
                {
                    CurrentLog.Info($"  SKIP [{className}] Id={elementId} (excluded category: {cat.Name})");
                    skipCategory = true;
                }
            }
            catch (Exception ex)
            {
                CurrentLog.Warn($"  SKIP [{className}] Id={elementId} — Category access threw: {ex.Message}");
                skipCategory = true;
            }

            if (skipCategory)
                continue;

            ParameterSet? parameters = null;
            try
            {
                parameters = element.Parameters;
            }
            catch (Exception ex)
            {
                CurrentLog.Error($"  SKIP [{className}] Id={elementId} — Parameters property threw: {ex.Message}");
                continue;
            }

            // Materialize to list to protect against enumerator crashes (common on MEP elements)
            List<Parameter> paramList;
            try
            {
                paramList = parameters?.Cast<Parameter>().Where(p => p is not null).ToList() ?? [];
            }
            catch (Exception ex)
            {
                CurrentLog.Error($"  SKIP [{className}] Id={elementId} — ParameterSet enumeration threw: {ex.Message}");
                continue;
            }

            foreach (var result in RenameSingleElementParameters(element, paramList, args, previewOnly))
                yield return result;
        }
    }

    private static IEnumerable<RenameResult> RenameSingleElementParameters(Element element, List<Parameter> parameters, RenameProjectEntitiesArgs args, bool previewOnly)
    {
        string className = element.GetType().Name;
        string elementId = element.Id.ToString();

        foreach (Parameter parameter in parameters)
        {
            // Defensive: skip if parameter or definition is null
            if (parameter is null || parameter.Definition is null)
                continue;

            if (!parameter.HasValue)
                continue;

            if (parameter.StorageType != StorageType.String)
                continue;

            string paramName = "(unknown)";
            string? currentValue = null;
            try
            {
                paramName = parameter.Definition.Name;
                currentValue = parameter.AsString();
                if (string.IsNullOrEmpty(currentValue))
                {
                    currentValue = parameter.AsValueString();
                }
            }
            catch (Exception ex)
            {
                CurrentLog.Error($"  READ FAIL [{className}] Id={elementId} Param={paramName}: {ex.Message}");
                continue;
            }

            if (string.IsNullOrEmpty(currentValue))
                continue;

            var newValue = MatchEvaluator.Replace(currentValue, args);
            if (newValue is null)
                continue;

            if (previewOnly)
            {
                yield return new RenameResult
                {
                    Category = "Parameter Values",
                    EntityId = elementId,
                    FieldName = paramName,
                    OldValue = currentValue,
                    NewValue = newValue,
                    Success = true
                };
                continue;
            }

            if (parameter.IsReadOnly)
            {
                yield return new RenameResult
                {
                    Category = "Parameter Values",
                    EntityId = elementId,
                    FieldName = paramName,
                    OldValue = currentValue,
                    NewValue = newValue,
                    Success = false,
                    Error = "Parameter is read-only"
                };
                continue;
            }

            RenameResult? result = null;
            try
            {
                parameter.Set(newValue);
                result = new RenameResult
                {
                    Category = "Parameter Values",
                    EntityId = elementId,
                    FieldName = paramName,
                    OldValue = currentValue,
                    NewValue = newValue,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                result = new RenameResult
                {
                    Category = "Parameter Values",
                    EntityId = elementId,
                    FieldName = paramName,
                    OldValue = currentValue,
                    NewValue = newValue,
                    Success = false,
                    Error = ex.Message
                };
            }

            if (result is not null)
                yield return result;
        }
    }
}
