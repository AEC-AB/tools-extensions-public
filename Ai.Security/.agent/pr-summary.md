# Pull request summary

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run `20260926-215844` is an incremental pass on `tools-extensions-public`. One new finding was filed: Low severity error message information disclosure in DaluxApiService. The Run `20260926-015629` findings (SEC-004 through SEC-006) are carried forward. Sweep forty percent complete.

## New findings

- SEC-007 (Low) — DaluxApiService base URL exposed in error messages — `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs` lines 122-130 and `src/Assistant/dotnet/DaluxCloudDownload/Services/DaluxApiService.cs` — HandleException<T> includes `_baseUrl` ("https://node1.field.dalux.com/service/api") and network diagnostic information in error messages returned to the client. Infrastructure endpoint disclosure; no credentials or secrets.

## Status changes

None — all findings remain Open.

## Reviewer attention

- SEC-007: The base URL in error messages is Low severity since it exposes an infrastructure endpoint but no credentials. Reviewers may consider this acceptable risk given it is already on the public internet.

## Not yet covered

- NuGet.config security (insecure source URLs or credentials)
- Directory.Build.props/targets (sensitive defaults)
- Build system command execution (unsanitized args in Build.cs)
- PrintPDF telemetry and logging (PII or secret leakage)
- .NET package versions (SBOM review)
- API version skew in DaluxApiService (deprecated versions)
- Shared code consistency across StreamBIM projects
- Tekla extension file operations (injection)
- Navisworks extension file operations (logging and error handling)
- StreamBIM diagnostics logging (sensitive data in output)
- Error handling pattern inconsistency (swallowed exceptions, info leakage)
- FluentFTP security (TLS enforcement)
- GitHub workflow secrets access
- Build pipeline permissions
