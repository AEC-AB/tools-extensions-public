# Args Developer Guide: Complete Reference

This is the comprehensive technical reference for writing and managing Args classes in Assistant extensions. It covers the complete lifecycle, attribute model, validation, features, rendering, and troubleshooting.

**Quick Navigation:**
- [What is an Args Class?](#what-is-an-args-class)
- [Args Lifecycle](#args-lifecycle)
- [Attribute Model](#attribute-model)
- [Validation](#validation)
- [Rendering Pipeline](#rendering-pipeline)
- [Features](#features) (Groups, Visibility, Enable/Disable, Versioning)
- [Serialization & Persistence](#serialization--persistence)
- [Troubleshooting](#troubleshooting)

---

## What is an Args Class?

An **Args class** (suffix `*Args.cs`) defines the user-configurable input for an extension. It serves as the bridge between:

- **Assistant UI** → User configures properties as form fields
- **Workflow storage** → Configuration is serialized and persisted
- **Your Command** → Hydrated Args instance is passed to `RunAsync(IAssistantExtensionContext context, TArgs args, CancellationToken cancellationToken)`

### Core Responsibilities

1. **Declare UI fields** via decorators (TextField, IntegerField, etc.)
2. **Enforce validation** via DataAnnotations attributes
3. **Define default values** that appear when users first add the extension
4. **Support versioning** for graceful evolution of your configuration

### Example

```csharp
public class ExportArgs
{
    [FolderPickerField(Label = "Output Folder")]
    [Required]
    public string OutputFolder { get; set; } = "";

    [OptionsField(Label = "Format")]
    public ExportFormat Format { get; set; } = ExportFormat.IFC;

    [IntegerField(Label = "Precision")]
    [Range(1, 8)]
    public int Precision { get; set; } = 4;
}
```

Each property becomes a form field. Users configure these in the Assistant UI. When the extension runs, your Command receives the hydrated Args.

---

## Args Lifecycle

Understanding the complete flow helps you write effective Args classes and debug issues.

### 1. Discovery & Registration

Extensions are discovered and activated when:
- **User adds the extension to an action** in the Assistant workflow builder
- **User activates the extension for configuration** in the UI (opening the extension's settings form)
- **The action executes** with this extension as a task

When activated, the framework:

```
Scans extension assemblies for your *Command class (implements IAssistantExtension)
  ↓
Looks for matching *Args class (by explicit type parameter)
  ↓
Reads Args class via reflection
  ↓
Extracts field and validation attributes
  ↓
Builds field definitions for the configuration form
  ↓
Renders the form for user configuration
```

**How the Args type is discovered:** The Command class declares the Args type explicitly as a generic type parameter:

```csharp
public class ExportCommand : IAssistantExtension<ExportArgs>
{
    public IExtensionResult Run(IAssistantExtensionContext context, ExportArgs args, CancellationToken cancellationToken)
    {
        // args is already typed as ExportArgs
    }
}
```

The framework reads the generic `IAssistantExtension<TArgs>` interface, extracts `TArgs`, and uses that to build the configuration form. No naming convention matching is needed.

### 2. UI Rendering

```
Assistant UI loads extension configuration form
  ↓
Reads your Args class via reflection
  ↓
For each public property:
  - Finds field attribute (TextField, IntegerField, etc.)
  - Creates matching UI control (TextBox, Spinner, Dropdown, etc.)
  - Applies validation attributes (Required, Range, etc.)
  - Sets default value from property initializer
  ↓
Renders form with all fields
```

**User sees:** A form with fields, labels, tooltips, and validation

### 3. User Input & Validation

```
User fills form fields
  ↓
User clicks "Run"
  ↓
For each field:
  - Runs DataAnnotations validators
  - Shows error message if validation fails
  ↓
If all valid:
  - Collects field values into a dictionary
  - Workflow proceeds
  ↓
If invalid:
  - Blocks execution
  - Shows error messages to user
```

**Your role:** Declare `[Required]`, `[Range]`, etc. to enforce constraints

### 4. Serialization

```
Workflow is saved by user
  ↓
Extension configuration is serialized
  ↓
For each public property in your Args:
  - Reads current value from form
  - Converts to JSON/storage format
  - Stores in workflow file or database
  ↓
Workflow file saved (includes your Args config)
```

### 5. Workflow Execution

```
User runs a saved workflow
  ↓
Assistant loads workflow definition
  ↓
Deserializes extension configurations
  ↓
For your Args:
  - Loads configuration from storage
  - Checks version marker [ArgsVersion(N)]
  - Applies upgrades if version has changed (IArgsUpgrade)
  - Deserializes into fresh Args instance
  ↓
Args instance is fully hydrated with saved values
```

### 6. Command Execution

```
Assistant calls your IAssistantExtension<TArgs>.RunAsync()
  ↓
Passes IAssistantExtensionContext and hydrated Args instance as parameters
  ↓
Your code:
  - Reads all properties (guaranteed to have values or defaults)
  - Performs work (read models, write files, etc.)
  - Returns Result.Text.Succeeded(...) or Result.Text.Failed(...)
  ↓
Result is logged and shown to user
```

**Example:**
```csharp
public async Task<IExtensionResult> RunAsync(
    IAssistantExtensionContext context,
    ExportArgs args,
    CancellationToken cancellationToken)
{
    var folder = args.OutputFolder;  // Has user's value or default
    var format = args.Format;  // Has user's selection
    // ... perform work ...
    return Result.Text.Succeeded("Exported successfully");
}
```

### Returning Results

Return an `IExtensionResult` from `RunAsync()`. The built-in `Result` factory covers most cases:

**`Result.Text.*` — result with a message shown to the user:**
```csharp
return Result.Text.Succeeded("Exported 42 elements.");
return Result.Text.PartiallySucceeded("Exported 40 of 42. 2 elements skipped.");
return Result.Text.Failed("Output folder not found.");
```

**`Result.Empty.*` — result without a message (status only):**
```csharp
return Result.Empty.Succeeded();
return Result.Empty.PartiallySucceeded();
return Result.Empty.Failed();
```

**Custom result via `IExtensionResult`** — implement the interface when you need to return structured data:
```csharp
public class MyResult : IExtensionResult
{
    public ExecutionResult Result { get; set; }
    public int ProcessedCount { get; set; }
    public string? AsText() => $"Processed {ProcessedCount} items";
}
```

See [Reference: Returning Results](./REFERENCE.md#returning-results) for the full table.

---

## Attribute Model

Your Args class uses two types of decorators:

### 1. Field Attributes (UI Declaration)

These tell Assistant how to render each property as a form field.

**All field attributes:**
- `TextField` — Text input (single or multi-line)
- `UrlField` — URL input (TextField variant)
- `IntegerField` — Integer input
- `DoubleField` — Floating-point input
- `BooleanField` — Checkbox/toggle
- `OptionsField` — Dropdown list
- `ChoiceField` — Radio buttons
- `FilePickerField` — File open dialog
- `FolderPickerField` — Folder open dialog
- `SaveFileField` — File save dialog
- `DateTimeField` — Date/time picker
- `ColorField` — Color picker
- `PasswordField` — Masked password input
- `ListField` — List/collection input
- `DictionaryField` — Key-value pairs

**Revit-specific field attributes:**
- `ElementSelectorField` — Revit element selection UI
- `FilterField` — Revit element filter UI
- `ValueCopyField` — Revit value-copy configuration UI

**Common properties across all field attributes:**

| Property | Type | Purpose |
|----------|------|---------|
| `Label` | string | Field label displayed to user |
| `Hint` | string? | Placeholder or suggestion text |
| `ToolTip` | string? | Help text on hover |
| `HelperText` | string? | Additional guidance below field |
| `Visibility` | string? | DSL expression to show/hide |
| `IsEnabled` | string? | DSL expression to enable/disable |
| `ShowLabel` | bool | Controls whether the label is rendered (default: true) |
| `Width` | int | Explicit width in pixels (`-1` uses natural width) |

**Example:**
```csharp
[TextField(
    Label = "Project Name",
    Hint = "e.g., Project Alpha",
    ToolTip = "Used in output filenames",
    HelperText = "Must be unique within your workspace")]
[Required]
public string ProjectName { get; set; } = "";
```

See [Reference](./REFERENCE.md) for detailed properties of each field type.

### CollectorType: Optional vs Required

Several field attributes implement `ICollectorTypeAttribute` and support `CollectorType`.

Use this rule of thumb:

- `TextField`: `CollectorType` is optional. It only provides suggestions; user input is still free text.
- `OptionsField` and `ChoiceField`: `CollectorType` is required when the property type is not enum.
- `OptionsField` and `ChoiceField`: `CollectorType` is optional when the property type is enum because enum values are used automatically.
- `ListField` and `DictionaryField`: `CollectorType` is optional and provides suggested values.
- Revit `ValueCopyField`: `CollectorType` must be provided.

Collector interface must match your platform context:

- Assistant extensions use `IAsyncAutoFillCollector<TArgs>`
- Revit extensions use `IRevitAutoFillCollector<TArgs>`

See [Reference: CollectorType Rules (ICollectorTypeAttribute)](./REFERENCE.md#collectortype-rules-icollectortypeattribute) for full examples.

### 2. Validation Attributes (Constraint Declaration)

These declare validation rules that run before `RunAsync()` is called.

**From `System.ComponentModel.DataAnnotations`:**

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[Required]` | Field cannot be empty | `[Required(ErrorMessage = "Name required")]` |
| `[Range(min, max)]` | Value in range | `[Range(1, 100)]` |
| `[StringLength(max)]` | Max string length | `[StringLength(100)]` |
| `[MinLength(n)]` | Min collection length | `[MinLength(1)]` |
| `[MaxLength(n)]` | Max collection length | `[MaxLength(10)]` |
| `[RegularExpression(pattern)]` | Regex match | `[RegularExpression(@"^\d{3}$")]` |
| `[EmailAddress]` | Valid email | `[EmailAddress]` |
| `[Url]` | Valid URL | `[Url]` |

**Example:**
```csharp
[IntegerField(Label = "Count")]
[Range(1, 100, ErrorMessage = "Must be 1-100")]
public int Count { get; set; } = 1;

[TextField(Label = "Email")]
[EmailAddress(ErrorMessage = "Invalid email")]
public string Email { get; set; } = "";
```

---

## Validation

Validation runs **before** your Command executes. It prevents invalid data from reaching your code.

### Validation Lifecycle

```
User fills form
  ↓
User clicks "Run"
  ↓
For each property in Args:
  - Gets field value from form
  - Applies all DataAnnotations validators
  - Checks [Required], [Range], [EmailAddress], etc.
  ↓
If all pass:
  - Args is considered valid
  - RunAsync() is called
  ↓
If any fail:
  - Validation error message is shown in UI
  - RunAsync() is NOT called
  - User must fix and retry
```

### What You Declare

```csharp
[TextField(Label = "Email")]
[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Invalid email format")]
public string Email { get; set; } = "";
```

### What User Experiences

1. **Field empty, user tries to run** → Error: "Email is required"
2. **Field has "not-an-email", user tries to run** → Error: "Invalid email format"
3. **Field has "user@example.com", user runs** → Validation passes, RunAsync() is called

### Custom Validation

For complex validation beyond DataAnnotations, perform checks in your Command:

```csharp
public async Task<IExtensionResult> RunAsync(
    IAssistantExtensionContext context,
    MyArgs args,
    CancellationToken cancellationToken)
{
    // DataAnnotations validators already passed
    // Perform additional business logic validation
    if (args.StartDate > args.EndDate)
        return Result.Text.Failed("Start date must be before end date");

    // All validation passed, proceed
    // ...
}
```

---

## Rendering Pipeline

The rendering pipeline controls how your Args class becomes a form that users interact with.

### Step 1: Attribute Reading

```
Assistant reads Args class type
  ↓
For each public property:
  - Finds decorators (TextField, Required, etc.)
  - Extracts metadata (Label, Range, Visibility, etc.)
  - Builds "FieldDefinition" object
```

**Example:**
```csharp
[TextField(Label = "Name", Hint = "Your full name")]
[StringLength(100)]
public string Name { get; set; } = "";
```

**FieldDefinition extracted:**
```json
{
  "PropertyName": "Name",
  "FieldType": "TextField",
  "Label": "Name",
  "Hint": "Your full name",
  "Validators": [
    { "Type": "StringLength", "Max": 100 }
  ],
  "DefaultValue": "",
  "PropertyPath": "Name"
}
```

### Step 2: CW.Assistant.Forms Rendering

```
For each FieldDefinition:
  - CW.Assistant.Forms library is invoked
  - FieldType (TextField, IntegerField, etc.) determines control type
  - Control is populated with Label, Hint, DefaultValue
  - Validators are registered for the control
  - Control is added to form
```

**You declare:**
```csharp
[TextField(Label = "Name")]
```

**User sees:**
- A TextBox control
- Label "Name" above it
- Maybe a tooltip or helper text

### Step 3: Visibility & Enable Evaluation

```
After form is rendered:
  - For each field with Visibility or IsEnabled expression
  - Expression evaluator reads current form state
  - Visibility expression: true → field shown; false → hidden
  - IsEnabled expression: true → editable; false → grayed out
```

**You declare:**
```csharp
[Visibility = "IncludeAdvanced"]
[TextField(Label = "Advanced Option")]
public string AdvancedOption { get; set; } = "";
```

**User sees:**
- Field hidden initially
- When user checks "IncludeAdvanced", field appears

### Step 4: User Interaction

```
User interacts with form
  ↓
Each change updates form model
  ↓
Visibility/Enable expressions re-evaluated
  ↓
Fields shown/hidden/enabled/disabled in real-time
```

### Step 5: Value Collection & Validation

```
User clicks "Run"
  ↓
Form collects all field values into dictionary
  ↓
For each field:
  - Gets value from control
  - Runs validators (Required, Range, etc.)
  - If invalid: shows error and stops
  - If valid: adds to dictionary
  ↓
All values collected in dictionary
```

### Step 6: Deserialization

```
For each property in Args:
  - Looks up value in dictionary
  - Type-converts (e.g., string "5" to int 5)
  - Sets property on new Args instance
  ↓
Args instance is now fully hydrated with user's values
```

### Design Principle: Contract Only

**You own:** What you declare (field attributes, validation)  
**CW.Assistant.Forms owns:** How it renders  
**Contract:** You declare, Forms renders and validates

You do not need to know or care how TextField renders internally. You declare the field type, properties, and constraints. Forms handles the rest.

---

## Features

Advanced Args capabilities for complex extensions.

### Feature 1: Groups & Organization

Organize related fields into **stacks** or **sections**. Both work by decorating a property whose type is a nested class — the nested class properties become the grouped child fields.

#### StackField — plain grouping

`StackField` groups fields without a card or heading. Default orientation is **horizontal**, so child fields appear side-by-side. The `StackOrientation` enum offers five layout modes: `Horizontal`, `Vertical`, `Grid`, `HorizontalLastFill`, and `HorizontalFirstFill`.

**Use it for:** dimension pairs, related numeric inputs, inline toggles.

```csharp
[StackField(
    Label = "Connection Basics",
    HelperText = "Required baseline inputs.")]
public ConnectionBasicsSettings ConnectionBasics { get; } = new();

public class ConnectionBasicsSettings
{
    [TextField(Label = "Project Code")]
    public string ProjectCode { get; set; } = "PROJECT-001";

    [IntegerField(Label = "Request Timeout (seconds)")]
    public int TimeoutSeconds { get; set; } = 30;

    [BooleanField(Label = "Enable Diagnostic Logging")]
    public bool EnableLogging { get; set; } = true;
}
```

**Result:** The three fields appear side-by-side (horizontal by default).

---

#### SectionField — card-based grouping

`SectionField` groups fields in a **card** with icon support. Default orientation is **vertical**.

Title/label behavior:
- `ShowLabel = false` (default): `Label` is shown as the card title.
- `ShowLabel = true`: card title is hidden and the standard field label is shown instead.
- When no field label is shown, the section uses the full available width.

**Use it for:** optional or advanced configuration you want to visually separate from the main form.

```csharp
[SectionField(
  Label = "Service Endpoint",
    Icon = IconKind.Web,
    HelperText = "Host and transport settings.")]
public EndpointConfigurationSettings EndpointConfiguration { get; } = new();

public class EndpointConfigurationSettings
{
    [UrlField(Label = "Service Base URL", Hint = "https://api.example.com")]
    public string ServiceBaseUrl { get; set; } = "https://api.example.com";

    [IntegerField(Label = "Service Port")]
    public int ServicePort { get; set; } = 443;

    [BooleanField(Label = "Use Secure Connection (SSL/TLS)")]
    public bool UseSsl { get; set; } = true;
}
```

**Result:** A card titled "Service Endpoint" with a globe icon, containing three vertically stacked fields.

---

#### Expandable sections

Set `IsExpandable = true` on a `SectionField` to let users collapse or expand the card. Use `IsExpanded = false` to start it collapsed (both default to `false`).

```csharp
[SectionField(
  Label = "Advanced Options",
    Icon = IconKind.Tune,
    IsExpandable = true,
    IsExpanded = false)]
public AdvancedReliabilitySettings AdvancedReliability { get; } = new();

public class AdvancedReliabilitySettings
{
    [IntegerField(Label = "Max Retry Attempts")]
    public int RetryCount { get; set; } = 3;

    [BooleanField(Label = "Dry Run Mode")]
    public bool IsDryRun { get; set; } = false;
}
```

**Result:** An "Advanced Options" card that starts collapsed. Users expand it only when they need it.

---

#### Visibility on sections

Apply `Visibility` on `StackField` or `SectionField` to show/hide the **entire group** based on a condition:

```csharp
[BooleanField(Label = "Show Advanced Options")]
public bool ShowAdvanced { get; set; } = false;

[SectionField(
  Label = "Advanced",
    Icon = IconKind.Settings,
    Visibility = nameof(ShowAdvanced))]
public AdvancedSettings Advanced { get; } = new();
```

---

#### Cross-group visibility

Fields outside a group can reference properties inside it using **dotted property paths**. Fields inside a group use plain property names to reference each other.

```csharp
[SectionField(Label = "Basic Config")]
public BasicConfiguration BasicConfig { get; } = new();

public class BasicConfiguration
{
    [BooleanField(Label = "Enable Logging")]
    public bool EnableLogging { get; set; } = false;

    // Inside the group: plain name reference
    [OptionsField(Label = "Log Level", Visibility = nameof(EnableLogging))]
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
}

// Outside the group: dotted path reference
[TextField(Label = "Log Path", Visibility = "BasicConfig.EnableLogging")]
public string? LogPath { get; set; }
```

---

#### StackField vs SectionField summary

| | StackField | SectionField |
|---|---|---|
| Card / heading | No | Yes (`Label` when `ShowLabel = false`) |
| Default orientation | `Horizontal` | `Vertical` |
| Orientation type | `StackOrientation` | `StackOrientation` |
| Icon support | No | Yes (`Icon = IconKind.X`) |
| Expand/collapse | No | Yes (`IsExpandable`) |
| Best for | Inline / side-by-side inputs | Optional or advanced settings |

---

### Feature 2: Visibility Conditions

Show/hide fields dynamically based on other field values using DSL expressions.

#### Syntax

Visibility DSL supports:
- **Comparisons:** `==`, `!=`, `>`, `>=`, `<`, `<=`
- **Logical:** `&&`, `||`, `!`
- **Collection:** `in`
- **Property access:** Simple names or dotted paths

#### Examples

**Simple equality:**
```csharp
[Visibility = "Mode == 'Advanced'"]
public string AdvancedOption { get; set; } = "";
```

**AND condition:**
```csharp
[Visibility = "EnableFilter && UseRegex"]
public string RegexPattern { get; set; } = "";
```

**Numeric comparison:**
```csharp
[Visibility = "Count > 5"]
public string LargeDataOption { get; set; } = "";
```

**NOT:**
```csharp
[Visibility = "!DisableAdvanced"]
public string AdvancedSetting { get; set; } = "";
```

**Collection membership:**
```csharp
[Visibility = "Format in ['IFC', 'DWG']"]
public string ExportPath { get; set; } = "";
```

**Nested property:**
```csharp
[Visibility = "Settings.EnableAdvanced"]
public string NestedOption { get; set; } = "";
```

#### Important: Property Paths

The property name in the DSL expression must match your C# property name:

```csharp
public bool IncludeArchive { get; set; }  // Property name

[Visibility = "IncludeArchive"]  // Use the property name
public string ArchiveLocation { get; set; }
```

**Nested paths:**
```csharp
[SectionField(Label = "Settings")]
public AdvancedSettings Settings { get; set; } = new();

public class AdvancedSettings
{
    [BooleanField(Label = "Enable")]
    public bool Enable { get; set; }
}

// Later, in a parent property:
[Visibility = "Settings.Enable"]  // Dotted path to nested property
public string EnabledOption { get; set; }
```

---

### Feature 3: Enable/Disable Conditions

Show a field but prevent editing based on conditions.

```csharp
[BooleanField(Label = "Use Custom Settings")]
public bool UseCustom { get; set; } = false;

[TextField(
    Label = "Custom Value",
    IsEnabled = "UseCustom")]  // Grayed out unless UseCustom is true
public string CustomValue { get; set; } = "";
```

**User experience:**
- "Custom Value" field is always visible
- But grayed out (non-interactive) unless "Use Custom Settings" is checked
- When checked, field becomes editable

**Difference from Visibility:**
- `Visibility = "condition"` → Field is completely hidden/shown
- `IsEnabled = "condition"` → Field is always visible but editable only under condition

---

### Feature 4: Versioning & Upgrades

Evolve your Args class over time without breaking existing workflows.

#### Mark Your Args Version

```csharp
[ArgsVersion(2)]  // Current version is 2
public class ExportArgs
{
    [TextField(Label = "Output Folder")]
    public string OutputFolder { get; set; } = "";

    [TextField(Label = "Description")]  // New in v2
    public string Description { get; set; } = "";
}
```

If you don't specify `[ArgsVersion]`, version defaults to 1.

#### Implement Upgrade Handler

```csharp
// Define v1 for reference (old version)
public class ExportArgsV1
{
    public string OutputFolder { get; set; } = "";
}

// Upgrade from v1 to v2
public class ExportArgsUpgradeV1ToV2 : IArgsUpgrade<ExportArgsV1, ExportArgs>
{
    public ExportArgs Upgrade(ExportArgsV1 from)
    {
        return new ExportArgs
        {
            OutputFolder = from.OutputFolder,
            Description = ""  // Default for new v2 field
        };
    }
}
```

#### What Happens When Workflow Runs

1. User saves a workflow with your extension (v2 args)
2. Version marker `__argsVersion = "2"` is stored alongside the config
3. Time passes, user upgrades your extension
4. User runs a workflow that was saved when v1 was current
5. Framework detects `__argsVersion = "1"`
6. Framework finds `IArgsUpgrade<V1, V2>` implementation
7. Framework calls `Upgrade()` to convert V1 config to V2
8. V2 Args instance is hydrated and passed to your Command
9. User doesn't lose data; new fields get defaults

#### Multi-Step Upgrades

If upgrading v1 → v2 → v3, implement both upgrades:

```csharp
public class UpgradeV1ToV2 : IArgsUpgrade<V1, V2> { ... }
public class UpgradeV2ToV3 : IArgsUpgrade<V2, V3> { ... }
```

Framework chains them automatically: V1 → (Upgrade) → V2 → (Upgrade) → V3

#### Reserved Keys

The version is stored under a reserved key in the serialization dictionary:
- Key: `__argsVersion`
- Value: `"2"` (string representation of version number)

This key is skipped when serializing your properties. It's handled automatically; you don't need to declare it.

---

## Serialization & Persistence

How your Args class is stored and restored.

### Serialization Format

Args are serialized into a **property dictionary** (key → value pairs):

```csharp
public class ExportArgs
{
    public string OutputFolder { get; set; } = "C:\\Output";
    public int CompressionLevel { get; set; } = 5;
}
```

**Serialized to:**
```json
{
  "OutputFolder": "C:\\Output",
  "CompressionLevel": "5",
  "__argsVersion": "1"
}
```

### Property Name Mapping

Property names in C# map directly to dictionary keys:

```csharp
public string ProjectName { get; set; }  // C# property name
// → serialized as "ProjectName" key in dictionary
```

**Important:** If you rename a property, old workflows will fail to deserialize.

### Handling Renames

To rename a property without breaking old workflows, use versioning:

```csharp
// v1: public string ProjectName

// v2: Renamed to Project
[ArgsVersion(2)]
public class ExportArgs
{
    public string Project { get; set; }  // Renamed
}

// Upgrade handler
public class UpgradeV1ToV2 : IArgsUpgrade<V1Args, V2Args>
{
    public V2Args Upgrade(V1Args from)
    {
        return new V2Args
        {
            Project = from.ProjectName  // Map old name to new name
        };
    }
}
```

### Type Conversion

Types are converted automatically when deserializing:

```csharp
[IntegerField(Label = "Count")]
public int Count { get; set; }

// Stored as: { "Count": "5" } (string in storage)
// Deserialized to: 5 (int in Args instance)
```

---

## Troubleshooting

### Issue: Field Not Appearing in UI

**Possible causes:**
1. Property is not public
2. Property has no field attribute
3. Property is `[Hidden]`
4. Visibility expression is false
5. Compile error in Args class

**Fix:**
```csharp
// ❌ Private—won't appear
private string Name { get; set; }

// ✅ Public with field attribute
[TextField(Label = "Name")]
public string Name { get; set; } = "";
```

Check your IDE for compile errors. The Args class must compile cleanly.

### Issue: Field Is Read-Only Unexpectedly

**Cause:** The property is declared as get-only (for example, `public string Name { get; }`).

**Behavior:** Get-only properties are rendered as read-only fields by design.

**Fix:** Add a public setter to make the field editable:
`public string Name { get; set; }`

### Issue: Validation Not Running

**Possible causes:**
1. Missing validation attribute
2. Typo in error message parameter
3. Custom validation not in Command

**Fix:**
```csharp
// ❌ Required not declared
[TextField(Label = "Name")]
public string Name { get; set; } = "";

// ✅ Required declared with error message
[TextField(Label = "Name")]
[Required(ErrorMessage = "Name is required")]
public string Name { get; set; } = "";
```

### Issue: Custom Error Message Not Showing

**Possible causes:**
1. Missing ErrorMessage parameter
2. Typo in attribute name

**Fix:**
```csharp
// ❌ Error message not specified
[Range(1, 100)]
public int Value { get; set; }

// ✅ Error message provided
[Range(1, 100, ErrorMessage = "Must be 1-100")]
public int Value { get; set; }
```

### Issue: Visibility DSL Not Working

**Possible causes:**
1. Property name in DSL doesn't match C# property name (case-sensitive)
2. Operator typo (`=` instead of `==`)
3. String literal syntax wrong (use single quotes in DSL)

**Fix:**
```csharp
public bool IncludeArchive { get; set; }

// ❌ Wrong: "includeArchive" (wrong case)
[Visibility = "includeArchive"]

// ✅ Correct: "IncludeArchive" (exact name)
[Visibility = "IncludeArchive"]

// ✅ With operator
[Visibility = "Format == 'IFC'"]  // Single quotes for strings
```

### Issue: Deserialization Fails After Property Rename

**Cause:** Old workflows have old property names in persisted data.

**Solution:** Use versioning and upgrade handler:

```csharp
[ArgsVersion(2)]
public class MyArgs
{
    public string NewPropertyName { get; set; }  // Renamed from OldName
}

public class MyArgsUpgrade : IArgsUpgrade<V1MyArgs, MyArgs>
{
    public MyArgs Upgrade(V1MyArgs from)
    {
        return new MyArgs
        {
            NewPropertyName = from.OldPropertyName
        };
    }
}
```

### Issue: Complex Validation Logic

**Cause:** Need validation beyond DataAnnotations attributes.

**Solution:** Validate in your Command:

```csharp
public async Task<IExtensionResult> RunAsync(
    IAssistantExtensionContext context,
    MyArgs args,
    CancellationToken cancellationToken)
{
    // Basic validators (Required, Range, etc.) already passed

    // Complex business logic validation
    if (args.StartDate > args.EndDate)
        return Result.Text.Failed("Start date must be before end date");

    if (args.Values.Count < 1)
        return Result.Text.Failed("At least one value required");

    // Validation passed
    // ... proceed with execution ...
}
```

---

## Best Practices

1. **Declare meaningful defaults** — Properties initialize with defaults shown in UI
2. **Use validation attributes** — Declare constraints with DataAnnotations
3. **Provide helpful tooltips** — ToolTip property explains what each field does
4. **Use nested classes for groups** — SectionField/StackField organize related properties
5. **Consider visibility for complex extensions** — Hide advanced options initially
6. **Plan for versioning** — Mark version from the start; implement upgrade handlers before property renames
7. **Keep Args serializable** — Use simple types (string, int, bool, List, enums)
8. **Test with actual workflows** — Verify serialization/deserialization with real saved workflows
9. **Document field purpose** — Label, Hint, and ToolTip should be clear to end users
10. **Reference the cookbook** — Start with patterns from [Cookbook](./COOKBOOK.md)

---

## See Also

- [Quick Start](./QUICK_START.md) — 5-10 minute hands-on intro
- [Cookbook](./COOKBOOK.md) — Copy-paste ready patterns
- [Reference](./REFERENCE.md) — Attribute and validator catalog
- [Platform Guides](./PLATFORM_GUIDES/) — Platform-specific details

---

**Next:** See a pattern in the [Cookbook](./COOKBOOK.md) that matches your use case, or check a [Platform Guide](./PLATFORM_GUIDES/) if working with Revit, AutoCAD, Tekla, or Navisworks.
