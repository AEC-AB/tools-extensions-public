using CW.Assistant.Extensions.Assistant.Collectors;

namespace INFRAIFCIDSValidation;

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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading IDS files: {message}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading IDS files: {message}"));
        }
        catch (InvalidOperationException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading IDS files: {message}"));
        }
        catch (ArgumentException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableIdsFilesCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading IDS files: {message}"));
        }
    }
}
