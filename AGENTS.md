# AGENTS

This file gives reusable guidance to coding agents working in the public Assistant extensions repository.

## Repository overview

This repo contains Assistant extensions for multiple integrations (Assistant desktop, AutoCAD, Revit, Tekla, Navisworks) and is intended for publicly shared extension patterns, examples, and packaging outputs.

## Repository layout

- `src/Assistant/`, `src/AutoCAD/`, `src/Revit/`, `src/Tekla/`, `src/Navisworks/`: integration roots.
- `src/*/dotnet/`: C#/.NET extension projects grouped by integration.
- Each extension typically includes an args class, a command class, and documentation.

## Always-on rules

- Use the matching extension context for each integration and return `IExtensionResult` from commands.
- Pass `cancellationToken` to cancellable work and check it between long-running steps.
- Use `Result.*` helpers for outcomes. Prefer `Result.Markdown.*` for execution summaries and diagnostics.
- Failure results should state what happened, why it happened when relevant, and exactly what the user should check next.
- Do not catch `Exception` or `OperationCanceledException`. Catch only expected platform exceptions you can convert into actionable failures.
- Start implementation tasks with `skills/docs-routing/SKILL.md`. It resolves the offline extension docs bundled with the project's resolved dependency version.

## Integration API inspection

Use `dotnet-inspect` when an extension needs Revit, AutoCAD, Navisworks, Tekla, or Assistant host-integration API signatures or members that the bundled extension docs do not cover. Install it once when it is unavailable:

```powershell
dotnet tool install -g dotnet-inspect
```

Run `dotnet-inspect skill` before the first inspection in a task. It provides the tool's current agent guidance. Target the resolved local assembly or NuGet package for the integration, then use:

```powershell
# Find relevant API names before guessing.
dotnet-inspect search "[PathToDll]" "FilteredElementCollector"

# Inspect a type's constructors, interfaces, methods, and properties.
dotnet-inspect type "[PathToDll]" "Autodesk.Revit.DB.Wall"

# Compare two package or assembly versions when behavior may have changed.
dotnet-inspect diff [SourceTarget] [DestinationTarget]
```

Use the default Markdown output for focused investigation. Add `--json`, `--tsv`, or `--table` when structured output helps; use `-v:d` only when decompiled implementation context is required.

## Skills

- `skills/docs-routing/SKILL.md` - resolve the offline NuGet documentation root and load the relevant guidance.
- `skills/args-evolution/SKILL.md` - apply when editing `*Args.cs`, upgrades, collectors, or field metadata.
- `skills/readme-help/SKILL.md` - apply before shipping `README.md` updates.
- `skills/platform-assistant/SKILL.md` - apply when changing Assistant command logic or Assistant collectors.
- `skills/platform-autocad/SKILL.md` - apply when changing AutoCAD command logic or AutoCAD collectors.
- `skills/platform-navisworks/SKILL.md` - apply when changing Navisworks command logic or Navisworks collectors.
- `skills/platform-revit/SKILL.md` - apply when changing Revit command logic, transactions, ValueCopy, or Revit collectors.
- `skills/platform-tekla/SKILL.md` - apply when changing Tekla command logic or Tekla collectors.

## Developer documentation

For comprehensive guides on extension development, configuration classes (Args), field attributes, validation, and platform-specific patterns, see the [Extension Development Documentation](./docs/README.md).

- **Getting started?** -> [Quick Start Guide](./docs/dotnet/QUICK_START.md)
- **Building with patterns?** -> [Cookbook](./docs/dotnet/COOKBOOK.md)
- **Deep technical reference?** -> [Args Developer Guide](./docs/dotnet/ARGS_DEVELOPER_GUIDE.md)
- **Looking up syntax?** -> [Reference](./docs/dotnet/REFERENCE.md)
