# Revit Extensions: Platform Guide

This guide covers Revit-specific patterns for writing extensions that integrate with Autodesk Revit.

## Quick Reference

- **Extension interface:** `IRevitExtension<TArgs>`
- **Execution context:** Inside Revit process with full API access
- **Transaction handling:** Required for all document modifications
- **Document access:** Current document + workset context
- **Supported versions:** Revit 2019 and later

## Getting Started

See [Quick Start](../QUICK_START.md) for Args/Command basics. This guide covers Revit-specific patterns.

Scope: these guides provide extension and integration patterns and hints, not a
complete API reference. Consult the [official Autodesk Revit API documentation](https://help.autodesk.com/view/RVT/2025/ENU/)
for full host API semantics.

## Choose Revit Template

When creating a Revit extension for Assistant, choose between:

1. **Revit Automation Extension for Assistant**
2. **Revit App Extension for Assistant**

### Revit Automation Extension for Assistant

Use this template when the extension should run as a task inside an Assistant automation action.

- Best for workflow/automation actions
- Focused on deterministic task execution (input -> run -> result)
- Typically no modeless application UI

### Revit App Extension for Assistant

Use this template when the extension is an interactive Revit app with a modeless UI.

- Best for interactive tools launched by users inside Revit
- Intended for Assistant action files used as buttons via Assistant Shortcuts
- Supports modeless WPF UI patterns so the window remains responsive while Revit work is dispatched on the Revit UI thread
- Better fit for user-driven app workflows than unattended automation actions

### Distribution note for Revit App Extensions

Assistant Shortcuts enable distributing model-based extension apps: you can assign app actions to a Revit model so users opening that model can access the shortcut button (requires Assistant to be installed).

## Transaction Context

All Revit document modifications must occur within a transaction.

Check `context.IsDryRun` before mutating the document. In dry-run mode, return a
summary of the intended change without starting a transaction. This setting
comes from the extension context; do not add another `Args` boolean for it.

### Transaction Pattern

```csharp
public IExtensionResult Run(
    IRevitExtensionContext context,
    MyArgs args,
    CancellationToken cancellationToken)
{
    var doc = GetCurrentDocument();  // Access current Revit document
    
    using (var trans = new Transaction(doc, "Extension operation"))
    {
        trans.Start();
        
        // Your document modifications here
        // e.g., Create elements, modify parameters, etc.
        
        trans.Commit();
    }
    
    return Result.Text.Succeeded("Revit operation completed");
}
```

## Document & Element Access

Get the active document from the extension context and query it with a
`FilteredElementCollector`. Check that `ActiveUIDocument` and `Document` are
available before accessing elements:

```csharp
var document = context.UIApplication.ActiveUIDocument?.Document;
if (document is null)
    return Result.Text.Failed("Revit has no active document.");

var walls = new FilteredElementCollector(document)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .ToElements();
```

For a focused, file-based link insertion example, see the [Insert a Revit link
recipe](../COOKBOOK.md#insert-a-revit-link) in the cookbook.

## Workset Handling

Enumerate user worksets with `FilteredWorksetCollector` and use the workset
identifier when filtering or reporting elements. Workset visibility and
editable ownership are document state, so check them before making changes:

```csharp
var userWorksets = new FilteredWorksetCollector(document)
    .OfKind(WorksetKind.UserWorkset)
    .ToWorksets();

foreach (var workset in userWorksets)
{
    var elementCount = new FilteredElementCollector(document)
        .WherePasses(new ElementWorksetFilter(workset.Id))
        .GetElementCount();

    // Use workset.Name, workset.Id, and elementCount in the result or log.
}
```

Do not assume a workset is editable in a central model. Handle ownership and
worksharing exceptions at the command boundary, and keep any transaction
that changes workset-related state short.

## CollectorType in Revit Extensions

For fields that support `CollectorType` (`ICollectorTypeAttribute`), Revit extensions must use collectors implementing `IRevitAutoFillCollector<TArgs>`.

- `TextField`: collector is optional and provides suggestions only.
- `OptionsField` and `ChoiceField`: when property type is not enum, provide `CollectorType` so users can pick from collected items.
- If property type is enum, enum values are used automatically and collector is optional.

Example:

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

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
