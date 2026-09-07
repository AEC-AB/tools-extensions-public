# CW.Assistant.ExtensionDocs.Bundle

This package ships the raw extension implementation Markdown docs for coding agents and other local consumers.

## What it provides

- `contentFiles/any/any/Resources/ExtensionDocs/*.md`
- `contentFiles/any/any/Resources/ExtensionDocs/PLATFORM_GUIDES/*.md`
- A transitive MSBuild property, `$(CWAssistantExtensionDocsPath)`, that resolves to the docs directory in the restored NuGet package.

The package does not copy documentation files to the consumer's `bin` or `obj` directories. The files remain in the NuGet package cache.

## Source of truth

The package source of truth is the markdown docs in:

- `docs/dotnet/QUICK_START.md`
- `docs/dotnet/ARGS_DEVELOPER_GUIDE.md`
- `docs/dotnet/COOKBOOK.md`
- `docs/dotnet/REFERENCE.md`
- `docs/dotnet/PLATFORM_GUIDES/*.md`

The package is a thin wrapper around the repo docs and does not require a parsed JSON bundle.

## Consumption

Extension projects receive this package transitively through `CW.Assistant.Extensions.Contracts`. Read the version selected by the resolved dependency graph rather than selecting or checking for a newer documentation version independently.

The package can also be referenced directly when a standalone consumer needs the docs:

```xml
<ItemGroup>
  <PackageReference Include="CW.Assistant.ExtensionDocs.Bundle" Version="<version>" />
</ItemGroup>
```

Read the resolved documentation path from the generated MSBuild property:

```powershell
dotnet msbuild -getProperty:CWAssistantExtensionDocsPath
```

Agents can use the returned path to load the Markdown files directly from the restored NuGet package. The property is only set when the consumer has not already defined `CWAssistantExtensionDocsPath`, so a consumer can override it when needed.
