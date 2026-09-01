## name: docs-routing
description: Use this skill when you need extension framework guidance before changing code in this repository


# Docs routing

Use this skill when you need extension framework guidance before changing code in this repository.

## Resolve the documentation root

1. Open the active project's `obj/project.assets.json`. Restore the project first if this file has not been generated.
2. In `libraries`, find `CW.Assistant.ExtensionDocs.Bundle/<version>`. This transitive dependency comes through `CW.Assistant.Extensions.Contracts`; use the exact resolved version recorded here.
3. For each local NuGet root in `packageFolders`, append `cw.assistant.extensiondocs.bundle/<version>/contentFiles/any/any/Resources/ExtensionDocs`.
4. Use the first existing directory as `ExtensionDocsRoot`. If none exists, package restore is incomplete; do not substitute docs from another version.

Read and search Markdown beneath `ExtensionDocsRoot` directly. This route is offline and does not check for newer documentation.

When maintaining the documentation package itself, edit `docs/dotnet/`; it is the source copied into the bundle.

After resolving `ExtensionDocsRoot`, read `ExtensionDocsRoot/AGENT.md` for the canonical reading order and platform guide selection.

