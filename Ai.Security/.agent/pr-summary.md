# Pull request summary - Session 11

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run `20260926-215844` session 11 reviewed the Backlog row "Navisworks extensions" covering ClashDetectiveRunner, OpenDocument, and SaveDocumentAs for data protection and logging. One new Medium finding filed: SEC-010 — SaveDocumentCommand.cs calls Directory.CreateDirectory() on unvalidated user-supplied path from SaveFileField, and both OpenDocument and SaveDocumentAs echo full file paths in success messages. Moved row from Backlog to Reviewed.

## New findings

- SEC-010 (Medium) — Navisworks SaveDocumentAs path traversal via unvalidated directory creation and file path information disclosure: `src/Navisworks/dotnet/SaveDocumentAs/SaveDocumentCommand.cs` line 11, `OpenDocument/OpenDocumentCommand.cs` line 26

## Status changes

- Backlog row "Navisworks extensions" moved to Reviewed — SEC-010 filed.

## Reviewer attention

- The Navisworks SaveFileField provides a UI file picker that constrains input in normal use, but extension configuration could override it. A directory creation bypass is defense-in-depth.
- ClashDetectiveRunner logs full exception objects (including stack traces) to System.Diagnostics.Trace — stack traces contain internal paths. Low severity.

## Not yet covered

- PrintPDF telemetry and logging (PII or secret leakage)
- StreamBIM diagnostics logging (sensitive data in output)
- Error handling pattern inconsistency (swallowed exceptions, info leakage)
- FluentFTP security (TLS enforcement)
- GitHub workflow secrets access
- Build pipeline permissions
