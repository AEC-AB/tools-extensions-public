# Pull request summary

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run `20260926-215844` is an incremental pass on `tools-extensions-public`. Two new findings filed across two sessions: SEC-007 (Low, base URL in error messages) and SEC-008 (Low, Tekla IFCExport path traversal). The Run `20260926-015629` findings (SEC-004 through SEC-006) are carried forward. Sweep approximately fifty percent complete.

## New findings

- SEC-007 (Low) — DaluxApiService base URL exposed in error messages — `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs` lines 122-130 and `src/Assistant/dotnet/DaluxCloudDownload/Services/DaluxApiService.cs` — HandleException<T> includes `_baseUrl` and network diagnostic information in error messages returned to the client. Infrastructure endpoint disclosure.

- SEC-008 (Low) — Tekla IFCExport path traversal via unvalidated output file path — `src/Tekla/dotnet/IFCExport/TeklaIFCExportCommand.cs` lines 20-39 — Output file path from XML config or SaveFileField override goes through Path.GetFullPath() with no traversal validation; Directory.CreateDirectory() auto-creates parents. Inconsistent with Revit DWGExport/NWCExport which sanitize filenames via Regex.Replace.

## Status changes

None — all findings remain Open.

## Reviewer attention

- SEC-007: The base URL in error messages is Low severity since it exposes an infrastructure endpoint but no credentials. Reviewers may consider this acceptable risk given it is already on the public internet.

- SEC-008: Local CAD extension with no network exposure, but the path traversal risk is real if an attacker supplies a crafted XML config. The fix involves validating the output path stays within an expected base directory and sanitizing filenames like the Revit extensions do.

## Not yet covered

- NuGet.config security (insecure source URLs or credentials)
- Directory.Build.props/targets (sensitive defaults)
- Build system command execution (unsanitized args in Build.cs)
- PrintPDF telemetry and logging (PII or secret leakage)
- .NET package versions (SBOM review)
- API version skew in DaluxApiService (deprecated versions)
- Shared code consistency across StreamBIM projects
- Navisworks extension file operations (logging and error handling)
- StreamBIM diagnostics logging (sensitive data in output)
- Error handling pattern inconsistency (swallowed exceptions, info leakage)
- FluentFTP security (TLS enforcement)
- GitHub workflow secrets access
- Build pipeline permissions
