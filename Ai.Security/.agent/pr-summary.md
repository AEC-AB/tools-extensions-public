# Pull request summary

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run `20260925-214417` is a first-pass full sweep of `tools-extensions-public`, covering the StreamBIM credential storage area, the Dalux API key handling area, and a git history audit for secrets. Headline: one High, one Low, one Info, three findings filed, sweep fifteen percent complete.

## New findings

- SEC-001 (Low) — StreamBIM credential storage and FTP transfer security — `src/Assistant/dotnet/StreamBim/` — Credentials properly stored in Windows Credential Manager (DPAPI); FTP uses TLS 1.2 Explicit. Minor defense-in-depth gaps: no explicit certificate validation callback on FTP client, password held as plaintext string in memory.
- SEC-002 (High) — Dalux API key handling — `src/Assistant/dotnet/DaluxCloudUpload/DaluxCloudUploadCommand.cs` line 30 — Broken credential lookup: the upload command reads the API key from Windows Credential Manager but then passes `args.ApiKey` (default value `"Dalux API Key"`) instead of the looked-up variable to `DaluxApiService`, making every upload fail with an auth error. HttpClient gaps in both upload and download services: no timeout, no certificate validation callback. Error messages include base URL.
- SEC-003 (Info) — Git history audit — `src/` across all branches — No secrets, credentials, or private keys found in git history. Searched all branches with `git log -p -S` for password, secret, key, apiKey, PRIVATE KEY, connection, Token/AuthToken/Bearer/api_token. All matches were legitimate code additions only.

## Status changes

None — this is a first run.

## Reviewer attention

- SEC-002 (High) is a functional bug in the upload extension that also exposes a lack of code review on credential handling: the credential lookup variable is computed and validated but never used in the service construction.
- SEC-002 notes that the download extension correctly passes the looked-up variable — the upload extension should follow the same pattern.

## Not yet covered

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
