# RunCommand Extension

## Overview
The RunCommand extension allows you to execute AutoCAD commands programmatically through Assistant. This extension provides a way to run single or multiple AutoCAD commands in sequence, making it useful for automating repetitive tasks and executing command scripts.

## What this Extension Does
- **Execute AutoCAD Commands**: Run any valid AutoCAD command as if typed in the command line
- **Batch Command Execution**: Execute multiple commands in sequence by separating them with newlines
- **Error Handling**: Provides detailed feedback on command success/failure status
- **Result Reporting**: Returns comprehensive results showing which commands succeeded or failed

## How to use this Extension
1. **Single Command**: Provide a single AutoCAD command string (e.g., "LINE")
2. **Multiple Commands**: Separate command input with newlines; the full multiline block is queued as one AutoCAD command stream
3. **Command Parameters**: Put command parameters and follow-up prompt responses on later lines, exactly as you would in a `.scr` script


## Result Format
The extension returns:
- **Overall Status**: Succeeded, Failed, or PartiallySucceeded
- **Queued Input Lines**: The non-empty lines that were queued for AutoCAD to process
- **Error Messages**: Specific error information when commands fail

## Requirements
- Active AutoCAD document (drawing must be open)
- Valid AutoCAD commands
- Proper command syntax and parameters

## Notes
- Commands are executed in the order provided
- Commands and prompt responses are queued in the order provided as one continuous AutoCAD input stream
- Each newline acts like pressing Enter, which allows interactive commands such as `LINE` to consume later lines as prompt input
- If one command fails, the extension continues with the remaining commands
- Empty or whitespace-only command lines are skipped