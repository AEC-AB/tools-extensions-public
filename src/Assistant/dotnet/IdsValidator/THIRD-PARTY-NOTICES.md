# Third-party components

This extension follows the repository's AGPL-3.0 licence. It uses the following .NET libraries through NuGet; their licences and notices continue to apply.

| Component | Version | Licence / source |
|---|---|---|
| Xbim.IDS.Validator.Core and Common | 1.0.187 | AGPL-3.0-only, copyright xbim Ltd. [Source and licence](https://github.com/xBimTeam/Xbim.IDS.Validator/tree/e370372b19deef3bedfca5f20f2d1f9056cab274). |
| Xbim.Essentials and model libraries | 6.0.578, transitive | [CDDL source and licence](https://github.com/xBimTeam/XbimEssentials). |
| Xbim.InformationSpecifications | 1.0.16, transitive | [CDDL-1.0 source](https://github.com/CBenghi/Xbim.Xids). |
| ids-lib | 1.0.124 | [MIT source and licence](https://github.com/buildingSMART/IDS-Audit-tool). |
| Microsoft.Extensions.Logging | 9.0.10 | [MIT source](https://github.com/dotnet/runtime). |
| Assistant extension SDK | Shared repository version | Supplied under its own package terms. |

`StandardFacetBinders.cs` adapts property-checking logic and the schema mapping helpers from the xBim source pinned above. Copyright xbim Ltd and contributors; AGPL-3.0-only. The changes complete IFC2X3 mapped entity requirements and material/profile property access, without replacing the engine's other facet implementations. See DEVELOPMENT.md.

BCF 2.1 schemas are from the [buildingSMART BCF XML specification](https://github.com/buildingSMART/BCF-XML/tree/release_2_1).

No runtime executables, dependency assemblies, source archives, package caches or IFC/IDS project data are committed with this extension. Standard .NET restore and publish assemble the dependencies for distribution.
