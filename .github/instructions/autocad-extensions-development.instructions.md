---
applyTo: '**/AutoCAD/dotnet/**'
---
# Assistant .NET Extension Development Guide for LLMs


## Core Concepts Overview

You are currently working on an AutoCAD Extension. Here is a brief overview of all the extension types available in the Assistant ecosystem.
Each extension type is designed to run in a specific environment and perform specific tasks.

1. **Assistant Extensions**: For desktop automation, outside of any specific application
2. **Revit Extensions**: For Revit Automation
3. **Tekla Extensions**: For Tekla Structures automation
4. **AutoCAD Extensions**: For AutoCAD task automation
5. **Navisworks Extensions**: For Navisworks task automation

---

## AutoCAD Extension Development (C#)

AutoCAD Extensions allow automation and customization of AutoCAD models and documents using C#. These extensions interact directly with the AutoCAD API and document model. Below are key guidelines and best practices for generating and maintaining AutoCAD extension code.

### Key AutoCAD Concepts

- **Document Access**: Use `Application.DocumentManager.MdiActiveDocument` to access the current document. Always check for null before proceeding.
- **Database and Transactions**: All model changes must be performed within a transaction (`db.TransactionManager.StartTransaction()`).
- **Document Locking**: Use `doc.LockDocument()` to prevent concurrent modifications.
- **BlockTable and BlockTableRecord**: Access the model space via the block table and block table record for writing entities.
- **Entity Creation**: Create and append entities (e.g., `Circle`, `DBText`) to the block table record, then add them to the transaction.
- **Selection**: Use `doc.Editor.SelectImplied()` to get currently selected objects.
- **Error Handling**: Always check for nulls and handle exceptions. Return clear error messages using the extension result pattern.

#### Example: Creating Entities in Model Space

```csharp
// Connect to active document
var doc = Application.DocumentManager.MdiActiveDocument;
if (doc is null)
    return Result.Text.Failed("AutoCAD has no active model open");
var db = doc.Database;
using var transaction = db.TransactionManager.StartTransaction();
using var documentLock = doc.LockDocument();
if (transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) is not BlockTable blockTable)
    return Result.Text.Failed("Cannot access to the BlockTable");
if (transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) is not BlockTableRecord blockTableRecord)
    return Result.Text.Failed("Cannot access to the BlockTableRecord");
// Create and append entities
var myCircle = new Circle { Center = new Point3d(0, 0, 0), Radius = 50 };
blockTableRecord.AppendEntity(myCircle);
transaction.AddNewlyCreatedDBObject(myCircle, true);
transaction.Commit();
```

#### Common AutoCAD API Types

- `Application`, `Document`, `Database`, `Transaction`, `BlockTable`, `BlockTableRecord`, `Circle`, `DBText`, `Point3d`, `AttachmentPoint`, `OpenMode`

#### AutoCAD-Specific UI Controls and Attributes

- `[CustomAutoCADAutoFill(typeof(MyAutoFillCollector))]`: Attach a custom collector for autofill suggestions using AutoCAD context.

**Example snippet for custom autofill collector:**
```csharp
[Description("ControlName")]
[CustomAutoCADAutoFill(typeof(ControlNameAutoFillCollector))]
public string ControlName { get; set; }

internal class ControlNameAutoFillCollector : IAutoCADAutoFillCollector<AutoCADAutomationExtensionArgs>
{
    public Dictionary<string, string> Get(AutoCADAutomationExtensionArgs args)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document is null) return result;
            // Custom logic here
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

For AutoCAD, use `[CustomAutoCADAutoFill(typeof(MyCollector))]` and implement `IAutoCADAutoFillCollector<TArgs>`. See the code snippet above for a template.

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
- `[CustomRevitAutoFill(typeof(CustomCollectorClass))]`: Custom Revit data collector
- `[CustomAutoFill(typeof(CustomCollectorClass))]`: Generic auto-fill collector
- `[AutoFill(SortOrder = SortOrder.SortByAscending)]`: Sort auto-fill values

- `[CustomAutoCADAutoFill(typeof(CustomCollectorClass))]`: Custom AutoCAD data collector (see above for usage)

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
2. Use transactions for all model modifications
3. Handle exceptions and provide clear error messages
4. Filter elements appropriately to improve performance
5. Include informative help documentation

7. For AutoCAD: Always lock the document before making changes, and commit the transaction after all modifications.
8. Use `AppendEntity` and `AddNewlyCreatedDBObject` for adding new entities to the model.
9. Use `SelectImplied` or other selection APIs to work with user-selected objects.
10. Use descriptive error messages for all failure cases.

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
    - For AutoCAD, ensure the document is not null and the transaction is properly committed.
