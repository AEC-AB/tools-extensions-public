# StreamBIM File Uploader

## Description

Uploads selected local files to StreamBIM.

## Configuration

Store the StreamBIM username and password in Windows Credential Manager. Use the saved credential's application ID when running this extension.

### Inputs

- StreamBIM Credentials: Application ID of the Windows Credential Manager entry used for the StreamBIM login.
- Project: Choose the StreamBIM project from the dropdown. Changing the credential application ID refreshes the available projects.
- Upload project files: Add one row per local source file. Use the file button in **From** to select the file and the cloud button in **To** to browse for its StreamBIM target folder. You can enter a project-relative target path such as `Uploads/Models`, or leave **To** empty to use the project root. Each row can use a different target folder.
- Verbose diagnostics: Writes a detailed per-file log for troubleshooting. Leave disabled for normal uploads.

## Functionality

1. Enter the credential application ID.
2. Select the StreamBIM project.
3. Add local files to **Upload project files** using the file button below the rows.
4. Choose a target folder for each row, or leave its **To** path empty to use the project root. The row menu lets you duplicate or remove a row.
5. Run the extension.

Each local file is uploaded into its row's target folder using its original filename. Rows run in order, and the result combines uploaded, skipped, and failed files from all rows. If multiple rows target the same remote filename, later rows can overwrite earlier uploads.

## Troubleshooting

- The result summarizes processed, uploaded, skipped, and failed files.
- If a selected file no longer exists, select it again and rerun the extension.
- If the credential cannot be found, confirm that its application ID matches the Windows Credential Manager entry.
- With Verbose diagnostics enabled, detailed upload logs are saved under `%TEMP%\StreamBIMUploader`. If an upload is cancelled or times out, use the newest log to identify the last scanned or uploaded path.

## FAQ

- Uploading folders and preserving a local folder structure are not supported. Select the individual files you want to upload.

## Browser and saved tasks

Saved selections appear when the task opens without loading remote items. Opening a populated target folder navigates to its saved location; an empty target opens at Home. Folder favorites are shared with StreamBIM Downloader for the same user, credential application ID, and project.

Each target folder must already exist. Clear a row's target path to upload to the project root. Saved version 1 tasks are upgraded automatically, preserving their project, local files, target folder, credentials, and diagnostic setting.

## Resources

- [StreamBIM](https://streambim.com/)

## Version History

- Argument version 2 uses a project dropdown and source–target rows. Version 1 tasks upgrade directly, preserving project, local files, target folder, credentials, and the diagnostic setting.
- Argument version 1 uses local file selections and a shared remote target folder.
