# StreamBIM File Uploader

Uploads selected files from a local folder to StreamBIM.

## Inputs

- Credential application id: Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to upload files to. After changing this field, click Reload before expecting the file suggestions to change.
- Upload folder: Local source folder containing the files to upload.
- Files to upload: One or more file or folder paths relative to the upload folder. Wildcards with `*` and `?` are supported. This list only refreshes when you click Reload.
- Target folder: Optional remote folder path inside the StreamBIM project (e.g., `Uploads/2024`). Leave empty to upload to the project root.
- Skip unchanged files: Skips files that already exist on StreamBIM with the same modified timestamp.

## Recommended workflow

1. Enter the credential application id.
2. Select the StreamBIM project.
3. Click Reload.
4. Select the local upload folder.
5. Open Files to upload and type a file name, folder path, or wildcard pattern.
6. Click Reload to refresh local file suggestions.
7. Repeat the type-and-reload flow until the wanted files appear, then run the extension.

## Notes

- The project picker options are loaded from StreamBIM using the configured credentials.
- The local file picker options are loaded from the selected upload folder.
- The UI does not refresh autofill suggestions automatically. Changing Project or Files to upload does not update suggestions until Reload is clicked.
- Folders named `_backup` and folders ending with `-revs`, and anything inside them, are ignored.
