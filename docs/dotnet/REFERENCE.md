# Reference: Field Attributes, Validation, and DSL Syntax

Quick lookup tables and detailed reference for writing Args classes.

---

## CollectorType Rules (ICollectorTypeAttribute)

Fields that implement `ICollectorTypeAttribute` support `CollectorType` and `CollectorSortOrder`.

Use these rules:

| Field kind | Is `CollectorType` required? | Notes |
|------------|------------------------------|-------|
| `TextField` | Optional | Used only for suggestions (AutoFill). Users can still type any value. |
| `OptionsField` | Required when property type is **not** enum | If property type is enum, enum values are used automatically and `CollectorType` is optional. |
| `ChoiceField` | Required when property type is **not** enum | If property type is enum, enum values are used automatically and `CollectorType` is optional. |
| `ListField` | Optional | Enables suggested values for list entries. |
| `DictionaryField` | Optional | Enables suggested values for dictionary values. |
| `ValueCopyField` (Revit) | Required | Must provide a Revit value-copy collector implementation. |

Collector interface must match the integration:

- Assistant extensions: `IAsyncAutoFillCollector<TArgs>`
- Revit extensions: `IRevitAutoFillCollector<TArgs>`
- AutoCAD extensions: `IAutoCADAutoFillCollector<TArgs>`
- Tekla extensions: `ITeklaAutoFillCollector<TArgs>`
- Navisworks extensions: `INavisworksAutoFillCollector<TArgs>`

**Assistant example (`IAsyncAutoFillCollector<TArgs>`):**
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

**Revit example (`IRevitAutoFillCollector<TArgs>`):**
```csharp
[OptionsField(
    Label = "Element parameter",
    CollectorType = typeof(CustomRevitAutoFillCollector))]
public string? ParameterName { get; set; }

internal class CustomRevitAutoFillCollector : IRevitAutoFillCollector<RevitExtensionDemoArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, RevitExtensionDemoArgs args)
    {
        var result = new Dictionary<string, string>();
        var document = uiApplication.ActiveUIDocument?.Document;

        if (document is null)
            return result;

        using var element = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_GenericModel)
            .FirstElement();

        if (element is null)
            return result;

        foreach (var parameter in element.GetOrderedParameters())
        {
            result[parameter.Definition.Name] = parameter.Definition.Name;
        }

        return result;
    }
}
```

---

## Field Attributes Catalog

All field attributes are in `CW.Assistant.Extensions.Contracts.Fields`.

### Namespace Imports

Prefer using a `GlobalUsings.cs` file (when available) for extension projects that combine fields, upgrades, and custom autofill collectors.

```csharp
global using CW.Assistant.Extensions.Contracts.Fields;
global using CW.Assistant.Extensions.Contracts.Fields.Revit;
global using CW.Assistant.Extensions.Contracts.Upgrade;
global using CW.Assistant.Extensions.Assistant.Collectors;

// Platform-specific custom autofill attributes
global using CW.Assistant.Extensions.Revit.Attributes;
global using CW.Assistant.Extensions.Tekla.Attributes;
global using CW.Assistant.Extensions.Navisworks.Attributes;
global using CW.Assistant.Extensions.AutoCAD.Attributes;
```

These imports cover:

- Field attributes and Revit-specific field attributes.
- Args upgrade handlers with `IArgsUpgrade<TFrom, TTo>`.
- Async autofill collectors with `IAsyncAutoFillCollector<TArgs>`.
- Platform custom autofill attributes:
    - `CustomRevitAutoFillAttribute`
    - `CustomTeklaAutoFillAttribute`
    - `CustomNavisworksAutoFillAttribute`
    - `CustomAutoCADAutoFillAttribute`

### Text Input Fields

#### TextField

