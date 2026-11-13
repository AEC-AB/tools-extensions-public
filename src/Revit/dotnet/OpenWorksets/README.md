# Open Worksets

## Description

**Open Worksets** opens closed worksets in the active Revit model from an Assistant workflow. Pick worksets from a list, match them by name pattern, or both. It is useful in large workshared models where worksets are closed to keep the model fast, and a workflow step (export, print, check) needs certain worksets open first.

The extension can also load Revit links that are placed on the worksets it opens. Without this, links on a newly opened workset often stay unloaded.

## Configuration

- **Worksets**: The user worksets to open, picked from the active model's workset list. Worksets that are already open are skipped and listed in the result. The list shows the worksets of the model that is active when you configure the extension. Worksets are stored by name, so the same configuration works in any model that uses the same workset names.

- **Workset name patterns**: One or more regular expressions. Every closed user workset whose name matches any pattern is opened. Matching is case-insensitive.
  - `^LINK` opens every workset whose name starts with "LINK".
  - `_Structural$` opens every workset whose name ends with "_Structural".
  - `Arch|MEP` opens every workset whose name contains "Arch" or "MEP".
  - `*` is not a wildcard on its own. Use `.*` to match any text.

  You must fill in **Worksets**, **Workset name patterns**, or both.

- **Reload unloaded links** (default: off): When on, the extension loads every Revit link that is placed on one of the opened worksets and is still unloaded once the worksets are open. This includes links that were unloaded with **Unload** or **Unload for me** in Manage Links. When off, the extension does not load any links.

## Functionality

### Description

When the extension runs, it:

1. Checks that a workshared Revit model is active and that at least one workset or pattern is configured.
2. Finds the closed worksets to open. Selected worksets that are already open, and names that do not exist in the model, are listed in the result.
3. Opens the worksets. Revit has no direct command to open a workset in an open model, so the extension creates a small temporary element on each closed workset and shows it in a temporary 3D view, which makes Revit open the workset. The temporary elements, the view, and any temporary workset are rolled back immediately, and your original view is restored.
4. Checks which worksets actually opened.
5. If **Reload unloaded links** is on, loads the links on the opened worksets that are not loaded. Nested links are skipped.
6. Reports the opened worksets, worksets still closed, and the load result for each link.

When the workflow runs as a dry run, the extension lists the worksets it would open and the links it would try to load, and changes nothing.

### How to Use

1. Open the workshared Revit model and make it the active document.
2. Add **Open Worksets** to your Assistant workflow.
3. Select worksets, enter name patterns, or both. Turn on **Reload unloaded links** if links on these worksets should be loaded.
4. Run the workflow. Revit briefly shows a temporary 3D view while the worksets open.
5. Read the result. It lists opened worksets, worksets still closed, worksets not found, and each link's load result.

## Troubleshooting

### Issue 1: "Revit has no active document"
- **Causes**: No model is open, or no model window is active.
- **Solution**: Open the model, click in one of its views, and run the extension again.

### Issue 2: "... is not workshared"
- **Causes**: The active model does not use worksharing, so it has no worksets.
- **Solution**: Run the extension on a workshared model.

### Issue 3: "No worksets are configured"
- **Causes**: Both **Worksets** and **Workset name patterns** are empty.
- **Solution**: Select at least one workset or enter at least one pattern.

### Issue 4: "No closed worksets to open"
- **Causes**: All selected worksets are already open, or no closed workset matches the patterns.
- **Solution**: Check the workset state in **Collaborate > Worksets**. Test patterns against your workset names on a site such as https://regex101.com/.

### Issue 5: Worksets listed as "Not found in the model"
- **Causes**: The workflow was configured in a model with different workset names, or a workset was renamed.
- **Solution**: Reselect the worksets from the list with the target model active, or use a name pattern instead.

### Issue 6: "... is not a valid regular expression"
- **Causes**: A pattern has a syntax error, such as an unmatched `(` or a leading `*`.
- **Solution**: Correct the pattern named in the message. Use `.*` instead of `*` for "any text".

### Issue 7: Worksets listed as "still closed"
- **Causes**: Revit did not open the workset, for example because it is not accessible from your local copy.
- **Solution**: Open the workset in **Collaborate > Worksets**, or synchronize with central and run the extension again.

### Issue 8: A link is "Not loaded"
- **Causes**: The link file was moved, renamed or cannot be reached (for example a disconnected network drive), or the link is nested in another link.
- **Solution**: Open **Manage Links** and fix the path with **Reload From**. For a nested link, reload its parent link.

### Issue 9: "has no 3D view type"
- **Causes**: The model has no 3D view family type, so the temporary view cannot be created.
- **Solution**: Add a 3D view type to the model (for example by transferring it from a template) and run the extension again.

## FAQ

- **Q: Does the extension leave anything behind in my model?**
  - **A:** No. The temporary elements, view and workset are rolled back. Only the opened worksets, and any links that were loaded, remain.

- **Q: Can it open worksets inside linked models?**
  - **A:** No. It only opens worksets in the active host model.

- **Q: Can I use Worksets and patterns together?**
  - **A:** Yes. The extension opens every workset matched by either.

- **Q: Why are links on an opened workset still unloaded?**
  - **A:** Turn on **Reload unloaded links**. Without it, the extension opens the worksets but does not load any links.

## Resources

- [Autodesk Revit Help](https://help.autodesk.com/view/RVT/2026/ENU/) (search for "worksets" and "Manage Links")
- [Extension development documentation](../../../../docs/README.md)

## Version History

- **Version 2.0.0**
  - Moved to the public extensions repository and rewritten with the current extension field attributes.
  - Worksets are now selected and stored by name instead of by ID, so a configuration can be reused across models. Configurations saved with version 1 must be set up again.
  - New **Reload unloaded links** option loads unloaded links placed on the opened worksets. Links are no longer loaded when the option is off; version 1 loaded links whose link type was on an opened workset.
  - The result now lists worksets that are already open, not found, or still closed after the run, plus each link's load result.
  - Clear errors for invalid name patterns, models that are not workshared, and models without a 3D view type.
  - Supports dry runs.

- **Version 1.0.1**
  - Added Revit 2026 support.

- **Version 1.0.0**
  - Initial release: opens closed worksets by selection or regex pattern and loads links whose type is on an opened workset.
