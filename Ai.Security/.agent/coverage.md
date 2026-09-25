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
| Structure mapping | Full codebase tree | 20260925-214417 | Repo map built from structure only; no code logic reviewed yet |
| Secrets | StreamBIM credential storage | 20260925-214417 | SEC-001: Credentials stored in Windows Credential Manager, FTP uses TLS 1.2 Explicit - overall sound, noted minor defense-in-depth gaps |
| Secrets / Auth | Dalux API key handling | 20260925-214417 | SEC-002: Broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes args.ApiKey instead of looked-up apiKey variable); HttpClient gaps (no timeout, no cert validation callback) |
| Secrets | Commit history for secrets | 20260925-214417 | SEC-003: git log -p -S audit across all branches for password, secret, key, apiKey, PRIVATE KEY, connection, Token/AuthToken/Bearer/api_token - no secrets found in history |

## Backlog (not yet reviewed, highest risk first)

| Category | Area | Why | Hint |
|---|---|---|---|
| Auth | Dalux API authentication | HttpClient with X-API-Key header - check for certificate validation, timeout, and transport security | DaluxApiService.cs |
| Data protection | StreamBIM file operations | StreamBIM uploader/downloader handle user files - check path validation, injection, and local file access | StreamBimPathHelper.cs, FailedFile.cs |
| Data protection | Dalux file path handling | Dalux download resolves folder paths from user input before writing to disk - check for path traversal | DaluxCloudDownloadCommand.cs, ResolveFolderAsync |
| Config | GitHub workflow secrets access | sync-extension-docs.yml references secrets.EXTENSION_DOCS_SYNC_PRIVATE_KEY - verify least-privilege scoping | .github/workflows/sync-extension-docs.yml |
| Config | NuGet.config security | Global and per-project nuget.config files - check for insecure source URLs or credentials | nuget.config, Tekla/*/nuget.config |
| Config | Directory.Build.props/targets | Shared MSBuild props may contain sensitive defaults or insecure settings | Revit/ and Tekla/ Directory.Build.props, .targets |
| Injection | Build system command execution | ExtensionBuilder.csproj produces dotnet run output - check for unsanitized args passed to build commands | Build.cs, Build.Compile.cs, Build.Final.cs |
| Logging | PrintPDF telemetry/logging | PrintPDF project includes Telemetry.cs and SimpleLogger.cs - check for PII or secret leakage | PrintPDF/Telemetry.cs, PrintPDF/SimpleLogger.cs |
| Dependencies | .NET package versions | SBOM exists at SBOM/tools-extensions-public.cdx.json - review package versions for known vulnerabilities | SBOM/tools-extensions-public.cdx.json |
| Inconsistency | API version skew | Multiple API versions in DaluxApiService (5.0, 5.1, 5.2, 1.0, 1.2) - check for deprecated or inconsistent usage | DaluxApiService.cs line references |
| Inconsistency | Shared code in multiple projects | StreamBim/ is shared via Compile Include across Uploader/Downloader - verify security logic is consistent | StreamBim/**/*.cs references |
| Inconsistency | nuget.config duplicates | Tekla has per-project nuget.config files while root has one - verify all sources are identical | Tekla/*/nuget.config vs root nuget.config |
| Data protection | Error message exposure | DaluxApiService error messages include base URL and network diagnostic info in exception handlers | HandleException method in DaluxApiService.cs |
| Config | Build pipeline permissions | build-dotnet-changed.yml triggers on push to main and PR events - verify required reviewers/branch protection | .github/workflows/build-dotnet-changed.yml |
| Auth | Workflow token scoping | sync-extension-docs.yml uses create-github-app-token with write permissions on contents and PRs | .github/workflows/sync-extension-docs.yml lines 46-54 |
| Auth/Config | FluentFTP security | FTP protocol may lack TLS; FluentFTP has SecureAuth option - check if used | StreamBimFtpClientFactory.cs, StreamBIMUploader/Downloader |

