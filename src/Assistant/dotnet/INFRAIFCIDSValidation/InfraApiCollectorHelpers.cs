using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;

namespace INFRAIFCIDSValidation;

internal static class InfraApiCollectorHelpers
{
    private const string ApiDllName = "AEC.Infra_Assistant_API.dll";
    private const string ApiTypeName = "INFRA_Assistant_API.InfraAssistantApi";
    private static readonly string DiagnosticsPath = BuildDiagnosticsPath();

    private static string BuildDiagnosticsPath()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] relativeParts = { "AEC AB", "Assistant", "Logs", "INFRAIFCIDSValidation.collector.log" };

        if (relativeParts.Any(Path.IsPathRooted))
        {
            throw new InvalidOperationException("Diagnostics path segments must be relative.");
        }

        return Path.Combine(new[] { basePath }.Concat(relativeParts).ToArray());
    }

    [SupportedOSPlatform("windows")]
    public static bool TryCreateApiInstance([NotNullWhen(true)] out object? api, out string error)
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
            var loadContext = new InfraAssemblyLoadContext(apiFolder);

            try
            {
                Assembly assembly = loadContext.LoadFromAssemblyPath(dllPath);
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
            catch (Exception) when (loadContext.Unload_Safe())
            {
                // Unreachable — Unload_Safe always returns false.
                throw;
            }
        }
        catch (ArgumentException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (PathTooLongException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (NotSupportedException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (System.Security.SecurityException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (FileNotFoundException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (FileLoadException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (IOException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (BadImageFormatException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (ReflectionTypeLoadException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (TypeLoadException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (MissingMethodException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (TargetInvocationException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (MemberAccessException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            error = FormatException(ex);
            Log($"TryCreateApiInstance failed: {error}");
            return false;
        }
    }

    private static string? ResolveApiDllPath(string installLocation)
    {
        static string CombineUnderInstall(string basePath, params string[] parts)
        {
            if (parts.Any(Path.IsPathRooted))
            {
                throw new InvalidOperationException("DLL path segments must be relative.");
            }

            return Path.Combine(basePath, Path.Combine(parts));
        }

        var candidates = new[]
        {
            CombineUnderInstall(installLocation, ApiDllName),
            CombineUnderInstall(installLocation, "net10.0-windows", ApiDllName),
            CombineUnderInstall(installLocation, "net9.0-windows", ApiDllName),
            CombineUnderInstall(installLocation, "net8.0-windows", ApiDllName),
        };

        try
        {
            var found = candidates.FirstOrDefault(File.Exists);
            if (found != null)
            {
                return found;
            }

            return Directory
                .EnumerateFiles(installLocation, ApiDllName, SearchOption.AllDirectories)
                .FirstOrDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (System.Security.SecurityException)
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
        catch (UnauthorizedAccessException)
        {
            // Best-effort logging only.
        }
        catch (IOException)
        {
            // Best-effort logging only.
        }
        catch (System.Security.SecurityException)
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
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (ObjectDisposedException)
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
            foreach (string? projectPath in EnumerateFilesSafe(root, "setup.xml").Select(Path.GetDirectoryName))
            {
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

        foreach (string basePath in new[]
        {
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.CommonApplicationData,
        }.Select(Environment.GetFolderPath))
        {
            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            {
                continue;
            }

            string infraRoot = Path.Join(basePath, "AEC AB", "AEC PLUS Infra");
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
            catch (UnauthorizedAccessException)
            {
                // Ignore inaccessible folders.
            }
            catch (DirectoryNotFoundException)
            {
                // Ignore folders that disappear during traversal.
            }
            catch (IOException)
            {
                // Ignore transient IO errors during traversal.
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
            catch (UnauthorizedAccessException)
            {
                // Ignore inaccessible folders.
            }
            catch (DirectoryNotFoundException)
            {
                // Ignore folders that disappear during traversal.
            }
            catch (IOException)
            {
                // Ignore transient IO errors during traversal.
            }

            foreach (string directory in directories)
            {
                pending.Enqueue(directory);
            }
        }
    }

    private sealed class InfraAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _assemblyFolder;

        public InfraAssemblyLoadContext(string assemblyFolder) : base(isCollectible: true)
        {
            _assemblyFolder = assemblyFolder;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName.Name))
            {
                return null;
            }

            string candidatePath = Path.Combine(_assemblyFolder, assemblyName.Name + ".dll");
            return File.Exists(candidatePath) ? LoadFromAssemblyPath(candidatePath) : null;
        }

        /// <summary>Always returns false; used as a no-op in catch-when clauses.</summary>
        public bool Unload_Safe()
        {
            Unload();
            return false;
        }
    }
}