Renders a single-line or multi-line text input.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label displayed to user |
| `Hint` | string | null | Placeholder text or suggestion |
| `ToolTip` | string | null | Help text on hover |
| `HelperText` | string | null | Additional guidance below field |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |
| `IsMultiline` | bool | false | If true, render as textarea |
| `MinLines` | int | 3 | Minimum lines (multiline only) |
| `MaxLines` | int | 6 | Maximum lines (multiline only) |
| `CollectorType` | Type? | null | AutoFill suggestion provider |
| `CollectorSortOrder` | SortOrder | None | Sort order for collector results |
| `Visibility` | string? | null | DSL expression to show/hide field |
| `IsEnabled` | string? | null | DSL expression to enable/disable field |

**Example:**
```csharp
[TextField(Label = "Project Name", Hint = "e.g., Project Alpha")]
public string ProjectName { get; set; } = "";

[TextField(Label = "Notes", IsMultiline = true, MaxLines = 5)]
public string Notes { get; set; } = "";
```

#### UrlField

Specialized TextField for URLs with validation.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide field |
| `IsEnabled` | string? | null | DSL expression to enable/disable field |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[UrlField(Label = "Repository URL")]
public string RepoUrl { get; set; } = "";
```

### Numeric Input Fields

#### IntegerField

Renders a numeric input constrained to integers.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `MinimumValue` | int | `int.MinValue` | Lower numeric bound (`int.MinValue` means no lower bound) |
| `MaximumValue` | int | `int.MaxValue` | Upper numeric bound (`int.MaxValue` means no upper bound) |
| `StepValue` | int | `1` | Increment/decrement step value |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[IntegerField(Label = "Repeat Count", Hint = "1-10")]
[Range(1, 10)]
public int RepeatCount { get; set; } = 1;
```

#### DoubleField

Renders a numeric input for floating-point numbers.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[DoubleField(Label = "Offset Distance", Hint = "in meters")]
public double Offset { get; set; } = 0.0;
```

### Boolean Field

#### BooleanField

Renders a checkbox or toggle.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label / checkbox text |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[BooleanField(Label = "Include Archive Items")]
public bool IncludeArchived { get; set; } = false;
```

### Selection Fields

#### OptionsField

Renders a dropdown list. Requires a collector or enum.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `CollectorType` | Type? | null | Custom option collector |
| `CollectorSortOrder` | SortOrder | None | Sort order for collector results |
| `MaxHeight` | int | `int.MinValue` | Max list height in pixels (`int.MinValue` = auto-size) |
| `CompactMode` | bool | false | Enables compact rendering for list items |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[OptionsField(Label = "Document Type")]
public ProjectType DocumentType { get; set; } = ProjectType.Residential;

public enum ProjectType
{
    Residential,
    Commercial,
    Industrial
}
```

When the property type is an enum:

- Enum member name is used as the stored key value.
- If `[Description("...")]` is present on an enum member, that description is used as the display value in the UI.
- If no `Description` is provided, the enum member name is used for display.

Example:
```csharp
public enum ProjectType
{
    [Description("Residential Building")]
    Residential,

    [Description("Commercial Building")]
    Commercial,

    Industrial
}
```

#### ChoiceField

Renders a radio button or segmented control.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Orientation` | ChoiceOrientation | Horizontal | Layout direction for choices |
| `CollectorType` | Type? | null | Custom option collector |
| `CollectorSortOrder` | SortOrder | None | Sort order for collector results |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[ChoiceField(Label = "Export Format")]
public ExportFormat Format { get; set; } = ExportFormat.IFC;

public enum ExportFormat
{
    IFC,
    DWG,
    PDF
}
```

`ChoiceField` follows the same enum display/key behavior as `OptionsField`.

### File & Folder Pickers

#### FilePickerField

Renders an "Open File" dialog button.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Button label |
| `Hint` | string | null | Dialog hint |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `FileExtensions` | string[] | [] | Allowed file extensions (e.g., ["json", "*"]) |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example (single file):**
```csharp
[FilePickerField(Label = "Select Template", FileExtensions = ["xml", "*"])]
public string? TemplateFile { get; set; }
```

**Example (multiple files):**
```csharp
[FilePickerField(Label = "Select Files", FileExtensions = ["json", "*"])]
public List<string> JsonFiles { get; set; } = new();
```

#### FolderPickerField

Renders an "Open Folder" dialog button.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Button label |
| `Hint` | string | null | Dialog hint |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[FolderPickerField(Label = "Output Directory")]
public string? OutputFolder { get; set; }
```

