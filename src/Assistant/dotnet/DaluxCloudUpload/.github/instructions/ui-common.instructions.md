---
applyTo: '**/*.cs'
---
# Extension UI and Framework Guide

Compact reference for args UI declarations. Keep this file short and explicit.

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

```csharp
public class MyArgs
{
    [TextField(Label = "Name")]
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;
}
```

## Shared Field Parameters

Common options used by many fields:

- `Label` (required)
- `ToolTip`
- `Hint`
- `HelperText`
- `Visibility`

Visibility examples:

```csharp
[TextField(Label = "Advanced", Visibility = nameof(ShowAdvanced))]
[TextField(Label = "Only Apple", Visibility = $"{nameof(Fruit)} == 'Apple'")]
[TextField(Label = "Adult Active", Visibility = $"{nameof(Age)} >= 18 && {nameof(IsActive)}")]
```

## Supported Fields (Compact Catalog)

| Field attribute | Typical property type | Key options | Minimal example |
|---|---|---|---|
| `TextField` | `string`, `string?` | `IsMultiline`, `MinLines`, `MaxLines` | `[TextField(Label = "Title")] public string Title { get; set; } = "";` |
| `UrlField` | `string`, `string?` | `Hint` | `[UrlField(Label = "Site")] [Url(ErrorMessage = "Invalid URL")] public string? Site { get; set; }` |
| `IntegerField` | `int` | `MinimumValue`, `MaximumValue`, `StepValue` | `[IntegerField(Label = "Count", MinimumValue = 0, MaximumValue = 100)] public int Count { get; set; } = 1;` |
| `DoubleField` | `double` | `MinimumValue`, `MaximumValue`, `StepValue` | `[DoubleField(Label = "Ratio")] public double Ratio { get; set; } = 1.0;` |
| `DateTimeField` | `DateTime` | `ShowTime` | `[DateTimeField(Label = "Run At", ShowTime = true)] public DateTime RunAt { get; set; } = DateTime.Now;` |
| `BooleanField` | `bool` | none | `[BooleanField(Label = "Enabled")] public bool Enabled { get; set; }` |
| `ColorField` | `System.Drawing.Color` | none | `[ColorField(Label = "Color")] public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Blue;` |
| `FilePickerField` | `string?`, `List<string>` | `FileExtensions` | `[FilePickerField(Label = "Input", FileExtensions = ["json","xml"])] public string? Input { get; set; }` |
| `FolderPickerField` | `string?`, `List<string>` | none | `[FolderPickerField(Label = "Folder")] public string? Folder { get; set; }` |
| `SaveFileField` | `string?` | `FileExtensions` | `[SaveFileField(Label = "Output", FileExtensions = ["csv"])] public string? Output { get; set; }` |
| `OptionsField` | `enum`, `string`, `List<enum>`, `List<string>` | `CompactMode`, `MaxHeight`, `CollectorSortOrder`, `CollectorType` | `[OptionsField(Label = "Status")] public Status Status { get; set; }` |
| `ChoiceField` | `enum` | `Orientation` | `[ChoiceField(Label = "Mode", Orientation = ChoiceOrientation.Vertical)] public Mode Mode { get; set; }` |
| `ListField` | `List<string>` | none | `[ListField(Label = "Tags")] public List<string> Tags { get; set; } = [];` |
| `DictionaryField` | `Dictionary<string,string>` | `CollectorType`, `CollectorSortOrder` | `[DictionaryField(Label = "Map")] public Dictionary<string, string> Map { get; set; } = [];` |
| `PasswordField` | `string` | none | `[PasswordField(Label = "Credential") ] public string CredentialAppId { get; set; } = "MyApp";` |

## Compact Usage Examples

```csharp
public enum Status { Draft, Active, Archived }

public class ExampleArgs
{
    [TextField(Label = "Name", ToolTip = "Display name")]
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [OptionsField(Label = "Status")]
    public Status Status { get; set; }

    [BooleanField(Label = "Show Advanced")]
    public bool ShowAdvanced { get; set; }

    [IntegerField(Label = "Retries", MinimumValue = 0, MaximumValue = 10, Visibility = nameof(ShowAdvanced))]
    public int Retries { get; set; } = 3;

    [FilePickerField(Label = "Files", FileExtensions = ["csv", "xlsx"])]
    public List<string> Files { get; set; } = [];

    [DictionaryField(Label = "Parameters")]
    public Dictionary<string, string> Parameters { get; set; } = [];
}
```

## Validation (Use With Any Field)

```csharp
[Required(ErrorMessage = "Required")]
[Range(1, 100, ErrorMessage = "Must be 1-100")]
[RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Use A-Z, 0-9, _")]
[MinLength(1, ErrorMessage = "At least one item")]
[AllowedValues(nameof(Status.Active), nameof(Status.Draft), ErrorMessage = "Choose Active or Draft")]
```

## AutoFill Collectors

- Generic: `IAsyncAutoFillCollector<TArgs>`.
- Platform-specific: defined in `platform.instructions.md`.
- `CollectorType` is commonly used on `TextField`, `OptionsField`, and `DictionaryField`.
- `CollectorSortOrder`: `SortOrder.SortByAscending`, `SortOrder.SortByDescending`, `SortOrder.None`.

```csharp
internal class StatusCollector : IAsyncAutoFillCollector<ExampleArgs>
{
    public Task<Dictionary<string, string>> Get(ExampleArgs args, CancellationToken cancellationToken)
        => Task.FromResult(new Dictionary<string, string> { ["A"] = "Active", ["D"] = "Draft" });
}
```

Use collector wiring where supported, for example:

```csharp
[OptionsField(Label = "Status", CollectorType = typeof(StatusCollector), CollectorSortOrder = SortOrder.SortByAscending)]
```

## Enum Labels and Read-Only Fields

```csharp
public enum ProcessStatus
{
    [Description("Not Started")] NotStarted,
    [Description("In Progress")] InProgress,
    [Description("Completed Successfully")] Completed
}

[TextField(Label = "Info")]
public string Info { get; } = "Read-only display value";
```

## Supported Property Types

- `string`, `string?`
- `int`, `double`
- `bool`
- `DateTime`
- `enum`
- `List<T>`
- `Dictionary<string, string>`
- `System.Drawing.Color`
