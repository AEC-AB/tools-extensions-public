# CW.Assistant.ExtensionDocs.Bundle

This package ships the raw extension implementation Markdown docs for Assistant MCP consumers.

## What it provides

- `contentFiles/any/any/Resources/ExtensionDocs/*.md`
- `contentFiles/any/any/Resources/ExtensionDocs/PLATFORM_GUIDES/*.md`
- A transitive MSBuild target that copies the docs to consumer output under:
  - `Resources/ExtensionDocs/`

## Source of truth

The package source of truth is the markdown docs in:

- `docs/dotnet/QUICK_START.md`
- `docs/dotnet/ARGS_DEVELOPER_GUIDE.md`
- `docs/dotnet/COOKBOOK.md`
- `docs/dotnet/REFERENCE.md`
- `docs/dotnet/PLATFORM_GUIDES/*.md`

The package is a thin wrapper around the repo docs and does not require a parsed JSON bundle.

## Consumption (CLI project)

Add a package reference in the consumer project:

```xml
<ItemGroup>
  <PackageReference Include="CW.Assistant.ExtensionDocs.Bundle" Version="<version>" />
</ItemGroup>
```

Then ensure your loader reads the copied markdown files from `Resources/ExtensionDocs/`.
