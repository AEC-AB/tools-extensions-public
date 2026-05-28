# Assistant Extensions: Platform Guide

This guide covers Assistant-specific patterns for writing extensions that run in the Assistant application environment.

## Quick Reference

- **Extension interface:** `IAssistantExtension`
- **Execution context:** Assistant core; no host application API access
- **Variable binding:** Read/write workflow variables
- **Typical use cases:** Data transformation, file processing, orchestration logic

## What Is an Assistant Extension?

Assistant extensions are the default choice for extensions that do not depend on Revit/AutoCAD/Tekla/Navisworks host APIs.

Choose an Assistant extension when your logic is primarily:

- External service/tool integration (for example StreamBIM, INFRA, Excel, REST APIs)
- Data/file processing outside a host model API
- Workflow orchestration and cross-system automation

If your extension must access a host application's model/document/selection/UI APIs, use that host's platform extension type instead (Revit/AutoCAD/Tekla/Navisworks).

## Getting Started

See [Quick Start](../QUICK_START.md) for a basic extension example. This guide covers Assistant-specific patterns.

## Variable Binding

Assistant extensions can read and write workflow variables.

### Reading Variables

```csharp
public async Task<IExtensionResult> RunAsync(
    IAssistantExtensionContext context,
    MyArgs args,
    CancellationToken cancellationToken)
{
    // Your extension receives the Args
    // Perform your work

    return Result.Text.Succeeded("Done");
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