#### SaveFileField

Renders a "Save As" dialog button.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Button label |
| `Hint` | string | null | Dialog hint |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `FileExtensions` | string[] | [] | Allowed file extensions (e.g., ["json", "*"]) |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[SaveFileField(Label = "Save As", FileExtensions = ["json", "*"])]
public string? OutputFile { get; set; }
```

### Date & Time

#### DateTimeField

Renders a date/time picker.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `ShowTime` | bool | true | Controls whether time selection is shown |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[DateTimeField(Label = "Project Start Date")]
public DateTime StartDate { get; set; } = DateTime.Today;
```

### Color & Other

#### ColorField

Renders a color picker.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[ColorField(Label = "Highlight Color")]
public string? HexColor { get; set; }
```

#### PasswordField

Renders a masked password input.

This field integrates with Windows Credential Manager.

Behavior based on property accessor:
- `get; set;`: the Application field is shown and editable in the UI so users can choose a credential application name (useful when one service has multiple base URLs/environments).
- get-only (`get;`): the Application field is hidden and not editable; only Username and Password are shown.

Stored credentials are saved to Windows Credential Manager.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example (editable application name):**
```csharp
[PasswordField(Label = "Service Credentials")]
public string? CredentialApplicationName { get; set; }
```

**Example (fixed application name, hidden in UI):**
```csharp
[PasswordField(Label = "Service Credentials")]
public string CredentialApplicationName { get; } = "MyService-Prod";
```

#### ListField

Renders a list selection input.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `CollectorType` | Type? | null | Custom option collector |
| `CollectorSortOrder` | SortOrder | None | Sort order for collector results |
| `MaxHeight` | int | `int.MinValue` | Max list height in pixels (`int.MinValue` = auto-size) |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[ListField(Label = "Categories", MaxHeight = 240)]
public List<string> Categories { get; set; } = new();
```

#### DictionaryField

Renders a key-value dictionary input.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `CollectorType` | Type? | null | Value suggestion collector |
| `CollectorSortOrder` | SortOrder | None | Sort order for collector results |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[DictionaryField(Label = "Metadata")]
public Dictionary<string, string> Metadata { get; set; } = new();
```

### Revit-Specific Fields

These attributes are in `CW.Assistant.Extensions.Contracts.Fields.Revit`.

#### ElementSelectorField

Selects one or more Revit elements.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Examples:**
```csharp
[ElementSelectorField(
    Label = "Selected Element Id from Revit",
    ToolTip = "The ElementId of the currently selected element in Revit.")]
public ElementId? SelectedElementId { get; set; }

[ElementSelectorField(
    Label = "Selected Element Ids from Revit",
    ToolTip = "The ElementIds of the currently selected elements in Revit.")]
public List<ElementId>? SelectedElementIds { get; set; }
```

#### FilterField

Renders a Revit element filter UI.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `DisableModelSelection` | bool | false | Disables model selection inside the filter UI |
| `UseActiveDocument` | bool | false | Restricts filtering to the active document |
| `DisableCategorySelection` | bool | false | Hides category selection from the UI |
| `Categories` | string[] | [] | Allowed category names; empty means all categories |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Examples:**
```csharp
[FilterField(
    Label = "Filtered Element Collector",
    ToolTip = "Control to define a selection filter for elements in the active Revit document.",
    UseActiveDocument = true)]
public FilteredElementCollector? FilterControl { get; set; }

