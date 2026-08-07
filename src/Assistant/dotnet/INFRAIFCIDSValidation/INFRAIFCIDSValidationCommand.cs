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

        InfraApiWrapper? api = InfraApiWrapper.TryCreate(out string loadError);
        if (api is null)
        {
            return Result.Text.Failed($"Failed to load INFRA API: {loadError}");
        }

        var diagnostics = new List<string>();

        IExtensionResult? projectError = ResolveProject(api, args.AutoProjectName, diagnostics, out string projectName, out string projectPath);
        if (projectError is not null)
        {
            return projectError;
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<string> selectedIfcFiles = IfcFileResolver.ResolveIfcFiles(context, args);
        if (selectedIfcFiles.Count == 0)
        {
            return Result.Text.Failed("No IFC files were selected.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            api.CreateMetadataFile(projectName, selectedIfcFiles.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to create metadata file: {ex.Message}");
        }

        IExtensionResult? idsError = ResolveIdsFiles(api, projectPath, args.AutoSelectedIdsFiles, out List<string> idsFilesToSave);
        if (idsError is not null)
        {
            return idsError;
        }

        try
        {
            api.WriteProjectPathToRegistry(projectName, projectPath);
            api.SaveSelectedIdsFilesToRegistry(projectName, idsFilesToSave);
            api.WriteOutputDirectoryToRegistry(projectName, args.OutputFolder!);
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
            api.LaunchInfraAutomation(arguments, projectName);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to launch INFRA automation: {ex.Message}");
        }

        string? firstNewFile = await WaitForFirstNewOutputFileAsync(args.OutputFolder!, outputFilesBefore, cancellationToken, 120);

        string summary = BuildSummary(projectName, selectedIfcFiles.Count, idsFilesToSave.Count, selectedCommands, args.OutputFolder!, firstNewFile, diagnostics, arguments, args.EnableDiagnostics);
        return Result.Text.Succeeded(summary);
    }

    private static IExtensionResult? ResolveProject(
        InfraApiWrapper api,
        string rawProjectName,
        List<string> diagnostics,
        out string projectName,
        out string projectPath)
    {
        projectName = string.Empty;
        projectPath = string.Empty;

        string? projectsLocation;
        try
        {
            projectsLocation = api.GetCommonProjectsLocation();
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

        Dictionary<string, string> projects;
        try
        {
            projects = api.ScanAllProjects();
        }
        catch (Exception ex) when (ex is InvalidOperationException or MissingMethodException)
        {
            diagnostics.Add($"ScanAllProjectsFallback={ex.Message}");
            projects = InfraApiCollectorHelpers.ScanProjectsFallback();
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Failed to scan projects: {ex.Message}");
        }

        if (projects.Count == 0)
        {
            diagnostics.Add("ScanAllProjects returned no projects; using fallback scan.");
            projects = InfraApiCollectorHelpers.ScanProjectsFallback();
        }

        diagnostics.Add($"ScanAllProjectsCount={projects.Count}");
        if (projects.Count > 0)
        {
            string sample = string.Join(", ", projects.Take(5).Select(pair => $"{pair.Value}|{pair.Key}"));
            diagnostics.Add($"ScanAllProjectsSample={sample}");
        }

        if (string.IsNullOrWhiteSpace(rawProjectName)
            || string.Equals(rawProjectName, "INFO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawProjectName, "ERROR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawProjectName, "__collector_probe__", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Text.Failed("Select a valid project from Project name.");
        }

        projectName = rawProjectName.Trim();
        string resolvedProjectName = projectName;
        projectPath = projects
            .Where(pair => string.Equals(pair.Value, resolvedProjectName, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .FirstOrDefault()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return Result.Text.Failed($"Selected project path is missing: {projectName}");
        }

        return null;
    }

    private static IExtensionResult? ResolveIdsFiles(
        InfraApiWrapper api,
        string projectPath,
        List<string> explicitIdsSelection,
        out List<string> idsFilesToSave)
    {
        if (explicitIdsSelection.Count > 0)
        {
            string idsRoot = Path.GetFullPath(Path.Combine(projectPath, "IDS"));

            idsFilesToSave = explicitIdsSelection
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(Path.Combine(idsRoot, path.Trim())))
                .Where(fullPath =>
                    fullPath.StartsWith(idsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(fullPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            string idsPath = Path.Combine(projectPath, "IDS");

            List<string> scannedIdsFiles;
            try
            {
                scannedIdsFiles = api.ScanIdsFiles(idsPath);
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

        return null;
    }

    private static string BuildSummary(
        string projectName,
        int ifcCount,
        int idsCount,
        List<InfraCommand> commands,
        string outputFolder,
        string? firstNewFile,
        List<string> diagnostics,
        string launchArguments,
        bool includeDiagnostics)
    {
        string commandList = string.Join(", ", commands);

        string summary = firstNewFile is not null
            ? $"INFRA validation for project '{projectName}' with {ifcCount} IFC file(s), {idsCount} IDS file(s), and {commands.Count} command(s): {commandList}."
                + $"\nFirst output file: {firstNewFile}"
                + $"\nOutput folder: {outputFolder}"
            : $"INFRA validation for project '{projectName}' launched with {ifcCount} IFC file(s), {idsCount} IDS file(s), and {commands.Count} command(s): {commandList}."
                + $"\nNo output file created yet. Check result in INFRA or in output folder: {outputFolder}";

        if (includeDiagnostics)
        {
            summary += "\nDiagnostics:";
            summary += string.Concat(diagnostics.Select(entry => $"\n- {entry}"));
            summary += $"\n- LaunchArguments='{launchArguments}'";
        }

        return summary;
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
}
