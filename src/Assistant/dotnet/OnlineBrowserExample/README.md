# Online browser example

## Description

Try Assistant's online file and folder fields with a fictional catalog. This extension needs no account or network connection. Running it reports the selected values; it does not read, copy, upload, or download files.

The example requires an Assistant SDK release containing `IOnlineBrowser<TArgs>` and the typed online browser fields. For local testing before that SDK is published, build against the matching contracts from the Assistant feature branch. Local package overrides belong outside this project.

## Configuration

- **Example project:** Changes the scope of saved browser locations and favorites. Both projects use the same fictional catalog.
- **One file:** A typed `OnlineBrowserSelection`. Read its nullable `SelectedItem` when the command runs.
- **One folder:** The same control restricted to folders. Folder navigation remains available in file-only pickers.
- **Remote items:** A typed `OnlineBrowserSelections` list. Browse repeatedly to add items; each browser opening selects one item.
- **Download destinations:** An `OnlineBrowserCopy` list pairing a remote item with a local destination folder.
- **Upload destinations:** The same list in `LocalToRemote` direction. Each row has a local source and a remote destination; an empty destination represents the project root.

All browser fields share favorites within this extension for the same project, using the same `FavoritesKey` and `ScopeProperties`. Each field retains its own last-used location. A field without a selected value opens at Home.

## Functionality

1. Add this project as an Assistant extension in Develop mode.
2. Browse **One file**, expand **Models**, and select a file. Browse **One folder** to select a folder.
3. Add several entries to **Remote items**, one per browser opening.
4. Add download or upload rows to inspect the source–target controls. No transfer is performed by this example.
5. Use the star to save a favorite. Open another browser field to see the same favorite for the current project.
6. Use **Load more** to see the next two catalog items. Reopen a populated field to see its folder path expanded alongside the other roots.
7. Run the extension to inspect the typed values.

## Implementing your provider

[OnlineBrowserExampleArgs.cs](./OnlineBrowserExampleArgs.cs) declares the fields. [ExampleFilesBrowser.cs](./ExampleFilesBrowser.cs) supplies the catalog through `IOnlineBrowser<OnlineBrowserExampleArgs>`. [OnlineBrowserExampleCommand.cs](./OnlineBrowserExampleCommand.cs) reads the hydrated selections without parsing JSON.

```csharp
[OnlineBrowserField(typeof(ExampleFilesBrowser),
    SelectionKind = OnlineBrowserSelectionKind.Files,
    FavoritesKey = "online-browser-example.content",
    ScopeProperties = [nameof(Project)])]
public OnlineBrowserSelection File { get; set; } = new();
```

`GetChildrenAsync` returns one page of immediate children. A null `ParentId` means Home; a folder's returned `Id` becomes the next request's `ParentId`. The continuation token includes the folder and project, so paging requires no state on the provider instance. Assistant creates a new provider for each request.

`GetLocationAsync` resolves a saved reference by ID, or by value when no ID was saved. It returns current item metadata and its folders in root-to-leaf order. For a folder, include that folder; for a file, include its containing folder. Here the ID differs from the saved path, so changing a catalog item's path without changing its ID demonstrates how a real provider can handle a rename.

**Archive** demonstrates `CanSelect = false`: it can be expanded and favorited, while its files remain selectable. Scope is a non-secret project name. A real provider should use its own secure credential store and pass cancellation through to its service calls.

## Troubleshooting

- **The project does not compile:** Restore an SDK containing the online browser contracts. An older published SDK does not contain this feature.
- **A saved item cannot be resolved:** Reopen the browser and choose a current catalog entry. This example deliberately does not fall back to a reused path when a saved ID is missing.
- **A listing token is rejected:** Reopen the browser. Tokens are specific to the current folder and project and are not saved with a task.
- **An empty copy row is invalid:** Fill in its source and destination, or remove it. Upload rows may leave the destination empty to represent the project root.

## FAQ

**Can this example connect to a cloud service?**

Replace the catalog lookup with your service client. Keep the provider contract, map your item IDs and values, and return only one page of children at a time. The extension command owns the actual transfer or other action.

**Can I reuse a built-in browser instead?**

Use the `OnlineBrowserProvider` constructor and name the required context properties, such as `ProjectProperty` for SharePoint. That path needs no custom provider class. Authentication and favorites come from the native browser.

## Resources

- [Extension development documentation](../../../../docs/README.md)
- [StreamBIM Downloader](../StreamBIMDownloader/README.md) and [Uploader](../StreamBIMUploader/README.md) use the fields with an actual service.

## Version history

- Initial example with fictional folders, files, paging, stable IDs, navigation-only folders, shared favorites, and typed transfer rows.
