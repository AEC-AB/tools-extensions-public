using CW.Assistant.Extensions.Assistant.Collectors;

namespace INFRAIFCIDSValidation;

public enum InfraCommand
{
    IFC_CHECK,
    STEP_SYNTAX,
    IFC_SCHEMA,
    IDS_VALIDATION,
}

public class INFRAIFCIDSValidationArgs
{
    [OptionsField(Label = "Project name", CollectorType = typeof(AvailableProjectsCollector), CollectorSortOrder = SortOrder.SortByAscending)]
    public string AutoProjectName { get; set; } = string.Empty;

    [OptionsField(Label = "IDS files", CollectorType = typeof(AvailableIdsFilesCollector), CollectorSortOrder = SortOrder.SortByAscending)]
    public List<string> AutoSelectedIdsFiles { get; set; } = [];

    [FilePickerField(Label = "IFC files", FileExtensions = ["ifc"], ToolTip = "Select IFC files, or edit raw data to use wildcard paths, Assistant variables, embedded variable placeholders, or regex entries.")]
    public List<string> IfcFiles { get; set; } = [];

    [FolderPickerField(Label = "Output folder")]
    [Required(ErrorMessage = "Output folder is required.")]
    public string? OutputFolder { get; set; }

    [OptionsField(Label = "Validation commands", CollectorType = typeof(AvailableValidationCommandsCollector), CollectorSortOrder = SortOrder.None)]
    public List<string> Commands { get; set; } =
    [
        nameof(InfraCommand.IFC_CHECK),
    ];

    [BooleanField(Label = "Close INFRA on completion")]
    public bool CloseOnCompletion { get; set; }

    [BooleanField(Label = "Enable diagnostics")]
    public bool EnableDiagnostics { get; set; }
}

[SupportedOSPlatform("windows")]
public class AvailableProjectsCollector : IAsyncAutoFillCollector<INFRAIFCIDSValidationArgs>
{
    public Task<Dictionary<string, string>> Get(INFRAIFCIDSValidationArgs args, CancellationToken cancellationToken)
    {
        Dictionary<string, string> Error(string message) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["ERROR"] = message,
        };

        try
        {
            InfraApiCollectorHelpers.Log("AvailableProjectsCollector started.");

            bool projectsLoadedFromApi = false;
            string loadError = string.Empty;
            var projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (InfraApiCollectorHelpers.TryCreateApiInstance(out object? api, out loadError))
            {
                string? projectsLocation = InfraApiCollectorHelpers.Invoke<string?>(api!, "GetCommonProjectsLocation");
                if (!string.IsNullOrWhiteSpace(projectsLocation) && Directory.Exists(projectsLocation))
                {
                    projects = InfraApiCollectorHelpers.Invoke<Dictionary<string, string>>(api!, "ScanAllProjects") ?? [];
                    projectsLoadedFromApi = true;
                }
                else
                {
                    loadError = $"Projects folder not found at: {projectsLocation}";
                    InfraApiCollectorHelpers.Log(loadError);
                }
            }

            if (projects.Count == 0)
            {
                projects = InfraApiCollectorHelpers.ScanProjectsFallback();
                InfraApiCollectorHelpers.Log($"AvailableProjectsCollector fallback discovered {projects.Count} project(s).");
            }
            else
            {
                InfraApiCollectorHelpers.Log($"AvailableProjectsCollector API discovered {projects.Count} project(s).");
            }

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["__collector_probe__"] = "Collector is alive",
            };

            foreach (KeyValuePair<string, string> project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string projectPath = project.Key;
                string projectName = project.Value;
                if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(projectPath))
                {
                    continue;
                }

                if (!options.ContainsKey(projectName))
                {
                    options.Add(projectName, projectName);
                }
            }

            if (options.Count == 0)
            {
                string reason = projectsLoadedFromApi
                    ? "No INFRA projects found."
                    : $"No INFRA projects found. API load failed: {loadError}";

                options["INFO"] = reason;
                InfraApiCollectorHelpers.Log($"AvailableProjectsCollector: {reason}");
            }

            return Task.FromResult(options);
        }
        catch (Exception ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableProjectsCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading projects: {message}"));
        }
    }
}

[SupportedOSPlatform("windows")]
public class AvailableIdsFilesCollector : IAsyncAutoFillCollector<INFRAIFCIDSValidationArgs>
{
    public Task<Dictionary<string, string>> Get(INFRAIFCIDSValidationArgs args, CancellationToken cancellationToken)
    {
        Dictionary<string, string> Error(string message) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["ERROR"] = message,
        };

