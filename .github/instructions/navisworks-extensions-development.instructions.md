---
applyTo: '**/src/Navisworks/dotnet/**/*.cs'
---
# Navisworks Platform Instructions

Use `ui-common.instructions.md` for all UI configuration and validation conventions.

## Navisworks API Context

- Access document via `Autodesk.Navisworks.Api.Application.ActiveDocument`.
- Work with current selection through `document.CurrentSelection.SelectedItems`.
- Guard null/empty context before processing.

```csharp
var document = Autodesk.Navisworks.Api.Application.ActiveDocument;
if (document is null)
    return Result.Text.Failed("Navisworks has no active model open");

var selectedItems = document.CurrentSelection.SelectedItems;
foreach (var item in selectedItems)
{
    var name = item.DisplayName;
}
```

## Navisworks AutoFill

Use `CustomNavisworksAutoFill` with `INavisworksAutoFillCollector<TArgs>` for context-aware option lists.

## Best Practices

1. Check active document before all API access.
2. Prefer read-only traversal for analysis workflows.
3. Catch collector exceptions to avoid UI failures.
4. Provide clear result messages for user feedback.
