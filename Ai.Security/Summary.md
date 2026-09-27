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
| Medium | 2 | 2 |
| Low | 4 | 4 |
| Info | 2 | 2 |
| **Total** | **11** | **11** |

| Status | Count |
|---|---|
| Open | 11 |

## Top open findings

1. **SEC-004** (Critical) — LISPRunner arbitrary Lisp execution: both script-file and inline modes pass user-supplied content to AutoCAD's `SendStringToExecute()` with zero sanitization. An attacker who can supply the script path or content gains arbitrary AutoCAD Lisp API access.

2. **SEC-005** (Critical) — RunCommand arbitrary command execution: user-supplied command strings passed directly to AutoCAD's `SendCommand()` with no whitelist, validation, or sanitization. Multi-line input allows command chaining and full AutoCAD API access.

3. **SEC-002** (High) — Dalux API key handling: broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes `args.ApiKey` with default value `"Dalux API Key"` instead of the looked-up credential variable to `DaluxApiService`); HttpClient gaps (no timeout, no certificate validation callback) in both upload and download services.

4. **SEC-006** (Medium) — DaluxCloudDownload path traversal via server-supplied relative path: `file.RelativePath` and `file.FileName` from the Dalux API are used directly in `Path.Combine()` without traversal validation. A compromised Dalux API could inject `../` sequences.

5. **SEC-010** (Medium) — Navisworks SaveDocumentAs path traversal via unvalidated directory creation: `Directory.CreateDirectory()` called on user-supplied path from `SaveFileField` without traversal validation; file paths also echoed in success messages.

## What changed since previous run

Run `20260927-005009` — incremental. No source changes (context/changes.md empty). Previous run 20260926-215844 completed fully: sweep done, 11 findings (2 Critical, 1 High, 2 Medium, 4 Low, 2 Info), backlog empty. No re-verification needed.

Run `20260926-015629` is an incremental pass. Three new Critical and Medium findings were filed (SEC-004, SEC-005, SEC-006). CI/CD workflows and scripts were verified as well-implemented. The backlog has been expanded with additional items identified during review.

Run `20260926-215844` — incremental. Reviewed all seven Tekla extension file operation modules (IFCExport, ReadIn, RefreshReferenceModels, SaveModel, SetSelectionFilter, WriteOut, ZoomToSelected). One new Low finding: SEC-008 (IFCExport path traversal via unvalidated output file path). All other Tekla extensions have safe file operations with no user-typed paths or injection vectors.

Run `20260926-215844` session 6 — incremental. Replaced the Backlog row "Tekla extensions file operations" with three smaller rows by sub-project: (1) IFCExport path traversal, (2) five file operation extensions, (3) SetSelectionFilter. Reviewed the first row (IFCExport = SEC-008): re-reviewed all four IFCExport files (TeklaIFCExportCommand.cs, TeklaIFCExportArgs.cs, IFCExportConfig.cs, GlobalUsings.cs), confirmed the finding is complete and accurate, moved to Reviewed. The second and third rows remain in the Backlog for a future session.

Run `20260926-215844` session 8 — incremental. Replaced the old single Backlog row "Tekla file operations (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected)" with three smaller rows: (1) five file operation extensions safe assessment, (2) macro builder helper string safety, (3) extension result pattern variance. Reviewed the first row (five extensions = SEC-009): traced all ten Command.cs and Args.cs files, confirmed all safe - SDK APIs only, no user file paths, hardcoded string literals. Moved to Reviewed.

Run `20260926-215844` session 9 — incremental. Reviewed Tekla macro builder helper string safety. Traced all five Tekla extensions' Command.cs, Args.cs, and GlobalUsings.cs. ReadIn and WriteOut use TeklaMacroBuilderHelper.Callback() with fully hardcoded literals; ReadIn Args has only boolean fields, WriteOut Args has none. Other three extensions don't use the macro builder. CW.Assistant.Extensions.Tekla.Helpers is an external NuGet package (not auditable), but no user-controlled data reaches it from this repo. No new finding. Moved row from Backlog to Reviewed.

Run `20260926-215844` session 11 — incremental. Reviewed Navisworks extensions (ClashDetectiveRunner, OpenDocument, SaveDocumentAs). SEC-010 filed: SaveDocumentAs path traversal via Directory.CreateDirectory() on unvalidated path (Medium), file path information disclosure in success messages (Low), exception detail logging to Trace. Moved row from Backlog to Reviewed.

Run `20260926-215844` session 12 — incremental. Reviewed StreamBIM diagnostics logging. SEC-011 filed: StreamBimUploadDiagnostics writes full file paths, FTP operation details, and raw exception messages to a temp directory log file (Low). Opt-in via VerboseDiagnostics flag. No credentials in logs. Moved row from Backlog to Reviewed.

## Areas not yet covered

Backlog is empty. All tracked categories have been reviewed in run 20260926-215844.
