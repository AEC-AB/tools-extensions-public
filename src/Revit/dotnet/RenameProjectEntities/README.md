# RenameProjectEntities

A Revit extension that performs bulk **find-and-replace** across all project entities. Rename parameter values, element names, view names, family symbol names, materials, worksets, levels, grids, project info, and more.

## Getting Started

1. Open a Revit model.
2. Configure the extension inputs (see **UI Controls** below).
3. Use **Preview Only** first to see a list of everything that would be renamed without making changes.
4. Disable **Preview Only** and run again to commit the changes.
5. Review the result message. It includes a summary report and a DETAILS section with a step-by-step operation log.

## UI Controls

| Control | Type | Description |
|---|---|---|
| **Find** | Text | The text to search for inside project entities. |
| **Replace** | Text | The replacement text. |
| **Search Scope** | Dropdown | `Everything`, `Names Only`, `Parameter Values Only`, `Parameter Names Only`, or `Custom`. |
| **Match Mode** | Dropdown | `Exact`, `Contains`, `StartsWith`, `EndsWith`, or `Regex`. |
| **Match Case** | Checkbox | Perform a case-sensitive search. |
| **Use Regex** | Checkbox | Interpret **Find** as a regular expression (overrides Match Mode). |
| **Preview Only** | Checkbox | Show what would be renamed without modifying the model. |

### Custom Scope Toggles

When **Search Scope** is set to `Custom`, the following checkboxes appear:

- Element & Type Names
- Parameter Values
- Parameter Names
- Views, Sheets & Schedules
- Families & Family Symbols
- Materials
- Project Info
- Levels, Grids & Reference Planes
- Worksets
- Phases

## What Gets Renamed

| Category | Scope |
|---|---|
| **Element & Type Names** | All instance and type `Element.Name` properties. |
| **Parameter Values** | All string parameter values on instances and types. |
| **Parameter Names** | Scans project/shared parameter definitions (report-only — Revit API does not allow renaming bound parameter definitions). |
| **Views & Sheets** | View names and sheet numbers. |
| **Families** | Family symbol (type) names. `Family.Name` is read-only and is skipped. |
| **Materials** | Material names. |
| **Project Info** | String parameter values on `ProjectInformation`. |
| **Levels, Grids & Ref Planes** | Names of levels, grids, and reference planes. |
| **Worksets** | User workset names. |
| **Phases** | Phase names. |

## Result Format

The extension returns a structured result message:

```
Rename Report: 142 changed, 3 failed.

[Parameter Values] Changed: 120, Failed: 2
[Element & Type Names] Changed: 15, Failed: 1
[Views & Sheets] Changed: 7, Failed: 0
...
```

- **Top section** — summary with per-category counts.
- **Failures** — reported as individual lines with Element ID, field name, and error.
- **DETAILS section** — full step-by-step log of the operation.

## Safety Features

- **Preview Only** — review all planned changes before committing.
- **Transaction rollback** — if cancellation is requested, all pending changes are rolled back.
- **Per-element try/catch** — even if a specific element crashes during parameter access, the error is logged and the renamer continues with the next element.
- **Collector materialization** — `FilteredElementCollector` results are copied to lists before iterating, avoiding iterator invalidation during modifications.
- **Category exclusions** — internally excluded categories that are known to crash during parameter enumeration (e.g., cameras, section boxes, systems).

## Known Limitations

| Limitation | Reason |
|---|---|
| **Parameter names cannot be renamed** | Revit's standard API prevents renaming `ExternalDefinition` or bound `InternalDefinition` objects. The renamer reports them but cannot modify them. |
| **Family `Name` cannot be renamed** | `Family.Name` is read-only. Only `FamilySymbol` names (family types) are renamed. |
| **Built-in parameters are read-only** | Some built-in Revit parameters are read-only. The renamer skips them gracefully and reports them as "read-only." |
| **Regular expressions** | Only basic regex is supported. Complex back-references or lookahead may behave unexpectedly. |
| **Linked models** | Only the active (open) model is processed. Linked RVT files are not affected. |

## Architecture

```
RenameProjectEntitiesCommand.cs    // Orchestrates renamers, manages transactions
├── RenameProjectEntitiesArgs.cs   // UI field definitions
├── MatchEvaluator.cs              // String matching & replacement engine
├── RenameReport.cs                // Aggregates rename results
├── RenamerLogger.cs               // In-memory step-by-step logging
├── CurrentLog.cs                  // Static logger accessor for renamers
└── Renamers/
    ├── IEntityRenamer.cs          // Renamer contract
    ├── ElementNameRenamer.cs
    ├── ParameterValueRenamer.cs
    ├── ParameterNameRenamer.cs
    ├── ViewRenamer.cs
    ├── FamilyRenamer.cs
    ├── MaterialRenamer.cs
    ├── ProjectInfoRenamer.cs
    ├── LevelGridRenamer.cs
    ├── WorksetRenamer.cs
    └── PhaseRenamer.cs
```