[FilterField(
    Label = "Filtered Element Collector with Selected Categories",
    ToolTip = "Control to define a selection filter for elements in the active Revit document, restricted to Walls and Doors.",
    UseActiveDocument = true,
    DisableCategorySelection = true,
    Categories = ["Walls", "Doors"])]
public FilteredElementCollector? FilterControlWithSelectedCategories { get; set; }
```

#### ValueCopyField

Renders a value-copy configuration control for Revit workflows.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string | (required) | Field label |
| `Hint` | string | null | Placeholder or suggestion |
| `ToolTip` | string | null | Help text |
| `HelperText` | string | null | Additional guidance below field |
| `CollectorType` | Type? | null | Collector for source/target value options |
| `Visibility` | string? | null | DSL expression to show/hide |
| `IsEnabled` | string? | null | DSL expression to enable/disable |
| `ShowLabel` | bool | true | Controls whether the field label is rendered |
| `Width` | int | -1 | Explicit field width in pixels (`-1` = auto/natural width) |

**Example:**
```csharp
[ValueCopyField(
    Label = "Value Copy from Revit Elements",
    ToolTip = """
    Control for copying values between Revit elements using a custom collector implementation.
    This control uses the Filtered Element Collector field for sources and
    the Filtered Element Collector with Selected Categories as targets.
    """,
    CollectorType = typeof(ValueCopyRevitCollector))]
public ValueCopy? ValueCopy { get; set; }
```

---

## Returning Results

Your `RunAsync()` method must return `Task<IExtensionResult>`. Use the built-in `Result` factory for the common cases.

### Result.Text — result with a message

Use when you want to show a message to the user after execution.

| Factory | Status | Use when |
|---------|--------|----------|
| `Result.Text.Succeeded(message)` | ✅ Succeeded | Work completed successfully |
| `Result.Text.PartiallySucceeded(message)` | ⚠️ Partially succeeded | Work completed but with warnings or partial failures |
| `Result.Text.Failed(message)` | ❌ Failed | Work could not complete |

```csharp
// Full success
return Result.Text.Succeeded($"Exported {count} elements to {args.OutputFolder}");

// Partial success
return Result.Text.PartiallySucceeded($"Exported {ok} of {total} elements. {failed} skipped.");

// Failure
return Result.Text.Failed($"Export failed: output folder does not exist.");
```

### Result.Empty — result without a message

Use when execution status is enough and there is no meaningful message to display.

| Factory | Status |
|---------|--------|
| `Result.Empty.Succeeded()` | ✅ Succeeded |
| `Result.Empty.PartiallySucceeded()` | ⚠️ Partially succeeded |
| `Result.Empty.Failed()` | ❌ Failed |

```csharp
return Result.Empty.Succeeded();
```

### Custom result via IExtensionResult

Implement `IExtensionResult` directly to return structured data or a richer result type.

```csharp
public class MyCustomResult : IExtensionResult
{
    public ExecutionResult Result { get; set; }
    public string? Summary { get; set; }
    public int ProcessedCount { get; set; }

    public string? AsText() => Summary;
}

// In RunAsync:
return new MyCustomResult
{
    Result = ExecutionResult.Succeeded,
    Summary = $"Processed {count} items",
    ProcessedCount = count
};
```

> Prefer `Result.Text.*` for the vast majority of extensions. Only implement `IExtensionResult` when you need to return structured data consumed by a downstream caller.

---

## Validation Attributes

Validation attributes from `System.ComponentModel.DataAnnotations` auto-wire UI validation.

### Common Validators

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[Required]` | Field cannot be empty | `[Required(ErrorMessage = "Name required")]` |
| `[StringLength(max)]` | Max string length | `[StringLength(100, ErrorMessage = "Max 100 chars")]` |
| `[Range(min, max)]` | Value must be in range | `[Range(1, 100)]` |
| `[RegularExpression(pattern)]` | Value must match regex | `[RegularExpression(@"^\d{3}-\d{4}$")]` |
| `[EmailAddress]` | Value must be valid email | `[EmailAddress]` |
| `[Url]` | Value must be valid URL | `[Url]` |
| `[MinLength(n)]` | List/string min length | `[MinLength(1, ErrorMessage = "Select at least one")]` |
| `[MaxLength(n)]` | List/string max length | `[MaxLength(10)]` |

