# Findings register

One row per finding. Ids are stable and never reused. Status is one of `Open`, `Fixed`, `Accepted risk`, `False positive`. Reviewers change status by editing this table (and the finding page); the agent respects it and only re-verifies.

| Id | Severity | Title | Status | Location | First seen | Last verified | Page |
|---|---|---|---|---|---|---|---|
| SEC-001 | Low | StreamBIM credential storage and FTP transfer security | Open | `src/Assistant/dotnet/StreamBim/` | 20260925-214417 | 20260925-214417 | SEC-001-streambim-credential-storage.md |
| SEC-002 | High | Dalux API key handling - broken credential lookup in upload and minor HttpClient gaps | Open | `src/Assistant/dotnet/DaluxCloudUpload/`, `src/Assistant/dotnet/DaluxCloudDownload/` | 20260925-214417 | 20260925-214417 | SEC-002-dalux-api-key-handling.md |
| SEC-003 | Info | Git history audit - no secrets found | Open | `src/` across all branches | 20260925-214417 | 20260925-214417 | SEC-003-git-history-audit-clean.md |
| SEC-004 | Critical | LISPRunner arbitrary AutoCAD Lisp script execution | Open | `src/AutoCAD/dotnet/LISPRunner/LISPRunnerCommand.cs` | 20260926-015629 | 20260926-015629 | SEC-004-lisprunner-arbitrary-execution.md |
| SEC-005 | Critical | RunCommand arbitrary AutoCAD command injection | Open | `src/AutoCAD/dotnet/RunCommand/RunCommandCommand.cs` | 20260926-015629 | 20260926-015629 | SEC-005-rancmd-arbitrary-execution.md |
| SEC-006 | Medium | DaluxCloudDownload path traversal via server-supplied relative path | Open | `src/Assistant/dotnet/DaluxCloudDownload/DaluxCloudDownloadCommand.cs` | 20260926-015629 | 20260926-015629 | SEC-006-dalux-path-traversal.md |
