---
applyTo: '**/Revit/dotnet/**'
---

# Assistant .NET Extension Development Guide for LLMs

## Core Concepts Overview

You are currenty working on a Revit Extension, but here is a brief overview of all the extension types available in the Assistant ecosystem.
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

## Revit Extension Development (Focus Area)

### Implementation Pattern

```csharp
// 1. Define Args class with input parameters
public class RevitExtensionArgs
{
    [Description("Parameter Name")]
    [ControlData(ToolTip = "Enter parameter name")]
    public string ParameterName { get; set; }
    
    // Value Copy functionality
    [ValueCopyCollector(typeof(ValueCopyRevitCollector))]
    public ValueCopy ValueCopy { get; set; }
}

// 2. Implement the extension logic
public class RevitExtensionCommand : IRevitExtension<RevitExtensionArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, RevitExtensionArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        
        if (document is null)
            return Result.Text.Failed("Revit has no active model open");
            
        // Get selected elements in the model
        var selectedObjects = context.UIApplication.ActiveUIDocument.Selection.GetElementIds();
        
        // Create a transaction to modify the model
        using var transaction = new Transaction(document, "RevitExtension");
        transaction.Start();
        
        // Modify the model elements
        foreach (var elementId in selectedObjects)
        {
            var element = document.GetElement(elementId);
            // Perform operations on Revit elements
        }
        
        transaction.Commit();
        
        // Return success result
        return Result.Text.Succeeded("Operation completed successfully");
    }
}

// 3. Implementing ValueCopy functionality
public class ValueCopyRevitCollector : IValueCopyRevitCollector<RevitExtensionArgs>
{
    public ValueCopyRevitSources GetSources(IValueCopyRevitContext context, RevitExtensionArgs args)
    {
        var filter = new FilteredElementCollector(context.Document).WhereElementIsElementType();
        return new ValueCopyRevitSources(filter);
    }

    public ValueCopyRevitTargets GetTargets(IValueCopyRevitContext context, RevitExtensionArgs args)
    {
        var filter = new FilteredElementCollector(context.Document).WhereElementIsElementType();
        return new ValueCopyRevitTargets(filter);
    }
}
```

### Key Revit-Specific Features

#### ValueCopy Functionality

ValueCopy enables parameter and property value copying between Revit elements:

1. **Setup in Args**:
   ```csharp
   [ValueCopyCollector(typeof(ValueCopyRevitCollector))]
   public ValueCopy ValueCopy { get; set; }
   ```

2. **Filter Element Selection**:
   - Filter by element type: `.WhereElementIsElementType()`
   - Filter by category: `.OfCategory(BuiltInCategory.OST_Walls)`

3. **Using ValueCopy in Command**:
   ```csharp
   var valueCopyHandler = context.GetHandler(args.ValueCopy);
   
   // Copy between elements
   var result = valueCopyHandler.Handle(sourceElement, targetElement);
   
   // Copy within same element
   var result = valueCopyHandler.Handle(targetElement);
   
   // Copy to multiple targets
   var results = valueCopyHandler.Handle(sourceElement, targetElements);
   ```

4. **Exception Handling**:
   ```csharp
   throw new InvalidConfigurationException("Custom Error Message!");
   ```

#### Custom AutoFill Collectors

Implement intelligent parameter suggestions:

```csharp
public class ExtensionArgs
{
    [CustomRevitAutoFill(typeof(ParameterAutoFillCollector))]
    public List<string> ParameterNames { get; set; }
}

public class ParameterAutoFillCollector : IRevitAutoFillCollector<ExtensionArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, ExtensionArgs args)
    {
        var result = new Dictionary<string, string>();
        
        try
        {
            var document = uiApplication.ActiveUIDocument.Document;
            
            // Fetch parameter names from the model
            var parameterNames = new List<string> { "Parameter1", "Parameter2", "Parameter3" };
            
            foreach (var parameterName in parameterNames)
            {
                result.Add(parameterName, parameterName);
            }
        }
        catch (Exception e)
        {
            result.Add(string.Empty, $"Failed to get autofill: {e.Message}");
        }
        
        return result;
    }
}
```

## Working with Revit Elements

Common operations:

1. **Access Document**:
   ```csharp
   var document = context.UIApplication.ActiveUIDocument?.Document;
   ```