**Example:**
```csharp
[TextField(Label = "Email")]
[Required]
[EmailAddress(ErrorMessage = "Invalid email address")]
public string Email { get; set; } = "";

[IntegerField(Label = "Count")]
[Range(1, 100, ErrorMessage = "Must be 1-100")]
public int Count { get; set; } = 1;
```

---

## Visibility & Enable/Disable DSL

Control when fields appear and whether they're interactive using conditional expressions.

### Visibility Expression

Use `Visibility` attribute to show/hide fields based on other field values.

**Syntax:**
```csharp
[TextField(Label = "Filter", Visibility = "IncludeFilter")]
public string FilterText { get; set; } = "";

[BooleanField(Label = "Include Filter")]
public bool IncludeFilter { get; set; } = false;
```

When `IncludeFilter` is true, `FilterText` is shown. When false, it's hidden.

**Operators:**
- `==` : Equals
- `!=` : Not equals
- `>`, `>=`, `<`, `<=` : Comparisons
- `&&` : AND
- `||` : OR
- `!` : NOT
- `in` : In collection

**Examples:**

Show field if Mode is "Advanced":
```csharp
[Visibility = "Mode == 'Advanced'"]
```

Show if EnableFilter is true AND UseRegex is also true:
```csharp
[Visibility = "EnableFilter && UseRegex"]
```

Show if Count is greater than 5:
```csharp
[Visibility = "Count > 5"]
```

Show if Status is one of several values:
```csharp
[Visibility = "Status in ['Pending', 'Review']"]
```

### Enable/Disable Expression

Use `IsEnabled` to show a field but prevent editing based on conditions.

**Example:**
```csharp
[TextField(Label = "Output File", IsEnabled = "Mode == 'Custom'")]
public string? OutputPath { get; set; }
```

The field is always visible, but grayed out unless `Mode == 'Custom'`.

---

## Grouping & Organization

Organize related fields into stacked or card-based sections using `[StackField]` and `[SectionField]`. Both attributes are applied to a property whose type is a **nested class** — the nested class properties become the grouped child fields.

### StackField

Groups fields in a plain stack with no card or heading. Default orientation is **horizontal**, so child fields appear side-by-side.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string? | null | Optional accessibility label |
| `Hint` | string? | null | Optional placeholder/helper text |
| `ToolTip` | string? | null | Help text on hover |
| `HelperText` | string? | null | Guidance text below the group |
| `ShowLabel` | bool | true | Controls whether the label is rendered |
| `Width` | int | -1 | Explicit container width in pixels (`-1` = auto/natural width) |
| `Orientation` | StackOrientation | **Horizontal** | `Vertical`, `Horizontal`, `Grid`, `HorizontalLastFill`, `HorizontalFirstFill` |
| `Visibility` | string? | null | DSL expression to show/hide the whole group |
| `IsEnabled` | string? | null | DSL expression to enable/disable the whole group |

**StackOrientation values:**

| Value | Layout |
|-------|--------|
| `Horizontal` | Fields arranged horizontally with wrapping (default) |
| `Vertical` | Fields stacked vertically |
| `Grid` | Fields arranged in an evenly distributed column grid |
| `HorizontalLastFill` | Fields horizontal; the **last** field fills remaining width |
| `HorizontalFirstFill` | Fields horizontal; the **first** field fills remaining width |

