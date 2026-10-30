# Pull request summary - Incremental (DaluxUser PII assessment)

Overwritten by the agent on every run. Plain Markdown, under 3500 characters, no `#` followed by digits, no secret values.

## Run scope and headline

Run 20261030-034207 -- incremental. context/changes.md is empty -- no source code changes since run 20261009-163727 (commit be9cc40). All 11 existing Open findings confirmed still valid (no changed files). New finding SEC-012: DaluxUser model in DaluxCloudUpload contains Email, FirstName, LastName personal data fields; GetProjectUsersAsync method exists but is not called from anywhere in the codebase (CLI extension only, not a web service). Info severity. Total: 12 findings (2 Critical, 1 High, 2 Medium, 4 Low, 3 Info).

## New findings

- SEC-012 (Info) -- Personal data in DaluxUser model with no identified access path: DaluxUser DTO contains Email, FirstName, LastName fields; GetProjectUsersAsync returns List<DaluxUser> from the Dalux API but has no callers in the repo. Personal data is transient only.

## Status changes

None. All existing findings remain Open with confirmed evidence. Updated last_verified timestamps on all entries to 20261030-034207.

## Reviewer attention

SEC-012 reviewers should confirm that callers of GetProjectUsersAsync (outside this repo or in future code) apply appropriate project-level authorization before returning names and emails of all project members.

## Not yet covered

Backlog is empty. All tracked categories have been reviewed: injection and RCE, path traversal, CI/CD security, credentials and secrets, NuGet config, dependencies, API version consistency, data protection, logging, diagnostics, FTP security, workflow permissions, telemetry, runtimes past end of support, and all five extension families (AutoCAD, Revit, Tekla, Navisworks, Assistant).
