namespace INFRAIFCIDSValidation;

internal static class IfcFileResolver
{
    public static List<string> ResolveIfcFiles(IAssistantExtensionContext context, INFRAIFCIDSValidationArgs args)
    {
        var resolvedFiles = new List<string>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var variableStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string entry in args.IfcFiles)
        {
            foreach (string filePath in ExpandIfcEntry(context, entry, variableStack).Where(filePath => seenFiles.Add(filePath)))
            {
                resolvedFiles.Add(filePath);
            }
        }

        return resolvedFiles;
    }

    private static IEnumerable<string> ExpandIfcEntry(IAssistantExtensionContext context, string? rawEntry, HashSet<string> variableStack)
    {
        string entry = NormalizeIfcEntry(rawEntry);
        if (string.IsNullOrWhiteSpace(entry))
        {
            yield break;
        }

        if (TryResolveInterpolatedEntry(context, entry, variableStack, out string interpolatedEntry)
            && !string.Equals(interpolatedEntry, entry, StringComparison.Ordinal))
        {
            foreach (string filePath in ExpandIfcEntry(context, interpolatedEntry, variableStack))
            {
                yield return filePath;
            }

            yield break;
        }

        if (File.Exists(entry))
        {
            yield return Path.GetFullPath(entry);
            yield break;
        }

        if (TryParseRegexEntry(entry, out string regexDirectory, out string regexPattern))
        {
            foreach (string filePath in ExpandRegexEntry(regexDirectory, regexPattern))
            {
                yield return filePath;
            }

            yield break;
        }

        if (ContainsWildcard(entry))
        {
            foreach (string filePath in ExpandWildcardEntry(entry))
            {
                yield return filePath;
            }

            yield break;
        }

        if (TryResolveVariableEntries(context, entry, out string variableName, out List<string> variableEntries))
        {
            if (!variableStack.Add(variableName))
            {
                yield break;
            }

            try
            {
                foreach (string variableEntry in variableEntries)
                {
                    foreach (string filePath in ExpandIfcEntry(context, variableEntry, variableStack))
                    {
                        yield return filePath;
                    }
                }
            }
            finally
            {
                variableStack.Remove(variableName);
            }
        }
    }

    private static string NormalizeIfcEntry(string? rawEntry)
    {
        if (string.IsNullOrWhiteSpace(rawEntry))
        {
            return string.Empty;
        }

        return rawEntry.Trim().Trim('"');
    }

    private static bool TryResolveVariableEntries(IAssistantExtensionContext context, string entry, out string variableName, out List<string> variableEntries)
    {
        variableName = ExtractVariableName(entry);
        variableEntries = [];

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return false;
        }

        string? rawValue = context.GetVariableValue(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        variableEntries = SplitMultiValue(rawValue);
        return variableEntries.Count > 0;
    }

    private static bool TryResolveInterpolatedEntry(IAssistantExtensionContext context, string entry, HashSet<string> variableStack, out string resolvedEntry)
    {
        resolvedEntry = entry;

        if (!entry.Contains("${", StringComparison.Ordinal))
        {
            return false;
        }

        bool replacedAny = false;

        string resolvedDoubleBrace = Regex.Replace(resolvedEntry, @"\$\{\{\s*(?<name>[^{}]+?)\s*\}\}", match =>
        {
            string variableName = match.Groups["name"].Value.Trim();
            string? variableValue = ResolveVariableValue(context, variableName, variableStack);
            if (string.IsNullOrWhiteSpace(variableValue))
            {
                return match.Value;
            }

            replacedAny = true;
            return variableValue;
        });

        resolvedEntry = Regex.Replace(resolvedDoubleBrace, @"\$\{\s*(?<name>[^{}]+?)\s*\}", match =>
        {
            string variableName = match.Groups["name"].Value.Trim();
            string? variableValue = ResolveVariableValue(context, variableName, variableStack);
            if (string.IsNullOrWhiteSpace(variableValue))
            {
                return match.Value;
            }

            replacedAny = true;
            return variableValue;
        });

        return replacedAny;
    }

    private static string? ResolveVariableValue(IAssistantExtensionContext context, string variableName, HashSet<string> variableStack)
    {
        if (string.IsNullOrWhiteSpace(variableName) || !variableStack.Add(variableName))
        {
            return null;
        }

        try
        {
            string? rawValue = context.GetVariableValue(variableName);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            string normalizedValue = NormalizeIfcEntry(rawValue);
            if (TryResolveInterpolatedEntry(context, normalizedValue, variableStack, out string interpolatedValue))
            {
                return interpolatedValue;
            }

            return normalizedValue;
        }
        finally
        {
            variableStack.Remove(variableName);
        }
    }

    private static string ExtractVariableName(string entry)
    {
        if (entry.StartsWith("var:", StringComparison.OrdinalIgnoreCase))
        {
            return entry[4..].Trim();
        }

        // ${{ Variable.Name }} resolves inside raw IFC entries as an extension-side placeholder.
        if (entry.StartsWith("${{", StringComparison.Ordinal) && entry.EndsWith("}}"))
        {
            return entry[3..^2].Trim();
        }

        // ${ VariableName } — single-brace shorthand
        if (entry.StartsWith("${", StringComparison.Ordinal) && entry.EndsWith('}'))
        {
            return entry[2..^1].Trim();
        }

        return entry;
    }

    private static List<string> SplitMultiValue(string rawValue)
    {
        return rawValue
            .Split(['\r', '\n', ';', ',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static bool ContainsWildcard(string entry)
    {
        return entry.IndexOfAny(['*', '?']) >= 0;
    }

    private static IEnumerable<string> ExpandWildcardEntry(string entry)
    {
        string directory = Path.GetDirectoryName(entry) ?? string.Empty;
        string pattern = Path.GetFileName(entry);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(pattern) || !Directory.Exists(directory))
        {
            yield break;
        }

        IEnumerable<string> matches;
        try
        {
            matches = Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly);
        }
        catch
        {
            yield break;
        }

        foreach (string filePath in matches)
        {
            yield return Path.GetFullPath(filePath);
        }
    }

    private static bool TryParseRegexEntry(string entry, out string directory, out string pattern)
    {
        const string prefix = "regex:";

        directory = string.Empty;
        pattern = string.Empty;

        if (!entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string payload = entry[prefix.Length..].Trim();
        int separatorIndex = payload.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
        {
            return false;
        }

        directory = payload[..separatorIndex].Trim().Trim('"');
        pattern = payload[(separatorIndex + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(directory) && !string.IsNullOrWhiteSpace(pattern);
    }

    private static IEnumerable<string> ExpandRegexEntry(string directory, string pattern)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        }
        catch (ArgumentException)
        {
            yield break;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            yield break;
        }

        foreach (string filePath in files.Where(filePath => regex.IsMatch(Path.GetFileName(filePath))))
        {
            yield return Path.GetFullPath(filePath);
        }
    }
}
