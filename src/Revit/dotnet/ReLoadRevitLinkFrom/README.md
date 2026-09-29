# Re Load Revit Link From

## Description
This extension reloads Revit links in a cloud-hosted Revit model from Autodesk Construction Cloud (ACC) or BIM 360. It helps keep your model up to date when linked models are moved to new folders, renamed, or replaced in ACC/BIM 360.

Use this when you want to quickly update many cloud links at once, instead of manually reloading each link inside Revit.

## Configuration

All configuration is done in the Assistant task UI. Each field below appears as an input in the task configuration.

- **Autodesk Client**
  - Connects the extension to Autodesk Construction Cloud / BIM 360.
  - Requires you to sign in with your Autodesk account.
  - Must be set for the extension to access folder contents and link data.

- **Hub ID** (Revit 2021 or older only)
  - The BIM 360 Hub identifier for your ACC/BIM 360 account.
  - Required when using `Folder Path` for Revit 2021 or older.
  - Format example: `b.a1eefaf7-effc-433f-873d-16b2cacccff4`.

- **Project ID** (Revit 2021 or older only)
  - The BIM 360 Project identifier.
  - Required for Revit 2021 or older.
  - Format example: `b.12f70284-3d6f-4dc0-9cd5-e870479f6430`.

- **Reload Mode**
  - Controls which links in the model are reloaded.
  - Options:
    - `From list of links` (default): Only the links that you select in the `Links` list will be reloaded.
    - `From available links`: All eligible links in the model will be reloaded.

- **Links**
  - A list of link names from the current Revit model.
  - You can auto-fill this list from the model using the Assistant UI.
  - When `Reload Mode` is `From list of links`, only the links listed here are updated.

- **Reload Option**
  - Defines how the extension finds the new location of the links in ACC/BIM 360.
  - Options:
    - `Folder ID`: Use a specific ACC/BIM 360 folder ID.
    - `Folder Path`: Use a folder path (for example `Project Files/02_WIP/Mechanical`).
    - `From 'Files' variable`: Use a pre-defined variable called `Files` that lists explicit file locations.

- **Folder ID**
  - Used when `Reload Option` is set to `Folder ID`.
  - ACC/BIM 360 folder identifier where the updated link models are stored.
  - Example: `urn:adsk.wipemea:fs.folder:co.3JQR7J6QTZnJ6Q1J7J6QTZaJ6Q1`.

- **Folder Path**
  - Used when `Reload Option` is set to `Folder Path`.
  - Text path to the folder containing the updated link models.
  - Example: `Project Files/02_WIP/Mechanical`.

- **Unload links**
  - If enabled, links are unloaded after being reloaded.
  - This helps reduce memory usage in large projects, especially when many links are present.

- **Re-load cloud links**
  - If enabled, the extension also reloads cloud-hosted links.
  - If disabled, only links loaded from local paths are considered.

### Using the `Files` variable (advanced option)
When `Reload Option` is set to `From 'Files' variable`, you must define a variable named `Files` in the Assistant workflow.

- Each line in `Files` represents one link.
- Format for each line:
  - Revit 2022 or newer:
    - `Project/Folder/File.rvt;{ModelGuid}`
  - Revit 2021 or older:
    - `Project/Folder/File.rvt;{ModelGuid};{ModelItemUrn}`

Example line (2021 or older):

```text
Project/Folder/File.rvt;33ae5b8e-b8d2-4cda-8c11-d0a840f25487;urn:adsk.wipemea:dm.lineage:QFG-TgvNQUGHl790h2swjw
```

## Functionality

### Description
When the extension runs, it performs the following steps:

1. Confirms that a Revit model is open and active.
2. Connects to ACC/BIM 360 using the Autodesk client and project information.
3. Collects all Revit links in the model and filters out:
   - Nested links
   - IFC links
   - Cloud links if `Re-load cloud links` is turned off
4. Based on `Reload Option`, it builds a list of link references from ACC/BIM 360:
   - From a specific folder ID
   - From a folder path (by walking down the folder hierarchy)
   - From the values in the `Files` variable
5. Decides which links in the model to reload based on `Reload Mode` and the `Links` list.
6. For each selected link:
   - Finds the matching model in ACC/BIM 360.
   - Requests Revit to reload the link from the new cloud location.
   - Optionally unloads the link after reload if `Unload links` is enabled.
7. Returns a summary listing which links were successfully reloaded and which failed, including error messages for failed ones.

### How to Use

1. **Prerequisites**
   - You must have an ACC/BIM 360-hosted Revit model open in Revit.
   - You must have appropriate permissions in ACC/BIM 360 to access the folders and models.
   - Make sure any required IDs or paths (Hub ID, Project ID, Folder ID / Folder Path) are known.

2. **Configure the task in Assistant**
   - Add a new task using the `Re Load Revit Link From` extension.
   - In the configuration:
     - Select or sign in with `Autodesk Client`.
     - For Revit 2021 or older, enter `Hub ID` and `Project ID`.
     - Choose a `Reload Mode`:
       - `From list of links` and then select desired `Links`, or
       - `From available links` to update all suitable links.
     - Choose a `Reload Option` and provide the appropriate `Folder ID`, `Folder Path`, or set up the `Files` variable.
     - Decide whether to enable `Unload links` and `Re-load cloud links`.

