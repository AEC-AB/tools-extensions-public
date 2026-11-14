using CW.Assistant.Extensions.Contracts.Browsing;
using CW.Assistant.Extensions.Contracts.Controls;
using CW.Assistant.Extensions.Contracts.Fields;

namespace OnlineBrowserExample;

public enum ExampleProject
{
    Campus,
    CivicCenter,
}

public sealed class OnlineBrowserExampleArgs
{
    private const string SharedFavorites = "online-browser-example.content";

    [OptionsField(Label = "Example project")]
    public ExampleProject Project { get; set; } = ExampleProject.Campus;

    [OnlineBrowserField(typeof(ExampleFilesBrowser),
        Label = "One file", SelectionKind = OnlineBrowserSelectionKind.Files,
        FavoritesKey = SharedFavorites, ScopeProperties = [nameof(Project)])]
    public OnlineBrowserSelection File { get; set; } = new();

    [OnlineBrowserField(typeof(ExampleFilesBrowser),
        Label = "One folder", SelectionKind = OnlineBrowserSelectionKind.Folders,
        FavoritesKey = SharedFavorites, ScopeProperties = [nameof(Project)])]
    public OnlineBrowserSelection Folder { get; set; } = new();

    [OnlineBrowserField(typeof(ExampleFilesBrowser),
        Label = "Remote items", SelectionKind = OnlineBrowserSelectionKind.FilesAndFolders,
        FavoritesKey = SharedFavorites, ScopeProperties = [nameof(Project)])]
    public OnlineBrowserSelections Items { get; set; } = new();

    [OnlineBrowserCopyField(typeof(ExampleFilesBrowser),
        Label = "Download destinations", SelectionKind = OnlineBrowserSelectionKind.FilesAndFolders,
        DestinationKind = OnlineBrowserDestinationKind.Folder,
        FavoritesKey = SharedFavorites, ScopeProperties = [nameof(Project)],
        HelperText = "Example configuration only. Running this extension does not download files.")]
    public OnlineBrowserCopy Downloads { get; set; } = new();

    [OnlineBrowserCopyField(typeof(ExampleFilesBrowser),
        Label = "Upload destinations", Direction = OnlineBrowserCopyDirection.LocalToRemote,
        SelectionKind = OnlineBrowserSelectionKind.Folders, AllowEmptyDestination = true,
        FavoritesKey = SharedFavorites, ScopeProperties = [nameof(Project)],
        HelperText = "Example configuration only. Running this extension does not upload files.")]
    public OnlineBrowserCopy Uploads { get; set; } = new();
}
