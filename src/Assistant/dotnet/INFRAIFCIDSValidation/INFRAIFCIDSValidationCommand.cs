
namespace INFRAIFCIDSValidation;

[SupportedOSPlatform("windows")]
public class INFRAIFCIDSValidationCommand : IAssistantExtension<INFRAIFCIDSValidationArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, INFRAIFCIDSValidationArgs args, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(args.OutputFolder))
        {
            return Result.Text.Failed("Output folder is required.");
        }

        if (!Directory.Exists(args.OutputFolder))
        {
            return Result.Text.Failed($"Output folder not found: {args.OutputFolder}");
        }

        List<InfraCommand> selectedCommands = args.Commands
            .Where(command => !string.IsNullOrWhiteSpace(command))
            .Select(command => Enum.TryParse<InfraCommand>(command.Trim(), ignoreCase: true, out InfraCommand parsed)
                ? (InfraCommand?)parsed
                : null)
            .Where(command => command.HasValue)
            .Select(command => command!.Value)
            .Distinct()
            .ToList();

        if (selectedCommands.Count == 0)
        {
            return Result.Text.Failed("Select at least one validation command.");
        }

        if (!InfraApiCollectorHelpers.TryCreateApiInstance(out object? api, out string loadError))
        {
            return Result.Text.Failed($"Failed to load INFRA API: {loadError}");
        }

        object apiInstance = api!;
        var diagnostics = new List<string>();

        string? projectsLocation;
        try
        {
            projectsLocation = Invoke<string?>(apiInstance, "GetCommonProjectsLocation");
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to get common projects location: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(projectsLocation) || !Directory.Exists(projectsLocation))
        {
            return Result.Text.Failed($"Projects folder not found at: {projectsLocation}");
        }

        diagnostics.Add($"GetCommonProjectsLocation='{projectsLocation}'");

        string projectName;
        string projectPath;
        Dictionary<string, string> projects = [];

        try
        {
            projects = Invoke<Dictionary<string, string>>(apiInstance, "ScanAllProjects") ?? [];
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to scan projects: {ex.Message}");
        }

        diagnostics.Add($"ScanAllProjectsCount={projects.Count}");
        if (projects.Count > 0)
        {
            string sample = string.Join(", ", projects.Take(5).Select(pair => $"{pair.Value}|{pair.Key}"));
            diagnostics.Add($"ScanAllProjectsSample={sample}");
        }

        if (string.IsNullOrWhiteSpace(args.AutoProjectName)
            || string.Equals(args.AutoProjectName, "INFO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(args.AutoProjectName, "ERROR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(args.AutoProjectName, "__collector_probe__", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Text.Failed("Select a valid project from Project name.");
        }

        projectName = args.AutoProjectName.Trim();
        projectPath = projects
            .Where(pair => string.Equals(pair.Value, projectName, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .FirstOrDefault()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return Result.Text.Failed($"Selected project path is missing: {projectName}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<string> selectedIfcFiles = ResolveIfcFiles(context, args);
        if (selectedIfcFiles.Count == 0)
        {
            return Result.Text.Failed("No IFC files were selected.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Invoke<object?>(apiInstance, "CreateMetadataFile", projectName, selectedIfcFiles.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to create metadata file: {ex.Message}");
        }

        List<string> idsFilesToSave;
        List<string> explicitIdsSelection = args.AutoSelectedIdsFiles;

        if (explicitIdsSelection.Count > 0)
        {
            idsFilesToSave = explicitIdsSelection
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.Combine(projectPath, "IDS", path.Trim()))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            string idsPath = Path.Combine(projectPath, "IDS");

            List<string> scannedIdsFiles;
            try
            {
                scannedIdsFiles = Invoke<List<string>>(apiInstance, "ScanIdsFiles", idsPath) ?? [];
            }
            catch
            {
                scannedIdsFiles = InfraApiCollectorHelpers.ScanIdsFilesFallback(idsPath);
            }

            idsFilesToSave = scannedIdsFiles
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (idsFilesToSave.Count == 0)
        {
            return Result.Text.Failed("No IDS files were found or selected.");
        }

        try
        {
            Invoke<object?>(apiInstance, "WriteProjectPathToRegistry", projectName, projectPath);
            Invoke<object?>(apiInstance, "SaveSelectedIdsFilesToRegistry", projectName, idsFilesToSave);

            Invoke<object?>(apiInstance, "WriteOutputDirectoryToRegistry", projectName, args.OutputFolder!);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to write INFRA registry settings: {ex.Message}");
        }

        string commandString = string.Join("|", selectedCommands.Select(c => c.ToString()));
        string arguments = $"--command {commandString}";
        if (args.CloseOnCompletion)
        {
            arguments += " --close-on-completion true";
        }

        IReadOnlyList<string> outputFilesBefore = GetOutputFiles(args.OutputFolder!);

        try
        {
            Invoke<object?>(apiInstance, "LaunchInfraAutomation", arguments, projectName);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to launch INFRA automation: {ex.Message}");
        }

        string? firstNewFile = await WaitForFirstNewOutputFileAsync(args.OutputFolder!, outputFilesBefore, cancellationToken, 120);

        string summary;
        if (firstNewFile is not null)
        {
            summary =
                $"INFRA validation for project '{projectName}' with {selectedIfcFiles.Count} IFC file(s), {idsFilesToSave.Count} IDS file(s), and {selectedCommands.Count} command(s): {string.Join(", ", selectedCommands)}."
                + $"\nFirst output file: {firstNewFile}"
                + $"\nOutput folder: {args.OutputFolder}";
        }
        else
        {
            summary =
                $"INFRA validation for project '{projectName}' launched with {selectedIfcFiles.Count} IFC file(s), {idsFilesToSave.Count} IDS file(s), and {selectedCommands.Count} command(s): {string.Join(", ", selectedCommands)}."
                + $"\nNo output file created yet. Check result in INFRA or in output folder: {args.OutputFolder}";
        }

        if (args.EnableDiagnostics)
        {
            summary += "\nDiagnostics:";
            summary += string.Concat(diagnostics.Select(entry => $"\n- {entry}"));
            summary += $"\n- LaunchArguments='{arguments}'";
        }

        return Result.Text.Succeeded(summary);
    }

    private static IReadOnlyList<string> GetOutputFiles(string outputFolder)
    {
        try
        {
            return Directory.EnumerateFiles(outputFolder, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFullPath)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<string?> WaitForFirstNewOutputFileAsync(
        string outputFolder,
        IReadOnlyList<string> existingFiles,
        CancellationToken cancellationToken,
        int timeoutSeconds = 120)
    {
        if (timeoutSeconds <= 0)
        {
            return null;
        }

        var existingSet = new HashSet<string>(existingFiles, StringComparer.OrdinalIgnoreCase);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        while (!linked.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), linked.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            IReadOnlyList<string> current = GetOutputFiles(outputFolder);
            string? firstNew = current.FirstOrDefault(file => !existingSet.Contains(file));
            if (firstNew is not null)
            {
                return firstNew;
            }
        }

        return null;
    }

    private static List<string> ResolveIfcFiles(IAssistantExtensionContext context, INFRAIFCIDSValidationArgs args)
    {
        var resolvedFiles = new List<string>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var variableStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string entry in args.IfcFiles)
        {
            foreach (string filePath in ExpandIfcEntry(context, entry, variableStack))
            {
                if (seenFiles.Add(filePath))
                {
                    resolvedFiles.Add(filePath);
                }
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
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

        foreach (string filePath in files)
        {
            if (regex.IsMatch(Path.GetFileName(filePath)))
            {
                yield return Path.GetFullPath(filePath);
            }
        }
    }

    private static T? Invoke<T>(object target, string methodName, params object?[] parameters)
    {
        MethodInfo? method = target.GetType().GetMethod(methodName);
        if (method == null)
        {
            throw new MissingMethodException($"Method not found: {methodName}");
        }

        object? result = method.Invoke(target, parameters);
        if (result is T typed)
        {
            return typed;
        }

        return default;
    }
}