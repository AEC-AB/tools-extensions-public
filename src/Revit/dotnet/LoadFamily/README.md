# Load Family

## Description

The **Load Family** extension lets you load one or more Revit family files (`.rfa`) into the active Revit model. You can pick individual files or point the extension at an entire directory and let it scan for families automatically. All load operations run inside a single transaction group, meaning the entire batch appears as one entry in Revit's undo history.

---

## UI Options

### Load Mode
Determines how families are selected. Choose one of two options:

| Option | Behaviour |
|---|---|
| **Select Files** | Opens a multi-select file picker — choose one or more `.rfa` files individually. |
| **Load from Directory** | Opens a folder picker — the extension scans the chosen folder for all `.rfa` files. |

---

### Select Files mode

| Field | Description |
|---|---|
| **Family Files** | Multi-file picker filtered to `.rfa`. Select as many files as needed. |

---

### Load from Directory mode

| Field | Description |
|---|---|
| **Family Directory** | Path to the folder containing `.rfa` files. |
| **Include Sub-directories** | When enabled, nested sub-folders are also scanned recursively. |

---

### Overwrite Options
These options apply in both modes and control what happens when a family with the same name already exists in the document.

| Field | Default | Description |
|---|---|---|
| **Overwrite if Found** | Off | Reload the family even if it already exists in the document. If off, existing families are skipped. |
| **Overwrite if in Use** | Off | Reload the family even if instances of it are placed in the model. Only applies when **Overwrite if Found** is on. If off and the family has placed instances, it is skipped. |
| **Overwrite Parameter Values** | Off | Reset family parameter values to the file's defaults when reloading. Only applies when an existing family is being overwritten. |

---

## Output

After the operation completes, a result message is shown with a one-line summary followed by a breakdown per outcome:

```
3 loaded · 1 skipped · 1 failed
  Loaded:  DoorFrame, WindowDouble, CurtainPanel
  Skipped (no changes):  GenericBeam
  Failed:  CorruptFamily (file not found)
```

| Result state | When it occurs |
|---|---|
| **Succeeded** | All resolved families were loaded (or had no changes). |
| **Partially Succeeded** | At least one family was loaded but one or more also failed. |
| **Failed** | No families could be loaded (e.g. empty selection, invalid path, or all files missing). |

Each family name in the breakdown is shown without the `.rfa` extension. Families that could not be found on disk are noted with `(file not found)`.

All loaded family symbols are automatically activated so they are immediately available for placement.