# Run log

Newest first, ten most recent runs only (the runner trims older entries; git history and the pull requests are the full record). Header written by the runner, completed by the agent.

## Run 20261009-163727 - incremental

- Date: 2026-10-09T16:37Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ be9cc40
- Items created: none (no new findings)
- Items updated: Findings.md (2 last_verified updates), findings.json (2 entries updated), coverage.md (commit row and Reviewed entry), Summary.md (changes section)
- Notes: Incremental run. Commit be9cc40 (Add Tekla 2025 support) changed only build configuration files (Directory.Build.props, 7 .csproj files) across src/Tekla/dotnet/ - no source code (.cs) files modified. SEC-008 (Tekla IFCExport path traversal) and SEC-009 (five Tekla extensions safe assessment) re-verified against be9cc40. Both findings confirmed still valid. SEC-008: IFCExportCommand.cs lines 30-38 still have no traversal validation and no filename sanitization, inconsistent with Revit extensions. SEC-009: all five file operation extensions still use only SDK APIs with hardcoded strings, no user-typed paths. No new findings. No secrets, no injection vectors, no new attack surfaces in the changed build config files.

## Run 20260927-005009 - incremental

- Date: 2026-09-27T00:50Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ 4658b6b
- Items created: none (no source changes)
- Items updated: run-log.md (this entry), pr-summary.md, Summary.md
- Notes: Incremental run. context/changes.md is empty — no source code changes since run 20260926-215844. The previous run completed its full sweep with 11 findings and an empty backlog. No re-verification needed. No new issues found. Run closed.

## Run 20260926-215844 - incremental

