# AssistantDemoExtension

A comprehensive demonstration of how to develop extensions for Assistant by AEC, showcasing all available UI field types, validation attributes, and conditional visibility features.

## Table of Contents

- [Overview](#overview)
- [Getting Started](#getting-started)
- [Extension Architecture](#extension-architecture)
- [UI Field Types](#ui-field-types)
- [Validation Attributes](#validation-attributes)
- [Conditional Visibility](#conditional-visibility)
- [Custom AutoFill Collectors](#custom-autofill-collectors)
- [Examples](#examples)

## Overview

This demo extension demonstrates how to create user-friendly input forms for Assistant extensions by defining properties with specific attributes in an Args class. The Assistant framework automatically generates the UI based on these property definitions.

![UI Generated Part 1](Resources/ArgsClassPart1.png)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Assistant by AEC platform
- Visual Studio or your preferred C# IDE

### Extension Structure

Every Assistant extension consists of three main components:

1. **Args Class** - Defines input parameters and generates UI controls
2. **Command Class** - Implements the extension logic (IExtension interface)
3. **Result Class** - Standardizes the output format

### The Command Class - IAssistantExtension Interface

The Command Class is where you implement the actual business logic that executes when users run your extension from the Assistant automation platform. This class must implement the `IAssistantExtension<TArgs>` interface.

#### Interface Definition

```csharp
public interface IAssistantExtension<TArgs>
{
    Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context, 
        TArgs args, 
        CancellationToken cancellationToken);
}
```

#### Implementation Example

```csharp
public class AssistantDemoExtensionCommand : IAssistantExtension<AssistantDemoExtensionArgs>
{
    public async Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context, 
        AssistantDemoExtensionArgs args, 
        CancellationToken cancellationToken)
    {
        // Access user-configured inputs from args
        var userInput = args.Input;
        
        // Perform your business logic
        await Task.Delay(300, cancellationToken);
        
        // Return a result
        return Result.Text.Succeeded($"Processed: {userInput}");
    }
}
```

#### Key Concepts

**Accessing User Configuration**

The `args` parameter provides access to all properties defined in your Args class that users have configured in the Assistant UI:

```csharp
var text = args.Input;
var number = args.IntegerInput;
var files = args.BrowseForMultipleFiles;
```

**Supporting Cancellation**

Always respect the `cancellationToken` to allow users to cancel long-running operations. Pass it to async operations:

```csharp
await SomeLongRunningOperationAsync(cancellationToken);
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

The Args class is where you define all user inputs. Each property becomes a UI control in the Assistant interface. The framework supports a wide range of field types and customization options.

```csharp
public class AssistantDemoExtensionArgs
{
    [TextField(Label = "Text input", ToolTip = "Help text")]
    [Required(ErrorMessage = "This field is required.")]
    public string Input { get; set; } = "Default value";
}
```

## UI Field Types

### Text Fields

#### Basic Text Input
```csharp
[TextField(
    Label = "Text input",
    Hint = "Enter some text",
    ToolTip = "Additional information",
    HelperText = "Description of the input")]
[Required(ErrorMessage = "This field is required.")]
public string Input { get; set; } = "Default input";
```

#### Multiline Text Input
```csharp
[TextField(
    Label = "Multiline Text input",
    ToolTip = "Enter multiple lines",
    IsMultiline = true,
    MinLines = 3,
    MaxLines = 6)]
public string TextInputMultiline { get; set; }
```

#### Read-Only Text
```csharp
[TextField(Label = "Read-Only Text")]
public string ReadOnlyText { get; } = "Cannot be modified";
```

### URL Fields
```csharp
[UrlField(Label = "URL input", Hint = "https://www.example.com")]
[Url(ErrorMessage = "Please enter a valid URL.")]
public string? UrlInput { get; set; }
```

### Numeric Fields

#### Integer Input
```csharp
[IntegerField(Label = "Integer input")]
public int IntegerInput { get; set; } = 5;
```

#### Integer Slider
```csharp
[IntegerField(
    Label = "Integer Slider",
    MinimumValue = 0,
    MaximumValue = 30,
    StepValue = 5)]
public int IntegerSliderInput { get; set; } = 15;
```

#### Double/Decimal Input
```csharp
[DoubleField(Label = "Number input")]
public double NumberInput { get; set; } = 10.5;
```

### Boolean Fields
```csharp
[BooleanField(Label = "Boolean input")]
public bool BooleanInput { get; set; }
```

### Date and Time Fields

#### Date and Time Picker
```csharp
[DateTimeField(
    Label = "Date and Time Picker",
    ShowTime = true)]
public DateTime DateAndTime { get; set; }
```

#### Date Only Picker
```csharp
[DateTimeField(
    Label = "Date Only Picker",
    ShowTime = false)]
public DateTime DateOnly { get; set; }
```

![UI Generated Part 2](Resources/ArgsClassPart2.png)

### File and Folder Pickers

#### Single File Browser
```csharp
[FilePickerField(
    Label = "Browse for File",
    Hint = "Select a JSON file",
    FileExtensions = ["json", "*"])]
public string? BrowseForFile { get; set; }
```

#### Multiple Files Browser
```csharp
[FilePickerField(
    Label = "Browse for Multiple Files",
    FileExtensions = ["json", "*"])]
public List<string> BrowseForMultipleFiles { get; set; } = [];
```

#### Folder Browser
```csharp
[FolderPickerField(Label = "Browse for Directory")]
public string? BrowseForDirectory { get; set; }
```

#### Save File Dialog
```csharp
[SaveFileField(
    Label = "Save File",
    Hint = "Save as JSON file",
    FileExtensions = ["json", "*"])]
public string? SaveFile { get; set; }
```

### Dropdown and Selection Fields

#### ComboBox with Enums
```csharp
[OptionsField(Label = "ComboBox with custom enums")]
public CustomEnum CustomEnumControl { get; set; } = CustomEnum.Option1;
```

#### ListBox (Multi-select)
```csharp
[OptionsField(
    Label = "ListBox with custom enums",
    CollectorSortOrder = SortOrder.SortByDescending,
    MaxHeight = 200)]
public List<CustomEnum> ListBoxWithEnum { get; set; } = [];
```

#### Compact ListBox
```csharp
[OptionsField(
    Label = "Compact ListBox",
    CompactMode = true)]
public List<CustomEnum> ListBoxCompact { get; set; } = [];
```

#### Radio Buttons
```csharp
[ChoiceField(Label = "RadioButton with custom enums")]
public CustomEnum RadioButtonWithEnum { get; set; }
```

#### Vertical Radio Buttons
```csharp
[ChoiceField(
    Label = "Vertical RadioButton",
    Orientation = ChoiceOrientation.Vertical)]
public CustomEnum RadioButtonVertical { get; set; }
```

### AutoFill Fields

#### Text Input with AutoFill
```csharp
[TextField(
    Label = "Text input with AutoFill",
    CollectorType = typeof(CustomAutoFillCollector))]
public string? AutoFillTextInput { get; set; }
```

#### Options with AutoFill
```csharp
[OptionsField(
    Label = "AutoFill input",
    CollectorType = typeof(CustomAutoFillCollector),
    CollectorSortOrder = SortOrder.SortByAscending)]
public string AutoFillInput { get; set; }
```

### List and Dictionary Fields

#### String List
```csharp
[ListField(Label = "String List input")]
public List<string> StringListInput { get; set; } = ["Item1", "Item2"];
```

#### String List with Options
```csharp
[OptionsField(
    Label = "String List with options",
    CollectorType = typeof(CustomAutoFillCollector))]
public List<string> StringListOptionsInput { get; set; } = [];
```

#### Dictionary (Key-Value Pairs)
```csharp
[DictionaryField(Label = "Dictionary input")]
public Dictionary<string, string> DictionaryInput { get; set; } = [];
```

#### Dictionary with Options
```csharp
[DictionaryField(
    Label = "Dictionary with options",
    CollectorType = typeof(CustomAutoFillCollector))]
public Dictionary<string, string> DictionaryWithOptions { get; set; } = [];
```

### Security Fields

#### Password/Credentials
```csharp
[PasswordField(
    Label = "Credentials for Application Id",
    ToolTip = "Select credentials from Credential Manager")]
public string CredentialsForApplicationId { get; } = "TestApplication";
```

### Color Picker
```csharp
[ColorField(Label = "Some color")]
public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;
```

![UI Generated Part 3](Resources/ArgsClassPart3.png)

## Validation Attributes

Assistant extensions support standard .NET validation attributes to ensure data integrity:

### Required Fields
```csharp
[Required(ErrorMessage = "This field is required.")]
public string Input { get; set; }
```

### URL Validation
```csharp
[Url(ErrorMessage = "Please enter a valid URL.")]
public string? UrlInput { get; set; }
```

### Regular Expression Validation
```csharp
[RegularExpression("Apple", ErrorMessage = "Please enter 'Apple' to proceed.")]
public string? TextInput { get; set; }
```

### Range Validation
```csharp
[Range(0, 120, ErrorMessage = "Please enter a valid age between 0 and 120.")]
public int NumericInput { get; set; }
```

### Allowed Values (for Enums)
```csharp
[AllowedValues(nameof(CustomEnum.Option2), nameof(CustomEnum.Option3),
    ErrorMessage = "Please select either Option2 or Option3.")]
public CustomEnum RadioButtonWithEnum { get; set; }
```

### Minimum Length
```csharp
[MinLength(3, ErrorMessage = "Please add at least 3 items to the list.")]
public List<string>? Items { get; set; }
```

## Conditional Visibility

One of the most powerful features is the ability to show or hide fields based on user input using the `Visibility` attribute.

### Basic Visibility
Show a field based on a boolean property:

```csharp
[BooleanField(Label = "Show the text field")]
public bool ShowTextField { get; set; }

[TextField(Visibility = nameof(ShowTextField))]
public string? TextInput { get; set; }
```

### Conditional Visibility with Value Comparison
Show a field when another field has a specific value:

```csharp
[TextField(Visibility = $"{nameof(ShowTextField)} && {nameof(TextInput)} == 'Apple'")]
public string ConditionalField { get; set; }
```

### Multiple Conditions
Combine multiple conditions:

```csharp
[TextField(
    Visibility = $"{nameof(NumericInput)} >= 18 && {nameof(OptionsInput)} == 'Beta'")]
public string Notification { get; } = "You are old enough!";
```

### List Count Conditions
Show fields based on collection counts:

```csharp
[TextField(Visibility = $"{nameof(Items)}.Count > 2")]
public string? MoreThanTwoItemsNotification { get; } = "You have added more than two items!";
```

### Complex Visibility Chain Example

```csharp
// Step 1: Enable with checkbox
[BooleanField(Label = "Show the text field by clicking this")]
public bool ShowTextField { get; set; }

// Step 2: Show text field, requires specific input
[TextField(
    HelperText = "Write 'Apple' to show more options.",
    Visibility = nameof(ShowTextField))]
[RegularExpression("Apple", ErrorMessage = "Please enter 'Apple' to proceed.")]
public string? TextInput { get; set; }

// Step 3: Show options field when text is correct
[OptionsField(
    HelperText = "Select Beta to get more options",
    Visibility = $"{nameof(ShowTextField)} && {nameof(TextInput)} == 'Apple'")]
[RegularExpression("Beta", ErrorMessage = "Please select 'Beta' to proceed.")]
public SampleEnum OptionsInput { get; set; }

// Step 4: Show numeric input when Beta is selected
[IntegerField(
    HelperText = "Are you over 18?",
    Visibility = $"{nameof(OptionsInput)} == 'Beta'")]
[Range(0, 120, ErrorMessage = "Please enter a valid age between 0 and 120.")]
public int NumericInput { get; set; }

// Step 5: Show notification when age requirement is met
[TextField(
    Visibility = $"{nameof(NumericInput)} >= 18 && {nameof(OptionsInput)} == 'Beta'")]
public string Notification { get; } = "You are old enough!";
```

## Custom AutoFill Collectors

Create dynamic dropdown options by implementing `IAsyncAutoFillCollector`:

```csharp
internal class CustomAutoFillCollector : IAsyncAutoFillCollector<AssistantDemoExtensionArgs>
{
    public Task<Dictionary<string, string>> Get(
        AssistantDemoExtensionArgs args, 
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>();
        
        // Generate options dynamically
        for (int i = 1; i <= 5; i++)
        {
            result.Add($"Key{i}", $"Display value {i}");
        }
        
        // You can access other args properties to customize options
        // if (!string.IsNullOrEmpty(args.Input))
        // {
        //     result.Add("custom", args.Input);
        // }
        
        return Task.FromResult(result);
    }
}
```

### Using Custom Collectors

```csharp
[OptionsField(
    Label = "AutoFill input",
    CollectorType = typeof(CustomAutoFillCollector),
    CollectorSortOrder = SortOrder.SortByAscending)]
public string AutoFillInput { get; set; }
```

## Examples

### Custom Enums with Descriptions

Define user-friendly display names for enum values:

```csharp
public enum CustomEnum
{
    [Description("Option 1")]
    Option1,

    [Description("Option 2")]
    Option2,

    [Description("Option 3")]
    Option3
}
```

### Progressive Form Example

Create a form that reveals fields step-by-step as the user provides valid input:

1. User checks a box → text field appears
2. User enters "Apple" → dropdown appears
3. User selects "Beta" → age field appears
4. User enters age 18 or older → notification appears

This creates an intuitive, guided user experience that prevents overwhelming users with too many options at once.

## Best Practices

1. **Use Meaningful Labels and Tooltips**: Help users understand what each field expects
2. **Provide Helper Text**: Give examples or additional context below fields
3. **Add Validation**: Use validation attributes to catch errors early
4. **Use Conditional Visibility**: Only show fields when they're relevant
5. **Set Sensible Defaults**: Pre-populate fields with reasonable default values
6. **Group Related Fields**: Use read-only text fields as section headers
7. **Use Appropriate Control Types**: Match the control to the data type and use case

## How to Use This Extension

This extension is designed purely for demonstration purposes. When run, it will display all the input values you've configured in the UI, helping you understand how different field types work and how your inputs are captured.

To explore:

1. Open the extension in Assistant
2. Experiment with different field types
3. Try the conditional visibility features
4. Test validation by entering invalid values
5. Examine the source code to see how each field is defined

## Additional Resources

- Review `AssistantDemoExtensionArgs.cs` for complete implementation examples
- Check the `.github/instructions/extensions-development-llm.instructions.md` file for detailed framework documentation
- Explore the generated UI screenshots in the `Resources` folder