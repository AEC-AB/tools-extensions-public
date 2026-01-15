---
applyTo: '**/Navisworks/dotnet/**'
---
# Assistant .NET Extension Development Guide for LLMs

## Core Concepts Overview

You are currenty working on a Navisworks Extension, but here is a brief overview of all the extension types available in the Assistant ecosystem.
 Each extension type is designed to run in a specific environment and perform specific tasks.
 
1. **Assistant Extensions**: For desktop automation, outside of any specific application
2. **Revit Extensions**: For Revit Automation
3. **Tekla Extensions**: For Tekla Structures automation
4. **AutoCAD Extensions**: For AutoCAD task automation
5. **Navisworks Extensions**: For Navisworks task automation

---
## Navisworks Extensions: Key Concepts and Patterns

Navisworks Extensions enable automation, analysis, and interaction with Navisworks models. They are typically used to:
- Query and process selected elements in the active model
- Implement custom UI controls and parameter autofill logic
- Integrate with the Navisworks API for model data extraction and manipulation

### Navisworks API Essentials
- Access the active document via `Autodesk.Navisworks.Api.Application.ActiveDocument`
- Work with selections using `document.CurrentSelection.SelectedItems`
- Retrieve element properties such as `DisplayName` from selected items
- Always check for `null` on the document and selection

#### Example: Accessing Selected Elements
```csharp
var document = Autodesk.Navisworks.Api.Application.ActiveDocument;
if (document is null)
    return Result.Text.Failed("Navisworks has no active model open");
var selectedObjects = document.CurrentSelection.SelectedItems;
foreach (var selectedObject in selectedObjects)
{
    var elementName = selectedObject.DisplayName;
    // Additional processing
}
```

### Navisworks Extension Command Structure
- Implement `INavisworksExtension<TArgs>`
- Use the `Run` method to access context, arguments, and cancellation token
- Return results using the standardized `Result.Text.Succeeded` or `Result.Text.Failed`

### Args Class and UI Controls for Navisworks
- Define input parameters in the Args class
- Use attributes for UI customization (see below)
- Implement custom autofill for Navisworks context using `[CustomNavisworksAutoFill]`

#### Example: Custom AutoFill Collector for Navisworks
```csharp
[Description("Element Name")]
[CustomNavisworksAutoFill(typeof(ElementNameAutoFillCollector))]
public string ElementName { get; set; }

internal class ElementNameAutoFillCollector : INavisworksAutoFillCollector<NavisworksAutomationExtensionArgs>
{
    public Dictionary<string, string> Get(NavisworksAutomationExtensionArgs args)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var document = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (document is null) return result;
            // Populate result with element names or other data
        }
        catch { }
        return result;
    }
}
```

---

## Extension Development Framework

All extensions follow a consistent pattern with three key components:

1. **Args Class**: Defines input parameters and UI controls
2. **Command Class**: Implements the extension logic (IExtension interface)
3. **Result Class**: Standardizes the output format

#### Custom AutoFill Collectors
Implement intelligent parameter suggestions:

## Extension UI Controls and Attributes

### Basic Control Attributes
- `[Description("Label")]`: Sets field label
- `[ControlData(ToolTip = "Help text")]`: Adds tooltip
- `[Required]`: Makes input mandatory
- `[DefaultValue("Default")]`: Sets default value
- `[ControlSettings("PropertyName", "Value")]`: Configure control properties

### Control Types
- `[ControlType(ControlType.ComboBox)]`: Dropdown selection
- `[ControlType(ControlType.ListBox)]`: Multi-selection list
- `[ControlType(ControlType.Option)]`: Single-option selection
- `[ControlType(ControlType.RadioButton)]`: Radio button group
- `[ControlType(ControlType.Browse)]`: File browser dialog
- `[ControlType(ControlType.Save)]`: File save dialog

### File System Controls
- `[FileExtension("json")]`: Filter by file extension
- `[ControlSettings("SelectFolder", "true")]`: Configure for folder selection

