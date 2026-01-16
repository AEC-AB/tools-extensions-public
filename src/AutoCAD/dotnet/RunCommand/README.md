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
2. **Multiple Commands**: Separate multiple commands with newline
3. **Command Parameters**: Include command parameters and options as you would type them in AutoCAD


## Result Format
The extension returns:
- **Overall Status**: Succeeded, Failed, or PartiallySucceeded
- **Individual Command Results**: Status and details for each executed command
- **Error Messages**: Specific error information when commands fail

## Requirements
- Active AutoCAD document (drawing must be open)
- Valid AutoCAD commands
- Proper command syntax and parameters

## Notes
- Commands are executed in the order provided
- If one command fails, the extension continues with the remaining commands
- Empty or whitespace-only command lines are skipped