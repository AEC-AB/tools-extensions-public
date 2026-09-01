# Extension documentation guidance

## Suggested reading order

After resolving `ExtensionDocsRoot`, read the documentation in this order:

1. `QUICK_START.md` for extension shape and execution model.
2. `ASSISTANT_MCP.md` when creating, testing, registering, publishing, or running an extension through Assistant.
3. The matching platform guide for runtime behavior (see the mapping below).
4. `ARGS_DEVELOPER_GUIDE.md` when changing configuration classes.
5. `COOKBOOK.md` for reusable implementation patterns.
6. `REFERENCE.md` for exact field syntax and validation rules.

Select the platform guide that matches the active extension template:

| Template | Guide |
| --- | --- |
| `AssistantAutomationExtension` | `PLATFORM_GUIDES/ASSISTANT.md` |
| `AutoCADAutomationExtension` | `PLATFORM_GUIDES/AUTOCAD.md` |
| `NavisworksAutomationExtension` | `PLATFORM_GUIDES/NAVISWORKS.md` |
| `RevitAutomationExtension` | `PLATFORM_GUIDES/REVIT.md` |
| `RevitAppExtension` | `PLATFORM_GUIDES/REVIT_APP_EXTENSION.md` |
| `TeklaAutomationExtension` | `PLATFORM_GUIDES/TEKLA.md` |
| `TeklaAppExtension` | `PLATFORM_GUIDES/TEKLA_APP_EXTENSION.md` |

## Integration API inspection

Use `dotnet-inspect` when the bundled extension docs do not cover the required Revit, AutoCAD, Navisworks, Tekla, or Assistant host-integration API. Install it once when unavailable:

```powershell
dotnet tool install -g dotnet-inspect
```

Run `dotnet-inspect skill` before the first inspection in a task. Target the resolved local assembly or NuGet package, then use:

```powershell
# Find API names before guessing.
dotnet-inspect search "[PathToDll]" "FilteredElementCollector"

# Inspect a type's constructors, interfaces, methods, and properties.
dotnet-inspect type "[PathToDll]" "Autodesk.Revit.DB.Wall"

# Compare two assembly or package versions.
dotnet-inspect diff [SourceTarget] [DestinationTarget]
```

Use the default Markdown output for focused investigation. Add `--json`, `--tsv`, or `--table` for structured output. Use `-v:d` only when decompiled implementation context is needed.