- Date: 2026-09-26T21:59Z
- Model: openai/spark-qwen3-35b
- Branch: security/ai-security
- Source tools-extensions-public @ 4658b6b
- Items created: SEC-008 (Low, Tekla IFCExport path traversal)
- Items updated: Findings.md (1 new row), findings.json (1 new entry), coverage.md (Reviewed entry for Tekla extensions file operations, backlog updated), Summary.md (count updated), pr-summary.md
- Notes: Session 4 (current) reviewed all seven Tekla extension file operation modules: IFCExport (Command.cs, Args.cs, IFCExportConfig.cs, Collector.cs), ReadIn (Command.cs, Args.cs), RefreshReferenceModels (Command.cs, Args.cs), SaveModel (Command.cs, Args.cs), SetSelectionFilter (Command.cs, Args.cs, SelectionFilterCollector), WriteOut (Command.cs, Args.cs), ZoomToSelected (Command.cs, Args.cs). SEC-008 filed: IFCExport output file path from XML config or SaveFileField goes through Path.GetFullPath() with no traversal validation and automatic directory creation; inconsistent with Revit DWGExport/NWCExport which sanitize filenames. All other Tekla extensions are safe - no user-typed paths, no injection vectors. Found no evidence of the other suspected issues in IFCExport (XXE via XmlSerializer is low risk by default, BasePointName validated before use).
- Session 6 (current): Replaced the single Backlog row "Tekla extensions file operations" with three smaller rows by sub-project/file group. Reviewed first row (IFCExport path traversal = SEC-008) - re-verified all IFCExport files, confirmed finding is complete. Also reviewed and moved to Reviewed the remaining two new rows: five file operation extensions (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected) are all safe - no user file path input, hardcoded macro callbacks. SetSelectionFilter validates null/whitespace on FilterName, passes only to Tekla's GetObjectsByFilterName() API, no file system access.
- Session 7 (current): Reviewed Tekla SetSelectionFilter - traced all code paths in SelectObjectFromSelectionFilterCommand.cs, SelectionFilterCollector.cs, SelectObjectFromSelectionFilterArgs.cs. FilterName validated for null/whitespace, then only passed to Tekla SDK's GetObjectsByFilterName() API. No file system, subprocess, or shell operations. No new finding. Moved row from Backlog to Reviewed.
- Session 8 (current): Replaced the old single Backlog row "Tekla file operations (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected)" with three smaller rows: (1) five file operation extensions safe assessment, (2) macro builder helper string safety, (3) extension result pattern variance. Reviewed the first row (five extensions = SEC-009): traced all ten Command.cs and Args.cs files, confirmed all safe - SDK APIs only, no user file paths, hardcoded string literals. Moved to Reviewed.
- Session 9 (current): Reviewed Tekla macro builder helper string safety. All five Tekla extensions (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected) traced - Command.cs files reviewed plus Args.cs and GlobalUsings.cs for each. ReadIn (TeklaReadInCommand.cs line 27) and WriteOut (TeklaWriteOutCommand.cs line 10) use TeklaMacroBuilderHelper.Callback() with fully hardcoded literals only ("acmdRunPluginMethod", "SharingToolsFeature;Tool.SharingAutomation;r0.00:00:00"/"w", "main_frame"). ReadIn Args has only boolean fields (Save, FailTask); WriteOut Args has no fields; other three extensions have minimal/empty Args. The helper class (CW.Assistant.Extensions.Tekla.Helpers) is an external NuGet package not auditable, but no user-controlled data flows to it from this repo. No new finding. Moved row from Backlog to Reviewed.
- Session 10 (current): Reviewed Tekla extension result pattern variance. All five Command.cs files (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected) and their Args/GlobalUsings reviewed. Variance confirmed: ReadIn/RefreshReferenceModels use Result.Text with descriptive messages; SaveModel always returns Succeeded regardless of ModelHandler.Save() bool; WriteOut returns Result.Empty.Succeeded(); ZoomToSelected always returns Succeeded with no error handling. No security boundary crossed — all local model operations only. No new finding. Moved row from Backlog to Reviewed.
- Session 11 (current): Reviewed Navisworks extensions (ClashDetectiveRunner, OpenDocument, SaveDocumentAs) for data protection and logging issues. SEC-010 filed: SaveDocumentCommand.cs calls Directory.CreateDirectory() on unvalidated path from SaveFileField (Medium path traversal); both OpenDocument and SaveDocumentAs echo full file paths in success messages (LOW info disclosure); ClashDetectiveRunner logs full exception details to Trace (stack trace leakage). Moved row from Backlog to Reviewed.
- Session 12 (current): Reviewed StreamBIM diagnostics logging. Traced all call sites in StreamBimUploadDiagnostics.cs, StreamBimUploadService.cs, and StreamBimFileTransferService.cs. SEC-011 filed: Verbose diagnostics log full local/remote file paths, FTP operation details, exception messages, and file sizes to a temp directory log file (Low severity). Opt-in via VerboseDiagnostics flag. No credentials in logs. Raw FluentFTP exception messages could reveal server infrastructure details. Moved row from Backlog to Reviewed.
- Session 13 (current): Reviewed all four remaining backlog items, all confirmed safe - no new findings. (1) FluentFTP security: StreamBimFtpClientFactory.cs uses FtpEncryptionMode.Explicit with TLS 1.2 only, single factory method, no config override possible. (2) GitHub workflow secrets access: sync-extension-docs.yml uses create-github-app-token@v2 scoped to tools-extensions-public only, permissions contents:write and pull-requests:write, no secret exposure. (3) Build pipeline permissions: build-dotnet-changed.yml safe PR triggers (GitHub restricts fork secrets), no sensitive ops, no explicit permissions block (uses repo defaults). (4) PrintPDF telemetry/logging: Telemetry.cs stores counters and sheet names (BIM metadata), SimpleLogger.cs accumulates in-memory only, both returned to Revit UI dialog, no PII or secrets. Backlog now empty. All 4 rows moved from Backlog to Reviewed.
- Session 14 (current): Sweep complete. Wrote pr-summary.md (step 5), completed run-log.md top entry, added codebase facts to learnings.md. All tracked areas reviewed. Total: 11 findings (2 Critical, 1 High, 2 Medium, 4 Low, 2 Info).

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

