# Open Worksets

## Description

The **Open Worksets** extension opens closed worksets in the active Revit model. Pick the worksets by name from the model, or match them with regular expressions when you want to open a whole group of worksets at once.

This is useful in large workshared models where worksets are kept closed to improve performance. Reopening them by hand means walking through the Worksets dialog for every workset and then reloading the linked models that live in them; the extension does both in one run.

Linked models are handled for you. Before the worksets are opened, the extension moves the links in those worksets aside so they do not get pulled into the process, and afterwards it reloads the ones that were not loaded.

**Host:** Revit (workshared models only). Supported in Revit 2019 and later.

## Configuration

| Field | Input | Effect |
|---|---|---|
| **Worksets** | Multi-select list, populated with the user worksets of the active document. Empty by default. | Each selected workset is opened if it is currently closed. Worksets that are already open are reported and skipped. |
| **Name Patterns** | List of regular expressions, for example `^LINK` or `_Structural$`. Empty by default. | Every closed workset whose name matches at least one pattern is opened. Matching is case-insensitive, so `link` also matches `LINK`. |

Notes on the fields:

- At least one of the two fields must be filled in. With both empty the extension fails immediately without touching the model.
- The two fields add up. You can select a few worksets by name and let the patterns pick up the rest.
- **Worksets** stores the numeric workset id of the model it was configured against, not the workset name. If you point the same workflow at a different model, re-pick the worksets; use **Name Patterns** instead when the workflow has to run against several models.
- **Name Patterns** takes .NET regular expression syntax. `LINK*` matches `LIN` followed by any number of `K`s, which is probably not what you want — write `^LINK` for "starts with LINK", or `.*LINK.*` for "contains LINK". Test patterns at <https://regex101.com/>.

## Functionality

### Description

When the extension runs it:

1. **Checks the model.** It requires an active, workshared Revit document and at least one workset or pattern in the configuration.
2. **Works out which worksets to open.** It collects the user worksets of the document, keeps the ones that are selected or that match a pattern, and drops the ones that are already open.
3. **Finds the linked models** whose link type lives in one of those worksets.
4. **Moves those links to a temporary workset**, so opening a workset does not drag its linked models into the temporary view.
5. **Creates a temporary 3D view**, named `Opening worksets...` plus a random suffix so it cannot clash with a view the model already has.
6. **Creates one throw-away element in each closed workset** and shows those elements in the temporary view. Showing an element is what makes Revit open its workset.
7. **Restores your original view** and rolls back every model change. The temporary view, elements and workset are discarded; only the open/closed state of the worksets remains.
8. **Reloads the linked models** that were found in step 3 and are not already loaded.
9. **Reports the result** as a table of worksets and a table of links with their status.

Warnings that Revit raises while the temporary elements are created are resolved or dismissed automatically, so the run does not stop on a dialog. All of it is rolled back regardless.

**Preconditions:** an active workshared model, and edit rights on the worksets you want to open.

**Side effects:** the worksets are opened and the links in them are reloaded. Nothing else in the model is changed, and no files are written.

### How to Use

#### Step 1: Prepare the model

- Open the workshared Revit project that contains the worksets.
- Make it the active document.
- Make sure you have edit access to the worksets. The extension cannot open worksets you are not allowed to modify.

#### Step 2: Configure the extension

- **By name:** pick the worksets from the **Worksets** list. The list is filled from the active document, so open the model first.
- **By pattern:** add one or more expressions to **Name Patterns**.
- Or combine the two.

#### Step 3: Run it

Run the extension. Revit briefly switches to a temporary 3D view while it works, then switches back to the view you started in. How long it takes depends on the number of worksets and on how large and how distant the linked models are.

#### Step 4: Check the result

The result lists every workset it tried to open and every link it touched:

```
## Open worksets in 'Tower-A-ARCH'

Opened 2 of 2 workset(s).

| Workset | Status |
| --- | --- |
| LINK_Structure | Opened |
| LINK_MEP | Opened |

| Link | Status |
| --- | --- |
| Tower-A-STR.rvt | LinkLoaded |
| Tower-A-MEP.rvt | Skipped, nested link. Reload the parent link instead. |
```

| Result state | When it occurs |
|---|---|
| **Succeeded** | Every requested workset was opened and every link was loaded or safely skipped. |
| **Partially Succeeded** | At least one workset stayed closed, or at least one link failed to load. |
| **Failed** | No workset was opened: no active document, the model is not workshared, no configuration, nothing matched, an invalid pattern, no 3D view could be created, or every requested workset stayed closed. |

When **Assistant runs the action as a dry run**, the extension reports the worksets it would open and the links it would reload, and leaves the document untouched.

## Troubleshooting

### Issue 1: "Revit has no active document"

- **Causes**: No Revit project is open, or the active document is not the one you expect.
- **Solution**:
  1. Open the `.rvt` project that contains the worksets.
  2. Click in its viewport so it becomes the active document.
  3. Run the extension again.

### Issue 2: "... is not workshared, so it has no worksets"

