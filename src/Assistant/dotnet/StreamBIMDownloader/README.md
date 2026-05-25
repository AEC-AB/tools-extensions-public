# StreamBIM File Downloader

Downloads selected files from StreamBIM to a local folder.

## Inputs

- Credential application id: Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to browse and download files from.
- Download folder: Local destination folder.
- Files to download: One or more file or folder paths inside the selected project. Wildcards with `*` and `?` are supported.
- Skip unchanged files: Skips files that already exist locally with the same modified timestamp.

## Notes

- The project and file picker options are loaded from StreamBIM using the configured credentials.
- Folders ending with `-revs`, and anything inside them, are ignored.