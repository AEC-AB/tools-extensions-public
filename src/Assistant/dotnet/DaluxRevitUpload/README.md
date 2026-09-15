# DaluxRevitUpload

## Description

Automates the **Dalux Field → Upload** flow that the Dalux Revit plugin exposes through its ribbon. Dalux has no public Revit upload API, so the plugin's WebView2 popup is normally driven by hand: the user clicks the ribbon button, picks a file row, fills in revision / date / dropdown columns, then clicks **Upload** and waits for the **Done** confirmation.

This extension runs that whole workflow unattended from an Assistant task:

1. Clicks the **Dalux** ribbon tab and the **Upload** button in the target Revit instance.
2. Discovers the WebView2 popup over the Chrome DevTools Protocol.
3. Locates the row matching your filename and fills the columns you specify.
4. Optionally clicks **Upload** and waits for the **Done** confirmation.

Use it when you need lights-out publishing of Revit deliverables to Dalux (nightly issues, batch revisions, locked-screen automation servers).

## Configuration

- **Revit Process ID** — PID of the Revit instance hosting the model. Typically populated by an upstream task that exposes `${{ revitprocessid }}`. Must be a positive integer.
- **Target Filename** — exact filename (without path) of the row to upload in the Dalux popup, for example `I90_BBH_A6_B72_K07_M00_F2_N001`. Match is case-insensitive substring against the row text.
- **Revision Increment** — numeric amount added to the current value of the row's **Revision** column. Default `0.1`. Set to `0` to leave the revision untouched. The new value is written back as a two-decimal string (e.g. `1.10`).
- **Trigger upload** — when enabled, clicks the **Upload** button after all column values are filled and waits (up to 12 hours) for the **Done** / **OK** confirmation. When disabled, the popup is left open with values filled for manual review.
- **Dalux Fields** — dictionary of `Column Header → Value` pairs. The column type (text input, dropdown, date picker) is detected automatically from the popup DOM. Dates may be entered in any recognizable format (`10 Jun 2026`, `10/06/2026`, `2026-06-10`); they are normalised to `dd MMM yyyy` before being sent to the date picker.

## Functionality

### Description

On `Run`, the extension:

1. Validates inputs and parses **Revit Process ID**.
2. Ensures the user-scope environment variable `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=0` is set (writes it if missing). This is what lets the Dalux popup expose a CDP endpoint.
3. Runs pre-flight diagnostics — WebView2 runtime version, Revit start time vs. env var write time, and CDP port conflict detection.
4. Uses Windows UI Automation to click the **Dalux** tab and **Upload** button in Revit's ribbon (works on a locked screen via `PostMessage`).
5. Polls for up to 2 minutes for the Dalux popup's CDP endpoint, primarily by reading `%LocalAppData%\DaluxWebView2\Default\EBWebView\DevToolsActivePort`.
6. Connects over WebSocket and injects a JavaScript driver that:
   - Waits for the row grid to fully render.
   - Clears any pre-selected rows.
   - Selects the row matching **Target Filename**.
   - For each entry in **Dalux Fields**, locates the column by header, detects the cell type, and writes the value (text input, dropdown selection, or date picker navigation).
   - Increments the **Revision** column by **Revision Increment** if that column exists.
7. If **Trigger upload** is enabled, clicks **Upload** and waits for the **Done** confirmation.
8. Returns the full audit log as the task result.

### How to use

1. Make sure the Dalux Revit plugin is installed and the **Dalux** ribbon tab is visible in Revit.
2. Make sure the WebView2 Evergreen Runtime is installed (required by the Dalux plugin itself).
3. On the very first run, the extension may write `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=0` to your user environment and ask you to **restart Revit** before continuing. The env var must be present in Revit's environment block at launch.
4. Open the Revit model that contains the file you want to upload and ensure you are signed in to Dalux inside Revit.
5. Configure the extension task:
   - Set **Revit Process ID** (usually from an upstream "find Revit process" task).
   - Set **Target Filename** to match the row in Dalux exactly.
   - Add a **Dalux Fields** entry for each column you want to update (e.g. `Revision Date` → `10 Jun 2026`, `Status` → `For Information`).
   - Enable **Trigger upload** for fully unattended runs.
