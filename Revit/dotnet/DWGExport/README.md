# DWGExport – User Guide

This guide explains how to export DWG files from your current model. It is written for users with no technical background.

## What you need before you start
- An open model.
- **DWG Exporter** installed (if it’s missing, export cannot run).
- A valid folder you can write to (a location you have permission to save files).

## Step-by-step workflow (start to finish)
1. **Destination Folder**
   - Choose where the exported DWG file(s) will be saved.
2. **Choose What To Export**
   - Pick what you want to export (a single item like the active view, or multiple items like all sheets).
3. Fill in any extra fields that appear based on what you selected in **Choose What To Export**.
4. **Naming Options**
   - Choose how the exported DWG file names should be created.
5. (Optional) Set additional naming details (prefix/suffix/separator or a custom naming pattern).
6. Adjust DWG settings (the section titled **Configure Revit DWG Settings**).
7. Run the export.
8. Review the result message:
   - If anything was skipped or failed, you will see a list of failures explaining what happened.

---

## Fields (what each one does)

### Destination Folder
**What it does**: Selects the folder where exported DWG file(s) will be saved.

**When it appears**: Always.

**Required**: Yes.

**If missing/invalid**:
- If empty, the export stops.
- If the folder doesn’t exist, the tool tries to create it.
- If it can’t access or create the folder (permissions, invalid path), the export stops.

---

### Choose What To Export
**What it does**: Controls what is exported.

**When it appears**: Always.

**Required**: Yes (a default is already selected).

**If invalid**: If an unsupported option is selected (not expected in normal use), the export fails.

Options you may see:
- **ActiveView**
- **AllViews**
- **AllSheets**
- **CustomFilter**
- **ViewSet**
- **UseRegexInViewSet**

Each option is explained in the “Export modes” section below.

---

### Set Filter Rules
**What it does**: Lets you define rules to pick which views/sheets should be exported (for example, by name).

**When it appears**: Only when **Choose What To Export** is set to **CustomFilter**.

**Required**: Yes for **CustomFilter**.

**If missing/invalid**:
- If this is empty or not set when using **CustomFilter**, the export stops.

---

### View/Sheet Set
**What it does**: Select a saved view/sheet set, or type the name of one.

**When it appears**: Only when **Choose What To Export** is set to **ViewSet**.

**Required**: Yes for **ViewSet**.

**If missing/invalid**:
- If empty, the export stops.
- If the named set cannot be found, the export stops.

---

### Regex Pattern
**What it does**: Finds view/sheet sets whose names match a pattern.

**When it appears**: Only when **Choose What To Export** is set to **UseRegexInViewSet**.

**Required**: Yes for **UseRegexInViewSet**.

**If missing/invalid**:
- If empty, the export stops.
- If the pattern is not valid, the export stops.
- If no set names match, the export stops with a message.

User-entered examples (you type these):
- `^DWG_.*` (matches names that start with “DWG_”)
- `Sheets` (matches any name containing “Sheets”)

---

### Naming Options
**What it does**: Controls how each exported DWG file is named.

**When it appears**: Always.

**Required**: Yes (a default is already selected).

Options you may see:
- **ViewNameOnly**
- **ModelNameOnly**
- **ModelNameAndViewName**
- **ViewNameAndModelName**
- **CustomNamingConvention**

How each option behaves is explained in “Naming / Output” below.

---

### Separator in File Name
**What it does**: Text placed between name parts (for example between model name and view name).

**When it appears**: Only when **Naming Options** is NOT **CustomNamingConvention**.

**Required**: No.

**If missing/invalid**:
- If left empty, names may run together (harder to read), but export can still run.

---

### Prefix in file name (Optional)
**What it does**: Adds text at the beginning of every exported DWG file name.

**When it appears**: Only when **Naming Options** is NOT **CustomNamingConvention**.

**Required**: No.

**If missing/invalid**: If empty, nothing is added.

---

### Suffix in file name (Optional)
**What it does**: Adds text at the end of every exported DWG file name (before the file extension).

**When it appears**: Only when **Naming Options** is NOT **CustomNamingConvention**.

**Required**: No.

**If missing/invalid**: If empty, nothing is added.

---

### Custom Naming Convention
**What it does**: Lets you build your own file naming pattern.