### Text Input Customization
- `[ControlSettings("IsMultiline", "True")]`: Enable multi-line text input
- `[ControlSettings("MinLines", "5")]`: Set minimum lines for text area
- `[ControlSettings("MaxLines", "10")]`: Set maximum lines for text area
- `[ControlSettings("Foreground", "Red")]`: Change text color

### List Controls
- `[ControlSettings("CompactMode", "true")]`: Compact display for lists
- `[ControlSettings("MaxHeight", "200")]`: Set maximum height for list controls
- `[ControlSettings("Orientation", "Vertical")]`: Set orientation for radio buttons

### Date Controls
- `[ControlSettings("ShowTime", "true")]`: Configure date picker to include time

### Auto-Fill Sources
- `[CustomNavisworksAutoFill(typeof(CustomCollectorClass))]`: Custom Navisworks data collector for autofill
- `[CustomAutoFill(typeof(CustomCollectorClass))]`: Generic auto-fill collector
- `[AutoFill(SortOrder = SortOrder.SortByAscending)]`: Sort auto-fill values

##### Navisworks AutoFill Collector Interface
Implement `INavisworksAutoFillCollector<TArgs>` to provide context-aware suggestions for UI controls. The collector's `Get` method receives the current arguments and should return a dictionary of key-value pairs for the control.

---

### Advanced Controls
- `[Authorization(Login.Autodesk)]`: Configure authorization for APIs
- `[BaseUrl("https://developer.api.autodesk.com/")]`: Set base URL for API client
- `public IExtensionHttpClient Client { get; set; }`: HTTP client for API calls

### Complex Data Types
- `public Dictionary<string, string> Dictionary { get; set; }`: Dictionary/key-value pair control
- `public List<CustomEnum> ListControl { get; set; }`: List of enum values
- `public DateTime DateControl { get; set; }`: Date and time picker

## Best Practices

1. Always check for null document/elements
2. Handle exceptions and provide clear error messages
3. Filter elements appropriately to improve performance
4. Include informative help documentation

### Navisworks-Specific Best Practices
- Always check `Application.ActiveDocument` for null before accessing model data
- Use `CurrentSelection.SelectedItems` to work with user-selected elements
- Avoid modifying the model directly unless necessary; prefer read-only operations for analysis extensions
- Catch and handle exceptions in custom autofill collectors to avoid UI errors
- Use descriptive error messages for user feedback

---

### Supported Property Types for Args
The following property types are supported for extension input arguments and will be rendered as UI controls:

- `string`
- `int`
- `double`
- `bool`
- `DateTime`
- `enum` (including custom enums)
- `List<T>` (e.g., `List<string>`, `List<int>`, `List<CustomEnum>`)
- `Dictionary<string, string>`
- Custom types for advanced controls (e.g., `IExtensionHttpClient`, `FilteredElementCollector`)

Use these types when defining properties in your Args class to ensure proper UI generation.

#### Common ControlSettings Options by Property Type

| Property Type         | Common ControlSettings Options         | Example Usage                                      |
|---------------------- |---------------------------------------|----------------------------------------------------|
| string                | IsMultiline, MinLines, MaxLines, Foreground | `[ControlSettings("IsMultiline", "True")]`     |
| List<T>               | MaxHeight, CompactMode                | `[ControlSettings("MaxHeight", "200")]`        |
| DateTime              | ShowTime                              | `[ControlSettings("ShowTime", "true")]`        |
| enum                  | Orientation (for radio buttons)       | `[ControlSettings("Orientation", "Vertical")]` |
| int                   | (with autofill)                       | `[ControlSettings("MaxHeight", "200")]`        |
| Dictionary            | MaxHeight                             | `[ControlSettings("MaxHeight", "200")]`        |

Not all options are valid for all types. Refer to the examples above and use the options that make sense for your property type.

## Quick Troubleshooting

- **Nothing happens**: Check for errors in exception handling
