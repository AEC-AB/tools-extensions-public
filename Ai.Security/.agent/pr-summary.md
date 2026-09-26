# Pull request summary

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run `20260926-015629` is an incremental pass on `tools-extensions-public`. No changes were detected in `context/changes.md`, but the full sweep continued. Two Critical RCE vulnerabilities were found in AutoCAD extensions (LISPRunner, RunCommand), one Medium path traversal in DaluxCloudDownload, and CI/CD workflows were verified as well-implemented. Headline: two Critical, one Medium, and three previously filed findings (High, Low, Info). Total six findings. Sweep twenty percent complete.

## New findings

- SEC-004 (Critical) — LISPRunner arbitrary AutoCAD Lisp execution — `src/AutoCAD/dotnet/LISPRunner/LISPRunnerCommand.cs` lines 20-57 — Both script-file and inline modes call `SendStringToExecute()` with user-supplied content (file path or inline string) and zero sanitization. Full AutoCAD Lisp API access.

- SEC-005 (Critical) — RunCommand arbitrary AutoCAD command injection — `src/AutoCAD/dotnet/RunCommand/RunCommandCommand.cs` lines 81-86 — User-supplied command strings passed directly to AutoCAD `SendCommand()` with no whitelist, validation, or sanitization. Multi-line input allows command chaining.

- SEC-006 (Medium) — DaluxCloudDownload path traversal via server-supplied relative path — `src/Assistant/dotnet/DaluxCloudDownload/DaluxCloudDownloadCommand.cs` lines 368-378, 314-316 — Server-supplied `RelativePath` and `FileName` used directly in `Path.Combine()` without traversal validation. Supply-chain attack vector if Dalux API is compromised.

## Status changes

None — all findings remain Open. The context/changes.md was empty, indicating no user changes to the codebase.

## Reviewer attention

- SEC-004 and SEC-005 are Critical RCE vulnerabilities in the AutoCAD extensions. These allow arbitrary AutoCAD command execution from user-supplied input. While intentional design may be to let users run arbitrary commands (the tools are CLI utilities), the security risk is that any script or file dropped in the working directory could execute. Reviewers should decide if command whitelisting, user confirmation, or signed scripts are required.

- SEC-006 is a Medium severity supply-chain risk. The path values come from the Dalux API, not the user directly, but a compromised Dalux server could inject `../` sequences. The fix is straightforward (validate resolved paths remain within the output folder).

- CI/CD workflows (sync-extension-docs.yml, validate-extension-docs.yml, build-dotnet-changed.yml) and scripts (Sync-ExtensionDocs.ps1, Test-MarkdownLinks.ps1) were reviewed and found to have proper input validation, path traversal protections, and private content scanning. No issues filed here.

## Not yet covered

- StreamBIM file path validation and injection (StreamBimPathHelper.cs, FailedFile.cs)
- StreamBIM autofill collectors (path validation before use)
- NuGet.config security (insecure source URLs or credentials)
- Directory.Build.props/targets (sensitive defaults)
- Build system command execution (unsanitized args in Build.cs)
- PrintPDF telemetry and logging (PII or secret leakage)
- .NET package versions (SBOM review)
- API version skew in DaluxApiService (deprecated versions)
- Shared code consistency across StreamBIM projects
- Revit extension file operations (injection)
- Tekla extension file operations (injection)
- Navisworks extension file operations (logging and error handling)
- StreamBIM diagnostics logging (sensitive data in output)
- Error handling pattern inconsistency (swallowed exceptions, info leakage)
- FluentFTP security (TLS enforcement)
