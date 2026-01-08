# NWCExport — User Guide

This tool exports **Navisworks NWC** files from your Revit project.

## Before you start

- You must have a Revit model open.
- **Navisworks Exporter** must be installed on the machine running the export.
- You should have at least one **3D view** available (the export only produces NWC from 3D views).

---

## Step-by-step workflow (start to finish)

1. **Destination Folder**
   - Choose where the exported `.nwc` file(s) will be saved.

2. **Choose What To Export**
   - Pick how you want to select what to export.
   - Depending on what you choose, additional fields will appear.

3. (If shown) **Set Filter Rules** / **View Set** / **Regex Pattern**
   - Configure how the tool finds the views to export.

4. **Naming Options**
   - Choose how exported files will be named.

5. (If shown) Naming fields
   - Fill in **Separator in File Name**, **Prefix in file name (Optional)**, **Suffix in file name (Optional)**, or **Custom Naming Convention**.

6. **Configure Revit NWC Settings**
   - Choose what information to include in the exported file(s).

7. Run the export
   - The tool exports one or more `.nwc` files into your **Destination Folder**.
   - If any items fail, you will get a message showing what succeeded and what failed.

---

## Export modes (Choose What To Export)

### Use Active View

**What it’s for**
- Export a single NWC from the view you are currently looking at.

**Fields that become visible**
- No additional selection fields. You only need **Destination Folder** and the naming/settings fields.

**What it exports**
- **One** `.nwc` file (from the active view).

**Common mistakes (and how to avoid them)**
- **Active view is not a 3D view** ? Switch to a 3D view and run again.
- **No active view available** ? Make sure a view tab is active in Revit.

---

### All Views

**What it’s for**
- Export all 3D views in the model.

**Fields that become visible**
- No additional selection fields.

**What it exports**
- **Multiple** `.nwc` files (one per 3D view).

**Common mistakes (and how to avoid them)**
- **Unexpected number of files** ? This exports every 3D view, so consider using **Set Filter Rules** or **View Set** if you only want a subset.
- **Some views are skipped** ? Only 3D views are exported.

---

### View Set

**What it’s for**
- Export the views that are contained in a saved view set.

**Fields that become visible**
- **View Set**

**What it exports**
- **Multiple** `.nwc` files (one per 3D view inside the selected view set).

**Common mistakes (and how to avoid them)**
- **View Set is empty** ? Choose a view set that actually contains views.
- **View Set not found** ? Make sure the name matches an existing view set.
- **Not all views export** ? Only 3D views inside the view set are exported.

---

### UseRegexInViewSet

**What it’s for**
- Export views from any view set whose name matches a pattern.

**Fields that become visible**
- **Regex Pattern**

**What it exports**
- **Multiple** `.nwc` files.
  - The tool checks view set names against your **Regex Pattern**.
  - For each matching view set, it exports the **3D views** inside that view set.

**Common mistakes (and how to avoid them)**
- **Regex Pattern is empty** ? Enter a pattern.
- **Invalid Regex Pattern** ? Simplify the pattern and try again.
- **No view set matches the pattern** ? Adjust the pattern to match your view set naming.

---

### Custom Filter

**What it’s for**
- Export only the views that match rules you define.

**Fields that become visible**
- **Set Filter Rules**

**What it exports**
- **Multiple** `.nwc` files (one per matching 3D view).

**Common mistakes (and how to avoid them)**
- **No rules set and too many exports** ? If you leave the rules empty, the selection may include many views; add rules to narrow it down.
- **Nothing exports** ? Make sure your rules actually match existing 3D views.
- **Some views are skipped** ? Only 3D views are exported.

---

## Field guide (every UI field)

### Destination Folder

**What it does**
- Selects the folder where exported `.nwc` files are saved.

**When it appears**
- Always.

**Required?**
- Yes.

**If missing/invalid**
- If empty: export stops and shows an error.
- If the folder does not exist: the tool tries to create it.
- If the folder cannot be accessed or created: export stops with an error.

---

### Choose What To Export

**What it does**
- Selects the export mode. This controls which views are exported.

**When it appears**
- Always.

**Required?**
- Yes (a default is already selected).

**If missing/invalid**
- If an unsupported option is selected (rare): export stops with an error.

---

### Set Filter Rules

**What it does**
- Lets you define rules to pick which views should be exported.

**When it appears**
- Only when **Choose What To Export** is **Custom Filter**.

**Required?**
- Yes (it must be configured).

**If missing/invalid**
- If it is not configured: export stops with an error.
- If rules select views that are not 3D: those views are skipped.

---

### View Set

**What it does**
- Selects (or lets you type) a view set name to export from.

**When it appears**
- Only when **Choose What To Export** is **View Set**.

**Required?**
- Yes.

**If missing/invalid**
- If empty: export stops with an error.
- If not found: export stops and reports it.
- If the view set contains no views: export stops and reports it.

---

### Regex Pattern

**What it does**
- A text pattern used to match view set names.

**When it appears**
- Only when **Choose What To Export** is **UseRegexInViewSet**.

**Required?**
- Yes.

**If missing/invalid**
- If empty: export stops with an error.
- If invalid: export stops with an error.
- If it matches no view sets: export stops and reports that nothing matched.

---

### Naming Options

**What it does**
- Controls how each exported file is named.

**When it appears**
- Always.

**Required?**
- Yes (a default is already selected).

**If missing/invalid**
- If an unsupported option is selected (rare): a safe fallback name is used.

---

### Separator in File Name

**What it does**
- The text inserted between name parts when a naming option combines multiple pieces.

**When it appears**
- Only when **Naming Options** is **not** **Custom Naming Convention**.

**Required?**
- Not required (a default `-` is provided).

