# StreamBIM File Uploader

## Description

Uploads selected local files to StreamBIM.

## Configuration

Store the StreamBIM username and password in Windows Credential Manager. Use the saved credential's application ID when running this extension.

### Inputs

- StreamBIM Credentials: Application ID of the Windows Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to upload files to.
- Files to upload: Select one or more local files. Each is uploaded directly into the target folder.
- Target folder: Optional remote folder path inside the StreamBIM project (e.g., `Uploads/2024`). Leave empty to upload to the project root. After selecting a project, browse for a destination folder or enter its relative path.
- Verbose diagnostics: Writes a detailed per-file log for troubleshooting. Leave disabled for normal uploads.

## Functionality

1. Enter the credential application ID.
2. Select the StreamBIM project.
3. Select one or more files.
4. Browse for a target folder in StreamBIM, or leave it empty to use the project root.
5. Run the extension.

Selected files are uploaded directly into the target folder.

## Troubleshooting

- The result summarizes processed, uploaded, skipped, and failed files.
- If a selected file no longer exists, select it again and rerun the extension.
- If the credential cannot be found, confirm that its application ID matches the Windows Credential Manager entry.
- With Verbose diagnostics enabled, detailed upload logs are saved under `%TEMP%\StreamBIMUploader`. If an upload is cancelled or times out, use the newest log to identify the last scanned or uploaded path.

## FAQ

- Uploading folders and preserving a local folder structure are not supported. Select the individual files you want to upload.

## Browser and saved tasks

Saved selections appear when the task opens without loading remote items. Opening the browser navigates to the saved location. Project and folder favorites are shared with StreamBIM Downloader for the same user and credential application ID. Folder favorites are also separated by project.

The target folder must already exist. Clear the target folder to upload to the project root.

## Version History

- Argument version 2 uses online browsers for the project and target folder. Existing saved tasks are upgraded automatically, preserving their paths, local files, credential application ID, and diagnostic setting.
