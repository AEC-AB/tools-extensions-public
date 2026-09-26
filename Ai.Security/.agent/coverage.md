# Coverage ledger

## Source commits at last run

| Repo | Branch | Commit | Run |
|---|---|---|---|
| tools-extensions-public | n/a (first run) | n/a | 20260925-214417 |

## Repo map

| Repo | Path | What it is | Entry points, config, pipelines |
|---|---|---|---|
| tools-extensions-public | `src/AutoCAD/dotnet/` | AutoCAD .NET extensions | LISPRunner.cs, RunCommand.cs (CLI entry points) |
| tools-extensions-public | `src/Navisworks/dotnet/` | Navisworks .NET extensions | StreamBIM* projects (StreamBIMUploader, StreamBIMDownloader), DaluxCloudUpload, DaluxCloudDownload (CLI extensions with FluentFTP/HttpClient) |
| tools-extensions-public | `src/Revit/dotnet/` | Revit .NET extensions | DWGExport, LoadFamily, NWCExport, PrintPDF, RevitExtensionDemo, ZoomToSelected (CLI extensions; PrintPDF has its own SimpleLogger, Telemetry) |
| tools-extensions-public | `src/Tekla/dotnet/` | Tekla .NET extensions | IFCExport, ReadIn, RefreshReferenceModels, SaveModel, SetSelectionFilter, WriteOut, ZoomToSelected |
| tools-extensions-public | `src/Assistant/dotnet/` | Assistant CLI framework + extensions | Assistant.csproj (shared framework), StreamBIM/ shared services, DaluxCloudUpload/Download, StreamBIMUploader/Downloader |
| tools-extensions-public | `build/` | Build system (dotnet CLI tool) | ExtensionBuilder.csproj, ExtensionBuilder.slnx, Build.Compile.cs, Build.ComputeChangedProjects.cs, Build.Final.cs, Build.cs |
| tools-extensions-public | `.github/workflows/` | CI/CD pipelines | build-dotnet-changed.yml, sync-extension-docs.yml, validate-extension-docs.yml |
| tools-extensions-public | `.github/scripts/` | CI/CD scripts | Sync-ExtensionDocs.ps1, Test-MarkdownLinks.ps1, tests/Test-Sync-ExtensionDocs.ps1 |
| tools-extensions-public | `nuget.config` | NuGet package source config (global) | n/a |
| tools-extensions-public | `src/Revit/dotnet/Directory.Build.props` | MSBuild props (shared Revit project config) | n/a |
| tools-extensions-public | `src/Revit/dotnet/Directory.Build.targets` | MSBuild targets (shared Revit build config) | n/a |
| tools-extensions-public | `src/Tekla/dotnet/Directory.Build.props` | MSBuild props (shared Tekla project config) | n/a |
| tools-extensions-public | `src/Tekla/dotnet/Directory.Build.targets` | MSBuild targets (shared Tekla build config) | n/a |
| tools-extensions-public | `src/Tekla/dotnet/*/nuget.config` | Per-project NuGet config (multiple Tekla projects) | n/a |

## Reviewed

