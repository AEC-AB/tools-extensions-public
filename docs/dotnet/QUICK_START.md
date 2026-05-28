# Quick Start: Build Your First Extension (5-10 minutes)

This guide gets you building your first Assistant extension in minutes. We'll walk through a working example, then explain each piece.

## The Simplest Extension: Hello World

Here's a complete, working extension with configuration UI:

### Step 1: Create the Args Class

Create `HelloWorldArgs.cs`—this defines what users configure in the Assistant UI:

```csharp
using CW.Assistant.Extensions.Contracts.Fields;
using System.ComponentModel.DataAnnotations;

namespace HelloWorld;

public class HelloWorldArgs
{
    [TextField(
        Label = "Your Name",
        Hint = "Enter your name",
        ToolTip = "The name will be included in the greeting")]
    [Required(ErrorMessage = "Name is required")]
    public string YourName { get; set; } = "";

    [IntegerField(
        Label = "Repeat Count",
        ToolTip = "How many times to repeat the greeting")]
    [Range(1, 10, ErrorMessage = "Must be between 1 and 10")]
    public int RepeatCount { get; set; } = 1;
}
```

**What's happening here?**
- `[TextField]` → Renders a text input box in the UI
- `[IntegerField]` → Renders a numeric input in the UI
- `[Required]` → Prevents users from leaving the field empty
- `[Range]` → Ensures the number is between 1 and 10
- Properties with get/set → User can change these in the UI

### Step 2: Use the Template Command Class

The template already includes `HelloWorldCommand.cs`. Assistant executes the `IAssistantExtension<HelloWorldArgs>` implementation when the extension runs:

```csharp
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HelloWorld;

public class HelloWorldCommand : IAssistantExtension<HelloWorldArgs>
{
    public async Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context,
        HelloWorldArgs args,
        CancellationToken cancellationToken)
    {
        // Build the output
        var messages = new List<string>();
        for (int i = 0; i < args.RepeatCount; i++)
        {
            messages.Add($"Hello {args.YourName}!");
        }

        var output = string.Join("\n", messages);

        // Return success with the output
        return Result.Text.Succeeded($"Greeting:\n{output}");
    }
}
```

**What's happening here?**
- `RunAsync()` → Called when the extension runs
- `context` → Provides access to Assistant APIs (logging, messaging, etc.)
- `args` → The user's configuration from the Args class (hydrated with their input, strongly typed)
- `cancellationToken` → Allows cancelling long-running work
- Build a result and return it using `Result.Text.*` for plain messages or `Result.Markdown.*` for structured rich output
- `IAssistantExtension<TArgs>` is the Assistant integration interface. Other integrations use their own interfaces (for example Revit, AutoCAD, Navisworks, and Tekla-specific extension interfaces)

### Step 3: User Runs It in Assistant

1. User adds your extension to a workflow in Assistant
2. Assistant UI renders your `HelloWorldArgs` properties as form fields
3. User fills in:
   - **Your Name:** "Alice"
   - **Repeat Count:** 3
4. User clicks "Run"
5. Your `HelloWorldCommand.RunAsync()` receives the hydrated config and executes
6. Output appears: "Hello Alice!\nHello Alice!\nHello Alice!"

---

## Breaking It Down: What Each Part Does

### Args Class = Configuration UI

Your `*Args.cs` class defines **what users configure**. Each public property with a field attribute becomes one form field:

```csharp
[TextField(Label = "Your Name", ...)]
public string YourName { get; set; } = "";
```

| Property | Rendered As | In Workflow |
|----------|-------------|------------|
| `YourName` (TextField) | Text input box | User enters text |
| `RepeatCount` (IntegerField) | Number spinner | User picks 1-10 |

The field attributes control the UI:
- `Label` → Field label shown to the user
- `Hint` → Placeholder text or suggestion
- `ToolTip` → Help text when user hovers
- `IsMultiline` → For large text (show as textarea)
- etc.

### Validation Attributes = Automatic Validation

Validation attributes from `System.ComponentModel.DataAnnotations` auto-wire UI validation:

```csharp
[Required(ErrorMessage = "Name is required")]
[Range(1, 10, ErrorMessage = "Must be between 1 and 10")]
```

Before the extension runs:
1. User fills in the form
2. Assistant validates against your attributes
3. If invalid, shows the error message and blocks execution
4. If valid, passes the args to your Command

Common validators:
- `[Required]` → Field must not be empty
- `[Range(min, max)]` → Number must be within range
- `[StringLength(max)]` → Text must be shorter than max
- `[RegularExpression(pattern)]` → Text must match regex
- See [Reference](./REFERENCE.md) for full list

### Command Class = Execution

Your template-provided `*Command.cs` implements `IAssistantExtension<TArgs>` for Assistant extensions:

```csharp
public async Task<IExtensionResult> RunAsync(
    IAssistantExtensionContext context,
    HelloWorldArgs args,
    CancellationToken cancellationToken)
{
    // args is the hydrated configuration
    // Do your work here
    // Return a result
    return Result.Text.Succeeded("Done");
}
```

**Lifecycle:**
1. User configures Args in UI (e.g., Name = "Alice", Count = 3)
2. Assistant serializes the config
3. Assistant deserializes it into your Args class instance
4. Assistant calls `RunAsync(context, args, cancellationToken)` with the hydrated instance
5. Your code reads `args.YourName` and `args.RepeatCount`
6. You perform your work (read models, write files, etc.)
7. Return a result — `Result.Text.*` for plain text, `Result.Markdown.*` for formatted output, or `Result.Empty.*` when no message is needed

---

## Common Questions

**Q: Can I make fields optional?**
A: Yes. Don't add `[Required]` and give a property a default value or make it nullable:
```csharp
[TextField(Label = "Optional notes")]
public string? Notes { get; set; }  // Can be null or empty
```

**Q: What if I need a dropdown list?**
A: Use `[OptionsField]` (see [Cookbook](./COOKBOOK.md) for examples).

**Q: Can I show/hide fields based on other fields?**
A: Yes! Use conditional visibility (see [Args Developer Guide](./ARGS_DEVELOPER_GUIDE.md#visibility-conditions) for details).

**Q: What field types are available?**
A: TextField, IntegerField, DoubleField, BooleanField, OptionsField, FilePickerField, FolderPickerField, ColorField, DateTimeField, and more. See [Reference](./REFERENCE.md#field-attributes) for the full catalog.

---

## Next Steps

- **Explore patterns?** → [Cookbook: Common Patterns](./COOKBOOK.md)
- **Need the full reference?** → [Args Developer Guide](./ARGS_DEVELOPER_GUIDE.md)
- **Working with Revit/AutoCAD/Tekla/etc.?** → [Platform-Specific Guides](./PLATFORM_GUIDES/)
- **See a real example?** → [`AssistantDemoExtension`](../../src/Assistant/dotnet/AssistantDemoExtension/) in this repo

---

**Pro tip:** The best way to learn is to start with a template and modify it. Your extension templates are in the [tools-extensions-templates](https://github.com/AEC-AB/tools-extensions-templates) repo.
