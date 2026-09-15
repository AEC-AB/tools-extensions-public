# StreamBIM File Downloader

Downloads selected files and folders from StreamBIM to a local destination folder.

## Before you start

Store the StreamBIM username and password in Windows Credential Manager. Use the saved credential's application ID when running this extension.

## Inputs

- StreamBIM Credentials: Application ID of the Windows Credential Manager entry used for the StreamBIM login.
- Project: StreamBIM project to download from.
- Destination folder: Local folder where downloaded files are saved.
- Files/Folders to download: One or more project-relative paths, such as `Planning/Electrical/design.pdf`. Wildcards with `*` and `?` are supported.
- Skip unchanged files: Skips files that already exist locally with the same modified timestamp.

## Downloading files

1. Enter the credential application ID.
2. Select the StreamBIM project.
3. Choose the destination folder.
4. Enter one file, folder, or wildcard path per row. For example, enter `Planning/Electrical/design.pdf` to download one file, or `Planning/Electrical` to download its files and subfolders.
5. Run the extension.

Individual files are saved directly in the destination folder. When downloading a folder, its subfolder structure is preserved below the destination folder.

## Results and recovery

- If all requested files are downloaded or skipped as unchanged, the extension succeeds.
- If only some files are available or downloadable, the extension returns partial success and lists each warning.
- If none of the requested files are found, the extension fails.
- If the credential cannot be found, confirm that its application ID matches the Windows Credential Manager entry.
- If a path is reported as missing, verify the project-relative path and try again. The reported message names the last folder that was found, so it shows where the path stops matching StreamBIM.

## Notes

- Paths are relative to the selected project. When a project contains a root folder with the same name as the project itself, keep that folder in the path exactly as the suggestions show it. A path that starts with the project name is tried as a real folder first, and only if that folder does not exist is the leading project name treated as a redundant prefix and removed, so pasting a full path that repeats the project name also works.
- Folders named `_backup` and folders ending with `-revs`, and anything inside them, are ignored.
