# Security review summary

<!-- pass-status: written by the runner -->
> **Incomplete pass.** Run 20260925-214417 did not finish: the model server stopped answering mid-run. The findings below are what it got to; the rest of the codebase is not reviewed yet. The next nightly sweep picks this repo up again.
<!-- /pass-status -->

Rewritten by the AEC security review agent on every run. Internal only.

## Scope

Run `20260925-214417` — first pass. Reviewed the StreamBIM credential storage and FTP transfer security area (`src/Assistant/dotnet/StreamBim/`, `src/Assistant/dotnet/StreamBIMUploader/`, `src/Assistant/dotnet/StreamBIMDownloader/`), the Dalux API key handling area (`src/Assistant/dotnet/DaluxCloudUpload/`, `src/Assistant/dotnet/DaluxCloudDownload/`), a full git history audit for secrets across all branches, and the Dalux API authentication area (`src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs`, `src/Assistant/dotnet/DaluxCloudDownload/Services/DaluxApiService.cs`).

## Counts

| Severity | Open | Total |
|---|---|---|
| High | 1 | 1 |
| Low | 1 | 1 |
| Info | 1 | 1 |
| **Total** | **3** | **3** |

| Status | Count |
|---|---|
| Open | 3 |

## Top open findings

1. **SEC-002** (High) — Dalux API key handling: broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes `args.ApiKey` with default value `"Dalux API Key"` instead of the looked-up credential variable to `DaluxApiService`); HttpClient gaps (no timeout, no certificate validation callback) in both upload and download services.

2. **SEC-001** (Low) — StreamBIM credential storage and FTP transfer security: minor defense-in-depth gaps — no explicit certificate validation callback on FTP client, password held as plaintext string in memory via `UserCredentials` record.

3. **SEC-003** (Info) — Git history audit: searched all branches with `git log -p -S` for password, secret, key, apiKey, PRIVATE KEY, connection, Token/AuthToken/Bearer/api_token — no secrets found in history.

## What changed since previous run

First run — no previous run.

## Areas not yet covered

- StreamBIM file operations (path validation, injection)
- Dalux file path handling (path traversal)
- GitHub workflow secrets access
- NuGet.config security
- Directory.Build.props/targets
- Build system command execution
- PrintPDF telemetry/logging
- .NET package versions (SBOM review)
- API version skew in DaluxApiService
- Shared code consistency across StreamBIM projects
- Error message exposure in DaluxApiService
- Build pipeline permissions
- Workflow token scoping
- FluentFTP security
