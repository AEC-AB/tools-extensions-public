---
name: docs-routing
description: Resolve the restored ExtensionDocs bundle before using extension guidance.
---

# Docs routing

Resolve the documentation root before reading platform guidance.

1. Identify the active project file (`.csproj`) and work from its directory. Ensure `obj/project.assets.json` exists; if it is missing, run:

```powershell
dotnet restore "<project.csproj>"
```

2. Query the generated documentation path first:

```powershell
dotnet msbuild "<project.csproj>" -getProperty:CWAssistantExtensionDocsPath
```

Record the raw property result. Resolve a relative value against the project directory. Accept the value only when it is an existing directory containing `AGENT.md`; use it as `ExtensionDocsRoot`.

3. When the property is empty or fails that validation, use the active restore metadata:

- Search only `obj/project.assets.json` for `CW.Assistant.Extensions.Docs/` and capture the exact resolved version from the library key, for example `CW.Assistant.Extensions.Docs/26.10.0`.
- Search the same file for `packageFolders` and capture every package root.
- For each package root, construct:

```text
<packageRoot>/cw.assistant.extensions.docs/<resolvedVersion>/contentFiles/any/any/Resources/ExtensionDocs
```

Accept a candidate only when it is an existing directory containing `AGENT.md`; use it as `ExtensionDocsRoot`.

4. If no candidate passes validation, stop and report all of the following:

- The raw `CWAssistantExtensionDocsPath` property result.
- The exact `CW.Assistant.Extensions.Docs/<version>` entry found, or that the docs package entry is missing.
- Every `packageFolders` root examined.
- The resolved platform extension package, for example `CW.Assistant.Extensions.Revit.2026`, and `CW.Assistant.Extensions.Contracts` versions.
- This recovery guidance:

```powershell
dotnet restore "<project.csproj>" --force-evaluate
```

If the docs package entry remains absent, update the platform or Contracts package reference to a version that declares `CW.Assistant.Extensions.Docs`, then run the restore command again.

5. Read `<ExtensionDocsRoot>/AGENT.md` and follow its documented reading order. Read the required linked documents before implementing or reviewing extension code.

Completion requires an existing `ExtensionDocsRoot` containing `AGENT.md`, the exact docs package version sourced from the active project's property or restore metadata, and the `AGENT.md` reading order completed before code work begins.