**When it appears**: Only when **Naming Options** is **CustomNamingConvention**.

**Required**: Yes when visible.

**If missing/invalid**:
- If empty, the export will fall back to using the view name.

What you can type (placeholders):
- `{ViewName}` will be replaced with the view/sheet name.
- `{ModelName}` will be replaced with the model name.
- `{AnyParameterName}` can be replaced using values from the view/sheet (if that parameter has a value).

Examples you type:
- `{ViewName}_{Phase}`
- `DWG_{ModelName}_{Discipline}`
- `Custom-{ViewName}-v1`

---

### Configure Revit DWG Settings
This section groups DWG export settings.

**When it appears**: Always (as a section title).

---

### ACA Preference
**What it does**: Controls how architecture objects are written into DWG.

**When it appears**: Always.

**Required**: No.

---

### Colors
**What it does**: Controls how colors are written into the DWG.

**When it appears**: Always.

**Required**: No.

---

### Export Of Solids
**What it does**: Controls how 3D solid geometry is exported (relevant for 3D views).

**When it appears**: Always.

**Required**: No.

---

### File Version
**What it does**: Selects the DWG file format version to export.

**When it appears**: Always.

**Required**: No.

---

### Export Unit
**What it does**: Controls the unit used when writing geometry.

**When it appears**: Always.

**Required**: No.

---

### Line Scaling
**What it does**: Controls how line patterns are scaled.

**When it appears**: Always.

**Required**: No.

---

### Export layer options
**What it does**: Controls how categories/graphics are mapped to DWG layers.

**When it appears**: Always.

**Required**: No.

---

### Target Unit
**What it does**: Sets the target unit for the exported DWG.

**When it appears**: Always.

**Required**: No.

---

### Text Treatment
**What it does**: Controls how text is translated into DWG.

**When it appears**: Always.

**Required**: No.

---

### Export Areas
**What it does**: If enabled, area/room boundaries and geometry are included.

**When it appears**: Always.

**Required**: No.

---

### Hide Reference Plane
**What it does**: If enabled, reference planes are hidden in the export.

**When it appears**: Always.

**Required**: No.

---

### Hide Scope Box
**What it does**: If enabled, scope boxes are hidden in the export.

**When it appears**: Always.

**Required**: No.

---

### Hide Unreferenced View Tags
**What it does**: If enabled, tags that are not referenced are hidden.

**When it appears**: Always.

**Required**: No.

---

### Merge Views
**What it does**: If enabled, views may be exported using external references (XRefs).

**When it appears**: Always.

**Required**: No.

---

### Preserve Coincident Lines
**What it does**: If enabled, overlapping/coincident lines are preserved.

**When it appears**: Always.

**Required**: No.

---

### Shared Coordinate
**What it does**: If enabled, export uses shared coordinates; otherwise it uses the internal origin.

**When it appears**: Always.

**Required**: No.

---

### Use Hatch Background Color
**What it does**: If enabled, hatch patterns export with their background color (if any).

**When it appears**: Always.

**Required**: No.

---

### Mark Nonplot Layers
**What it does**: If enabled, layers whose names contain the provided nonplot suffix will be marked as non-plot.

**When it appears**: Always.

**Required**: No.

Common note:
- This only has an effect when it is enabled and a suffix is provided.

---

### Nonplot Suffix
**What it does**: The text used to identify layers that should be marked as non-plot.

**When it appears**: Only when **Mark Nonplot Layers** is enabled.

**Required**: No.

**If missing/invalid**:
- If empty, nothing will be marked as non-plot.

---

## Export modes (Choose What To Export)

### ActiveView
**What it’s for**: Export the currently active view (the one you are looking at).

**Fields that become visible**: No extra fields.

**What it exports/produces**:
- Exports one DWG file.
- If the active view is a sheet, it exports that sheet.

**Common mistakes**:
- Active view is not printable ? export stops.
- You expected multiple exports ? use **AllViews**, **AllSheets**, **CustomFilter**, **ViewSet**, or **UseRegexInViewSet**.

---

### AllViews
**What it’s for**: Export all printable views in the model.

**Fields that become visible**: No extra fields.

**What it exports/produces**:
- Exports multiple DWG files (one per exported view).

