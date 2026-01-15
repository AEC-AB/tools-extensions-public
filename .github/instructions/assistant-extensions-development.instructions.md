---
applyTo: '**/Assistant/dotnet/**'
---
# Assistant .NET Extension Development Guide

## Extension Framework

Extensions consist of three components:

1. **Args Class**: Input parameters with Field Attributes (generates UI automatically)
2. **Command Class**: Business logic implementing `IAssistantExtension<TArgs>`
3. **Result Class**: Standardized output using `IExtensionResult`

## Command Class Implementation

Implement `IAssistantExtension<TArgs>` interface:

```csharp
public class MyExtensionCommand : IAssistantExtension<MyExtensionArgs>
{
    public async Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context, 
        MyExtensionArgs args, 
        CancellationToken cancellationToken)
    {
        // Access user inputs via args parameter
        var input = args.SomeProperty;
        
        // CRITICAL: Always pass cancellationToken to async operations
        await SomeOperationAsync(cancellationToken);
        
        // Return standardized result
        return Result.Text.Succeeded("Success message");
    }
}
```

### Result Types

```csharp
// Text results
Result.Text.Succeeded("message")
Result.Text.PartiallySucceeded("warning message")
Result.Text.Failed("error message")

// Empty results (no message)
Result.Empty.Succeeded()
Result.Empty.PartiallySucceeded()
Result.Empty.Failed()
```

Custom results implement `IExtensionResult` with `ExecutionResult` enum (Failed, PartiallySucceeded, Succeeded).

## Args Class - Field Attributes

Define properties with Field Attributes to generate UI controls. `Label` is **required** for all attributes.

### Common Parameters
- `Label`: Display name (required)
- `ToolTip`: Hover help text
- `Hint`: Placeholder text
- `HelperText`: Description below field
- `Visibility`: Conditional display expression

### Text Fields
```csharp
// Basic text
[TextField(Label = "Name", Hint = "Enter name", ToolTip = "User's full name")]
public string Name { get; set; } = "Default";

// Multiline text
[TextField(Label = "Description", IsMultiline = true, MinLines = 3, MaxLines = 6)]
public string Description { get; set; }

// URL with validation
[UrlField(Label = "Website", Hint = "https://example.com")]
[Url(ErrorMessage = "Invalid URL")]
public string? Website { get; set; }
```

### Numeric Fields
```csharp
// Integer with slider
[IntegerField(Label = "Count", MinimumValue = 0, MaximumValue = 100, StepValue = 5)]
public int Count { get; set; } = 10;

// Double
[DoubleField(Label = "Value")]
public double Value { get; set; }
```

### Date and Boolean
```csharp
[DateTimeField(Label = "Date", ShowTime = false)]  // Date only
[DateTimeField(Label = "DateTime", ShowTime = true)]  // Date and time
public DateTime Date { get; set; }

[BooleanField(Label = "Enabled")]
public bool IsEnabled { get; set; }

[ColorField(Label = "Theme Color")]
public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Blue;
```

### File System Pickers
```csharp
// Single file
[FilePickerField(Label = "Config File", FileExtensions = ["json", "xml", "*"])]
public string? FilePath { get; set; }

// Multiple files
[FilePickerField(Label = "Documents", FileExtensions = ["pdf", "docx"])]
public List<string> Files { get; set; } = [];

// Folder
[FolderPickerField(Label = "Output Directory")]
public string? FolderPath { get; set; }

// Multiple folders
[FolderPickerField(Label = "Source Directories")]
public List<string> Folders { get; set; } = [];

// Save file dialog
[SaveFileField(Label = "Export As", FileExtensions = ["json", "csv"])]
public string? SavePath { get; set; }
```

### Selection Controls
```csharp
// ComboBox (single select from enum)
[OptionsField(Label = "Status")]
public MyEnum Status { get; set; }

// ListBox (multi-select)
[OptionsField(Label = "Options", MaxHeight = 200, CollectorSortOrder = SortOrder.SortByAscending)]
public List<MyEnum> SelectedOptions { get; set; } = [];

// Compact ListBox
[OptionsField(Label = "Items", CompactMode = true)]
public List<string> Items { get; set; } = [];

// Radio buttons
[ChoiceField(Label = "Choice", Orientation = ChoiceOrientation.Vertical)]
public MyEnum Choice { get; set; }
```

### Collections
```csharp
// String list
[ListField(Label = "Tags", HelperText = "Add tags")]
public List<string> Tags { get; set; } = ["Tag1", "Tag2"];

// Dictionary (key-value pairs)
[DictionaryField(Label = "Settings")]
public Dictionary<string, string> Settings { get; set; } = [];
```

### Security
```csharp
// Credentials from Credential Manager
[PasswordField(Label = "API Credentials", ToolTip = "Select from Credential Manager")]
public string CredentialAppId { get; set; } = "MyApp";
```

## Validation Attributes

Apply standard .NET validation to properties. Always provide `ErrorMessage`:

