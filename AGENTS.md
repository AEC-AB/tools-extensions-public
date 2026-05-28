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
- Start docs and implementation tasks by using the `extension-docs` assistant docs MCP tool for current guidance.
- If the tool is unavailable, use `skills/mcp-setup/README.md` to restore the assistant MCP server before continuing.

## Skills

- `skills/docs-routing/README.md` - start here to load the right `extension-docs` content and reading order.
- `skills/mcp-setup/README.md` - restore the assistant MCP server when `extension-docs` is unavailable.
- `skills/args-evolution/README.md` - apply when editing `*Args.cs`, upgrades, collectors, or field metadata.
- `skills/readme-help/README.md` - apply before shipping `README.md` updates.
- `skills/platform-assistant/README.md` - apply when changing Assistant command logic or Assistant collectors.
- `skills/platform-autocad/README.md` - apply when changing AutoCAD command logic or AutoCAD collectors.
- `skills/platform-navisworks/README.md` - apply when changing Navisworks command logic or Navisworks collectors.
- `skills/platform-revit/README.md` - apply when changing Revit command logic, transactions, ValueCopy, or Revit collectors.
- `skills/platform-tekla/README.md` - apply when changing Tekla command logic or Tekla collectors.

## Developer documentation

For comprehensive guides on extension development, configuration classes (Args), field attributes, validation, and platform-specific patterns, see the [Extension Development Documentation](./docs/README.md).

- **Getting started?** -> [Quick Start Guide](./docs/dotnet/QUICK_START.md)
- **Building with patterns?** -> [Cookbook](./docs/dotnet/COOKBOOK.md)
- **Deep technical reference?** -> [Args Developer Guide](./docs/dotnet/ARGS_DEVELOPER_GUIDE.md)
- **Looking up syntax?** -> [Reference](./docs/dotnet/REFERENCE.md)
