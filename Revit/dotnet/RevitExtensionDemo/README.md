# RevitExtensionDemo

A comprehensive demonstration of how to develop extensions for Revit by AEC, showcasing all available Revit-specific UI field types, validation attributes, and custom collectors.

## Table of Contents

- [Overview](#overview)
- [Getting Started](#getting-started)
- [Extension Architecture](#extension-architecture)
- [Revit-Specific UI Field Types](#revit-specific-ui-field-types)
- [Custom Revit AutoFill Collectors](#custom-revit-autofill-collectors)
- [ValueCopy Functionality](#valuecopy-functionality)
- [Examples](#examples)

## Overview

This demo extension demonstrates how to create Revit-aware input forms for Revit extensions by defining properties with Revit-specific attributes in an Args class. The Revit extension framework automatically generates the UI based on these property definitions and provides seamless integration with Revit's API.

## Getting Started

### Prerequisites

- .NET 10 SDK
- Assistant by AEC platform
- Autodesk Revit
- Visual Studio or your preferred C# IDE

### Extension Structure

Every Revit extension consists of three main components:

1. **Args Class** - Defines input parameters and generates UI controls with Revit-specific data
2. **Command Class** - Implements the extension logic (IRevitExtension interface)
3. **Result Class** - Standardizes the output format

### The Command Class - IRevitExtension Interface

The Command Class is where you implement the actual business logic that executes when users run your extension from the Assistant automation platform. This class must implement the `IRevitExtension<TArgs>` interface.

#### Interface Definition

```csharp
public interface IRevitExtension<TArgs>
{
    IExtensionResult Run(
        IRevitExtensionContext context, 
        TArgs args, 
        CancellationToken cancellationToken);
}
```

#### Implementation Example

```csharp
public class RevitExtensionDemoCommand : IRevitExtension<RevitExtensionDemoArgs>
{
    public IExtensionResult Run(
        IRevitExtensionContext context, 
        RevitExtensionDemoArgs args, 
        CancellationToken cancellationToken)
    {
        // Access the active Revit document
        var document = context.UIApplication.ActiveUIDocument?.Document;
        
        if (document is null)
        {
            return Result.Text.Failed("No active document found.");
        }
        
        // Access user-configured inputs from args
        var selectedElements = args.SelectedElementIds;
        
        // Create a transaction for modifying the Revit document
        using var trans = new Transaction(document, "Extension Transaction");
        trans.Start();
        
        // Perform your business logic here
        // ...
        
        trans.Commit();
        
        // Return a result
        return Result.Text.Succeeded("Operation completed successfully");
    }
}
```

#### Key Concepts

**Accessing the Revit Context**

The `context` parameter provides access to the Revit application and active document:

```csharp
var uiApp = context.UIApplication;
var document = context.UIApplication.ActiveUIDocument?.Document;
```

**Working with Transactions**

All modifications to the Revit document must be wrapped in a transaction:

```csharp
using var trans = new Transaction(document, "Transaction Name");
trans.Start();
// Make changes to the document
trans.Commit();
```

**Accessing User Configuration**

The `args` parameter provides access to all properties defined in your Args class:

```csharp
var elementId = args.ElementId;
var filter = args.FilterControl;
var categories = args.RevitCategories;
```

**Supporting Cancellation**

Always respect the `cancellationToken` to allow users to cancel long-running operations:

```csharp
if (cancellationToken.IsCancellationRequested)
{
    trans.RollBack();
    return Result.Text.Failed("Operation cancelled");
}
```

**Returning Results**

Use the built-in `Result` helper class to return standardized results:

```csharp
// Text results with different status levels
Result.Text.Succeeded("Operation completed successfully");
Result.Text.PartiallySucceeded("Completed with warnings");
Result.Text.Failed("Operation failed: error message");

// Empty results (no message)
Result.Empty.Succeeded();
Result.Empty.PartiallySucceeded();
Result.Empty.Failed();
```

**Custom Results**

You can also implement the `IExtensionResult` interface for custom result types:

```csharp
public interface IExtensionResult
{
    ExecutionResult Result { get; set; }
    string? AsText();
}

public enum ExecutionResult
{
    Failed,
    PartiallySucceeded,
    Succeeded
}
```

## Extension Architecture

### Args Class

The Args class is where you define all user inputs. Each property becomes a UI control in the Assistant interface. The framework supports both standard field types and Revit-specific field types that integrate with the active Revit document.

```csharp
public class RevitExtensionDemoArgs
{
    [TextField(Label = "Text with Revit AutoFill")]
    [RevitAutoFill(RevitAutoFillSource.Phases)]
    public string? TextBoxWithAutoComplete { get; set; }
}
```

## Revit-Specific UI Field Types

### Authorization and HTTP Client

#### Autodesk Authorization
```csharp
[Authorization(Login.Autodesk)]
[BaseUrl("https://developer.api.autodesk.com/")]
public IExtensionHttpClient? AutodeskClient { get; set; }
```

This field automatically handles authentication with Autodesk services and provides an HTTP client configured for API calls.

### Revit AutoFill Fields

The `RevitAutoFill` attribute populates fields with data from the active Revit document.

#### Phases AutoComplete
```csharp
[TextField(
    Label = "TextBox with Revit AutoComplete",
    ToolTip = "TextBox control with Phases in active Revit file as auto complete sorted by ascending order.")]
[RevitAutoFill(RevitAutoFillSource.Phases, SortOrder = SortOrder.SortByAscending)]
public string? TextBoxWithAutoComplete { get; set; }
```

#### Custom Filtered Elements
```csharp
[ListField(
    Label = "List of Sheet Numbers",
    ToolTip = "List of strings with autocomplete populated from Sheet Numbers in the active Revit document.",
    MaxHeight = 200)]
[RevitAutoFill(
    RevitAutoFillSource.ByCustomFilter, 
    RevitType = typeof(View), 
    RevitBuiltInCategory = "OST_Sheets", 
    WhereElementIsType = false, 
    ParameterName = "Sheet Number")]
public List<string> SheetNumbersList { get; set; } = [];
```

#### Revit Categories
```csharp
[OptionsField(
    Label = "Revit Categories ListBox",
    ToolTip = "ListBox control with CompactMode displaying Revit categories as element IDs.",
    CompactMode = true)]
[RevitAutoFill(RevitAutoFillSource.Categories)]
public List<int> RevitCategories { get; set; } = [];
```

#### Family and Type Dictionary
```csharp
[DictionaryField(
    Label = "Family and Type Dictionary",
    ToolTip = "Dictionary control populated with Revit family and type combinations sorted in descending order.")]
[RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByDescending)]
public Dictionary<string, string> FamilyTypeDictionary { get; set; } = [];
```

#### Element ID from Family and Type
```csharp
[OptionsField(
    Label = "Element Id from Family and Type",
    ToolTip = "When the datatype is int and the control has an autofill source, you will get the ElementId of the selected element.")]
[RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByAscending)]
public int ElementId { get; set; }
```

#### Element UniqueId from Family and Type
```csharp
[OptionsField(
    Label = "Element UniqueId from Family and Type",
    ToolTip = "When the datatype is string, controltype is ComboBox and the control has an autofill source, you will get the UniqueId of the selected element.")]
[RevitAutoFill(RevitAutoFillSource.FamilyAndType, SortOrder = SortOrder.SortByAscending)]
public string? ElementUniqueId { get; set; }
```

### Filter Fields

#### Basic Filtered Element Collector
```csharp
[FilterField(
    Label = "Filtered Element Collector",
    ToolTip = "Control to define a selection filter for elements in the active Revit document.",
    UseActiveDocument = true)]
public FilteredElementCollector? FilterControl { get; set; }
```

#### Filtered Element Collector with Pre-selected Categories
```csharp
[FilterField(
    Label = "Filtered Element Collector with Selected Categories",
    ToolTip = "Control to define a selection filter for elements in the active Revit document, restricted to Walls and Doors.",
    UseActiveDocument = true,
    DisableCategorySelection = true,
    Categories = ["Walls", "Doors"])]
public FilteredElementCollector? FilterControlWithSelectedCategories { get; set; }
```

#### Multi-Document Filter
```csharp
[FilterField(
    Label = "Filtered Element Collector for Multiple Documents",
    ToolTip = "Control to define a selection filter for elements in multiple Revit documents.")]
public Dictionary<Document, FilteredElementCollector>? FilterControlMultipleDocuments { get; set; }
```

### Element Selection Fields

#### Single Element Selection
```csharp
[ElementSelectorField(
    Label = "Selected Element Id from Revit",
    ToolTip = "The ElementId of the currently selected element in Revit.")]
public ElementId? SelectedElementId { get; set; }
```

#### Multiple Element Selection
```csharp
[ElementSelectorField(
    Label = "Selected Element Ids from Revit",
    ToolTip = "The ElementIds of the currently selected elements in Revit.")]
public List<ElementId>? SelectedElementIds { get; set; }
```

### Custom Collectors

#### Custom Revit AutoFill Collector
```csharp
[OptionsField(
    Label = "Custom Revit AutoFill Collector",
    ToolTip = "ComboBox control with custom Revit-specific autofill data collector implementation.",
    CollectorType = typeof(CustomRevitAutoFillCollector))]
public string? CustomRevitCollector { get; set; }
```

#### Custom AutoFill Collector with Sorting
```csharp
[OptionsField(
    Label = "Custom AutoFill Collector",
    ToolTip = "ComboBox control with custom autofill data collector implementation.",
    CollectorType = typeof(CustomRevitAutoFillCollector),
    CollectorSortOrder = SortOrder.SortByAscending)]
public string? CustomCollector { get; set; }
```

### ValueCopy Field

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

## Custom Revit AutoFill Collectors

Create dynamic dropdown options populated from Revit data by implementing `IRevitAutoFillCollector`:

### Basic Collector Example

```csharp
internal class CustomRevitAutoFillCollector : IRevitAutoFillCollector<RevitExtensionDemoArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, RevitExtensionDemoArgs args)
    {
        var result = new Dictionary<string, string>();
        
        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;
            
            if (document is null)
                return result;
            
            // Get first Generic Model element
            using var element = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .FirstElement();
            
            // Populate options from element parameters
            foreach (var parameter in element.GetOrderedParameters())
            {
                result.Add(parameter.Definition.Name, parameter.Definition.Name);
            }
        }
        catch
        {
            // ignore
        }
        
        return result;
    }
}
```

### Simple Options Collector

```csharp
public class CustomAsyncAutoFillCollector : IRevitAutoFillCollector<RevitExtensionDemoArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, RevitExtensionDemoArgs args)
    {
        var options = new Dictionary<string, string>
        {
            { "OptionA", "Option A" },
            { "OptionB", "Option B" },
            { "OptionC", "Option C" }
        };
        
        return options;
    }
}
```

### Using Custom Collectors

```csharp
[OptionsField(
    Label = "Custom Revit AutoFill Collector",
    CollectorType = typeof(CustomRevitAutoFillCollector))]
public string? CustomRevitCollector { get; set; }
```

## ValueCopy Functionality

The ValueCopy feature allows you to copy parameter values between Revit elements with a custom UI control.

### Implementing a ValueCopy Collector

```csharp
public class ValueCopyRevitCollector : IValueCopyRevitCollector<RevitExtensionDemoArgs>
{
    public ValueCopyRevitSources GetSources(IValueCopyRevitContext context, RevitExtensionDemoArgs args)
    {  
        // Use filter from args if provided
        if (args.FilterControl is not null)
        {
            return new ValueCopyRevitSources(args.FilterControl);
        }
        
        // Otherwise use a default filter
        var filter = new FilteredElementCollector(context.Document)
            .OfCategory(BuiltInCategory.OST_GenericModel)
            .WhereElementIsElementType();
        return new ValueCopyRevitSources(filter);
    }
    
    public ValueCopyRevitTargets GetTargets(IValueCopyRevitContext context, RevitExtensionDemoArgs args)
    {
        // Use filter from args if provided
        if (args.FilterControlWithSelectedCategories is not null)
        {
            return new ValueCopyRevitTargets(args.FilterControlWithSelectedCategories);
        }
        
        // Otherwise use a default filter
        var filter = new FilteredElementCollector(context.Document)
            .OfCategory(BuiltInCategory.OST_Walls)
            .WhereElementIsElementType();
        return new ValueCopyRevitTargets(filter);
    }
}
```

### Using ValueCopy in Your Command

```csharp
public IExtensionResult Run(IRevitExtensionContext context, RevitExtensionDemoArgs args, CancellationToken cancellationToken)
{
    var document = context.UIApplication.ActiveUIDocument?.Document;
    
    if (args.ValueCopy is null)
    {
        return Result.Text.Failed("No ValueCopy configuration provided.");
    }
    
    using var trans = new Transaction(document, "ValueCopy Transaction");
    trans.Start();
    
    // Get the ValueCopy handler
    var valueCopyHandler = context.GetHandler(args.ValueCopy);
    
    // Use the handler to copy values between elements
    // var result = valueCopyHandler.Handle(sourceElement, targetElement);
    
    trans.Commit();
    
    return Result.Text.Succeeded("Values copied successfully");
}
```

## Examples

### RevitAutoFillSource Options

The `RevitAutoFillSource` enum provides several built-in sources for populating fields:

- `Phases` - All phases in the active document
- `Categories` - All Revit categories
- `FamilyAndType` - Family and type combinations
- `ByCustomFilter` - Use custom filtering with additional parameters

### Working with FilteredElementCollector

Filter fields provide users with a UI to configure element selection criteria:

```csharp
[FilterField(
    Label = "Select Elements",
    UseActiveDocument = true,
    Categories = ["Walls", "Floors", "Roofs"])]
public FilteredElementCollector? Elements { get; set; }
```

In your command, use the configured filter:

```csharp
if (args.Elements != null)
{
    foreach (Element element in args.Elements)
    {
        // Process each element
    }
}
```

### Element Selection

Element selector fields allow users to select elements directly in the Revit UI:

```csharp
// Single selection
[ElementSelectorField(Label = "Select an Element")]
public ElementId? SelectedElement { get; set; }

// Multiple selection
[ElementSelectorField(Label = "Select Multiple Elements")]
public List<ElementId>? SelectedElements { get; set; }
```

### HTTP Client Integration

Use the authorized HTTP client for Autodesk API calls:

```csharp
public IExtensionResult Run(IRevitExtensionContext context, RevitExtensionDemoArgs args, CancellationToken cancellationToken)
{
    if (args.AutodeskClient is null)
    {
        return Result.Text.Failed("No Autodesk Client configuration provided.");
    }
    
    var client = new AccClient(args.AutodeskClient);
    var hubs = client.GetHubs();
    
    // Work with Autodesk Construction Cloud data
    
    return Result.Text.Succeeded($"Found {hubs.Data.Count} hubs");
}
```

## Best Practices

1. **Always Check for Active Document**: Verify that a document is active before attempting operations
2. **Use Transactions Properly**: Wrap all document modifications in transactions
3. **Respect Cancellation Tokens**: Check for cancellation in long-running operations
4. **Handle Null Values**: Many Revit operations can return null; always validate
5. **Provide Meaningful Error Messages**: Help users understand what went wrong
6. **Use Appropriate Filter Types**: Choose the right filter field configuration for your use case
7. **Leverage Custom Collectors**: Create reusable collectors for common data patterns
8. **Document Your Fields**: Use Labels, ToolTips, and HelperText to guide users
9. **Use Element IDs vs UniqueIds**: Choose the appropriate identifier type for your needs
   - `int` (ElementId) - For operations within a single session
   - `string` (UniqueId) - For persistent references across sessions

## How to Use This Extension

This extension is designed purely for demonstration purposes. When run, it demonstrates:

1. Integration with Autodesk Construction Cloud APIs
2. Working with Revit element filters
3. ValueCopy functionality for parameter transfer
4. Various Revit-specific UI controls

To explore:

1. Open the extension in Assistant with an active Revit document
2. Experiment with different filter configurations
3. Try selecting elements in Revit
4. Explore the Revit AutoFill options
5. Examine the source code to see how each field is defined

## Additional Resources

- Review `RevitExtensionDemoArgs.cs` for complete implementation examples
- Review `RevitExtensionDemoCommand.cs` for command implementation patterns
- Explore custom collectors in the `Collectors` folder
- Check the Revit API documentation for detailed information on FilteredElementCollector and other Revit types