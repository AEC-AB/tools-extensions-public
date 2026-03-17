# SaveModel Extension

## Overview
The SaveModel extension allows you to save the active AutoCAD drawing through Assistant. It supports both quick-saving the current file and saving to a new file path.

## What this Extension Does
- **Quick Save**: Save the active drawing to its existing file path
- **Save As**: Save the active drawing to a new file path with a new name
- **Error Handling**: Provides clear feedback when no document is open or a required save path is missing

## How to use this Extension

### Quick Save
1. Leave **Save with new name** unchecked
2. Run the extension — the active drawing is saved in place (equivalent to `QSAVE`)

### Save As (new name / path)
1. Check **Save with new name**
2. Enter a full file path in the **Save path** field (`.dwg` or `*` for all files)
3. Run the extension — the drawing is saved to the specified path


## Result Format
The extension returns:
- **Succeeded**: Confirmation message with the saved file path
- **Failed**: Error message explaining why the save could not be completed (e.g., no active document, missing save path)

## Requirements
- An active AutoCAD document must be open
- If saving with a new name, a valid file path must be provided

## Supported AutoCAD Versions
2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026