**If missing/invalid**
- If you clear it, names may be harder to read, but export can still run.

---

### Prefix in file name (Optional)

**What it does**
- Adds text at the start of every exported file name.

**When it appears**
- Only when **Naming Options** is **not** **Custom Naming Convention**.

**Required?**
- No.

**If missing/invalid**
- If empty, nothing is added.

---

### Suffix in file name (Optional)

**What it does**
- Adds text at the end of every exported file name (before `.nwc`).

**When it appears**
- Only when **Naming Options** is **not** **Custom Naming Convention**.

**Required?**
- No.

**If missing/invalid**
- If empty, nothing is added.

---

### Custom Naming Convention

**What it does**
- Lets you type a custom file name pattern.
- You can use placeholders like:
  - `{ViewName}`
  - `{ModelName}`
  - Any view parameter name in braces (for example: `{Discipline}`, `{Phase}`)

**When it appears**
- Only when **Naming Options** is **Custom Naming Convention**.

**Required?**
- Yes.

**If missing/invalid**
- If empty: the tool falls back to using the view name.
- If your pattern contains placeholders that don’t exist or don’t have values, those parts may remain unchanged or be left out (depending on what’s available).

---

### Configure Revit NWC Settings

This is a section title used to group the settings below.

---

### Parameters

**What it does**
- Controls how parameter information is included in the export.

**When it appears**
- Always.

**Required?**
- Yes (a default is selected).

**If missing/invalid**
- Export uses the default selection.

---

### Navisworks Coordinates

**What it does**
- Sets the coordinate system used for the exported file.

**When it appears**
- Always.

**Required?**
- Yes (a default is selected).

**If missing/invalid**
- Export uses the default selection.

---

### Convert Element Properties

**What it does**
- When enabled, more element property information is included.

**When it appears**
- Always.

**Required?**
- No (defaults to on).

**If missing/invalid**
- If off, exported files may contain less detail.

---

### Find Missing Materials

**What it does**
- Tries to resolve materials that otherwise might be missing.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, some materials may not appear as expected.

---

### Divide File Into Levels

**What it does**
- Splits the export into levels.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If on, you may get a different output structure than expected.

---

### Export ElementIds

**What it does**
- Includes element IDs in the exported file.

**When it appears**
- Always.

**Required?**
- No (defaults to on).

**If missing/invalid**
- If off, element IDs will not be available in the output.

---

### Export Links

**What it does**
- Includes linked models found in the main model.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, linked models are not included.

---

### Export Parts

**What it does**
- Includes Part elements in the export.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, parts are not included.

---

### Export Room As Attribute

**What it does**
- Exports room information as an attribute.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, room attributes are not included.

---

### Export Room Geometry

**What it does**
- Includes room geometry in the export.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, room geometry is not included.

---

### Export Urls

**What it does**
- Includes URL information in the export.

**When it appears**
- Always.

**Required?**
- No.

**If missing/invalid**
- If off, URL information is not included.

---

### Faceting Factor

**What it does**
- Controls how smooth curved geometry looks in the export.

**When it appears**
- Only in Revit versions where this field is available.

**Required?**
- No (default is `1`).

**If missing/invalid**
- Export uses the default value.

---

### Convert Lights

**What it does**
- Includes lights in the export.

**When it appears**
- Only in Revit versions where this field is available.

**Required?**
- No.

**If missing/invalid**
- If off, lights are not included.

---

### Convert Linked CAD Formats

**What it does**
- Includes linked CAD files in the export.

**When it appears**
- Only in Revit versions where this field is available.

**Required?**
- No.

**If missing/invalid**
- If off, linked CAD files are not included.

---

## Naming / Output

### How the file name is built

1. The tool first creates a base name using **Naming Options**.
2. If **Prefix in file name (Optional)** is filled in, it is added to the beginning.
3. If **Suffix in file name (Optional)** is filled in, it is added to the end.
4. Any characters that are not allowed in Windows file names are automatically replaced with `_`.
5. The file is saved as: `<FileName>.nwc` in your **Destination Folder**.

### What each Naming Options choice produces

- **ViewNameOnly**: the view name
- **ModelNameOnly**: the model name
- **ModelNameAndViewName**: model name + **Separator in File Name** + view name
- **ViewNameAndModelName**: view name + **Separator in File Name** + model name
- **Custom Naming Convention**: your pattern (with placeholders replaced where possible)

### Examples

Assume:
- Destination Folder: `C:\Exports\NWC`
- Model name: `Hospital_A`
- View name: `NWC_3D_Coordination`
- Separator in File Name: `-`
- Prefix in file name (Optional): `CO_`
- Suffix in file name (Optional): `_v2`

1. **ViewNameOnly** ? `CO_NWC_3D_Coordination_v2.nwc`
2. **ModelNameOnly** ? `CO_Hospital_A_v2.nwc`
3. **ModelNameAndViewName** ? `CO_Hospital_A-NWC_3D_Coordination_v2.nwc`
4. **ViewNameAndModelName** ? `CO_NWC_3D_Coordination-Hospital_A_v2.nwc`
5. **Custom Naming Convention** with `NWC_{ModelName}_{ViewName}` ? `NWC_Hospital_A_NWC_3D_Coordination.nwc`

Custom Naming Convention examples you can type:
- `NWC_{ModelName}_{ViewName}`
- `{ViewName}_{Phase}`
- `IFC_{Discipline}_{ViewName}`

---

## Results and messages

- If everything succeeds, you’ll see a success message telling you how many files were exported.
- If some exports fail, you’ll see a partial success message including a “Failures” list.
- If the export cannot start (for example, **Destination Folder** is empty or **Navisworks Exporter** is missing), you’ll see an error message and nothing (or only some items) will be exported.

