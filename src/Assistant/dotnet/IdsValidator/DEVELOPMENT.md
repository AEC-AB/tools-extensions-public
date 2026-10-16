# Development and verification

This is a pure C# Assistant extension. Prerequisites are .NET SDK 10 and the repository's NuGet access. It requires no setup scripts or separate runtime installation.

## Shared build settings

The project imports the integration's existing `Directory.Build.props` and `Directory.Build.targets`. Target framework, platform, language version, Assistant SDK version and assembly naming remain owned by those shared files. Explicit SDK imports let Assistant MCP discover the shared references and ensure shared assembly naming is applied before the SDK calculates project-reference paths. No shared settings are copied into a local `Configurations.targets` file and no shared file is changed.

From the repository root:

```powershell
dotnet build src/Assistant/dotnet/IdsValidator/IdsValidator.csproj --configuration Release
dotnet publish src/Assistant/dotnet/IdsValidator/IdsValidator.csproj --configuration Release --output artifacts/IdsValidator
```

The published folder contains the C# extension and its NuGet dependency assemblies. Generated output, private `.extension` registration metadata and test downloads are ignored by Git.

## Tests

### Automatic unit tests

```powershell
dotnet test tests/IdsValidator.UnitTests/IdsValidator.UnitTests.csproj --configuration Release
```

The xUnit suite uses small IFC and IDS fixtures authored in C#. It tests the production validation service, managed engine and report writers without downloading the buildingSMART corpus. Coverage includes pass/fail outcomes, actual/expected evidence, version selection, cardinalities, invalid and missing inputs, cancellation, source integrity, Markdown/JSON/HTML output and BCF element links. These tests need the .NET SDK and restored NuGet dependencies; they need no archive, external process or model data download.

`.github/workflows/validate-ids-validator.yml` runs these tests for relevant pull requests, verifies extension publishing and uploads TRX results. It never invokes the conformance runner.

### On-demand buildingSMART conformance

Run the **IDS buildingSMART conformance (on demand)** workflow manually in GitHub Actions, selecting the desired branch. It has only a `workflow_dispatch` trigger, so pull requests and pushes do not start it. Before the workflow is merged into the default branch, use the local command below.

```powershell
dotnet run --configuration Release --project tests/IdsValidator/IdsValidator.Conformance.csproj -- tests/IdsValidator/.cache/cases tests/IdsValidator/artifacts
```

The C# runner downloads the official buildingSMART corpus at commit `870f9c4e6e8f414e737b4d84ca1aa9b46fc6c8f3`, verifies archive SHA-256 `6035F80D1C40104AC7AEC8896954D40BFDA0A2F22A75CBF12A95283E7C2AB8B8`, and validates all 334 paired cases. Only IFC/IDS data is extracted; no code from the ZIP is executed. It also generates integration regressions for version selection, malformed XML, schema input instead of IDS, missing files, malformed IFC, IFC4X3, material properties and IFC2X3 mappings. No source project IFC/IDS files are included or altered.

The runner calls the production `ValidationService`, managed engine, report writer and BCF writer. It checks expected statuses, selected diagnostics, input hashes and BCF schema/reference integrity. Results are written to `test-results.json` with per-case JSON, HTML, Markdown and BCF reports. `.github/workflows/validate-ids-conformance.yml` runs this suite only on demand and uploads its machine-readable result.

Assistant MCP is also used to build and run the production command, including a valid model, three known violations, invalid IDS and missing files. The PR evidence records those command statuses independently of the console tests. There is no production test-suite UI.

## Standards boundaries

Original IDS bytes are audited with buildingSMART ids-lib before xBim reads them. `xsi:schemaLocation` selects supported IDS 1.0; author revision metadata does not select a schema. Explicit draft, future or ambiguous versions fail without conversion. Missing declarations are audited as IDS 1.0 with a warning.

xBim runs with full entity evidence, exact entity types, derived attributes disabled, schema migration disabled, strict auditing, no specification filter, no runtime tokens and no entity exclusions. IFC files open in read-only, in-memory mode. Incompatible IFC-version metadata is reported without skipping specifications. Cancellation is checked after upstream calls because upstream may convert cancellation into a result.

Overall technical errors take precedence over requirement failures and successes. Every specification, entity and message is inspected rather than trusting only the upstream overall status. Empty optional specifications pass; empty required specifications produce explicit cardinality findings. Multiple messages are grouped by their original facet reference, retaining stable requirement ordinals. Original facet XML preserves complete expected restrictions, while native typed observations and related entity identities preserve actual evidence. BCF links always identify the tested entity, rather than a property mentioned in a diagnostic.

## Scoped xBim conformance corrections

`StandardFacetBinders.cs` extends xBim 1.0.187 using its supported binder registration and protected constraint/datatype helpers. Source base: commit `e370372b19deef3bedfca5f20f2d1f9056cab274`, AGPL-3.0-only, copyright xbim Ltd and contributors.

- IFC2X3 entity requirements use the same schema occurrence/type mapping already used by upstream applicability. Native predefined-type checks remain intact; other schemas use native validation.
- Property facets can read material and profile property definitions, including IFC2X3 `IfcExtendedMaterialProperties` and IFC4 material properties. Selection also includes these resources. Ordinary object/type properties continue to use native validation.

These address four official expected-pass cases that initially failed in the unmodified engine. Additional negative regressions check incorrect values, datatypes and mapped classes, and material property applicability. No test case names or project identifiers occur in production logic. Conformance evidence is not buildingSMART certification.

- IFC2X3 attribute-only applicability uses native attribute checks over actual model entities, avoiding upstream class discovery that includes types absent from IFC2X3. Attribute filters after resource-property selections also retain native checking semantics.