2. **Get Selected Elements**:
   ```csharp
   var selectedIds = context.UIApplication.ActiveUIDocument.Selection.GetElementIds();
   ```

3. **Element Collection**:
   ```csharp
   var collector = new FilteredElementCollector(document);
   var walls = collector.OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType();
   ```

4. **Transactions**:
   ```csharp
   using var transaction = new Transaction(document, "Description");
   transaction.Start();
   // Modify elements
   transaction.Commit();
   ```

5. **Parameter Access**:
   ```csharp
   var parameter = element.LookupParameter("ParameterName");
   var value = parameter.AsString(); // or AsDouble(), AsInteger(), etc.
   parameter.Set(newValue);
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
- `[CustomRevitAutoFill(typeof(CustomCollectorClass))]`: Custom Revit data collector
- `[CustomAutoFill(typeof(CustomCollectorClass))]`: Generic auto-fill collector
- `[RevitAutoFill(RevitAutoFillSource.Phases)]`: Use built-in Revit phases
- `[RevitAutoFill(RevitAutoFillSource.Categories)]`: Use Revit categories
- `[RevitAutoFill(RevitAutoFillSource.FamilyAndType)]`: Use family types
- `[RevitAutoFill(RevitAutoFillSource.ByCustomFilter)]`: Custom filtered auto-fill
- `[AutoFill(SortOrder = SortOrder.SortByAscending)]`: Sort auto-fill values

### Advanced Controls
- `[Authorization(Login.Autodesk)]`: Configure authorization for APIs
- `[BaseUrl("https://developer.api.autodesk.com/")]`: Set base URL for API client
- `public IExtensionHttpClient Client { get; set; }`: HTTP client for API calls
- `public FilteredElementCollector FilterControl { get; set; }`: Element filter control

### Element ID Access
- Use `int` type with RevitAutoFill to get ElementId
- Use `string` type with RevitAutoFill to get UniqueId

### Complex Data Types
- `public Dictionary<string, string> Dictionary { get; set; }`: Dictionary/key-value pair control
- `public List<CustomEnum> ListControl { get; set; }`: List of enum values
- `public DateTime DateControl { get; set; }`: Date and time picker

## Best Practices

1. Always check for null document/elements
2. Use transactions for all model modifications
3. Handle exceptions and provide clear error messages
4. Filter elements appropriately to improve performance
5. Use ValueCopy for complex parameter transfers
6. Include informative help documentation

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
- **Transaction issues**: Ensure Start() and Commit() are paired
- **Element not found**: Verify element selection filters
- **Parameter problems**: Confirm parameter existence and type match

## Example: Parameter Copy Extension

```csharp
public class ParameterCopyArgs
{
    [Description("Source Parameter")]
    [CustomRevitAutoFill(typeof(ParameterAutoFillCollector))]
    public string SourceParameter { get; set; }
    
    [Description("Target Parameter")]
    [CustomRevitAutoFill(typeof(ParameterAutoFillCollector))]
    public string TargetParameter { get; set; }
}

public class ParameterCopyCommand : IRevitExtension<ParameterCopyArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, ParameterCopyArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        if (document == null) return Result.Text.Failed("No active document");
        
        var selectedIds = context.UIApplication.ActiveUIDocument.Selection.GetElementIds();
        if (!selectedIds.Any()) return Result.Text.Failed("No elements selected");
        
        using var transaction = new Transaction(document, "Parameter Copy");
        transaction.Start();
        
        int successCount = 0;
        foreach (var id in selectedIds)
        {
            var element = document.GetElement(id);
            var sourceParam = element.LookupParameter(args.SourceParameter);
            var targetParam = element.LookupParameter(args.TargetParameter);
            
            if (sourceParam != null && targetParam != null && sourceParam.StorageType == targetParam.StorageType)
            {
                switch (sourceParam.StorageType)
                {
                    case StorageType.String:
                        targetParam.Set(sourceParam.AsString());
                        break;
                    case StorageType.Double:
                        targetParam.Set(sourceParam.AsDouble());
                        break;
                    case StorageType.Integer:
                        targetParam.Set(sourceParam.AsInteger());
                        break;
                    case StorageType.ElementId:
                        targetParam.Set(sourceParam.AsElementId());
                        break;
                }
                successCount++;
            }
        }
        
        transaction.Commit();
        return Result.Text.Succeeded($"Copied parameter values for {successCount} elements");
    }
}
```
