namespace AddSharedParameters.Extensions;

public static class AddSharedParameterArgsExtensions
{
    public static IEnumerable<string> GetNormalizedParameterNames(this AddSharedParametersArgs args)
    {
        return NormalizeAutoFillSelections(args.ParameterNames);
    }

    public static IEnumerable<string> GetNormalizedCategoryNames(this AddSharedParametersArgs args)
    {
        return NormalizeAutoFillSelections(args.CategoryNames);
    }

    public static IEnumerable<string> GetNormalizedCategoryNamesToRemove(this AddSharedParametersArgs args)
    {
        return NormalizeAutoFillSelections(args.CategoryNamesToRemove);
    }

#if R2024_OR_GREATER
    public static ForgeTypeId GetParameterGroup(this AddSharedParametersArgs args)
    {
        try
        {
            return new ForgeTypeId(args.ParameterGroup);
        }
        catch (Exception e)
        {
            throw new AddSharedParameterFailedException($"Failed to set parameter group, {args.ParameterGroup} was not valid", e);
        }
    }
#else

    public static BuiltInParameterGroup GetParameterGroup(this AddSharedParametersArgs args)
    {
        return args.ParameterGroup;
    }
#endif

    private static IEnumerable<string> NormalizeAutoFillSelections(IEnumerable<string>? selections)
    {
        if (selections is null)
            yield break;

        foreach (var selection in selections)
        {
            var normalizedSelection = NormalizeAutoFillSelection(selection);

            if (!string.IsNullOrWhiteSpace(normalizedSelection))
                yield return normalizedSelection;
        }
    }

    private static string? NormalizeAutoFillSelection(string? selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return null;

        var trimmedSelection = selection.Trim();

        if (trimmedSelection.StartsWith("{") && trimmedSelection.Contains("\"Key\""))
        {
            const string keyMarker = "\"Key\":\"";
            var keyStartIndex = trimmedSelection.IndexOf(keyMarker, StringComparison.Ordinal);

            if (keyStartIndex >= 0)
            {
                keyStartIndex += keyMarker.Length;
                var keyEndIndex = trimmedSelection.IndexOf('"', keyStartIndex);

                if (keyEndIndex > keyStartIndex)
                    return trimmedSelection.Substring(keyStartIndex, keyEndIndex - keyStartIndex);
            }
        }

        return trimmedSelection;
    }
}