**Example — side-by-side dimension inputs:**
```csharp
[StackField(
    Label = "Dimensions",
    HelperText = "Width and Height in millimeters.")]
public DimensionsSettings Dimensions { get; } = new();

public class DimensionsSettings
{
    [IntegerField(Label = "Width")]
    public int Width { get; set; } = 100;

    [IntegerField(Label = "Height")]
    public int Height { get; set; } = 100;
}
```

### SectionField

Groups fields in a **card** with icon and optional expand/collapse behavior. Default orientation is **vertical**.

Title/label behavior for `SectionField`:
- `ShowLabel = false` (default): `Label` is shown as the card title.
- `ShowLabel = true`: card title is hidden, and the standard field label is shown instead.
- When no field label is shown, the section uses the full available width.

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Label` | string? | null | Card title when `ShowLabel = false`; field label when `ShowLabel = true` |
| `ShowLabel` | bool | **false** | Controls whether the field label is shown (`true`) or hidden (`false`) |
| `Icon` | IconKind | `IconKind.DoNotUse` | Heading icon — omit (or leave default) for no icon |
| `Orientation` | StackOrientation | **Vertical** | `Vertical`, `Horizontal`, `Grid`, `HorizontalLastFill`, `HorizontalFirstFill` |
| `Width` | int | -1 | Explicit container width in pixels (`-1` = auto/natural width) |
| `Hint` | string? | null | Optional placeholder/helper text |
| `IsExpandable` | bool | **false** | Allow the user to expand/collapse the card |
| `IsExpanded` | bool | **false** | Initial expanded state (only meaningful when `IsExpandable = true`) |
| `ToolTip` | string? | null | Help text on hover |
| `HelperText` | string? | null | Guidance text below the card |
| `Visibility` | string? | null | DSL expression to show/hide the whole section |
| `IsEnabled` | string? | null | DSL expression to enable/disable the whole section |

> `SectionField` always renders as a card. `StackField` never does.

**Example — expandable section with icon:**
```csharp
[SectionField(
    Label = "Advanced Options",
    Icon = IconKind.Tune,
    IsExpandable = true,
    IsExpanded = false,
    HelperText = "Expand to configure retry and execution behavior.")]
public AdvancedSettings Advanced { get; } = new();

public class AdvancedSettings
{
    [BooleanField(Label = "Dry Run Mode")]
    public bool IsDryRun { get; set; } = false;

    [IntegerField(Label = "Max Retry Attempts")]
    public int RetryCount { get; set; } = 3;
}
```

### StackField vs SectionField at a glance

| | StackField | SectionField |
|---|---|---|
| Card / heading | No | Yes (`Label` when `ShowLabel = false`) |
| Default orientation | `Horizontal` | `Vertical` |
| Orientation type | `StackOrientation` | `StackOrientation` |
| Icon support | No | Yes (`Icon = IconKind.X`) |
| Expand/collapse | No | Yes (`IsExpandable`) |
| Best for | Inline / side-by-side inputs | Optional or advanced settings |

### Visibility on groups and sections

Both `StackField` and `SectionField` support `Visibility` and `IsEnabled` to conditionally show/hide or enable/disable the **entire group**.

**Hide a section unless a flag is enabled:**
```csharp
[BooleanField(Label = "Show Advanced Options")]
public bool ShowAdvanced { get; set; } = false;

[SectionField(
    Label = "Advanced",
    Icon = IconKind.Settings,
    Visibility = nameof(ShowAdvanced))]
public AdvancedSettings Advanced { get; } = new();
```

**Cross-group visibility with dotted paths:**

Any `Visibility` or `IsEnabled` expression can reference a nested property using its full dot-notation path from the form root:

```csharp
[SectionField(Label = "Connection")]
public ConnectionSettings Connection { get; } = new();

public class ConnectionSettings
{
    [BooleanField(Label = "Enable Logging")]
    public bool EnableLogging { get; set; } = false;
}

