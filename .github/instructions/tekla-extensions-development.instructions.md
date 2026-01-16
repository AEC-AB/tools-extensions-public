---
applyTo: '**/Tekla/dotnet/**'
---

# Assistant .NET Extension Development Guide for LLMs

## Core Concepts Overview

You are currenty working on a Tekla Extension, but here is a brief overview of all the extension types available in the Assistant ecosystem.
 Each extension type is designed to run in a specific environment and perform specific tasks.
 
1. **Assistant Extensions**: For desktop automation, outside of any specific application
2. **Revit Extensions**: For Revit Automation
3. **Tekla Extensions**: For Tekla Structures automation
4. **AutoCAD Extensions**: For AutoCAD task automation
5. **Navisworks Extensions**: For Navisworks task automation

## Extension Development Framework

All extensions follow a consistent pattern with three key components:

1. **Args Class**: Defines input parameters and UI controls
2. **Command Class**: Implements the extension logic (IExtension interface)
3. **Result Class**: Standardizes the output format

## Tekla Extensions Development

### Core Tekla Concepts
- **Model**: The Model class represents a single model open in Tekla Structures. Before interaction with the model, the user will have to create one instance of this class.
- **ModelHandler**: Provides information about the currently open Tekla Structures model
- **ModelObjectSelector**: The ModelObjectSelector class can be used to select objects from the Tekla Structures user interface. Currently, these selections both select the objects from the database and highlight them visually.

### Key Tekla APIs
- **Tekla.Structures.Model**: Core namespace for model interaction
- **Tekla.Structures.Model.UI**: Contains UI-related classes like ModelObjectSelector
- **Tekla.Structures.Geometry3d**: Contains the required basic 3D geometric classes that are used by Tekla Structures. Additionally, some helper functionality to ease the usage of these classes is provided.
- **Tekla.Structures.Drawing**: For creating and modifying drawings

### Tekla Element Types
- **Beam**: Linear structural elements
- **Column**: Vertical structural elements
- **Plate**: Flat structural elements
- **BoltArray**: Collection of bolts
- **Reinforcement**: Concrete reinforcement elements
- **Assembly**: Defines a single manufacturing unit: objects that are bolted or welded together in the workshop. An assembly has a main part and secondary assemblables attached to it. The number of secondaries is limited to 2048. Hierarchical assemblies can also have subassemblies attached to them and they can have a parent assembly instance.
- **Connection**: A connection is something that connects two or more parts together.

### Tekla Extension Best Practices
1. Always check model connection status before operations
2. Filter objects appropriately for better performance
3. Handle null values and provide clear error messages
4. Release references to model objects when finished

### Common Tekla Operations Code Examples
```csharp
// Create a new model instance
var model = new Model();

// Check if connected to an open model
if (!model.GetConnectionStatus())
    return Result.Text.Failed("No active Tekla model");

// Get selected objects
var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
var selectedObjects = selector.GetSelectedObjects();

// Begin a transaction
model.CommitChanges();

// Create a new beam
var beam = new Beam();
beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
beam.EndPoint = new Tekla.Structures.Geometry3d.Point(1000, 0, 0);
beam.Profile = new Profile("HEA300");
beam.Material = new Material("S355J2");
beam.Insert();

// Commit changes to the model
model.CommitChanges();
```

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
- `[CustomAutoFill(typeof(CustomCollectorClass))]`: Generic auto-fill collector
- `[CustomTeklaAutoFill(typeof(CustomTeklaCollectorClass))]`: Custom Tekla data collector
- `[AutoFill(SortOrder = SortOrder.SortByAscending)]`: Sort auto-fill values

### Custom Tekla AutoFill Collectors
```csharp
[Description("Profile Name")]
[CustomTeklaAutoFill(typeof(ProfileNameAutoFillCollector))]
public string ProfileName { get; set; }

internal class ProfileNameAutoFillCollector : ITeklaAutoFillCollector<TeklaAutomationExtensionArgs>
{
    public Dictionary<string, string> Get(TeklaAutomationExtensionArgs args)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var model = new Model();
            if (!model.GetConnectionStatus()) return result;
            
            // Get available profiles from the model
            var profileNames = new List<string>();
            var modelProfileEnumerator = model.GetProfileNames();
            while (modelProfileEnumerator.MoveNext())
            {
                profileNames.Add(modelProfileEnumerator.Current.ToString());
            }
            
            // Add to result dictionary
            foreach (var profile in profileNames)
            {
                result.Add(profile, profile);
            }
        }
        catch { }
        return result;
    }
}
```

### Advanced Controls
- `[Authorization(Login.Autodesk)]`: Configure authorization for APIs
- `[BaseUrl("https://developer.api.autodesk.com/")]`: Set base URL for API client
- `public IExtensionHttpClient Client { get; set; }`: HTTP client for API calls

### Complex Data Types
- `public Dictionary<string, string> Dictionary { get; set; }`: Dictionary/key-value pair control
- `public List<CustomEnum> ListControl { get; set; }`: List of enum values
- `public DateTime DateControl { get; set; }`: Date and time picker

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
