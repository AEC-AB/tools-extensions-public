# INFRA IFC IDS Validation

## Description
This Assistant extension runs INFRA validation automation for IFC and IDS workflows.

Use it when you want to validate one or more IFC files against selected IDS files for a specific INFRA project, and store results in a defined output folder.

Typical outcomes:
- INFRA metadata is prepared for the selected IFC files.
- Project-specific IDS selection and output path are written for INFRA automation.
- INFRA automation starts with selected validation commands.

Host platform:
- Assistant (Windows)

## Configuration
The extension UI fields map to the settings below.

- Project name: Select a discovered INFRA project name (automatic discovery only).
- IDS files: Optional explicit IDS selection relative to the selected project's IDS folder. If none are selected, IDS files are scanned automatically from the project's IDS folder.
- IFC files: One or more IFC input entries. In the normal picker UI this behaves like a standard file picker. In raw data view it also supports these formats:
	- Exact file path: C:\Models\File.ifc
	- Wildcard all files in folder: C:\Models\*
	- Wildcard matching files in folder: C:\Models\*.ifc
	- Assistant variable name: MyIfcFiles
	- Assistant variable with prefix: var:MyIfcFiles
	- Assistant variable with braces: ${MyIfcFiles}
	- Loop/task variable in raw data: Loop.Path
	- Combined variable and wildcard: ${{ Loop.Path }}\*.ifc where Loop.Path contains folder paths
	- Regex entry scoped to a folder: regex:C:\Models|^.*\.ifc$
	Variable values may contain multiple entries separated by newline, semicolon, comma, or pipe.
	Important: IFC files raw data is passed through to the extension. The extension resolves plain variable names like Loop.Path and embedded placeholders like ${{ Loop.Path }} or ${{ Loop.Path }}\*.ifc at runtime.
- Output folder: Required existing folder where INFRA output can be reviewed.
- Validation commands: One or more commands to run. Available values:
	- IFC_CHECK
	- STEP_SYNTAX
	- IFC_SCHEMA
	- IDS_VALIDATION
- Close INFRA on completion: If enabled, passes close-on-completion option to INFRA automation.
- Enable diagnostics: Adds discovery and launch diagnostic lines to the success summary.

## Functionality
### Description
When run, the extension validates inputs, resolves project, IFC, and IDS selections, prepares INFRA metadata and registry values, then launches INFRA automation with the selected validation commands.

### How To Use
1. Ensure INFRA is installed and at least one project exists.
2. Select Project name from discovered INFRA projects.
3. Add IFC files.
4. If needed, right-click IFC files and open raw data to enter wildcard paths, variable names, or regex entries.
5. For loop-based folder expansion, enter entries like ${{ Loop.Path }}\*.ifc directly in IFC files textfield.
6. Choose IDS files directly or leave empty to scan automatically.
7. Select Output folder.
8. Select one or more Validation commands.
9. Optionally enable Close INFRA on completion and Enable diagnostics.
10. Run the extension.

### Preconditions
- Windows environment.
- INFRA API assembly available from INFRA installation.
- Selected output folder already exists.
- Selected IFC and IDS files exist on disk after wildcard, variable, or regex expansion.

### Outputs And Side Effects
- Metadata creation via INFRA API for selected IFC files.
- INFRA registry updates for project path, IDS selection, and output directory.
- INFRA automation process launch with selected command arguments.
- Assistant result summary showing project, file counts, and commands.

## Troubleshooting
### Issue: Output folder is required or not found
- Causes: Output folder is empty or points to a non-existing path.
- Solution: Select an existing local folder in Output folder.

### Issue: Failed to load INFRA API
- Causes: INFRA not installed, registry install location missing, or API DLL unavailable.
- Solution: Verify INFRA installation and reinstall or repair INFRA if needed.

### Issue: No valid project in automatic mode
- Causes: Project picker returned INFO or ERROR item, or project path no longer exists.
- Solution: Re-open selection and choose a valid project name.

### Issue: No IFC files were selected
- Causes: Empty IFC input, wrong variable name, invalid wildcard or regex entry, or no matching files on disk.
- Solution: Verify each IFC files entry in raw data. Confirm wildcard paths point to an existing folder, variable values contain valid entries, embedded placeholders resolve to usable paths, and regex entries use the format regex:folder|pattern.

### Issue: No IDS files were found or selected
- Causes: IDS files list empty and IDS folder has no .ids files, or selected files do not exist.
- Solution: Select IDS files explicitly or verify IDS folder contents.

### Issue: Failed to launch INFRA automation
- Causes: INFRA API launch call failed due to environment or installation mismatch.
- Solution: Run INFRA manually once, verify project can open, then retry. If it persists, enable diagnostics and review the result details.

### Diagnostics Log
Collector diagnostics are written to:
- %LOCALAPPDATA%\AEC AB\Assistant\Logs\INFRAIFCIDSValidation.collector.log

## FAQ
- Q: Can I run multiple validation commands at once?
	- A: Yes. Select one or more values in Validation commands.

- Q: What can I put in IFC files raw data?
	- A: Exact file paths, wildcard paths, Assistant variable names, var:VariableName, ${VariableName}, Loop.Path, combined placeholder entries like ${{ Loop.Path }}\*.ifc, and regex entries using regex:folder|pattern.

- Q: How do I combine a folder variable with *.ifc in IFC files?
	- A: Enter it directly in IFC files raw data as ${{ Loop.Path }}\*.ifc. The extension resolves the placeholder first, then expands the wildcard.

- Q: Are subfolders included when using wildcard or regex entries?
	- A: No. Expansion is limited to the top level of the specified folder.

## Resources
- [Assistant extension quick start](../../../../docs/dotnet/QUICK_START.md)
- [Args developer guide](../../../../docs/dotnet/ARGS_DEVELOPER_GUIDE.md)
- [Assistant platform guide](../../../../docs/dotnet/PLATFORM_GUIDES/ASSISTANT.md)

## Version History
- Version 0.0.1 - 2026-06-17
	- Initial public README with complete user guidance for configuration, execution, and troubleshooting.
- IFC files now support raw exact paths, wildcard paths, Assistant variables, and regex-scoped entries through one field.