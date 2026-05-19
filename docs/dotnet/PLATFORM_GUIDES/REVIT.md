# Revit Extensions: Platform Guide

This guide covers Revit-specific patterns for writing extensions that integrate with Autodesk Revit.

## Quick Reference

- **Extension interface:** `IRevitExtension` (or `IExternalCommand`)
- **Execution context:** Inside Revit process with full API access
- **Transaction handling:** Required for all document modifications
- **Document access:** Current document + workset context
- **Supported versions:** Revit 2019 and later

## Getting Started

See [Quick Start](../QUICK_START.md) for Args/Command basics. This guide covers Revit-specific patterns.

## Transaction Context

All Revit document modifications must occur within a transaction.

### Transaction Pattern

```csharp
public async Task<Result> ExecuteAsync(object? args, CancellationToken cancellationToken)
{
    var config = (MyArgs)args!;
    var doc = GetCurrentDocument();  // Access current Revit document
    
    using (var trans = new Transaction(doc, "Extension operation"))
    {
        trans.Start();
        
        // Your document modifications here
        // e.g., Create elements, modify parameters, etc.
        
        trans.Commit();
    }
    
    return Result.Success("Revit operation completed");
}
```

## Document & Element Access

*(Content to be added based on Revit SDK patterns)*

## Workset Handling

*(Content to be added based on Revit workset patterns)*

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

        try
        {
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
        }
        catch
        {
            // ignore
        }

        return result;
    }
}
```

---

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
