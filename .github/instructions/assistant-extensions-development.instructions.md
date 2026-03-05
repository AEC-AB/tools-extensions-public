---
applyTo: '**/src/Assistant/dotnet/**/*.cs'
---
# Assistant Platform Instructions

Use `ui-common.instructions.md` for all UI configuration and validation conventions.

## Platform Scope

Assistant extensions run outside a host CAD/BIM process. Use them for desktop automation, file workflows, and external API orchestration.

## Command Contract

Implement the Assistant extension contract and return `IExtensionResult`.

```csharp
public class MyExtensionCommand : IAssistantExtension<MyArgs>
{
    public async Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context,
        MyArgs args,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(cancellationToken);
        return Result.Text.Succeeded("Success");
    }
}
```

## Best Practices

1. Validate file paths and URLs before execution.
2. Pass cancellation tokens to all long-running operations.
3. Return actionable, user-friendly error messages.
4. Keep logic host-agnostic and testable.
