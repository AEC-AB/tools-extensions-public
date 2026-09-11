# StreamBIM File Uploader

Uploads selected local files to StreamBIM.

## Before you start

Store the StreamBIM username and password in Windows Credential Manager. Use the saved credential's application ID when running this extension.

## Inputs

- StreamBIM Credentials: Application ID of the Windows Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to upload files to.
- Files to upload: Select one or more local files. Each is uploaded directly into the target folder.
- Target folder: Optional remote folder path inside the StreamBIM project (e.g., `Uploads/2024`). Leave empty to upload to the project root. After selecting a project, type part of a folder path and click Reload to see matching folder suggestions.
- Verbose diagnostics: Writes a detailed per-file log for troubleshooting. Leave disabled for normal uploads.

## Uploading files

1. Enter the credential application ID.
2. Select the StreamBIM project.
3. Select one or more files.
4. Optionally enter a target folder in StreamBIM.
5. Run the extension.

Selected files are uploaded directly into the target folder.

## Results and recovery

- The result summarizes processed, uploaded, skipped, and failed files.
- If a selected file no longer exists, select it again and rerun the extension.
- If the credential cannot be found, confirm that its application ID matches the Windows Credential Manager entry.
- With Verbose diagnostics enabled, detailed upload logs are saved under `%TEMP%\StreamBIMUploader`. If an upload is cancelled or times out, use the newest log to identify the last scanned or uploaded path.

## Notes

- Uploading folders and preserving a local folder structure are not supported. Select the individual files you want to upload.
