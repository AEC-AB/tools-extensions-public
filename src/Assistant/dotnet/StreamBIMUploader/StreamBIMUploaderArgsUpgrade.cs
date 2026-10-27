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
        return new StreamBIMUploaderArgs
        {
            ApplicationName = previousVersion.ApplicationName,
            Project = CreateSelection(previousVersion.Project, previousVersion.ApplicationName),
            Files = previousVersion.Files ?? [],
            TargetFolder = CreateSelection(previousVersion.TargetFolder, $"{previousVersion.ApplicationName}:{previousVersion.Project}"),
            VerboseDiagnostics = previousVersion.VerboseDiagnostics,
        };
    }

    private static OnlineBrowserSelection CreateSelection(string? value, string? scope)
    {
        OnlineBrowserSelectionItem? item = string.IsNullOrWhiteSpace(value) ? null : new()
        {
            Value = value,
            Name = value.TrimEnd('/', '\\').Split('/', '\\').LastOrDefault() ?? value,
            Kind = OnlineBrowserItemKind.Folder,
            Provider = OnlineBrowserProvider.Custom,
            Scope = scope,
        };
        return new OnlineBrowserSelection { SelectedItem = item, Json = JsonSerializer.Serialize(item) };
    }
}
