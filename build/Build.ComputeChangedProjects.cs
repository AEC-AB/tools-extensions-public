using LibGit2Sharp;
using Microsoft.Build.Evaluation;
using Nuke.Common;
using Nuke.Common.Utilities.Collections;

partial class Build : NukeBuild
{
    private readonly Dictionary<string, Project> _loadedProject = [];

    private List<string> _projectsToBuild = [];

    Target ComputeChangedProjects => _ => _
        .Executes(() =>
    {
        var gitFolder = Repository.Discover(EnvironmentInfo.WorkingDirectory);
        if (string.IsNullOrWhiteSpace(gitFolder))
        {
            BuildAllProjects("Git repository not found.");
            return;
        }
        var sourceDir = NormalizePath(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(gitFolder)!)!, "src"));
        using var repo = new Repository(gitFolder);

        var currentBranch = repo.Head;
        var otherBranch = repo.Branches[PullRequestBaseBranch];
        if (currentBranch?.Tip == null || otherBranch?.Tip == null)
        {
            BuildAllProjects($"Base branch '{PullRequestBaseBranch}' not found or has no commits.");
            return;
        }

        var changes = repo.Diff.Compare<TreeChanges>(
            otherBranch.Tip.Tree,
            currentBranch.Tip.Tree
        );

        var projects = Directory.GetFiles(sourceDir, "*.csproj", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var changedFiles = changes
            .Where(x => x.Status is ChangeKind.Added or ChangeKind.Modified or ChangeKind.Deleted or ChangeKind.Renamed)
            .Select(x => x.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var changedPropsFiles = changedFiles
            .Where(path => Path.GetFileName(path) == "Directory.Build.props")
            .ToList();

        var projectsToBuild = new List<string>();

        // If any Directory.Build.props files changed, rebuild all projects in the corresponding integration (all projects in subdirectories of the changed props file)
        if (changedPropsFiles.Count > 0)
        {
            var changedProps = changedPropsFiles
                .Select(path => Path.GetDirectoryName(path)!)
                .Select(dir => NormalizePath(dir))
                .ToList();

            var subProjects = projects
                 .Where(projDir => changedProps.Any(c => projDir.Contains(c)));

            projectsToBuild.AddRange(subProjects);
        }

        foreach (var projectPath in projects)
        {
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var changesInProject = changedFiles
                .Where(path => NormalizePath(path).StartsWith(NormalizePath(projectDir) + "/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (changesInProject.Count > 0)
            {
                projectsToBuild.Add(projectPath);
            }
        }

        _projectsToBuild.AddRange(projectsToBuild
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase));

        // Print projects to build
        Console.WriteLine("The following projects will be built:");
        foreach (var project in _projectsToBuild)
        {
            Console.WriteLine(project.TrimStart(sourceDir).TrimStart('/'));
        }
    });

    void BuildAllProjects(string reason)
    {
        var sourceDir = NormalizePath(Path.Combine(EnvironmentInfo.WorkingDirectory, "src"));
        var projects = Directory.GetFiles(sourceDir, "*.csproj", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"ComputeChangedProjects fallback: {reason} Building all projects.");
        _projectsToBuild.AddRange(projects);
    }

    static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim();
}
