# Security review summary

<!-- pass-status: written by the runner -->
> **Incomplete pass.** Run 20260926-015629 did not finish: the model server stopped answering mid-run. The findings below are what it got to; the rest of the codebase is not reviewed yet. The next nightly sweep picks this repo up again.
<!-- /pass-status -->

> **Incomplete pass.** Run 20260925-214417 did not finish: the model server stopped answering mid-run. The findings below are what it got to; the rest of the codebase is not reviewed yet. The next nightly sweep picks this repo up again.

Rewritten by the AEC security review agent on every run. Internal only.

## Scope

Run `20260925-214417` — first pass. Reviewed the StreamBIM credential storage and FTP transfer security area, the Dalux API key handling area, a full git history audit for secrets across all branches, and the Dalux API authentication area.

Run `20260926-015629` — incremental. Reviewed the AutoCAD LISPRunner and RunCommand extensions (Critical RCEs), the DaluxCloudDownload path handling (Medium path traversal), and CI/CD workflows and scripts (verified input validation, path traversal protections, private content scanning).

## Counts

| Severity | Open | Total |
|---|---|---|
| Critical | 2 | 2 |
| High | 1 | 1 |
| Medium | 1 | 1 |
| Low | 1 | 1 |
| Info | 1 | 1 |
| **Total** | **6** | **6** |

| Status | Count |
|---|---|
| Open | 6 |

## Top open findings

1. **SEC-004** (Critical) — LISPRunner arbitrary Lisp execution: both script-file and inline modes pass user-supplied content to AutoCAD's `SendStringToExecute()` with zero sanitization. An attacker who can supply the script path or content gains arbitrary AutoCAD Lisp API access.

2. **SEC-005** (Critical) — RunCommand arbitrary command execution: user-supplied command strings passed directly to AutoCAD's `SendCommand()` with no whitelist, validation, or sanitization. Multi-line input allows command chaining and full AutoCAD API access.

3. **SEC-002** (High) — Dalux API key handling: broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes `args.ApiKey` with default value `"Dalux API Key"` instead of the looked-up credential variable to `DaluxApiService`); HttpClient gaps (no timeout, no certificate validation callback) in both upload and download services.

4. **SEC-006** (Medium) — DaluxCloudDownload path traversal via server-supplied relative path: `file.RelativePath` and `file.FileName` from the Dalux API are used directly in `Path.Combine()` without traversal validation. A compromised Dalux API could inject `../` sequences.

5. **SEC-001** (Low) — StreamBIM credential storage: credentials properly stored in Windows Credential Manager (DPAPI), FTP uses TLS 1.2 Explicit. Minor defense-in-depth gaps: no explicit certificate validation callback on FTP client, password held as plaintext string in memory.

## What changed since previous run

Run `20260926-015629` is an incremental pass. Three new Critical and Medium findings were filed (SEC-004, SEC-005, SEC-006). CI/CD workflows and scripts were verified as well-implemented. The backlog has been expanded with additional items identified during review.

## Areas not yet covered

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
