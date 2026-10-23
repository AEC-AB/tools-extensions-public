using CW.Assistant.Extensions.Assistant.Collectors;

namespace INFRAIFCIDSValidation;

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

            if (projects.Count == 0)
            {
                string reason = projectsLoadedFromApi
                    ? "No INFRA projects found."
                    : $"No INFRA projects found. API load failed: {loadError}";

                options["INFO"] = reason;
                InfraApiCollectorHelpers.Log($"AvailableProjectsCollector: {reason}");
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
            InfraApiCollectorHelpers.Log($"AvailableProjectsCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading projects: {message}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableProjectsCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading projects: {message}"));
        }
        catch (InvalidOperationException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableProjectsCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading projects: {message}"));
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            string message = InfraApiCollectorHelpers.FormatException(ex);
            InfraApiCollectorHelpers.Log($"AvailableProjectsCollector failed: {message}");
            return Task.FromResult(Error($"Failed loading projects: {message}"));
        }
    }
}
