---
applyTo: '**/src/Tekla/dotnet/**/*.cs'
---
# Tekla Platform Instructions

Use `ui-common.instructions.md` for all UI configuration and validation conventions.

## Tekla API Context

- Create a `Model` and verify connection with `GetConnectionStatus()`.
- Use `ModelObjectSelector` for selected object workflows.
- Commit changes when model updates are complete.

```csharp
var model = new Model();
if (!model.GetConnectionStatus())
    return Result.Text.Failed("No active Tekla model");

var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
var selectedObjects = selector.GetSelectedObjects();

// Update model
model.CommitChanges();
```

## Tekla AutoFill

Use `CustomTeklaAutoFill` with `ITeklaAutoFillCollector<TArgs>` for model-driven suggestions.

## Best Practices

1. Verify model connection before operations.
2. Filter model objects for performance.
3. Handle null/cast issues explicitly.
4. Commit only after successful updates.
