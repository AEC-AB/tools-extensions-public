using Microsoft.Build.Locator;
using Nuke.Common;

partial class Build : NukeBuild
{
    [Parameter("Pull request base branch")]
    public string PullRequestBaseBranch { get; } = Environment.GetEnvironmentVariable("GITHUB_BASE_REF") ?? "origin/main";

    public static int Main()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
        return Execute<Build>(x => x.Final);
    }
}
