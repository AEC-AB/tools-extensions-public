# Tekla Extensions: Platform Guide

This guide covers Tekla Structures-specific patterns for writing extensions that integrate with Trimble Tekla Structures.

## Quick Reference

- **Extension interface:** `ITeklaExtension<TArgs>`
- **Execution context:** Tekla Structures process with Model API access
- **Model transactions:** Commit required for model changes
- **Model access:** Through Tekla Model singleton
- **Supported versions:** Tekla Structures 2020 and later

## Getting Started

See [Quick Start](../QUICK_START.md) for Args/Command basics. This guide covers Tekla-specific patterns.

## Choose Tekla Template

When creating a Tekla extension for Assistant, choose between:

1. **Tekla Automation Extension for Assistant**
2. **Tekla App Extension for Assistant**

### Tekla Automation Extension for Assistant

Use this template when the extension should run as a task inside an Assistant automation action.

- Best for workflow/automation actions
- Focused on deterministic task execution (input -> run -> result)
- Typically no modeless application UI

### Tekla App Extension for Assistant

Use this template when the extension is an interactive Tekla app with a modeless UI.

- Best for user-driven tools launched inside Tekla
- Better fit for shortcut/button-driven usage than unattended automation
- Supports app-style MVVM/WPF interaction patterns for responsive UI while model operations are executed

## Model API Access

Tekla extensions access the model through the Tekla Model API.

### Basic Pattern

```csharp
public IExtensionResult Run(ITeklaExtensionContext context, MyArgs args, CancellationToken cancellationToken)
{
    var model = new Model();
    
    if (!model.GetConnectionStatus())
        return Result.Text.Failed("Could not connect to Tekla model");
    
    // Your model operations here
    
    model.CommitChanges();
    
    return Result.Text.Succeeded("Tekla operation completed");
}
```

## Model Operations & Selectors

*(Content to be added based on Tekla API patterns)*

---

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
