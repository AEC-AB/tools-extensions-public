# SEC-010: Navisworks SaveDocumentAs path traversal via unvalidated directory creation and file path information disclosure

| | |
|---|---|
| Severity | Medium - `Directory.CreateDirectory()` called on user-supplied path with zero traversal validation; defense-in-depth gap. The SaveFileField UI constrains input in normal use, but configuration-level manipulation could bypass it. Information disclosure is additional LOW concern in success messages. |
| Status | Open |
| Category | Data protection / Injection / Path Traversal |
| Location | `src/Navisworks/dotnet/SaveDocumentAs/SaveDocumentCommand.cs` lines 8-12, line 15 |
| First seen | run 20260926-215844 at commit 4658b6b |
| Last verified | run 20260926-215844 |
| Introduced | commit 4658b6b, 2026-09-26 (not determined if earlier) |

## What

`SaveDocumentCommand.cs` extracts the parent directory from a user-supplied file path (via `Path.GetDirectoryName()`) and creates that directory with `Directory.CreateDirectory()` without any path traversal validation. If the path contains sequences like `..\..\..\Windows\` or an absolute path outside the intended scope, the code will create (or traverse into) unintended directories. Additionally, the success message on line 15 returns the full file path to the caller, which could be logged or displayed, leaking project/customer directory structures.

## Evidence

**SaveDocumentAs/SaveDocumentCommand.cs** lines 8-15:

```csharp
var saveFolder = Path.GetDirectoryName(args.Path);
if (!string.IsNullOrEmpty(saveFolder) && !Directory.Exists(saveFolder))
{
    Directory.CreateDirectory(saveFolder);
}
var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
doc.SaveFile(args.Path);
return Result.Text.Succeeded($"Saved: {args.Path}");
```

**OpenDocument/OpenDocumentCommand.cs** line 26 also echoes the full path:

```csharp
return Result.Text.Succeeded($"Opened file at path: {args.Path}");
```

**SaveDocumentAs/SaveDocumentArgs.cs** line 7 defines the field:

```csharp
[SaveFileField(Label = "Path", Hint = "Save file path", ToolTip = "File Path(ex: C:\\MyFile.nwd)")]
public string Path { get; set; } = string.Empty;
```

**ClashDetectiveRunner/ClashDetectiveRunTestCommand.cs** line 39 and 77 write full exception details to trace:

```csharp
System.Diagnostics.Trace.WriteLine($"Failed running all clash tests: {ex}");
System.Diagnostics.Trace.WriteLine($"Failed running clash test '{requestedTestName}': {ex}");
```

## Impact

- **Directory creation abuse (Medium)**: An attacker with access to extension configuration (e.g., in the CW Assistant task settings) could set the `Path` field to include path traversal sequences. `Directory.CreateDirectory()` would create the specified directory tree, potentially placing files in unexpected locations on the local machine.
- **Information disclosure (Low)**: Full file paths are returned in success messages by both OpenDocument and SaveDocumentAs. These could be captured in logs, UI screens, or exported reports, revealing internal directory structures that may encode project names, customer identifiers, or organizational hierarchy.
- **Stack trace leakage (Info)**: Full exception objects (including stack traces with internal assembly paths) are written to `System.Diagnostics.Trace`, which could be captured by any process with access to the trace output.

## What a fix involves

1. Add path validation before `Directory.CreateDirectory()`: resolve the full path with `Path.GetFullPath()`, resolve it against a known safe base directory (or the current working directory), and verify the result does not escape the expected scope using `Path.GetRelativePath()` or a `StartsWith()` check on the normalized path.
2. Sanitize the file path in success messages: use only the file name and a truncated or generic directory indicator rather than the full absolute path.
3. Log structured exception data without raw `ToString()` on the full exception object, or at least exclude stack trace information from trace output in production builds.

## References

- SEC-008: Tekla IFCExport path traversal via unvalidated output file path (same vector: `Directory.CreateDirectory()` on user-supplied path without validation)
- SEC-007: Base URL exposed in API error messages (information disclosure pattern)