**Common mistakes**:
- Sheets are not included ? sheets are skipped in this mode. Use **AllSheets** if you want sheets.
- Some views are skipped because they cannot be printed ? check printability of those views.

---

### AllSheets
**What it’s for**: Export all printable sheets in the model.

**Fields that become visible**: No extra fields.

**What it exports/produces**:
- Exports multiple DWG files (one per exported sheet).

**Common mistakes**:
- Some sheets are skipped because they cannot be printed ? ensure those sheets are printable.

---

### CustomFilter
**What it’s for**: Export only the views/sheets that match rules you define.

**Fields that become visible**:
- **Set Filter Rules**

**What it exports/produces**:
- Exports one DWG per selected view/sheet (often multiple files).

**Common mistakes**:
- Forgetting to define rules ? export stops because **Set Filter Rules** is required in this mode.
- Rules match nothing ? export finishes with “No views or sheets were exported” (and may include failure details).

---

### ViewSet
**What it’s for**: Export everything contained in a named view/sheet set.

**Fields that become visible**:
- **View/Sheet Set**

**What it exports/produces**:
- Exports one DWG per item inside the selected set (often multiple files).

**Common mistakes**:
- Typo in the set name ? “View set not found.”
- Set contains items that cannot be printed ? those items are skipped and shown as failures.

---

### UseRegexInViewSet
**What it’s for**: Export view/sheet sets whose names match your **Regex Pattern**.

**Fields that become visible**:
- **Regex Pattern**

**What it exports/produces**:
- Exports one DWG per printable view/sheet inside every matching set (often many files).

**Common mistakes**:
- Pattern is empty or invalid ? export stops.
- Pattern matches no set names ? export stops with “No view set matches the regex pattern.”
- Pattern matches more sets than intended ? narrow the pattern (for example add start/end markers like `^` and `$`).

---

## Naming / Output

### How file names are generated
The export creates a DWG file name for each exported view/sheet, then saves it to **Destination Folder**.

1. The tool starts from the choice in **Naming Options**:
   - **ViewNameOnly**: uses the view/sheet name.
   - **ModelNameOnly**: uses the model name.
   - **ModelNameAndViewName**: combines model name + **Separator in File Name** + view/sheet name.
   - **ViewNameAndModelName**: combines view/sheet name + **Separator in File Name** + model name.
   - **CustomNamingConvention**: uses the pattern you type in **Custom Naming Convention**.

2. Optional additions:
   - If **Prefix in file name (Optional)** is filled in, it is added to the beginning.
   - If **Suffix in file name (Optional)** is filled in, it is added to the end.

3. Invalid filename characters are automatically replaced with `_`.

### Examples (realistic)
Assume:
- Model name: `Airport_T1`
- View/Sheet name: `A101 - Ground Floor`
- **Separator in File Name**: `-`
- **Prefix in file name (Optional)**: `IFC_`
- **Suffix in file name (Optional)**: `_RevB`

Examples of resulting names:
1. **Naming Options** = **ViewNameOnly** ? `IFC_A101 - Ground Floor_RevB`
2. **Naming Options** = **ModelNameOnly** ? `IFC_Airport_T1_RevB`
3. **Naming Options** = **ModelNameAndViewName** ? `IFC_Airport_T1-A101 - Ground Floor_RevB`
4. **Naming Options** = **ViewNameAndModelName** ? `IFC_A101 - Ground Floor-Airport_T1_RevB`
5. **Naming Options** = **CustomNamingConvention** with `{ModelName}_{ViewName}` ? `Airport_T1_A101 - Ground Floor`

Tip: If you use **Custom Naming Convention** and include a placeholder that has no value, that part may remain unchanged in the name. Use only placeholders you know exist and have values.

---

## Understanding results and failures
- If nothing is exported, you will get a message saying no views or sheets were exported.
- If some items export and some fail/are skipped, you will get a message that the export completed with failures, plus a numbered list explaining each issue.

Common reasons items are skipped or fail:
- A view/sheet cannot be printed.
- A sheet was included in **AllViews** (sheets are skipped in that mode).
- The chosen view/sheet set cannot be found.
- No view/sheet set matches your **Regex Pattern**.
- The destination folder is missing or not writable.