- **Causes**: The active model has no worksharing enabled, so there are no worksets to open.
- **Solution**: Run the extension on a workshared model, or enable worksharing on this one (**Collaborate > Worksets**).

### Issue 3: "No closed worksets matched the configuration"

- **Causes**: Every requested workset is already open, the patterns match nothing, or the saved workset selection belongs to a different model.
- **Solution**:
  1. Open **Collaborate > Worksets** in Revit and confirm the worksets you want really are closed.
  2. Test the pattern with something broad first, such as `.` which matches every name, then narrow it down.
  3. If the message names workset ids that the document does not have, the configuration was saved against another model — re-pick the worksets, or switch to **Name Patterns**.

### Issue 4: "A name pattern is not a valid regular expression"

- **Causes**: A pattern in **Name Patterns** is not valid .NET regex syntax, for example an unclosed `(` or `[`.
- **Solution**: The message names the problem. Fix the pattern and run again. Test it at <https://regex101.com/> first.

### Issue 5: A workset is reported as "Still closed"

- **Causes**: You do not have edit rights on the workset, or another user owns it.
- **Solution**: Check ownership in **Collaborate > Worksets**, ask the owner to relinquish it, or contact your model administrator. Then run the extension again.

### Issue 6: A link is reported as not loaded

- **Causes**: The link path is broken or the file has moved, the link is a nested link, or the linked file is not reachable (for example a disconnected network drive).
- **Solution**:
  1. For a nested link, reload its parent link instead. Nested links cannot be reloaded directly.
  2. Otherwise open **Manage > Manage Links** and check the path and status of the link, then repair it there.

### Issue 7: "No temporary 3D view could be created"

- **Causes**: The model has no 3D view family type, or Revit refused to create an isometric view from any of them. The message quotes what Revit reported.
- **Solution**: Try creating a 3D view manually in Revit. If that also fails, the model's view types need attention before the extension can run.

### Issue 8: The run takes a long time

- **Causes**: Many worksets, heavy geometry, or linked models on a slow network location.
- **Solution**: Open fewer worksets per run, and check the connection to the location where the linked models are stored.

### Known limitations

- Only the worksets of the host model are opened. Worksets inside linked models are not touched.
- Nested links are reported but never reloaded.
- The **Worksets** selection is tied to the model it was configured against, because it stores workset ids.

## FAQ

- **Q: Can I open worksets inside a linked model?**
  - **A:** No. The extension works on the active host model only. Open the linked model as its own project to change its worksets.

- **Q: Does this change my current view or view settings?**
  - **A:** No. It switches to a temporary 3D view while it works and switches back afterwards. The temporary view is discarded.

- **Q: Do the temporary elements and the temporary workset end up in my model?**
  - **A:** No. Everything the extension creates is rolled back. Only the open/closed state of the worksets and the reloaded links remain.

- **Q: Can I open worksets I do not have permission for?**
  - **A:** No. Revit's permission model still applies. Those worksets are reported as "Still closed".

- **Q: Can I use Worksets and Name Patterns together?**
  - **A:** Yes, and it is a common setup: pick the odd ones by name, let a pattern cover a family of worksets such as `^LINK`.

- **Q: How do I tell whether a workset is open or closed?**
  - **A:** Open **Collaborate > Worksets** in Revit. The **Opened** column shows the state of each workset.

- **Q: Why does the extension need to create elements at all?**
  - **A:** The Revit API has no direct "open this workset" call. Showing an element that belongs to a workset is what forces Revit to open it, which is why a throw-away element and view are created and then rolled back.

## Resources

- [Revit Extensions platform guide](../../../../docs/dotnet/PLATFORM_GUIDES/REVIT.md)
- [Extension Development Documentation](../../../../docs/README.md)
- [.NET regular expression syntax](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)

## Version History

- **Version 1.1.0**
  - Fixed: the extension failed with "Unable to create 3D view" in any model that already contained a view named `Opening worksets...`. The temporary view now gets a random suffix, a name Revit rejects no longer costs the run the view, and the error Revit actually reported is included in the failure message instead of being discarded.
  - Migrated into the public Assistant extensions repository.
  - Results are now a Markdown report with a workset table and a link table, and report the real open/closed state after the run instead of assuming success.
  - Worksets that are already open are skipped consistently, whether they were picked by name or matched by a pattern.
  - Invalid regular expressions, non-workshared models and missing 3D view types now fail with a message that says what to do next, instead of raising an error.
  - Warnings raised while the temporary elements are created are suppressed, so the run no longer stops on a dialog.
  - Supports Assistant dry runs: the report lists what would change and the model is left untouched.
  - The **Worksets** field stores ids as text; saved configurations from earlier versions are migrated automatically and keep their selection.

- **Version 1.0.1**
  - Revit 2026 support.
  - Nested links are detected and reported instead of failing to reload.

- **Version 1.0.0 - Initial Release**
  - Opens closed worksets by name or regular expression.
  - Moves linked models aside during the operation and reloads them afterwards.
  - Reports workset and link status.
