using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Build.Evaluation;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    [Parameter("Event name from GitHub Actions.")]
    readonly string EventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") ?? string.Empty;

    [Parameter("Pull request base branch")]
    readonly string PullRequestBaseBranch = Environment.GetEnvironmentVariable("GITHUB_BASE_REF") ?? "origin/main";

    [Parameter("Pull request base SHA.")]
    readonly string PullRequestBaseSha = string.Empty;

    [Parameter("Push before SHA.")]
    readonly string PushBeforeSha = string.Empty;

    [GitRepository]
    readonly GitRepository GitRepository = null!;

    [Parameter("GitHub output file path.")]
    readonly string OutputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT") ?? string.Empty;

    Target DetermineChanged => _ => _
        .Executes(() =>
        {
            var projects = GetChangedProjects().ToList();
            var needsNet10 = projects.Any(project => NormalizePath(project).StartsWith("Assistant/", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                File.AppendAllLines(OutputPath, new[]
                {
                    $"projects={System.Text.Json.JsonSerializer.Serialize(projects)}",
                    $"needs_net10={needsNet10.ToString().ToLowerInvariant()}"
                });
            }

            LogProjects(projects);
        });

    Target BuildChanged => _ => _
        .Executes(() =>
        {
            var projects = GetChangedProjects().ToList();
            if (projects.Count == 0)
            {
                Console.WriteLine("No projects to build.");
                return;
            }

            foreach (var project in projects)
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

                foreach (var configuration in releaseConfigurations)
                {
                    Console.WriteLine($"Building {project} ({configuration})");
                    DotNetBuild(settings => settings
                        .SetProjectFile(project)
                        .SetConfiguration(configuration));
                }
            }
        });

    public static int Main() => Execute<Build>(x => x.BuildChanged);

    IEnumerable<string> GetChangedProjects()
    {
        var repoRoot = Repository.Discover(EnvironmentInfo.WorkingDirectory);
        using var repo = new Repository(repoRoot);

        var currentBranch = repo.Head;     
        var otherBranch = repo.Branches[PullRequestBaseBranch];

        var changes = repo.Diff.Compare<TreeChanges>(
            currentBranch.Tip.Tree,
            otherBranch.Tip.Tree
        );


        var allProjects = RunGitLines("ls-files \"**/*.csproj\"")
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var changedFiles = new List<string>();
        var headSha = string.IsNullOrWhiteSpace(GitRepository?.Commit) ? "HEAD" : GitRepository.Commit;

        bool rebuildAll = ShouldRebuildAll(changedFiles, headSha);

        if (!rebuildAll)
        {
            var normalizedFiles = changedFiles.Select(NormalizePath).ToList();
            if (normalizedFiles.Any(path => Regex.IsMatch(path, @"(^|/)Directory\.Build\.props$", RegexOptions.IgnoreCase)))
            {
                rebuildAll = true;
            }
        }

        if (rebuildAll)
        {
            return allProjects;
        }

        var changedFileSet = new HashSet<string>(changedFiles.Select(NormalizePath), StringComparer.OrdinalIgnoreCase);
        var projectsToBuild = new List<string>();
        foreach (var project in allProjects)
        {
            var projectDir = NormalizePath(Path.GetDirectoryName(project) ?? string.Empty);
            var prefix = projectDir.Length == 0 ? string.Empty : projectDir + "/";
            if (changedFileSet.Any(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                projectsToBuild.Add(project);
            }
        }

        return projectsToBuild
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase);
    }

    private bool ShouldRebuildAll(List<string> changedFiles, string headSha)
    {
        var rebuildAll = false;
        if (string.Equals(EventName, "workflow_dispatch", StringComparison.OrdinalIgnoreCase))
        {
            rebuildAll = true;
        }
        else if (string.Equals(EventName, "pull_request", StringComparison.OrdinalIgnoreCase))
        {
            var baseSha = PullRequestBaseSha;
            if (string.IsNullOrWhiteSpace(baseSha))
            {
                rebuildAll = true;
            }
            else
            {
                changedFiles.AddRange(RunGitLines($"diff --name-only {baseSha} {headSha}"));
            }
        }
        else
        {
            var baseSha = PushBeforeSha;
            if (string.IsNullOrWhiteSpace(baseSha) ||
                baseSha.Equals("0000000000000000000000000000000000000000", StringComparison.OrdinalIgnoreCase))
            {
                rebuildAll = true;
            }
            else
            {
                changedFiles.AddRange(RunGitLines($"diff --name-only {baseSha} {headSha}"));
            }
        }

        return rebuildAll;
    }


    private static IEnumerable<string> GetProjectConfigurations(string projectPath)
    {
        var project = new Project(projectPath);
        return
            project.ConditionedProperties.TryGetValue("Configuration", out var values)
                ? values
                : [];
    }

    private static void LogProjects(IReadOnlyCollection<string> projects)
    {
        Console.WriteLine("Projects to build:");
        if (projects.Count == 0)
        {
            Console.WriteLine(" - none");
            return;
        }

        foreach (var project in projects)
        {
            Console.WriteLine($" - {project}");
        }
    }

    static IEnumerable<string> RunGitLines(string arguments) =>
        RunProcessLines("git", arguments);

    static IEnumerable<string> RunProcessLines(string fileName, string arguments)
    {
        var process = ProcessTasks.StartProcess(fileName, arguments, logOutput: false);
        process.AssertZeroExitCode();
        return process.Output.Select(output => output.Text).Where(text => !string.IsNullOrWhiteSpace(text));
    }

    static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim();
}
