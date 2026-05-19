# Assistant Extensions: Platform Guide

This guide covers Assistant-specific patterns for writing extensions that run in the Assistant application environment.

## Quick Reference

- **Extension interface:** `IAssistantExtension`
- **Execution context:** Assistant core; no host application API access
- **Variable binding:** Read/write workflow variables
- **Typical use cases:** Data transformation, file processing, orchestration logic

## Getting Started

See [Quick Start](../QUICK_START.md) for a basic extension example. This guide covers Assistant-specific patterns.

## Variable Binding

Assistant extensions can read and write workflow variables.

### Reading Variables

```csharp
public async Task<Result> ExecuteAsync(object? args, CancellationToken cancellationToken)
{
    var config = (MyArgs)args!;
    
    // Your extension receives the Args
    // Perform your work
    
    return Result.Success("Done");
}
```

## CollectorType in Assistant Extensions

For fields that support `CollectorType` (`ICollectorTypeAttribute`), Assistant extensions must use collectors implementing `IAsyncAutoFillCollector<TArgs>`.

- `TextField`: collector is optional and used only for suggestions.
- `OptionsField` and `ChoiceField`: when property type is not enum, provide `CollectorType` so the UI has items to pick from.
- If property type is enum, enum values are used automatically and collector is optional.

Example:

```csharp
[TextField(
    Label = "Text input with AutoFill",
    CollectorType = typeof(CustomAutoFillCollector))]
public string? AutoFillTextInput { get; set; }

internal class CustomAutoFillCollector : IAsyncAutoFillCollector<AssistantDemoExtensionArgs>
{
    public Task<Dictionary<string, string>> Get(
        AssistantDemoExtensionArgs args,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>();
        for (int i = 1; i <= 5; i++)
        {
            result.Add($"Key{i}", $"Display value {i}");
        }

        return Task.FromResult(result);
    }
}
```

## Integration-Specific Patterns

*(Content to be added based on Assistant SDK patterns)*

---

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
