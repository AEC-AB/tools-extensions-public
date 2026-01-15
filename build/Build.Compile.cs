using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using BuildResult = (string Project, bool Success, string Configuration, string? ErrorMessage);

partial class Build : NukeBuild
{
    Target Compile => _ => _
        .DependsOn(ComputeChangedProjects)
        .OnlyWhenDynamic(() => _projectsToBuild.Count > 0)
        .Executes(() =>
        {
            var buildResults = new List<BuildResult>();
            foreach (var project in _projectsToBuild)
            {
                var configurations = GetProjectConfigurations(project).ToList();
                var releaseConfigurations = configurations
                    .Where(config => config.StartsWith("Release", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (releaseConfigurations.Count == 0)
                {
                    Console.WriteLine($"No Release configurations found for {project}. Skipping.");
                    continue;
                }

                var projectName = Path.GetFileNameWithoutExtension(project);
                foreach (var configuration in releaseConfigurations)
                {
                    Console.WriteLine($"Building {projectName} ({configuration})");
                    try
                    {
                        DotNetBuild(settings => settings
                            .SetProjectFile(project)
                            .SetConfiguration(configuration));

                        Console.WriteLine($"Successfully built {projectName} ({configuration})");
                        buildResults.Add((projectName, true, configuration, null));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to build {projectName} ({configuration}): {e.Message}");
                        buildResults.Add((projectName, false, configuration, e.Message));
                    }
                }
            }

            foreach (var result in buildResults)
            {
                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[SUCCESS] {result.Project} ({result.Configuration}) built successfully.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAILURE] {result.Project} ({result.Configuration}) failed to build. Error: {result.ErrorMessage}");
                }
            }
            
            Console.ResetColor();
            if (buildResults.Any(x => x.Success == false))
            {
                throw new Exception("One or more projects failed to build.");
            }
        });

    private IEnumerable<string> GetProjectConfigurations(string projectPath)
    {
        Project project = LoadProject(projectPath);

        var instance = project.CreateProjectInstance();
        var raw = instance.GetPropertyValue("Configurations");
        var expanded = instance.ExpandString(raw);

        return expanded
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private Project LoadProject(string projectPath)
    {

        if (_loadedProject.TryGetValue(projectPath, out var cachedProject))
        {
            return cachedProject;
        }

        var project = new Project(projectPath);
        return _loadedProject[projectPath] = project;
    }
}
