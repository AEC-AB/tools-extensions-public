# Platform guide

Use this skill when editing `*Command.cs`, collector code, or Revit-specific runtime logic.

## Load these docs first

Use the `extension-docs` MCP tool:

1. `operation=content` with document id `revit`
2. `operation=search` with focused topics such as `transaction` or `valuecopy` when you need examples

## API context

- Access the active document through `context.UIApplication.ActiveUIDocument?.Document`.
- Return a failure result if no active document is open.
- Wrap model changes in a `Transaction`.

## Platform rules

- Use `ValueCopy` when the workflow copies parameter values between Revit element sets.
- Use `IRevitAutoFillCollector<TArgs>` or Revit-specific autofill attributes when values should be populated from the active model.
- Keep transactions short and focused.
- Verify parameter existence and storage types before setting values.
- Use filtered collectors to avoid full-model scans and guard against empty selections when the command depends on user selection.
- Prefer Revit terminology in labels and result messages.