        try
        {
            string projectName = args.AutoProjectName;

            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector started for ProjectName='{projectName}'.");

            if (string.IsNullOrWhiteSpace(projectName)
                || string.Equals(projectName, "ERROR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(projectName, "INFO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(projectName, "__collector_probe__", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new Dictionary<string, string>());
            }

            bool idsLoadedFromApi = false;
            string loadError = string.Empty;

            Dictionary<string, string> projects;
            if (InfraApiCollectorHelpers.TryCreateApiInstance(out object? api, out loadError))
            {
                string? projectsLocation = InfraApiCollectorHelpers.Invoke<string?>(api!, "GetCommonProjectsLocation");
                if (!string.IsNullOrWhiteSpace(projectsLocation) && Directory.Exists(projectsLocation))
                {
                    projects = InfraApiCollectorHelpers.Invoke<Dictionary<string, string>>(api!, "ScanAllProjects") ?? [];
                    idsLoadedFromApi = true;
                }
                else
                {
                    loadError = $"Projects folder not found at: {projectsLocation}";
                    InfraApiCollectorHelpers.Log(loadError);
                    projects = InfraApiCollectorHelpers.ScanProjectsFallback();
                }
            }
            else
            {
                projects = InfraApiCollectorHelpers.ScanProjectsFallback();
            }

            string? projectPath = projects
                .Where(pair => string.Equals(pair.Value, projectName, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                InfraApiCollectorHelpers.Log("AvailableIdsFilesCollector did not find matching project path.");
                return Task.FromResult(new Dictionary<string, string>
                {
                    ["INFO"] = "No matching project path found for selected project.",
                });
            }

            string idsPath = Path.Combine(projectPath, "IDS");
            List<string> idsFiles;
            if (idsLoadedFromApi && InfraApiCollectorHelpers.TryCreateApiInstance(out object? idsApi, out _))
            {
                idsFiles = InfraApiCollectorHelpers.Invoke<List<string>>(idsApi!, "ScanIdsFiles", idsPath) ?? [];
            }
            else
            {
                idsFiles = InfraApiCollectorHelpers.ScanIdsFilesFallback(idsPath);
                InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector fallback discovered {idsFiles.Count} IDS file(s).");
            }

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string filePath in idsFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(idsPath, filePath);

                if (!options.ContainsKey(relativePath))
                {
                    options.Add(relativePath, relativePath);
                }
            }

            if (options.Count == 0)
            {
                string reason = idsLoadedFromApi
                    ? "No IDS files found for selected project."
                    : $"No IDS files found for selected project. API load failed: {loadError}";

                options["INFO"] = reason;
                InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector: {reason}");
            }

            return Task.FromResult(options);
        }
        catch (Exception ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading IDS files: {message}"));
        }
    }
}

[SupportedOSPlatform("windows")]
public class AvailableValidationCommandsCollector : IAsyncAutoFillCollector<INFRAIFCIDSValidationArgs>
{
    public Task<Dictionary<string, string>> Get(INFRAIFCIDSValidationArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(InfraCommand.IFC_CHECK)] = nameof(InfraCommand.IFC_CHECK),
            [nameof(InfraCommand.STEP_SYNTAX)] = nameof(InfraCommand.STEP_SYNTAX),
            [nameof(InfraCommand.IFC_SCHEMA)] = nameof(InfraCommand.IFC_SCHEMA),
            [nameof(InfraCommand.IDS_VALIDATION)] = nameof(InfraCommand.IDS_VALIDATION),
        };

        return Task.FromResult(options);
    }
}

