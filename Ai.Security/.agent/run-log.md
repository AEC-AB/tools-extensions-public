# Run log

Newest first, ten most recent runs only (the runner trims older entries; git history and the pull requests are the full record). Header written by the runner, completed by the agent.

## Run 20260926-015629 - incremental

- Date: 2026-09-26T01:57Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ 4658b6b
- Items created: SEC-004 (Critical, LISPRunner RCE), SEC-005 (Critical, RunCommand RCE), SEC-006 (Medium, Dalux path traversal)
- Items updated: Findings.md (3 new rows), findings.json (3 new entries), coverage.md (4 new Reviewed entries, backlog updated), pr-summary.md
- Notes: Incremental run with no changes in context/changes.md. Reviewed AutoCAD LISPRunner and RunCommand extensions - found two Critical RCE vulnerabilities via unsanitized command injection into AutoCAD. Reviewed DaluxCloudDownload - found Medium severity path traversal via server-supplied relative paths. Verified CI/CD workflows (sync-extension-docs.yml, validate-extension-docs.yml, build-dotnet-changed.yml) and scripts (Sync-ExtensionDocs.ps1, Test-MarkdownLinks.ps1) - input validation, path traversal protections, and private content scanning all properly implemented. Total findings: 6 (2 Critical, 1 High, 1 Medium, 1 Low, 1 Info). Sweep progress: ~20% complete (core AutoCAD extensions and Assistant extensions reviewed).

## Run 20260925-214417 - first run (full pass)

- Date: 2026-09-25T21:45Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ 4658b6b
- Items created: SEC-001 (Low, StreamBIM credential storage), SEC-002 (High, Dalux API key handling), coverage.md repo map, Summary.md, Findings.md, findings.json
- Items updated: coverage.md (moved StreamBIM credential storage and Dalux API key handling from Backlog to Reviewed)
- Notes: First run. Reviewed StreamBIM credential storage area: credentials properly stored in Windows Credential Manager (DPAPI), FTP uses TLS 1.2 Explicit encryption, no secrets in logs or git history. SEC-001 filed as Low severity (minor defense-in-depth gaps: no explicit FTP certificate validation callback, password in memory as string). Also reviewed Dalux API key handling: discovered broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes args.ApiKey default "Dalux API Key" instead of looked-up credential variable) and HttpClient gaps (no timeout, no certificate validation callback). SEC-002 filed as High severity. 2 findings written. Remaining backlog: 17 items. Session 2: Git history audit (SEC-003) - searched all branches with git log -p -S for password, secret, key, apiKey, PRIVATE KEY, connection, Token/AuthToken/Bearer/api_token - no secrets found in history. 1 finding written, 3 total. Remaining backlog: 16 items.

