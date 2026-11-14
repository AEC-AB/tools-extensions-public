using System.Text.Json;
using CW.Assistant.Extensions.Contracts.Browsing;
using CW.Assistant.Extensions.Contracts.Controls;
using CW.Assistant.Extensions.Contracts.Upgrade;

namespace StreamBIMUploader;

public sealed class StreamBIMUploaderArgsV1
{
    public string ApplicationName { get; set; } = "StreamBIM";
    public string Project { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
    public string TargetFolder { get; set; } = string.Empty;
    public bool VerboseDiagnostics { get; set; }
}

public sealed class StreamBIMUploaderArgsUpgradeV1ToV2 : IArgsUpgrade<StreamBIMUploaderArgsV1, StreamBIMUploaderArgs>
{
    public StreamBIMUploaderArgs Upgrade(StreamBIMUploaderArgsV1 previousVersion)
    {
        var paths = (previousVersion.Files ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
        if (paths.Count == 0 && !string.IsNullOrWhiteSpace(previousVersion.TargetFolder))
            paths.Add(string.Empty);
        var targets = paths.Select(path => new OnlineBrowserCopyTarget
        {
            Source = new OnlineBrowserSelectionItem
            {
                Value = path,
                Name = System.IO.Path.GetFileName(path),
                Kind = OnlineBrowserItemKind.File,
            },
            Destination = previousVersion.TargetFolder,
            DestinationItem = CreateDestination(previousVersion),
        }).ToArray();

        return new StreamBIMUploaderArgs
        {
            ApplicationName = previousVersion.ApplicationName,
            Project = previousVersion.Project,
            Transfers = new OnlineBrowserCopy { Targets = targets, Json = JsonSerializer.Serialize(targets) },
            VerboseDiagnostics = previousVersion.VerboseDiagnostics,
        };
    }

    private static OnlineBrowserSelectionItem? CreateDestination(StreamBIMUploaderArgsV1 previousVersion) =>
        string.IsNullOrWhiteSpace(previousVersion.TargetFolder) ? null : new()
        {
            Value = previousVersion.TargetFolder,
            Name = previousVersion.TargetFolder.TrimEnd('/', '\\').Split('/', '\\').LastOrDefault() ?? previousVersion.TargetFolder,
            Kind = OnlineBrowserItemKind.Folder,
            Provider = OnlineBrowserProvider.Custom,
            Scope = $"{previousVersion.ApplicationName}:{previousVersion.Project}",
        };
}
