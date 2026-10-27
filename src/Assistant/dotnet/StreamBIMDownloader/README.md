# StreamBIM File Downloader

Downloads selected files and folders from StreamBIM to a local destination folder.

## Before you start

Store the StreamBIM username and password in Windows Credential Manager. Use the saved credential's application ID when running this extension.

## Inputs

- StreamBIM Credentials: Application ID of the Windows Credential Manager entry used for the StreamBIM login.
- Project: Select the StreamBIM project with the online folder browser.
- Destination folder: Local folder where downloaded files are saved.
- Files/Folders to download: Browse project folders and select one or more files or folders, including items in different folders. Values remain project-relative paths, such as `Planning/Electrical/design.pdf`. You can still enter paths with `*` and `?` wildcards manually.
- Skip unchanged files: Skips files that already exist locally with the same modified timestamp.

## Downloading files

1. Enter the credential application ID.
2. Select the StreamBIM project.
3. Choose the destination folder.
4. Open the files and folders browser. Open folders with the arrow, check the items to download, and confirm with Select. To use a wildcard, enter one path per row manually.
5. Run the extension.

Individual files are saved directly in the destination folder. When downloading a folder, its subfolder structure is preserved below the destination folder.

## Results and recovery

- If all requested files are downloaded or skipped as unchanged, the extension succeeds.
- If only some files are available or downloadable, the extension returns partial success and lists each warning.
- If none of the requested files are found, the extension fails.
- If the credential cannot be found, confirm that its application ID matches the Windows Credential Manager entry.
- If a path is reported as missing, verify the project-relative path and try again. The reported message names the last folder that was found, so it shows where the path stops matching StreamBIM.

## Notes

- Paths are relative to the selected project. When a project contains a root folder with the same name as the project itself, keep that folder in the path exactly as the browser shows it. A path that starts with the project name is tried as a real folder first, and only if that folder does not exist is the leading project name treated as a redundant prefix and removed, so pasting a full path that repeats the project name also works.
- Folders named `_backup` and folders ending with `-revs`, and anything inside them, are ignored.
- The online browser loads one folder at a time. A listing failure appears in the browser; use Refresh to retry. Cancel leaves the field unchanged.
- StreamBIM returns each FTP folder as one complete page. The shared browser also supports Load more for providers with paged APIs.
- File favorites are resolved and selected directly. Folder favorites open the folder for browsing.
- Opening the browser returns to the last selected folder, or the containing folder of the last selected file. Saved paths remain visible before the browser loads any remote items.
- StreamBIM resolves saved references by their FTP path. It does not recover moved or renamed items by ID; extensions backed by services with stable IDs can implement that behavior through the same browser API.
- Use the stars to save favorites. Favorites belong to the current Assistant user and can be shared with compatible extensions using the same favorites keys. File and folder favorites are kept separately for each project and credential application ID.
- Extension developers can share project favorites with `FavoritesKey = "streambim.projects"` and scope `ApplicationName`. Share file and folder favorites with `FavoritesKey = "streambim.project-content"` and scopes `Project` and `ApplicationName`. Use the same saved project-relative paths. Last-selected folders remain separate for each extension field.
- Building this version requires Assistant extension packages containing `OnlineBrowserFieldAttribute` and `IOnlineBrowser<TArgs>`.