6. Run the task and check the returned audit log to confirm which row was matched, which columns were filled, and whether the **Done** confirmation arrived.

## Troubleshooting

### Issue 1: "Revit Process ID is required..."
- **Causes**: The upstream task that exposes the Revit PID did not run, or the variable name is misspelled.
- **Solution**: Confirm an earlier task in the workflow exposes `revitprocessid` (or equivalent) and that this field is bound to that variable.

### Issue 2: Extension asks you to restart Revit
- **Causes**: `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` was missing or had the wrong value; Revit was launched before the variable existed.
- **Solution**: Close Revit completely, then start it again. The extension already wrote the variable; it only takes effect for new Revit processes.

### Issue 3: "Dalux popup CDP endpoint" never found (2-minute timeout)
- **Causes**: The Dalux ribbon button did not open the WebView2 popup; the Dalux plugin is not installed; the WebView2 runtime is missing; or another process is binding the CDP port.
- **Solution**:
  - Verify the **Dalux** tab is present on the Revit ribbon and that clicking **Upload** manually opens the popup.
  - Reinstall the WebView2 Evergreen Runtime.
  - Check the audit log for "Foreign process holds port" warnings and close the offending process.

### Issue 4: Target row not found / wrong row selected
- **Causes**: **Target Filename** does not appear in the visible row text, or another row contains the same substring.
- **Solution**: Use the most specific unique portion of the filename. Match is case-insensitive substring; supply enough characters to be unique within the project.

### Issue 5: A column was skipped or filled with the wrong value
- **Causes**: The column header in **Dalux Fields** does not exactly match the popup header; the column is off-screen; the value cannot be parsed as a date for a date-picker column.
- **Solution**:
  - Open the Dalux popup manually and copy the column header text verbatim into the dictionary key.
  - For date columns, use `dd MMM yyyy` (e.g. `10 Jun 2026`) — other formats are accepted but only if they parse under the current locale.
  - For dropdown columns, supply the option text exactly as it appears in the dropdown.

### Issue 6: "Upload" was clicked but "Done" never appeared
- **Causes**: Network upload still in progress (large files); Dalux returned a server-side error; Revit lost focus during a modal prompt.
- **Solution**: Inspect the audit log for the last polled state. The extension waits up to 12 hours for **Done**; very large files can legitimately take a long time. If the upload failed in Dalux, the popup typically shows a red error banner — review it manually and retry.

### Known limitations
- Windows-only, x64. Requires the WebView2 Evergreen Runtime.
- The **Revision** column is always written with two decimal places.
- Only one row per run — to upload several files, schedule one task per filename.
- The popup must reach a fully rendered state within 10 minutes of opening.

## FAQ

- **Q: Does this use the Dalux REST API?**
  - **A:** No. Dalux exposes no Revit upload API, so this extension drives the same WebView2 popup a human would. No Dalux credentials are read or transmitted by the extension itself — Dalux is already signed in from inside Revit.

- **Q: Can it run on a locked workstation?**
  - **A:** Yes. The ribbon click uses `PostMessage`, which bypasses the active desktop, and the WebView2 popup is driven entirely over CDP.

- **Q: How do I upload several files in one workflow?**
  - **A:** Add one task per filename. The extension intentionally targets a single row per run so the audit trail stays clear.

- **Q: Can I leave the popup open for manual review instead of uploading?**
  - **A:** Yes. Leave **Trigger upload** off. All columns will be filled, but the **Upload** button will not be clicked.

- **Q: Does Revision Increment support large jumps like 1.0?**
  - **A:** Yes. The current numeric value is parsed from the cell, added to the increment, and written back. `2.10` + `1.0` becomes `3.10`.

## Resources

- [Extension Development Documentation](../../../../docs/README.md)
- [Assistant Platform Guide](../../../../docs/dotnet/PLATFORM_GUIDES/ASSISTANT.md)

## Version History

- **Version 0.1.0 — 2026-06-04**
  - Initial release: ribbon click, CDP discovery, row match, column fill (text / dropdown / date picker), optional Upload with Done wait.
