## name: platform-autocad
description: Use this skill when editing `*Command.cs`, collector code, or AutoCAD-specific runtime logic


# Platform guide

Use this skill when editing `*Command.cs`, collector code, or AutoCAD-specific runtime logic.

## Load these docs first

Use the `extension-docs` MCP tool:

1. `operation=content` with document id `autocad`
2. `operation=search` with focused topics such as `document lock` or `transaction` when you need examples

## API context

- Access the active document with `Application.DocumentManager.MdiActiveDocument`.
- Lock the document before write operations with `doc.LockDocument()`.
- Use database transactions for entity creation and modification.

## Platform rules

- Use `IAutoCADAutoFillCollector<TArgs>` when option values should come from the active drawing.
- Validate objects returned by `GetObject` casts before using them.
- Commit transactions only after successful model updates.
- Return explicit failure messages when the document or drawing context is missing.
- Prefer AutoCAD terminology in field labels and results.

