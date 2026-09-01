## name: docs-routing
description: Use this skill when you need extension framework guidance before changing code in this repository


# Docs routing

Use this skill when you need extension framework guidance before changing code in this repository.

## Resolve the documentation root

1. If the active project's `obj/project.assets.json` is missing, restore the project first. Then search the file for the `libraries` entry `CW.Assistant.ExtensionDocs.Bundle/<version>` (for example, `rg -n '"CW\.Assistant\.ExtensionDocs\.Bundle/' obj/project.assets.json`) and capture the exact resolved version without reading the whole file.
2. This transitive dependency comes through `CW.Assistant.Extensions.Contracts`; use the exact resolved version recorded in that entry.
3. For each local NuGet root in `packageFolders`, append `cw.assistant.extensiondocs.bundle/<version>/contentFiles/any/any/Resources/ExtensionDocs`.
4. Use the first existing directory as `ExtensionDocsRoot`. If none exists, package restore is incomplete; do not substitute docs from another version.

Read and search Markdown beneath `ExtensionDocsRoot` directly. This route is offline and does not check for newer documentation.

When maintaining the documentation package itself, edit `docs/dotnet/`; it is the source copied into the bundle.

After resolving `ExtensionDocsRoot`, read `ExtensionDocsRoot/AGENT.md` for the canonical reading order and platform guide selection.

