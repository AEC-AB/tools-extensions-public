namespace INFRAIFCIDSValidation;

/// <summary>
/// Typed wrapper over the dynamically-loaded INFRA API instance.
/// Exposes named methods instead of raw reflection calls.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class InfraApiWrapper
{
    private readonly object _api;

    private InfraApiWrapper(object api)
    {
        _api = api;
    }

    /// <summary>
    /// Loads the INFRA API DLL and returns a wrapper, or sets <paramref name="error"/> and returns null on failure.
    /// </summary>
    public static InfraApiWrapper? TryCreate(out string error)
    {
        if (!InfraApiCollectorHelpers.TryCreateApiInstance(out object? api, out error))
        {
            return null;
        }

        string[] requiredMethods =
        [
            nameof(GetCommonProjectsLocation),
            nameof(ScanAllProjects),
            nameof(ScanIdsFiles),
            nameof(CreateMetadataFile),
            nameof(WriteProjectPathToRegistry),
            nameof(SaveSelectedIdsFilesToRegistry),
            nameof(WriteOutputDirectoryToRegistry),
            nameof(LaunchInfraAutomation)
        ];

        Type apiType = api!.GetType();
        string[] missingMethods = requiredMethods
            .Where(methodName => apiType.GetMethod(methodName) is null)
            .ToArray();
        if (missingMethods.Length > 0)
        {
            error = $"The installed INFRA API is incompatible. Missing required methods: {string.Join(", ", missingMethods)}. Update or repair the INFRA installation to a version compatible with this extension.";
            return null;
        }

        return new InfraApiWrapper(api!);
    }

    public string? GetCommonProjectsLocation()
        => Invoke<string?>("GetCommonProjectsLocation");

    public Dictionary<string, string> ScanAllProjects()
        => Invoke<Dictionary<string, string>>("ScanAllProjects") ?? [];

    public List<string> ScanIdsFiles(string idsPath)
        => Invoke<List<string>>("ScanIdsFiles", idsPath) ?? [];

    public void CreateMetadataFile(string projectName, string[] ifcFiles)
        => Invoke<object?>("CreateMetadataFile", projectName, ifcFiles);

    public void WriteProjectPathToRegistry(string projectName, string projectPath)
        => Invoke<object?>("WriteProjectPathToRegistry", projectName, projectPath);

    public void SaveSelectedIdsFilesToRegistry(string projectName, List<string> idsFiles)
        => Invoke<object?>("SaveSelectedIdsFilesToRegistry", projectName, idsFiles);

    public void WriteOutputDirectoryToRegistry(string projectName, string outputDirectory)
        => Invoke<object?>("WriteOutputDirectoryToRegistry", projectName, outputDirectory);

    public void LaunchInfraAutomation(string arguments, string projectName)
        => Invoke<object?>("LaunchInfraAutomation", arguments, projectName);

    private T? Invoke<T>(string methodName, params object?[] parameters)
    {
        MethodInfo? method = _api.GetType().GetMethod(methodName);
        if (method == null)
        {
            throw new MissingMethodException($"Method not found: {methodName}");
        }

        try
        {
            object? result = method.Invoke(_api, parameters);
            if (result is T typed)
            {
                return typed;
            }

            return default;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            throw new InvalidOperationException(InfraApiCollectorHelpers.FormatException(tie), tie.InnerException);
        }
    }
}
