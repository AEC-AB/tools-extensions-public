---
applyTo: '**/src/Revit/dotnet/**/*.cs'
---
# Revit Platform Instructions

Use `ui-common.instructions.md` for all UI configuration and validation conventions.

## Revit API Context

- Access active document via `context.UIApplication.ActiveUIDocument?.Document`.
- Return a failure result if no active document exists.
- Use `Transaction` for model modifications.

```csharp
var document = context.UIApplication.ActiveUIDocument?.Document;
if (document is null)
    return Result.Text.Failed("Revit has no active model open");

using var transaction = new Transaction(document, "My Extension");
transaction.Start();
// Modify elements
transaction.Commit();
```

## ValueCopy

Use ValueCopy for parameter copy workflows.

```csharp
[ValueCopyCollector(typeof(ValueCopyRevitCollector))]
public ValueCopy ValueCopy { get; set; }
```

Implement `IValueCopyRevitCollector<TArgs>` to provide sources and targets.

## Revit AutoFill

Use `CustomRevitAutoFill` and `IRevitAutoFillCollector<TArgs>` for document-driven suggestions.

## Best Practices

1. Keep transactions short and focused.
2. Verify parameter existence and storage types before setting values.
3. Use filtered collectors to avoid full-model scans.
4. Guard against empty selections when required by command logic.
