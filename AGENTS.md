# AGENTS

## Repository overview

This repo contains Assistant extensions for multiple integrations (Assistant desktop, AutoCAD, Revit, Tekla, Navisworks). Each integration has its own conventions and API usage, so follow the matching custom instructions before editing or adding extensions.

## Repository layout

- `src/Assistant/`, `src/AutoCAD/`, `src/Revit/`, `src/Tekla/`, `src/Navisworks/`: integration roots.
- `src/*/dotnet/`: C#/.NET extension projects grouped by integration.
- Each extension typically includes an args class, a command class, and documentation (see per-integration instructions).

## Custom instructions by integration

These files live in `.github/instructions/` and apply to matching paths:

- Shared UI conventions: `.github/instructions/ui-common.instructions.md` (applyTo `**/src/**/dotnet/**/*.cs`)
- Assistant .NET extensions: `.github/instructions/assistant-extensions-development.instructions.md` (applyTo `**/src/Assistant/dotnet/**/*.cs`)
- AutoCAD .NET extensions: `.github/instructions/autocad-extensions-development.instructions.md` (applyTo `**/src/AutoCAD/dotnet/**/*.cs`)
- Navisworks .NET extensions: `.github/instructions/navisworks-extensions-development.instructions.md` (applyTo `**/src/Navisworks/dotnet/**/*.cs`)
- Revit .NET extensions: `.github/instructions/revit-extensions-development.instructions.md` (applyTo `**/src/Revit/dotnet/**/*.cs`)
- Tekla .NET extensions: `.github/instructions/tekla-extensions-development.instructions.md` (applyTo `**/src/Tekla/dotnet/**/*.cs`)

Read the relevant instructions for the integration you are working on. They define required patterns (args/command/result), UI attributes, and integration-specific API expectations.
