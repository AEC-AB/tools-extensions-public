# Cookbook: Common Extension Patterns

Ready-to-use code snippets and patterns for building extensions. Each pattern is self-contained and points to the detailed reference.

---

## 1. Simple Text Input with Required Validation

**Use case:** Collect user input that cannot be empty.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace MyExtension;

public class MyArgs
{
    [TextField(
        Label = "Project Name",
        Hint = "Enter project name",
        ToolTip = "This will be used in the output filename")]
    [Required(ErrorMessage = "Project name is required")]
    public string ProjectName { get; set; } = "";
}
```

**What happens:**
- User sees a text input labeled "Project Name"
- If left empty and user tries to run, validation blocks execution with error message
- When valid, `args.ProjectName` contains the text

**Reference:** [TextField](./REFERENCE.md#textfield), [Required validator](./REFERENCE.md#common-validators)

---

## 2. Number Input with Range Validation

**Use case:** Collect a number that must be within a specific range.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace MyExtension;

public class MyArgs
{
    [IntegerField(
        Label = "Offset Distance",
        Hint = "1-100 mm",
        ToolTip = "Distance to offset the geometry")]
    [Range(1, 100, ErrorMessage = "Must be between 1 and 100 mm")]
    public int OffsetDistance { get; set; } = 10;
}
```

**What happens:**
- User sees a number spinner with default value 10
- User can only enter/select 1-100
- If they try to type a larger number, validation rejects it

**Reference:** [IntegerField](./REFERENCE.md#integerfield), [Range validator](./REFERENCE.md#common-validators)

---

## 3. Boolean Flag to Enable/Disable Features

**Use case:** Let user toggle optional features on/off.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [BooleanField(Label = "Include Archive Items")]
    public bool IncludeArchived { get; set; } = false;

    [BooleanField(Label = "Skip Validation")]
    public bool SkipValidation { get; set; } = false;
}
```

**Command usage:**
```csharp
if (args.IncludeArchived)
{
    // Include archived items in processing
}
if (args.SkipValidation)
{
    // Skip validation checks
}
```

**Reference:** [BooleanField](./REFERENCE.md#booleanfield)

---

## 4. Dropdown Selection from Enum

**Use case:** Let user choose from predefined options.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [OptionsField(Label = "Document Type")]
    public DocumentType Type { get; set; } = DocumentType.Architectural;
}

public enum DocumentType
{
    Architectural,
    Structural,
    MEP,
    Coordination
}
```

If you decorate enum members with `[Description("...")]`, the description is shown in the UI while the enum member name is kept as the key value.

**Command usage:**
```csharp
switch (args.Type)
{
    case DocumentType.Architectural:
        // Handle architectural
        break;
    case DocumentType.Structural:
        // Handle structural
        break;
    // ...
}
```

**Reference:** [OptionsField](./REFERENCE.md#optionsfield)

---

## 5. File Picker (Single File)

**Use case:** Let user select a file from disk.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [FilePickerField(
        Label = "Select Template",
        Hint = "Choose an Excel template",
        FileExtensions = ["xlsx", "xls", "*"])]
    public string? TemplateFile { get; set; }
}
```

**Command usage:**
```csharp
if (string.IsNullOrEmpty(args.TemplateFile))
    return Result.Text.Failed("Template file is required");

var content = File.ReadAllText(args.TemplateFile);
// Process the file
```

**Reference:** [FilePickerField](./REFERENCE.md#filepickerfield)

---

## 6. Multiple File Selection

**Use case:** Let user select multiple files.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.Collections.Generic;

namespace MyExtension;

public class MyArgs
{
    [FilePickerField(
        Label = "Select Files",
        Hint = "Choose JSON files to process",
        FileExtensions = ["json", "*"])]
    public List<string> SelectedFiles { get; set; } = new();
}
```

**Command usage:**
```csharp
foreach (var file in args.SelectedFiles)
{
    var data = JsonSerializer.Deserialize(File.ReadAllText(file));
    // Process each file
}
```

