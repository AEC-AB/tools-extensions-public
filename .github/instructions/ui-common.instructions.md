---
applyTo: '**/src/**/dotnet/**/*.cs'
---
# Extension UI and Framework Guide

Compact reference for args UI declarations.

## Core Pattern

1. Args class defines fields with UI attributes.
2. Command class reads `args` and returns `IExtensionResult`.
3. Always pass `CancellationToken` to async work.

Command results:

```csharp
Result.Text.Succeeded("message");
Result.Text.PartiallySucceeded("warning");
Result.Text.Failed("error");
Result.Empty.Succeeded();
Result.Empty.PartiallySucceeded();
Result.Empty.Failed();
```

## Shared Field Parameters

- `Label` (required)
- `ToolTip`
- `Hint`
- `HelperText`
- `Visibility`

## Supported Fields

`TextField`, `UrlField`, `IntegerField`, `DoubleField`, `DateTimeField`, `BooleanField`, `ColorField`, `FilePickerField`, `FolderPickerField`, `SaveFileField`, `OptionsField`, `ChoiceField`, `ListField`, `DictionaryField`, `PasswordField`.

## Validation

Use standard .NET validation attributes with clear error messages.

```csharp
[Required(ErrorMessage = "Required")]
[Range(1, 100, ErrorMessage = "Must be 1-100")]
[RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Use A-Z, 0-9, _")]
```

## AutoFill Collectors

- Generic: `IAsyncAutoFillCollector<TArgs>`.
- Platform-specific collector interfaces are documented in each platform instructions file.

```csharp
internal class StatusCollector : IAsyncAutoFillCollector<ExampleArgs>
{
    public Task<Dictionary<string, string>> Get(ExampleArgs args, CancellationToken cancellationToken)
        => Task.FromResult(new Dictionary<string, string> { ["A"] = "Active" });
}
```