| Category | Area | Run | Notes |
|---|---|---|---|
| Injection / RCE | LISPRunner command injection | 20260926-015629 | SEC-004: Both script-file and inline modes pass user content to AutoCAD via SendStringToExecute() with zero sanitization |
| Injection / RCE | RunCommand command injection | 20260926-015629 | SEC-005: User-supplied command strings passed directly to AutoCAD SendCommand() with no whitelist or validation |
| Injection / Path Traversal | DaluxCloudDownload path traversal | 20260926-015629 | SEC-006: Server-supplied RelativePath and FileName used in Path.Combine without traversal validation |
| CI/CD security | GitHub workflows and scripts | 20260926-015629 | Verified input validation, security scanning, and token scoping in sync-extension-docs.yml, validate-extension-docs.yml, and build-dotnet-changed.yml. Sync-ExtensionDocs.ps1 has path traversal protection for .nupkg entries and private content scanning. |
| Injection / Path Traversal | StreamBIM file path handling | 20260926-215844 | Reviewed StreamBimPathHelper.cs, autofill collectors, and file transfer services - path traversal protection in CreateLocalPath (bounds check), NormalizeRelativePath (rejects . and .. segments), and autofill input validation. No new finding. |
| Injection | StreamBIM autofill collectors | 20260926-215844 | All four autofill collectors validated - Uploader calls NormalizeRelativePath() which rejects . and .. segments; Downloader autofill is read-only FTP listing (no write risk); project root collector only lists FTP root. No new finding. |
| Config | NuGet.config security | 20260926-215844 | Reviewed root nuget.config and 7 Tekla per-project nuget.config files - all reference official NuGet source (api.nuget.org/v3), use <clear/> to prevent inheritance, no credentials or third-party sources. No finding. |
| Config | Directory.Build.props/targets | 20260926-215844 | Reviewed Revit and Tekla Directory.Build.props and .targets - no secrets, no insecure defaults, proper package isolation (PrivateAssets=all). Minor hygiene: CW.Assistant.Extensions packages use wildcard version 26.* (reproducibility concern). No finding. |
| Dependencies | .NET package versions | 20260926-215844 | SBOM reviewed - 7 NuGet packages. Microsoft.CSharp 4.7.0 is old (.NET Core 3.1 era). All others current. No CVE claims possible offline. Transitive deps not in SBOM. Dependencies.md updated with full detail. |
| Inconsistency | API version skew | 20260926-215844 | Reviewed DaluxApiService.cs in both upload and download projects - uses 5 API versions (1.0, 1.2, 5.0, 5.1, 5.2) consistently across both services. No cross-service version mismatch. Info: older versions (1.0, 1.2) could break silently if Dalux deprecates them. No new finding. |
| Inconsistency | Shared code in multiple projects | 20260926-215844 | StreamBim/ is a single shared library (CredentialProvider, FtpClientFactory, PathHelper, ExceptionHelper, Models/UserCredentials) referenced by both Uploader and Downloader via Compile Include. Both consumers use identical code. No inconsistency. Security logic is centralized and consistent. No new finding. |
| Data protection | Error message exposure | 20260926-215844 | SEC-007: HandleException<T> in DaluxApiService.cs lines 122-130 includes _baseUrl ("https://node1.field.dalux.com/service/api") in error messages returned to client. LOW severity information disclosure. Same code in DaluxCloudDownload version. Fix: remove _baseUrl from returned error text. |
| Inconsistency | nuget.config duplicates | 20260926-215844 | Reviewed 8 nuget.config files (1 root + 7 Tekla). All reference official NuGet source (api.nuget.org/v3) with <clear/> to prevent inheritance. SetSelectionFilter/nuget.config uses different whitespace/indentation but is semantically identical. No credential or third-party source differences. LOW hygiene: inconsistent formatting across 8 identical-semantic files. No new finding. |
| Injection | Revit extensions file operations | 20260926-215844 | Reviewed all five Revit Command.cs files (DWGExport, LoadFamily, NWCExport, PrintPDF, ZoomToSelected) plus ExportFileHelpers.cs and RevitExtensionDemoCommand.cs. File operations are UI-driven (file/folder picker), not user-typed. Output names are sanitized: DWGExport/NWCExport use `Regex.Replace(fileName, "[<>:\"/\\|?*]", "_")`; PrintPDF uses `ExportFileHelpers.SanitizeFileName()` (replaces all invalid filename chars with `_`) and GUID-based temp files. No path traversal, command injection, or SQL injection vectors. All path operations are local file system, no remote calls. No new finding. |
| Injection / Path Traversal | Tekla IFCExport path traversal | 20260926-215844 | SEC-008: Output file path from XML config (deserialized via XmlSerializer) or SaveFileField override goes through Path.GetFullPath() with no traversal validation and automatic Directory.CreateDirectory(). Inconsistent with Revit DWGExport/NWCExport which sanitize filenames. Reviewed full IFCExport codebase: TeklaIFCExportCommand.cs, TeklaIFCExportArgs.cs, IFCExportConfig.cs, GlobalUsings.cs - all code paths traced. |
| Injection / Path Traversal | Tekla IFCExport path traversal (re-verify) | 20260926-215844 | Session 6: Re-reviewed all IFCExport files, confirmed SEC-008 finding is complete and accurate. Two converging input vectors traced (ExportConfigFilePath→File.ReadAllBytes→XmlSerializer→IFCExportConfig.OutputFile; FilePathOverride→exportConfig.OutputFile), user-editable XML content confirmed, Directory.CreateDirectory auto-creates parent dirs. No additional findings in IFCExport (XmlSerializer safe against XXE by default, BasePointName validated via GetBasePointByName before use). |
| Injection | Tekla SetSelectionFilter | 20260926-215844 | Reviewed SelectObjectFromSelectionFilterCommand.cs, SelectionFilterCollector.cs, SelectObjectFromSelectionFilterArgs.cs. FilterName validated for null/whitespace (line 19), then passed to Tekla SDK's GetObjectsByFilterName() API (line 32). No file system, subprocess, or shell operations. SelectionFilterCollector constrains input to valid Tekla filter names from model property file directories. No finding. |

## Backlog (not yet reviewed, highest risk first)

| Category | Area | Why | Hint |
|---|---|---|---|
| Injection | Tekla file operations (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected) | 20260926-215844 | Five extensions share same pattern: no user file path input at all. ReadIn uses ModelHistory API, RefreshReferenceModels iterates ReferenceModel objects, SaveModel calls ModelHandler.Save(), WriteOut uses macro runner, ZoomToSelected uses drawing UI APIs. Hard part: need to verify none of the macro builder strings (e.g., "acmdRunPluginMethod", "SharingToolsFeature") could be attacker-controlled via a separate path. Fix: confirm all macro callbacks are hardcoded string literals. |
| Data protection | Navisworks extensions | 20260926-215844 | ClashDetectiveRunner, OpenDocument, SaveDocumentAs, ZoomToSelected handle model files - check logging and error handling | Navisworks/dotnet/*/ |
| Logging | StreamBIM diagnostics logging | StreamBIMUploadDiagnostics logs file paths and operations - check for sensitive data in diagnostics output | StreamBIMUploader/Services/StreamBimUploadDiagnostics.cs |
| Inconsistency | Error handling patterns | Varied catch/return patterns across projects - check for swallowed exceptions or info leakage | Compare error handling in DaluxApiService vs StreamBimFileTransferService |
| Auth/Config | FluentFTP security | FTP protocol may lack TLS; FluentFTP has SecureAuth option - check if used | StreamBimFtpClientFactory.cs, StreamBIMUploader/Downloader |
| Config | GitHub workflow secrets access | sync-extension-docs.yml references secrets.EXTENSION_DOCS_SYNC_PRIVATE_KEY - verify least-privilege scoping | .github/workflows/sync-extension-docs.yml |
| Config | Build pipeline permissions | build-dotnet-changed.yml triggers on push to main and PR events - verify required reviewers/branch protection | .github/workflows/build-dotnet-changed.yml |
| Logging | PrintPDF telemetry/logging | PrintPDF project includes Telemetry.cs and SimpleLogger.cs - check for PII or secret leakage | PrintPDF/Telemetry.cs, PrintPDF/SimpleLogger.cs |

