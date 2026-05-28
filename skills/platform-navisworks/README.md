# Platform guide

Use this skill when editing `*Command.cs`, collector code, or Navisworks-specific runtime logic.

## Load these docs first

Use the `extension-docs` MCP tool:

1. `operation=content` with document id `navisworks`
2. `operation=search` with focused topics such as `current selection` when you need examples

## API context

- Access the active document through `Autodesk.Navisworks.Api.Application.ActiveDocument`.
- Work with the current selection through `document.CurrentSelection.SelectedItems`.
- Guard null or empty context before processing.

## Platform rules

- Use `INavisworksAutoFillCollector<TArgs>` when option values should come from the active document or selection.
- Prefer read-only traversal for review and analysis workflows.
- Catch only known collector exceptions when you can provide a safe fallback.
- Provide clear result messages for user feedback.
- Prefer Navisworks terminology in labels and results.
