# Learnings and standing instructions

Lessons for future runs, written by the agent. People steer the agent in [../README.md](../README.md), not here.

## From humans

Standing instructions, requests and questions for the agent are written in [../README.md](../README.md), not here.

## From the agent

- tools-extensions-public contains five CAD product extension families: AutoCAD (LISPRunner, RunCommand), Revit (DWGExport, LoadFamily, NWCExport, PrintPDF, ZoomToSelected), Tekla (IFCExport, ReadIn, RefreshReferenceModels, SaveModel, SetSelectionFilter, WriteOut, ZoomToSelected), Navisworks (ClashDetectiveRunner, OpenDocument, SaveDocumentAs), and a shared Assistant framework (StreamBIMUploader/Downloader, DaluxCloudUpload/Download).
- AutoCAD extensions use SendStringToExecute() and SendCommand() - no sanitization, full access to AutoCAD APIs. Both have Critical RCE findings.
- Revit extensions use UI file pickers (SaveFileField/FilePickerField) and sanitize filenames with Regex.Replace for output paths.
- Tekla IFCExport uses XmlSerializer for config deserialization; output path goes through Path.GetFullPath() only - no filename sanitization. Inconsistent with Revit approach.
- All Tekla extensions except IFCExport operate purely on SDK model objects with no user file input.
- Navisworks SaveDocumentAs echoes full file paths in success messages; ClashDetectiveRunner logs full stack traces to Trace.
- StreamBIM shared library (referenced by both Uploader and Downloader): CredentialManager for DPAPI storage, FluentFTP with TLS 1.2 Explicit, no certificate validation callback.
- DaluxApiService is shared identically between DaluxCloudUpload and DaluxCloudDownload; both use string concatenation for API version endpoints.
- Build system uses Nuke.Common (version 10.1.0) with four MSBuild-compatible steps; all secrets scoped via GitHub App token.
- CI workflows: sync-extension-docs.yml (GitHub App token, private content scanning), validate-extension-docs.yml (PR-only, no secrets), build-dotnet-changed.yml (PR-only, no sensitive ops).
- SBOM shows 7 NuGet packages: FluentFTP 48.0.1, Meziantou.Framework.Win32.CredentialManager 1.7.3, Microsoft.CSharp 4.7.0 (old), System.ComponentModel.Annotations 5.0.0, LibGit2Sharp 0.31.0 (build only), NuGet.Frameworks 7.9.0 (build only), Nuke.Common 10.1.0 (build only).
- PrintPDF has its own SimpleLogger (in-memory) and Telemetry (in-memory counters) - both returned to UI dialog, no file I/O.
- StreamBIM uploader diagnostics logs to %TEMP%\StreamBIMUploader\ - no log rotation. Opt-in via VerboseDiagnostics flag.
- Eight nuget.config files total (1 root + 7 Tekla per-project); all reference api.nuget.org/v3 with <clear/>; semantically identical but different whitespace in one file.
- Directory.Build.props and .targets files for Revit and Tekla projects have no secrets or insecure defaults; CW.Assistant.Extensions packages use wildcard version 26.* (reproducibility concern).
- DaluxUser model (DaluxCloudUpload/Models/DaluxModels.cs) contains three personal data fields (Email, FirstName, LastName). GetProjectUsersAsync method exists but is not called from anywhere in the codebase — this is a CLI-only extension, not a web service. Personal data is transient only, no persistence or export in this repo.