3. **Run the task**
   - Start the Assistant workflow.
   - The extension will connect to ACC/BIM 360 and attempt to reload links.
   - In large projects, this may take some time; do not close Revit or the model while it runs.

4. **Verify results**
   - Review the task output text:
     - It lists links successfully reloaded.
     - It lists links that failed with a brief reason.
   - In Revit, check the `Manage Links` dialog to verify that links now point to the expected models.
   - If `Unload links` was enabled, confirm that the links are unloaded as intended.

> Tip: Consider adding screenshots of a typical configuration and the `Manage Links` dialog to this section to guide new users.

## Troubleshooting

### Issue 1: "Revit has no active model open"
- **Causes**: The extension was run when no Revit model was open, or Revit was in a state where no document was active.
- **Solution**: Open the desired cloud-hosted Revit model and make sure it is the active document, then run the task again.
- **Resources**: Revit documentation on opening and working with cloud models.

### Issue 2: "Project Id is null" or "Hub Id is null"
- **Causes**:
  - For Revit 2022 or newer: The extension could not read project information from the open model.
  - For Revit 2021 or older: `Project ID` or `Hub ID` is missing in the configuration.
- **Solution**:
  - Check that you are working on a valid ACC/BIM 360 cloud model.
  - For older Revit versions, confirm that `Hub ID` and `Project ID` are correctly entered.
- **Resources**: ACC/BIM 360 project settings, project administration documentation.

### Issue 3: "Please enter the Folder ID" or "Please enter Reload From location"
- **Causes**: `Folder ID` or `Folder Path` is missing while the corresponding `Reload Option` is selected.
- **Solution**: Ensure that:
  - When using `Folder ID`, the `Folder ID` field is filled in.
  - When using `Folder Path`, the `Folder Path` field is filled in and correctly spelled.
- **Resources**: ACC/BIM 360 web UI to copy folder IDs or confirm folder paths.

### Issue 4: "Folder not found" or no links found in folder
- **Causes**:
  - `Folder Path` does not match the folder structure in ACC/BIM 360.
  - The folder does not contain Revit models with valid model GUIDs.
- **Solution**:
  - Verify the exact folder path and spelling.
  - Check in ACC/BIM 360 that the folder contains the intended Revit models.
- **Resources**: ACC/BIM 360 Files/Project Files structure.

### Issue 5: Problems with the `Files` variable ("Required 'Files' variable not found", "Variable 'Files' is empty", or format errors)
- **Causes**:
  - The `Files` variable is not defined in the Assistant workflow.
  - The variable is empty, or some lines are not in the required format.
- **Solution**:
  - Define a `Files` variable in the workflow.
  - Make sure each line follows the expected format with the correct number of `;`-separated parts.
  - For older Revit versions, confirm that the GUID and item URN are valid.
- **Resources**: Assistant documentation on workflow variables.

### Issue 6: Some links report "Link not found in the list"
- **Causes**: The extension could not find a matching model in ACC/BIM 360 for the link's type name.
- **Solution**:
  - Confirm that the ACC/BIM 360 file names (display names) match the link type names in Revit.
  - Ensure you are pointing to the correct folder or `Files` list.
- **Resources**: Revit `Manage Links` dialog and ACC/BIM 360 file list.

### Issue 7: Mixed success – some links succeed, others fail
- **Causes**: Certain links may be in folders without matching models, have invalid ACC/BIM 360 metadata, or encounter permission issues.
- **Solution**:
  - Review the detailed output from the task, which lists each failed link and the error message.
  - Fix issues for specific links (move files, correct names, adjust permissions) and rerun the task.

> Suggestion: Include screenshots of a failing configuration and a corrected one to help users diagnose these cases.

## FAQ

- **Q: When should I use this extension?**
  - **A:** Use this extension when linked Revit models hosted in ACC/BIM 360 have been moved, renamed, or replaced, and you want to update many links in a cloud-hosted model in one go. It is especially useful during model reorganization, folder restructuring, or project phase transitions.

- **Q: Does this work with local or IFC links?**
  - **A:** The extension ignores IFC links and nested links. It is designed primarily for regular Revit links, especially cloud-hosted ones. Local links are only considered if `Re-load cloud links` is disabled and the links are stored on local paths that still meet the filter rules.

- **Q: Do file names need to match between Revit and ACC/BIM 360?**
  - **A:** Yes. The extension matches links based on the link type name in Revit and the display name of the model in ACC/BIM 360. If names do not match, the link will be reported as "Link not found in the list".

- **Q: Can I run this on very large projects?**
  - **A:** Yes, but reloading many links can take time. Using `Unload links` after reload can help manage memory usage. Ensure you have a stable connection to ACC/BIM 360 while it runs.

- **Q: What Revit versions are supported?**
  - **A:** The extension contains logic for both Revit 2021 or older (using BIM 360 server integration and `Hub ID`/`Project ID`) and Revit 2022 or newer (using model GUIDs and project IDs read directly from the cloud model). Your Assistant environment selects the correct behavior based on the Revit version.

---

*This documentation was generated based on the extension's code structure.*