internal static class InfraApiCollectorHelpers
{
    private const string ApiDllName = "AEC.Infra_Assistant_API.dll";
    private const string ApiTypeName = "INFRA_Assistant_API.InfraAssistantApi";
    private static readonly string DiagnosticsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AEC AB",
        "Assistant",
        "Logs",
        "INFRAIFCIDSValidation.collector.log");

    [SupportedOSPlatform("windows")]
    public static bool TryCreateApiInstance(out object? api, out string error)
    {
        api = null;
        error = string.Empty;

        try
        {
            string? installLocation = GetInstallLocation();
            if (string.IsNullOrWhiteSpace(installLocation))
            {
                error = "InstallLocation not found in HKLM (both Registry64 and Registry32 views were checked).";
                Log(error);
                return false;
            }

            string? dllPath = ResolveApiDllPath(installLocation);
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                error = $"API DLL not found under InstallLocation '{installLocation}'.";
                Log(error);
                return false;
            }

            Log($"Using INFRA API DLL: {dllPath}");

            string apiFolder = Path.GetDirectoryName(dllPath) ?? installLocation;

            ResolveEventHandler resolver = (_, eventArgs) =>
            {
                string? requestedName = new AssemblyName(eventArgs.Name).Name;
                if (string.IsNullOrWhiteSpace(requestedName))
                {
                    return null;
                }

                string candidatePath = Path.Combine(apiFolder, requestedName + ".dll");
                return File.Exists(candidatePath) ? Assembly.LoadFrom(candidatePath) : null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += resolver;

            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);
                Type? apiType = assembly.GetType(ApiTypeName)
                    ?? assembly.GetTypes().FirstOrDefault(type => string.Equals(type.Name, "InfraAssistantApi", StringComparison.Ordinal));

                if (apiType == null)
                {
                    error = $"Type '{ApiTypeName}' not found in '{dllPath}'.";
                    Log(error);
                    return false;
                }

                api = Activator.CreateInstance(apiType);
                if (api == null)
                {
                    error = $"Could not create instance of '{apiType.FullName}'.";
                    Log(error);
                    return false;
                }

                return true;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }
        catch (Exception ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
    }

    private static string? ResolveApiDllPath(string installLocation)
    {
        var candidates = new[]
        {
            Path.Combine(installLocation, ApiDllName),
            Path.Combine(installLocation, "net10.0-windows", ApiDllName),
            Path.Combine(installLocation, "net9.0-windows", ApiDllName),
            Path.Combine(installLocation, "net8.0-windows", ApiDllName),
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        try
        {
            return Directory
                .EnumerateFiles(installLocation, ApiDllName, SearchOption.AllDirectories)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public static T? Invoke<T>(object target, string methodName, params object?[] parameters)
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

    public static string FormatException(Exception ex)
    {
        if (ex is TargetInvocationException tie && tie.InnerException != null)
        {
            return $"{tie.InnerException.GetType().Name}: {tie.InnerException.Message}";
        }

        return $"{ex.GetType().Name}: {ex.Message}";
    }

    public static void Log(string message)
    {
        try
        {
            string? diagnosticsDirectory = Path.GetDirectoryName(DiagnosticsPath);
            if (!string.IsNullOrWhiteSpace(diagnosticsDirectory))
            {
                Directory.CreateDirectory(diagnosticsDirectory);
            }

            File.AppendAllText(DiagnosticsPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort logging only.
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetInstallLocation()
    {
        const string baseRegistryPath = @"SOFTWARE\AEC AB\AEC PLUS Infra";

        try
        {
            string? from64 = GetInstallLocationFromView(RegistryView.Registry64, baseRegistryPath);
            if (!string.IsNullOrWhiteSpace(from64))
            {
                return from64;
            }

            return GetInstallLocationFromView(RegistryView.Registry32, baseRegistryPath);
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetInstallLocationFromView(RegistryView view, string baseRegistryPath)
    {
        using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
        using RegistryKey? baseKey = localMachine.OpenSubKey(baseRegistryPath);
        if (baseKey == null)
        {
            return null;
        }

        string[] subKeyNames = baseKey.GetSubKeyNames();
        int highestVersion = subKeyNames
            .Where(name => int.TryParse(name, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();

        if (highestVersion == 0)
        {
            return null;
        }

        string setupPath = $"{baseRegistryPath}\\{highestVersion}\\Setup";
        using RegistryKey? setupKey = localMachine.OpenSubKey(setupPath);
        if (setupKey == null)
        {
            return null;
        }

        return setupKey.GetValue("InstallLocation") as string;
    }

    public static Dictionary<string, string> ScanProjectsFallback()
    {
        var projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in GetFallbackRoots())
        {
            foreach (string setupFile in EnumerateFilesSafe(root, "setup.xml"))
            {
                string? projectPath = Path.GetDirectoryName(setupFile);
                if (string.IsNullOrWhiteSpace(projectPath))
                {
                    continue;
                }

                string projectName = Path.GetFileName(projectPath);
                if (string.IsNullOrWhiteSpace(projectName) || projects.ContainsKey(projectPath))
                {
                    continue;
                }

                projects[projectPath] = projectName;
            }
        }

        return projects;
    }

    public static List<string> ScanIdsFilesFallback(string idsPath)
    {
        if (string.IsNullOrWhiteSpace(idsPath) || !Directory.Exists(idsPath))
        {
            return [];
        }

        return EnumerateFilesSafe(idsPath, "*.ids").ToList();
    }

    private static IEnumerable<string> GetFallbackRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Environment.SpecialFolder folder in new[]
        {
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.CommonApplicationData,
        })
        {
            string basePath = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            {
                continue;
            }

            string infraRoot = Path.Combine(basePath, "AEC AB", "AEC PLUS Infra");
            if (!Directory.Exists(infraRoot))
            {
                continue;
            }

            roots.Add(infraRoot);

            string projects = Path.Combine(infraRoot, "Projects");
            if (Directory.Exists(projects))
            {
                roots.Add(projects);
            }

            string cloudProjects = Path.Combine(infraRoot, "CloudProjects");
            if (Directory.Exists(cloudProjects))
            {
                roots.Add(cloudProjects);
            }
        }

        return roots;
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root, string pattern)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            yield break;
        }

        var pending = new Queue<string>();
        pending.Enqueue(root);

        while (pending.Count > 0)
        {
            string current = pending.Dequeue();

            IEnumerable<string> files = [];
            try
            {
                files = Directory.EnumerateFiles(current, pattern, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                // Ignore inaccessible folders.
            }

            foreach (string file in files)
            {
                yield return file;
            }

            IEnumerable<string> directories = [];
            try
            {
                directories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                // Ignore inaccessible folders.
            }

            foreach (string directory in directories)
            {
                pending.Enqueue(directory);
            }
        }
    }
}
