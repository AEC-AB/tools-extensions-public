# Learnings and standing instructions

Lines here are instructions for future runs. Humans may add lines at any time; the agent never removes a human-written line.

## From humans

- Report only. Never change code, config or pipelines. Describe what a fix involves; do not write it.
- Never copy a secret value or personal data into any page. Location and kind only.
- Export folders (`*Exports/`) hold customer data files: report their presence in git as a data-protection finding once; do not enumerate their contents.

## From the agent

- DaluxCloudUpload/DaluxApiService.cs uses a new HttpClient() per instance with no timeout or certificate validation callback; HttpClient has no dependency injection or HttpMessageHandler pooling.
- Both DaluxCloudUpload and DaluxCloudDownload share identical credential lookup patterns via Meziantou.Framework.Win32.CredentialManager, but the upload command has a bug where it ignores the lookup result.
- DaluxApiService constructs API endpoints by string concatenation in both projects (e.g., `5.1/projects`, `5.1/projects/{projectId}/tasks`); no SQL injection risk but path traversal could be a concern if folder names from API responses are used in file operations.
- StreamBIM projects use FluentFTP with TLS 1.2 Explicit; no certificate validation callback is set on the FTP client.
- No logging framework is used in Dalux extensions; errors are returned as plain text Result objects.
- `UserCredentials` record in StreamBIM holds passwords as plaintext strings in memory; no SecureString usage.

