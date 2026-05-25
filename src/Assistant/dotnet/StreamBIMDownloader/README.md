# StreamBIM File Downloader

Downloads selected files from StreamBIM to a local folder.

## Inputs

- Credential application id: Credential Manager entry used for the StreamBIM login.
- Root folder: Starting folder in StreamBIM.
- Download folder: Local destination folder.
- Max depth: Maximum folder depth used when collecting selectable files.
- Files to download: One or more relative file or folder paths under the root folder. Wildcards with `*` and `?` are supported.
- Skip unchanged files: Skips files that already exist locally with the same modified timestamp.

## Notes

- The file picker options are loaded from StreamBIM using the configured credentials.
- Folders ending with `-revs` are ignored.