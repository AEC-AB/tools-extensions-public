# SEC-005: RunCommand arbitrary AutoCAD command injection

| | |
|---|---|
| Severity | Critical - Arbitrary AutoCAD command execution from user-supplied input allows remote code execution |
| Status | Open |
| Category | Injection / RCE |
| Location | `src/AutoCAD/dotnet/RunCommand/RunCommandCommand.cs` lines 81-86 |
| First seen | run 20260926-015629 at commit 0cbd897 |
| Last verified | run 20260926-015629 |
| Introduced | commit 183a206, 2026-09-15 (initial commit of AutoCAD extensions) |

## What

The RunCommand extension allows users to supply arbitrary AutoCAD command strings that are then executed via `SendCommand()`. The extension splits the input on newlines and executes each line as a separate AutoCAD command without any sanitization, validation, or whitelist. AutoCAD commands include not just drawing operations but also system commands, file operations, and .NET API access. This means a user can execute virtually any command available within the AutoCAD environment, including those that can affect the file system, network, or other applications.

## Evidence

**RunCommandCommand.cs** - direct command injection (lines 81-86):

```csharp
private static void RunCommand(string command)
{
    dynamic acadApp = Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
    var thisDrawing = acadApp.ActiveDocument;
    thisDrawing.SendCommand(command);
}
```

**RunCommandCommand.cs** - multi-command execution (lines 29-54):

```csharp
var commands = args.Commands?.Split('\n') ?? new string[] { };

foreach (var command in commands)
{
    if (string.IsNullOrWhiteSpace(command))
        continue;
        
    try
    {
        RunCommand(command);
        // ...
    }
    catch (System.Exception e)
    {
        commandResults.Add(new CommandResult
        {
            Succeeded = false,
            CommandResults = command,
            ErrorMessage = e.Message
        });
    }
}
```

**RunCommandArgs.cs** shows the input source:

```csharp
// src/AutoCAD/dotnet/RunCommand/RunCommandArgs.cs line 7
[Required(ErrorMessage = "AutoCAD Command(s) is required.")]
public string Commands { get; init; } = string.Empty;
```

No sanitization or validation of `args.Commands` occurs before execution.

## Impact

An attacker who can supply commands to this extension gains full access to the AutoCAD command environment. AutoCAD commands can:

- Execute system commands via `Shell` or `_(shell)` 
- Access the file system via `_-insert`, `_-wedge`, and other file-based commands
- Call .NET methods via `-.NETLOAD` and `_(.NET)` commands
- Make network requests through AutoCAD's .NET API
- Modify AutoCAD drawings and settings

This is a direct remote code execution vulnerability. The multi-line input capability means multiple commands can be chained in a single execution.

## What a fix involves

1. **Implement a command whitelist**: Only allow a predefined set of safe AutoCAD commands (e.g., `LINE`, `CIRCLE`, `DIMLINEAR`) and reject all others.
2. **Sanitize arguments**: For each whitelisted command, validate that arguments contain only expected character classes (alphanumeric, specific punctuation).
3. **Remove multi-command support**: Single-command execution is safer than allowing command chains.
4. **Consider a safer alternative API**: Instead of `SendCommand()`, use the .NET API directly to perform specific drawing operations that the extension is designed for.
5. **Add audit logging**: Log all commands attempted and their results.

## References

- SEC-004 (LISPRunner) — similar unsanitized execution vulnerability in Lisp scripts
- AutoCAD .NET API documentation
- OWASP Input Validation Cheat Sheet