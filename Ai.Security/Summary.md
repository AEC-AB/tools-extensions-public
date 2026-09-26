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
| Low | 2 | 2 |
| Info | 2 | 2 |
| **Total** | **8** | **8** |

| Status | Count |
|---|---|
| Open | 8 |

## Top open findings

1. **SEC-004** (Critical) — LISPRunner arbitrary Lisp execution: both script-file and inline modes pass user-supplied content to AutoCAD's `SendStringToExecute()` with zero sanitization. An attacker who can supply the script path or content gains arbitrary AutoCAD Lisp API access.

2. **SEC-005** (Critical) — RunCommand arbitrary command execution: user-supplied command strings passed directly to AutoCAD's `SendCommand()` with no whitelist, validation, or sanitization. Multi-line input allows command chaining and full AutoCAD API access.

3. **SEC-002** (High) — Dalux API key handling: broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes `args.ApiKey` with default value `"Dalux API Key"` instead of the looked-up credential variable to `DaluxApiService`); HttpClient gaps (no timeout, no certificate validation callback) in both upload and download services.

4. **SEC-006** (Medium) — DaluxCloudDownload path traversal via server-supplied relative path: `file.RelativePath` and `file.FileName` from the Dalux API are used directly in `Path.Combine()` without traversal validation. A compromised Dalux API could inject `../` sequences.

5. **SEC-001** (Low) — StreamBIM credential storage: credentials properly stored in Windows Credential Manager (DPAPI), FTP uses TLS 1.2 Explicit. Minor defense-in-depth gaps: no explicit certificate validation callback on FTP client, password held as plaintext string in memory.

## What changed since previous run

Run `20260926-015629` is an incremental pass. Three new Critical and Medium findings were filed (SEC-004, SEC-005, SEC-006). CI/CD workflows and scripts were verified as well-implemented. The backlog has been expanded with additional items identified during review.

Run `20260926-215844` — incremental. Reviewed all seven Tekla extension file operation modules (IFCExport, ReadIn, RefreshReferenceModels, SaveModel, SetSelectionFilter, WriteOut, ZoomToSelected). One new Low finding: SEC-008 (IFCExport path traversal via unvalidated output file path). All other Tekla extensions have safe file operations with no user-typed paths or injection vectors.

Run `20260926-215844` session 6 — incremental. Replaced the Backlog row "Tekla extensions file operations" with three smaller rows by sub-project: (1) IFCExport path traversal, (2) five file operation extensions, (3) SetSelectionFilter. Reviewed the first row (IFCExport = SEC-008): re-reviewed all four IFCExport files (TeklaIFCExportCommand.cs, TeklaIFCExportArgs.cs, IFCExportConfig.cs, GlobalUsings.cs), confirmed the finding is complete and accurate, moved to Reviewed. The second and third rows remain in the Backlog for a future session.

Run `20260926-215844` session 8 — incremental. Replaced the old single Backlog row "Tekla file operations (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected)" with three smaller rows: (1) five file operation extensions safe assessment, (2) macro builder helper string safety, (3) extension result pattern variance. Reviewed the first row (five extensions = SEC-009): traced all ten Command.cs and Args.cs files, confirmed all safe - SDK APIs only, no user file paths, hardcoded string literals. Moved to Reviewed.

## Areas not yet covered

- StreamBIM file path validation and injection (StreamBimPathHelper.cs, FailedFile.cs)
- Tekla macro builder helper string safety (CW.Assistant.Extensions.Tekla.Helpers/TeklaMacroBuilderHelper.cs)
- Tekla extension result pattern variance (Result.Text vs Result.Empty.Succeeded consistency)
- Navisworks extension file operations (logging and error handling)
- PrintPDF telemetry and logging (PII or secret leakage)
- StreamBIM diagnostics logging (sensitive data in output)
- Error handling pattern inconsistency (swallowed exceptions, info leakage)
- FluentFTP security (TLS enforcement)
- GitHub workflow secrets access
- Build pipeline permissions
