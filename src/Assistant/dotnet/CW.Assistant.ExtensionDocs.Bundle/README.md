# CW.Assistant.ExtensionDocs.Bundle

This package ships a generated `extension-docs.bundle.json` for Assistant MCP consumers.

## What it provides

- `contentFiles/any/any/Resources/ExtensionDocs/extension-docs.bundle.json`
- A transitive MSBuild target that copies the bundle to consumer output under:
  - `Resources/ExtensionDocs/extension-docs.bundle.json`

## Source of truth

The bundle is generated from markdown docs in:

- `docs/dotnet/QUICK_START.md`
- `docs/dotnet/ARGS_DEVELOPER_GUIDE.md`
- `docs/dotnet/COOKBOOK.md`
- `docs/dotnet/REFERENCE.md`
- `docs/dotnet/PLATFORM_GUIDES/*.md`

Generation script:

- `build/Generate-ExtensionDocsBundle.ps1`

Recommended packaging flow:

1. Generate the bundle.
2. Pack this project.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/Generate-ExtensionDocsBundle.ps1
dotnet pack src/Assistant/dotnet/CW.Assistant.ExtensionDocs.Bundle/CW.Assistant.ExtensionDocs.Bundle.csproj -c Release
```

## Consumption (CLI project)

Add a package reference in the consumer project:

```xml
<ItemGroup>
  <PackageReference Include="CW.Assistant.ExtensionDocs.Bundle" Version="<version>" />
</ItemGroup>
```

Then ensure your loader reads:

- `Resources/ExtensionDocs/extension-docs.bundle.json` from output/base directory.
