# Tekla Extensions: Platform Guide

This guide covers Tekla Structures-specific patterns for writing extensions that integrate with Trimble Tekla Structures.

## Quick Reference

- **Extension interface:** `IAssistantExtension` (via Tekla integration layer)
- **Execution context:** Tekla Structures process with Model API access
- **Model transactions:** Commit required for model changes
- **Model access:** Through Tekla Model singleton
- **Supported versions:** Tekla Structures 2020 and later

## Getting Started

See [Quick Start](../QUICK_START.md) for Args/Command basics. This guide covers Tekla-specific patterns.

## Model API Access

Tekla extensions access the model through the Tekla Model API.

### Basic Pattern

```csharp
public async Task<Result> ExecuteAsync(object? args, CancellationToken cancellationToken)
{
    var config = (MyArgs)args!;
    var model = new Model();
    
    if (!model.GetConnectionStatus())
        return Result.Error("Could not connect to Tekla model");
    
    // Your model operations here
    
    model.CommitChanges();
    
    return Result.Success("Tekla operation completed");
}
```

## Model Operations & Selectors

*(Content to be added based on Tekla API patterns)*

---

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