```csharp
[Required(ErrorMessage = "This field is required")]
[Range(0, 120, ErrorMessage = "Age must be 0-120")]
[RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase letters")]
[MinLength(3, ErrorMessage = "At least 3 items required")]
[Url(ErrorMessage = "Invalid URL format")]
[AllowedValues(nameof(Status.Active), nameof(Status.Pending), 
    ErrorMessage = "Select Active or Pending")]
```

## Conditional Visibility

Show/hide fields dynamically using `Visibility` parameter with expressions:

```csharp
// Boolean condition
[BooleanField(Label = "Show Advanced")]
public bool ShowAdvanced { get; set; }

[TextField(Label = "Advanced Setting", Visibility = nameof(ShowAdvanced))]
public string? AdvancedSetting { get; set; }

// Value comparison
[TextField(Visibility = $"{nameof(Input)} == 'Apple'")]
public string? ConditionalField { get; set; }

// Multiple conditions
[TextField(Visibility = $"{nameof(Age)} >= 18 && {nameof(Status)} == 'Active'")]
public string Notification { get; } = "Eligible";

// Collection count
[TextField(Visibility = $"{nameof(Items)}.Count > 2")]
public string? ItemsNote { get; } = "More than 2 items added";
```

## AutoFill Collectors (Dynamic Options)

Provide dynamic dropdown options by implementing `IAsyncAutoFillCollector<TArgs>`:

```csharp
internal class MyCollector : IAsyncAutoFillCollector<MyExtensionArgs>
{
    public Task<Dictionary<string, string>> Get(
        MyExtensionArgs args, 
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, string>();
        
        // Generate options dynamically
        for (int i = 1; i <= 5; i++)
            options[$"Key{i}"] = $"Display Value {i}";
        
        // Access other args properties for conditional options
        if (!string.IsNullOrEmpty(args.SomeProperty))
            options["custom"] = args.SomeProperty;
        
        return Task.FromResult(options);
    }
}
```

**Usage**: Add `CollectorType = typeof(MyCollector)` to:
- `TextField` (autocomplete suggestions)
- `OptionsField` (dropdown/listbox options)
- `DictionaryField` (value options)

**Sorting**: Use `CollectorSortOrder = SortOrder.SortByAscending` or `SortOrder.SortByDescending` or `SortOrder.None`

## Custom Enums with Display Names

Use `[Description]` for user-friendly enum values:

```csharp
public enum ProcessStatus
{
    [Description("Not Started")]
    NotStarted,
    
    [Description("In Progress")]
    InProgress,
    
    [Description("Completed Successfully")]
    Completed
}
```

## Read-Only Fields

Use getter-only properties for display-only fields:

```csharp
[TextField(Label = "Information")]
public string ReadOnlyInfo { get; } = "This cannot be edited";

[TextField(Label = "Section Header")]
public string SectionTitle { get; } = "*** Configuration Section ***";
```

## Property Type Mapping

| Property Type | Field Attributes | UI Control |
|---------------|------------------|------------|
| `string` | TextField, UrlField, OptionsField | Text box, autocomplete, dropdown |
| `string?` | TextField, UrlField | Optional text input |
| `int` | IntegerField | Number input or slider |
| `double` | DoubleField | Decimal number input |
| `bool` | BooleanField | Checkbox |
| `DateTime` | DateTimeField | Date/time picker |
| `enum` | OptionsField, ChoiceField | Dropdown or radio buttons |
| `List<string>` | ListField, OptionsField, FilePickerField, FolderPickerField | String list, multi-select, multiple files/folders |
| `List<enum>` | OptionsField | Multi-select dropdown/listbox |
| `Dictionary<string,string>` | DictionaryField | Key-value pair editor |
| `System.Drawing.Color` | ColorField | Color picker |

## Best Practices

1. **Always provide `Label`** - Required for all Field Attributes
2. **Use meaningful `ToolTip` and `HelperText`** - Guide users on what to input
3. **Add validation with `ErrorMessage`** - Catch errors before execution
4. **Set sensible defaults** - Initialize properties with common values
5. **Use conditional visibility** - Show fields only when relevant
6. **Respect `CancellationToken`** - Pass to all async operations in Command Class
7. **Return appropriate result status** - Use Succeeded, PartiallySucceeded, or Failed
8. **Use read-only properties** - For section headers and informational text
9. **Provide clear error messages** - Help users understand validation failures
10. **Use AutoFill collectors** - For dynamic or context-dependent options

## Quick Reference

```csharp
// Args Class - Define UI
public class MyExtensionArgs
{
    [TextField(Label = "Input", ToolTip = "Help text")]
    [Required(ErrorMessage = "Required")]
    public string Input { get; set; } = "Default";
    
    [BooleanField(Label = "Enable Feature")]
    public bool EnableFeature { get; set; }
    
    [TextField(Label = "Feature Setting", Visibility = nameof(EnableFeature))]
    public string? FeatureSetting { get; set; }
}

// Command Class - Implement Logic
public class MyExtensionCommand : IAssistantExtension<MyExtensionArgs>
{
    public async Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context,
        MyExtensionArgs args,
        CancellationToken cancellationToken)
    {
        // Use args.Input, args.EnableFeature, etc.
        // Always pass cancellationToken
        await DoWorkAsync(cancellationToken);
        
        return Result.Text.Succeeded("Done!");
    }
}
```
