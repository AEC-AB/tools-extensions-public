---
applyTo: '**/src/AutoCAD/dotnet/**/*.cs'
---
# AutoCAD Platform Instructions

Use `ui-common.instructions.md` for all UI configuration and validation conventions.

## AutoCAD API Context

- Access active document with `Application.DocumentManager.MdiActiveDocument`.
- Lock document for model writes using `doc.LockDocument()`.
- Use database transactions for entity creation/modification.

```csharp
var doc = Application.DocumentManager.MdiActiveDocument;
if (doc is null)
    return Result.Text.Failed("AutoCAD has no active model open");

var db = doc.Database;
using var transaction = db.TransactionManager.StartTransaction();
using var documentLock = doc.LockDocument();

// Write model entities and commit
transaction.Commit();
```

## AutoCAD AutoFill

Use `CustomAutoCADAutoFill` with `IAutoCADAutoFillCollector<TArgs>` for document-aware options.

## Best Practices

1. Always lock the document before write operations.
2. Validate objects returned by `GetObject` casts.
3. Commit transactions only after successful model updates.
4. Return explicit failure messages when context is missing.