**Reference:** [FilePickerField](./REFERENCE.md#filepickerfield)

---

## 7. Folder Selection

**Use case:** Let user select an output directory.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace MyExtension;

public class MyArgs
{
    [FolderPickerField(Label = "Output Directory")]
    [Required(ErrorMessage = "Output directory is required")]
    public string? OutputFolder { get; set; }
}
```

**Command usage:**
```csharp
var outputPath = Path.Combine(args.OutputFolder!, "result.json");
File.WriteAllText(outputPath, jsonContent);
```

**Reference:** [FolderPickerField](./REFERENCE.md#folderpickerfield)

---

## 8. Conditional Field Visibility

**Use case:** Show/hide fields based on user selections.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [ChoiceField(Label = "Export Format")]
    public ExportFormat Format { get; set; } = ExportFormat.IFC;

    [TextField(
        Label = "IFC Version",
        Hint = "e.g., IFC2x3, IFC4",
        Visibility = "Format == 'IFC'")]  // Only show if Format == IFC
    public string? IfcVersion { get; set; }

    [TextField(
        Label = "DWG Version",
        Hint = "e.g., 2020, 2021",
        Visibility = "Format == 'DWG'")]  // Only show if Format == DWG
    public string? DwgVersion { get; set; }
}

public enum ExportFormat { IFC, DWG, PDF }
```

**What happens:**
- User selects export format
- Fields for IFC version/DWG version appear/disappear based on selection
- Only relevant fields are shown

**Reference:** [Visibility expressions](./REFERENCE.md#visibility--enabledisable-dsl)

---

## 9. Conditional Enable/Disable

**Use case:** Show a field but allow editing only under certain conditions.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [BooleanField(Label = "Use Custom Settings")]
    public bool UseCustom { get; set; } = false;

    [TextField(
        Label = "Custom Output Path",
        IsEnabled = "UseCustom")]  // Grayed out unless UseCustom is true
    public string? CustomPath { get; set; }
}
```

**What happens:**
- "Custom Output Path" field is always visible
- Grayed out unless "Use Custom Settings" is checked
- Users cannot edit until they enable it

**Reference:** [Enable/Disable expressions](./REFERENCE.md#enabledisable-expression)

---

## 10. Grouped Related Fields

**Use case:** Organize related settings into a collapsible group.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [TextField(Label = "Project Name")]
    public string ProjectName { get; set; } = "";

    [SectionField(
        Label = "Export Options",
        IsExpandable = true,
        IsExpanded = false)]  // Collapsed by default
    public ExportSettings Export { get; set; } = new();
}

public class ExportSettings
{
    [OptionsField(Label = "Format")]
    public ExportFormat Format { get; set; } = ExportFormat.IFC;

    [BooleanField(Label = "Include Metadata")]
    public bool IncludeMetadata { get; set; } = true;

    [IntegerField(Label = "Compression Level")]
    [Range(0, 9)]
    public int CompressionLevel { get; set; } = 5;
}

public enum ExportFormat { IFC, DWG }
```

**What happens:**
- "Project Name" appears at the top
- "Export Options" appears as a collapsible card, initially collapsed
- User clicks to expand and sees Format, Metadata, and Compression fields

**Reference:** [SectionField](./REFERENCE.md#sectionfield)

---

## 11. Readonly Display Field

**Use case:** Show information to the user that they cannot edit.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [TextField(
        Label = "Extension Version",
        ToolTip = "Current version of this extension")]
    public string Version { get; } = "1.2.0";  // No setter = readonly

    [TextField(Label = "Project Name")]
    public string ProjectName { get; set; } = "";
}
```

**Or explicitly:**
```csharp
[TextField(Label = "Timestamp")]
public string Timestamp => DateTime.Now.ToString("O");
```

**What happens:**
- Version field renders as text but cannot be edited
- Useful for displaying computed values or metadata

**Reference:** [IsReadOnly property](./REFERENCE.md#textfield)

---

## 12. Versioned Args with Automatic Upgrade

**Use case:** Evolve your Args class without breaking existing workflows.

**Original Args (v1):**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgsV1  // Version 1 (default)
{
    [TextField(Label = "Project Name")]
    public string ProjectName { get; set; } = "";

    [FolderPickerField(Label = "Output Directory")]
    public string? OutputFolder { get; set; }

    [TextField(Label = "Retry Count")]
    public string RetryCountText { get; set; } = "3";
}
```

**Updated Args (v2):**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts;
using System;

namespace MyExtension;

[ArgsVersion(2)]  // Mark as version 2
public class MyArgs
{
    [TextField(Label = "Project Name")]
    public string ProjectName { get; set; } = "";

    [SectionField(Label = "Execution")]
    public ExecutionSettings Execution { get; set; } = new();
}

public class ExecutionSettings
{
    [FolderPickerField(Label = "Output Directory")]
    public string? OutputFolder { get; set; }

    [IntegerField(Label = "Retry Count")]
    public int RetryCount { get; set; } = 3;

    [BooleanField(Label = "Dry Run")]
    public bool DryRun { get; set; }
}

// Upgrade handler
public class MyArgsUpgradeV1ToV2 : IArgsUpgrade<MyArgsV1, MyArgs>
{
    public MyArgs Upgrade(MyArgsV1 from)
    {
        int retryCount;
        if (!int.TryParse(from.RetryCountText, out retryCount) || retryCount < 0)
        {
            retryCount = 3;
        }

        return new MyArgs
        {
            ProjectName = from.ProjectName,
            Execution = new ExecutionSettings
            {
                OutputFolder = from.OutputFolder,
                RetryCount = retryCount,
                DryRun = false
            }
        };
    }
}
```

**What happens:**
- Old workflows saved with v1 Args load automatically
- Framework finds `MyArgsUpgradeV1ToV2` and runs it, migrating to v2
- Flat v1 fields are moved into the new `Execution` section in v2
- `RetryCountText` is transformed from text to integer with a safe fallback
- Existing values are preserved and the new `DryRun` field gets a default

**Reference:** [Args Versioning](./REFERENCE.md#args-versioning)

---

## 13. Multiline Text Input

**Use case:** Collect multi-line text (notes, descriptions, scripts).

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [TextField(
        Label = "Description",
        IsMultiline = true,
        MinLines = 3,
        MaxLines = 10,
        ToolTip = "Enter detailed description")]
    public string Description { get; set; } = "";
}
```

**What happens:**
- Renders as a textarea, not a single-line input
- Starts with 3 lines visible; can expand up to 10 lines
- Suitable for longer text entries

**Reference:** [TextField IsMultiline](./REFERENCE.md#textfield)

---

## 14. Email Validation

**Use case:** Ensure user enters a valid email address.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace MyExtension;

public class MyArgs
{
    [TextField(Label = "Contact Email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = "";
}
```

**What happens:**
- TextField validates that input is a valid email
- Shows error "Enter a valid email address" if invalid

**Reference:** [EmailAddress validator](./REFERENCE.md#common-validators)

---

## 15. Optional Field with Nullable Type

**Use case:** Make a field optional (user can leave it empty).

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [TextField(Label = "Required Field")]
    [Required]
    public string RequiredField { get; set; } = "";

    [TextField(Label = "Optional Notes")]
    public string? OptionalNotes { get; set; }  // Can be null
}
```

**Command usage:**
```csharp
if (!string.IsNullOrEmpty(args.OptionalNotes))
{
    // Use optional field if provided
    ProcessNotes(args.OptionalNotes);
}
```

**What happens:**
- "Optional Notes" field can be left empty
- No validation error if empty
- Value is null or empty string if not filled

**Reference:** [Required validator](./REFERENCE.md#common-validators)

---

## 16. Side-by-Side Fields with StackField

**Use case:** Place related fields on the same row (e.g., width/height, start/end dates, paired toggles).

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;

namespace MyExtension;

public class MyArgs
{
    [StackField(
        Label = "Dimensions",
        HelperText = "Width and Height in millimeters.")]
    public DimensionsSettings Dimensions { get; } = new();
}

public class DimensionsSettings
{
    [IntegerField(Label = "Width", Hint = "mm")]
    public int Width { get; set; } = 100;

    [IntegerField(Label = "Height", Hint = "mm")]
    public int Height { get; set; } = 100;
}
```

**Command usage:**
```csharp
var width = args.Dimensions.Width;
var height = args.Dimensions.Height;
```

**What happens:**
- Width and Height render side-by-side (horizontal is the default orientation)
- No card or heading — just inline layout
- Nested class properties are individually validated and serialized

**Reference:** [StackField](./REFERENCE.md#stackfield)

---

## 17. Multi-Section Form with Icons and Expandable Sections

**Use case:** Organize a complex form into visually distinct cards, with optional expandable sections for advanced options.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts;

namespace MyExtension;

public class MyArgs
{
    [TextField(Label = "Job Name", ToolTip = "Friendly name for this run.")]
    public string JobName { get; set; } = "Sample run";

    [SectionField(
        Label = "Service Endpoint",
        Icon = IconKind.Web,
        HelperText = "Host and transport settings.")]
    public EndpointConfigurationSettings EndpointConfiguration { get; } = new();

    [SectionField(
        Label = "Advanced Options",
        Icon = IconKind.Tune,
        IsExpandable = true,
        IsExpanded = false,
        HelperText = "Expand to configure retry behavior.")]
    public AdvancedReliabilitySettings AdvancedReliability { get; } = new();
}

public class EndpointConfigurationSettings
{
    [UrlField(Label = "Service Base URL", Hint = "https://api.example.com")]
    public string ServiceBaseUrl { get; set; } = "https://api.example.com";

    [IntegerField(Label = "Service Port")]
    public int ServicePort { get; set; } = 443;

    [BooleanField(Label = "Use Secure Connection (SSL/TLS)")]
    public bool UseSsl { get; set; } = true;
}

public class AdvancedReliabilitySettings
{
    [IntegerField(Label = "Max Retry Attempts")]
    public int RetryCount { get; set; } = 3;

    [BooleanField(Label = "Dry Run Mode")]
    public bool IsDryRun { get; set; } = false;
}
```

**Command usage:**
```csharp
var url = args.EndpointConfiguration.ServiceBaseUrl;
var retries = args.AdvancedReliability.RetryCount;
var dryRun = args.AdvancedReliability.IsDryRun;
```

**What happens:**
- "Job Name" appears as a top-level field
- "Service Endpoint" renders as a card with a globe icon, always expanded
- "Advanced Options" renders as a card with a tune icon, initially collapsed
- Users click to expand the advanced section when needed

**Reference:** [SectionField](./REFERENCE.md#sectionfield)

---

## 18. Section-Wide Visibility Toggle

**Use case:** Show or hide an entire section based on a top-level checkbox.

**Args class:**
```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using CW.Assistant.Extensions.Contracts;

namespace MyExtension;

public class MyArgs
{
    [TextField(Label = "Project Name")]
    public string ProjectName { get; set; } = "";

    [BooleanField(Label = "Configure Advanced Export Settings")]
    public bool ShowAdvancedExport { get; set; } = false;

    [SectionField(
        Label = "Advanced Export",
        Icon = IconKind.FileExport,
        Visibility = nameof(ShowAdvancedExport))]
    public AdvancedExportSettings AdvancedExport { get; } = new();
}

public class AdvancedExportSettings
{
    [OptionsField(Label = "Compression Level")]
    public CompressionLevel Compression { get; set; } = CompressionLevel.Normal;

    [BooleanField(Label = "Include Metadata")]
    public bool IncludeMetadata { get; set; } = true;
}

public enum CompressionLevel { None, Fast, Normal, Maximum }
```

**What happens:**
- The "Advanced Export" card is completely hidden until the user checks the toggle
- When the user checks "Configure Advanced Export Settings", the card appears
- The toggle itself is always visible
- Validation inside the hidden section does not run while it is hidden

**Reference:** [Grouping visibility](./REFERENCE.md#visibility-on-groups-and-sections), [SectionField](./REFERENCE.md#sectionfield)

---

## Next Steps

- **Deep reference?** → [Reference](./REFERENCE.md)
- **Complete guide?** → [Args Developer Guide](./ARGS_DEVELOPER_GUIDE.md)
- **Need platform details?** → [Platform Guides](./PLATFORM_GUIDES/)
- **See a real extension?** → [`AssistantDemoExtension`](../../src/Assistant/dotnet/AssistantDemoExtension/)

---

**Tip:** All patterns in this cookbook are tested against the demo extension. When you see a pattern that matches your use case, copy it and adapt to your field names and validation rules.
