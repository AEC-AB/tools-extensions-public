# StreamBIM File Downloader

## Description

Download files and folders from StreamBIM in Assistant. Each source–target row pairs a remote source with the local folder where its files should be saved, so one task can download to several local folders.

## Configuration

- **StreamBIM Credentials:** Application ID of the Windows Credential Manager entry containing your StreamBIM username and password. The default is `StreamBIM`.
- **Project:** Choose the StreamBIM project from the dropdown. Changing the credential application ID refreshes the available projects.
- **Download project files:** Add one or more rows. In **From**, browse for a remote file or folder, or enter a project-relative path such as `Models/Structure.ifc`. You can enter `*` and `?` wildcards manually. In **To**, select the local destination folder for that row. Every row requires both paths.
- **Skip unchanged files:** When enabled, skip files that already exist at that row's destination with the same modified timestamp.

## Functionality

1. Store your StreamBIM username and password in Windows Credential Manager, then enter the matching credential application ID in Assistant.
2. Select the project.
3. Use the cloud button to add remote files or folders. Use **Add row** to enter a source manually.
4. Choose a local folder in **To** for each row. The row menu lets you duplicate a row, copy its paths, or remove it.
5. Run the extension and review its summary.

Individual files and wildcard matches are saved directly in the row's destination folder. A selected folder downloads recursively, preserving its subfolder structure below that destination. Missing local destination folders are created during the transfer. Rows run in order, and their downloaded, skipped, and failed files are combined into one result. If rows target the same local filename, a later transfer can overwrite that file.

## Troubleshooting

- **A credential is missing:** Confirm that the application ID matches your Windows Credential Manager entry and that it contains the StreamBIM username and password.
- **A row is incomplete:** Enter a remote source in **From** and a local folder in **To** for every row. Remove any unused blank rows.
- **A remote path is missing:** Verify the selected project and project-relative path. The result names the last folder found and may suggest similar names. Correct that path and run again.
- **A folder listing fails:** Use **Refresh** in the online browser. Cancel leaves your configured rows unchanged.
- **A local file cannot be written:** Check access to the row's destination folder and close applications holding that file, then run again.
- **Some transfers fail:** The summary reports partial success and lists failures when other files downloaded or were skipped. If no requested file can be found or transferred, the extension fails.

## FAQ

**Can I download to different local folders?**

Yes. Each row has its own destination folder. Duplicating a row retains its source and destination so you can edit either path.

**Can I select items from different remote folders?**

Yes. The online browser loads one folder at a time and supports selecting files and folders. You can also enter one wildcard path per row manually.

**How are favorites and saved locations handled?**

Use the stars in the file and folder browser to save favorites. Favorites belong to the current Assistant user and are separated by project and credential application ID. Compatible StreamBIM extensions share favorites. Opening a populated source navigates to its saved location; an empty source opens at Home. Saved paths stay visible before remote items load.

**Can the extension recover a moved or renamed remote item?**

StreamBIM resolves saved selections by FTP path. If an item moves or is renamed, browse to its new location and select it again.

**Are any folders excluded?**

Folders named `_backup`, folders ending with `-revs`, and their contents are ignored.

**What happens when the project name is also a real folder?**

Keep the path exactly as the browser shows it. A leading project name is tried as a real folder first. If that folder does not exist, a repeated project-name prefix is removed and the shorter path is tried.

## Resources

- [Extension development documentation](../../../../../docs/README.md)
- The Assistant field tooltips describe each input; the online browser provides favorites and saved-folder navigation.

## Version History

- **Argument version 2:** Projects use a dropdown and downloads use source–target rows. Version 1 tasks upgrade directly, preserving project, source paths, local destinations, credentials, and the skip-unchanged setting.
- **Argument version 1:** Configured project and file paths with a shared local destination folder.