// Visible only when Connection.EnableLogging is true
[TextField(Label = "Log Path", Visibility = "Connection.EnableLogging")]
public string? LogPath { get; set; }
```

Fields inside a group can reference each other using simple property names (no prefix needed within the same group):

```csharp
public class ConnectionSettings
{
    [BooleanField(Label = "Enable Logging")]
    public bool EnableLogging { get; set; } = false;

    [OptionsField(Label = "Log Level", Visibility = nameof(EnableLogging))]
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
}
```

---

## Args Versioning

Evolve your Args class without breaking existing workflows using `[ArgsVersion]` and `IArgsUpgrade<TFrom, TTo>`.

### Version Marker

Mark your Args class with a version:

```csharp
[ArgsVersion(2)]
public class MyArgs
{
    [TextField(Label = "Name")]
    public string Name { get; set; } = "";

    [TextField(Label = "Description")]  // New in v2
    public string Description { get; set; } = "";
}
```

### Upgrade Handler

Implement `IArgsUpgrade<TFrom, TTo>` to migrate from v1 to v2:

```csharp
public class MyArgsUpgrade : IArgsUpgrade<V1Args, V2Args>
{
    public V2Args Upgrade(V1Args from)
    {
        return new V2Args
        {
            Name = from.Name,
            Description = ""  // Default for new field
        };
    }
}
```

The framework chains upgrades automatically when running workflows persisted with older versions.

---

## Common Patterns

See [Cookbook](./COOKBOOK.md) for copy-paste ready patterns for:
- Required field with validation
- Grouped related settings
- Conditional visibility based on checkbox
- Dropdown with auto-fill options
- Readonly display field
- Versioned Args with automatic upgrade
- And more

---

## Rendering & UI Contract

**Field Rendering Pipeline:**

1. **Declaration** → You decorate properties with field attributes (TextField, IntegerField, etc.)
2. **Registration** → Assistant reads your Args class via reflection
3. **Rendering** → CW.Assistant.Forms library converts attributes to UI controls
4. **User Input** → User fills the form; validation runs (DataAnnotations)
5. **Serialization** → User config serialized to workflow file
6. **Hydration** → On workflow run, config deserialized into fresh Args instance
7. **Execution** → ExecuteAsync() receives the hydrated Args

**What You Declare:**
```csharp
[TextField(Label = "Name")]
[Required]
public string Name { get; set; } = "";
```

**What User Sees:**
- Text input box labeled "Name"
- Red error if left empty
- Placeholder or helper text if provided

**What ExecuteAsync() Gets:**
- `args.Name` is populated with user's text
- You can rely on `[Required]` being satisfied (validation already ran)

---

## Troubleshooting

**Q: My field isn't showing up in the UI**
A: Check that:
1. Property is public with get/set
2. Property has a field attribute (`[TextField]`, `[IntegerField]`, etc.)
3. Property is not `[Hidden]` or `Visibility` expression is false
4. No compile errors in your Args class

**Q: Why is my field read-only even though I did not set ReadOnly?**
A: If the property is declared as get-only (for example, `public string Name { get; }`), the rendered field is read-only by design. Add a public setter (`set;`) to make the field editable.

**Q: Validation runs but my custom message doesn't appear**
A: Ensure your `[Required]`, `[Range]`, etc. attributes include `ErrorMessage`:
```csharp
[Range(1, 10, ErrorMessage = "Must be 1-10")]
public int Value { get; set; }
```

**Q: Field is grayed out but I want it enabled**
A: Check `IsEnabled` expression or remove `[ReadOnly]`:
```csharp
[TextField(Label = "Output", IsEnabled = "Mode == 'Custom'")]
```

**Q: How do I reference a nested field in visibility DSL?**
A: Use dotted paths:
```csharp
[Visibility = "Settings.EnableAdvanced"]
public string AdvancedOption { get; set; }
```

---

**Next:** [Cookbook: Common Patterns](./COOKBOOK.md) or [Args Developer Guide](./ARGS_DEVELOPER_GUIDE.md)
