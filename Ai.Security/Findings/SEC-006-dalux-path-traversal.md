# SEC-006: DaluxCloudDownload path traversal via server-supplied relative path

| | |
|---|---|
| Severity | Medium - Server-supplied relative path used in file output without path validation creates a supply-chain attack vector |
| Status | Open |
| Category | Injection / Path Traversal |
| Location | `src/Assistant/dotnet/DaluxCloudDownload/DaluxCloudDownloadCommand.cs` lines 368-378 |
| First seen | run 20260926-015629 at commit 0cbd897 |
| Last verified | run 20260926-015629 |
| Introduced | commit 0cbd897, 2026-09-22 (DaluxCloudDownload initial code) |

## What

When downloading folder contents from the Dalux API, the extension uses `file.RelativePath` from the Dalux server response directly to construct local output paths (line 368-369). While `file.FileName` is used directly in single-file downloads (line 314), the relative path is a server-provided value that could contain path traversal sequences (`../`) if the Dalux API server is compromised, misconfigured, or affected by a bug. The code does not validate or sanitize the relative path before combining it with the user-specified output folder. An attacker who controls the Dalux server could write files to arbitrary locations on the user's machine within the output folder hierarchy.

## Evidence

**DaluxCloudDownloadCommand.cs** - folder download using server-relative path (lines 367-378):

```csharp
var relativePath = string.IsNullOrEmpty(file.RelativePath) ? file.FileName : $"{file.RelativePath}/{file.FileName}";
var outputPath = Path.Combine(outputFolder, relativePath);
var outputDir = Path.GetDirectoryName(outputPath);

if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

await File.WriteAllBytesAsync(outputPath, content.Data, [value redacted]);
```

**DaluxCloudDownloadCommand.cs** - single file download using server-supplied filename (lines 314-316):

```csharp
var outputPath = Path.Combine(outputFolder, findFile.Data.FileName);
await File.WriteAllBytesAsync(outputPath, content.Data, [value redacted]);
return DaluxApiResponse<string>.Success(outputPath);
```

The `file` object is `DaluxFile` returned from the Dalux API (`DaluxCloudDownload.Models`). The `RelativePath` and `FileName` fields are not validated against path traversal.

## Impact

If the Dalux API server is compromised, an attacker could:
- Inject `../` sequences into `RelativePath` or `FileName` responses
- Escape the intended output folder by navigating up the directory tree
- Overwrite arbitrary files on the user's machine (depending on what `outputFolder` allows)
- Place malicious files in system directories if the user specifies a privileged output folder

This is Medium severity because: (1) the path values come from the Dalux server, not directly from the user, requiring a server compromise or a Dalux bug; (2) the paths are combined with the user-specified output folder, so successful traversal is limited to within that parent directory; (3) defense-in-depth is needed since the output folder is a reasonable assumption for where downloaded files go.

## What a fix involves

1. **Validate relative paths**: Before using `file.RelativePath` or `file.FileName`, normalize the full path and verify it remains within the output folder.
2. **Example approach**: Resolve `Path.GetFullPath(Path.Combine(outputFolder, relativePath))` and check that it starts with `Path.GetFullPath(outputFolder) + Path.DirectorySeparatorChar`.
3. **Sanitize filenames**: Remove or reject filenames containing `..`, null bytes, or paths with `:` on Windows.
4. **Add unit tests** for path traversal attempts in both single-file and folder-download code paths.
5. The prior fix in StreamBIM (commit c5864f9) addressed path issues in that project but did not apply to DaluxCloudDownload.

## References

- CWE-22: Path Traversal
- CVE history of NuGet/nupkg path traversal in Sync-ExtensionDocs.ps1 line 52-53 shows the team has this awareness for .nupkg extraction but did not apply it to Dalux downloads