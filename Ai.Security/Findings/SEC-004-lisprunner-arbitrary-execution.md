# SEC-004: LISPRunner arbitrary AutoCAD Lisp script execution

| | |
|---|---|
| Severity | Critical - Arbitrary Lisp code execution from user-supplied file paths or inline content |
| Status | Open |
| Category | Injection / RCE |
| Location | `src/AutoCAD/dotnet/LISPRunner/LISPRunnerCommand.cs` lines 20-57 |
| First seen | run 20260926-015629 at commit 0cbd897 |
| Last verified | run 20260926-015629 |
| Introduced | commit 183a206, 2026-09-15 (initial commit of AutoCAD extensions) |

## What

The LISPRunner extension allows users to execute arbitrary AutoCAD Lisp code through two modes: (1) specifying a file path to a `.lsp` script file, and (2) providing inline Lisp code. Both modes pass the user-supplied content directly to AutoCAD via `SendStringToExecute()` / `SendCommand()` without any sanitization or validation. An attacker who can persuade a user to select a malicious Lisp file or enter malicious inline code can execute arbitrary commands within the AutoCAD process — including file system access, network calls, and model manipulation. The extension treats the input as trusted and does not restrict which Lisp functions may be called.

## Evidence

**LISPRunnerCommand.cs** - script file mode (lines 20-38):

```csharp
private static IExtensionResult RunScriptFile(Autodesk.AutoCAD.ApplicationServices.Document doc, LISPRunnerArgs args)
{
    var scriptPath = args.LispScriptPath?.Trim();
    if (string.IsNullOrWhiteSpace(scriptPath))
    {
        return Result.Text.Failed("A Lisp script file must be selected in SelectFile mode.");
    }

    var fullScriptPath = System.IO.Path.GetFullPath(scriptPath);
    if (!System.IO.File.Exists(fullScriptPath))
    {
        return Result.Text.Failed($"The selected Lisp script was not found: {fullScriptPath}");
    }

    var lispLoadCommand = BuildLoadCommand(fullScriptPath);
    doc.SendStringToExecute(lispLoadCommand, activate: true, wrapUpInactiveDoc: false, echoCommand: false);

    return Result.Text.Succeeded($"Queued Lisp script file for execution: {fullScriptPath}");
}
```

The file path is used directly in a `(load "...")` call. No file extension validation, no content inspection.

**LISPRunnerCommand.cs** - inline mode (lines 40-51):

```csharp
private static IExtensionResult RunInlineScript(Autodesk.AutoCAD.ApplicationServices.Document doc, LISPRunnerArgs args)
{
    var inlineScript = args.InlineScript?.Trim();
    if (string.IsNullOrWhiteSpace(inlineScript))
    {
        return Result.Text.Failed("Inline Lisp script content is required in Inline mode.");
    }

    doc.SendStringToExecute(inlineScript + " ", activate: true, wrapUpInactiveDoc: false, echoCommand: false);

    return Result.Text.Succeeded("Queued inline Lisp script for execution.");
}
```

The inline script content is sent directly to AutoCAD with no sanitization.

**LISPRunnerArgs.cs** confirms the input sources:

```csharp
// src/AutoCAD/dotnet/LISPRunner/LISPRunnerArgs.cs line 8
public string? LispScriptPath { get; init; }

// line 9
public string? InlineScript { get; init; }
```

## Impact

An attacker who can trick a user into running this extension with a malicious Lisp file or inline script gains code execution within the AutoCAD process context. AutoCAD Lisp can: read/write arbitrary files on the user's machine, make network requests, modify the AutoCAD database, send commands to other AutoCAD APIs, and access the Windows clipboard. This is a direct remote code execution vulnerability.

## What a fix involves

1. **Remove inline mode entirely** or restrict it to a safe subset of Lisp functions (e.g., math only).
2. **For file mode**: restrict allowed file extensions to `.lsp` or `.fas`, restrict file paths to a known directory whitelist, and optionally scan file content against a blocklist of dangerous Lisp functions (`vl-file-delete`, `vlax-invoke`, `vla-`, etc.).
3. **Add logging** of which scripts are executed for audit purposes.
4. **Consider a sandbox approach**: execute scripts in a restricted AutoCAD document context with limited API access.

## References

- SEC-005 (RunCommandCommand) — same pattern of unsanitized command injection into AutoCAD
- AutoCAD .NET API: `SendStringToExecute`, `SendCommand`