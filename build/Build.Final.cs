using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    Target Final => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Console.WriteLine("Finished");
        });
}
