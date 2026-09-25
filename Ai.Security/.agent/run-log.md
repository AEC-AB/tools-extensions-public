# Run log

Newest first, ten most recent runs only (the runner trims older entries; git history and the pull requests are the full record). Header written by the runner, completed by the agent.

## Run 20260925-214417 - first run (full pass)

- Date: 2026-09-25T21:45Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ 4658b6b
- Items created: SEC-001 (Low, StreamBIM credential storage), SEC-002 (High, Dalux API key handling), coverage.md repo map, Summary.md, Findings.md, findings.json
- Items updated: coverage.md (moved StreamBIM credential storage and Dalux API key handling from Backlog to Reviewed)
- Notes: First run. Reviewed StreamBIM credential storage area: credentials properly stored in Windows Credential Manager (DPAPI), FTP uses TLS 1.2 Explicit encryption, no secrets in logs or git history. SEC-001 filed as Low severity (minor defense-in-depth gaps: no explicit FTP certificate validation callback, password in memory as string). Also reviewed Dalux API key handling: discovered broken credential lookup in DaluxCloudUploadCommand.cs line 30 (passes args.ApiKey default "Dalux API Key" instead of looked-up credential variable) and HttpClient gaps (no timeout, no certificate validation callback). SEC-002 filed as High severity. 2 findings written. Remaining backlog: 17 items.

