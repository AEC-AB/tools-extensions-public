# StreamBIM File Downloader

Downloads selected files from StreamBIM to a local folder.

## Inputs

- Credential application id: Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to browse and download files from. After changing this field, click Reload before expecting the file suggestions to change.
- Download folder: Local destination folder.
- Files to download: One or more file or folder paths inside the selected project. Wildcards with `*` and `?` are supported. This list only refreshes when you click Reload.
- Skip unchanged files: Skips files that already exist locally with the same modified timestamp.

## Recommended workflow

1. Enter the credential application id.
2. Select the StreamBIM project.
3. Click Reload.
4. Open Files to download and type part of a folder or file path.
5. Click Reload again to refresh suggestions for the current path.
6. If the first suggestion tells you to select a folder ending with `/`, choose one of the folder suggestions ending with `/` and click Reload once more to load suggestions up to 3 levels deeper from that folder.
7. Repeat the type-and-reload flow until the wanted file or folder appears, then run the extension.

## Notes

- The project and file picker options are loaded from StreamBIM using the configured credentials.
- The UI does not refresh autofill suggestions automatically. Changing Project or Files to download does not update suggestions until Reload is clicked.
- Folders ending with `-revs`, and anything inside them, are ignored.