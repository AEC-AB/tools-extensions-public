# SEC-008: Tekla IFCExport path traversal via unvalidated output file path

| | |
|---|---|
| Severity | Low - Local CAD extension running under user context, no network exposure, but lacks defense-in-depth path validation that comparable Revit exports have (sanitization present in DWGExport/NWCExport but absent here). |
| Status | Open |
| Category | Injection / Path Traversal |
| Location | `src/Tekla/dotnet/IFCExport/TeklaIFCExportCommand.cs` lines 20-39 |
| First seen | run 20260926-215844 at commit 4658b6b |
| Last verified | run 20260926-215844 |
| Introduced | Not determined |

## What

The Tekla IFCExport extension allows the output file path to be set either from an XML configuration file selected by the user or via a `SaveFileField` dialog override. Neither path is validated against directory traversal (`..` sequences) before being normalized with `Path.GetFullPath()` and used to create directories and write the exported IFC file. The Revit equivalents (DWGExport, NWCExport) sanitize filenames by stripping invalid path characters, but Tekla's IFCExport performs no such sanitization. An attacker who can supply a crafted config XML file could redirect the IFC export output to an arbitrary directory on the local filesystem.

## Evidence

`src/Tekla/dotnet/IFCExport/TeklaIFCExportCommand.cs`:

```csharp
// Line 12-16: User selects config file via FilePickerField
[FilePickerField(
    Label = "IFC Export Config File",
    ToolTip = "Path to the IFC export configuration XML file",
    Hint = "Select a IFC export config file",
    FileExtensions = ["xml","*"])]
public string? ExportConfigFilePath { get; set; }

// Line 14: Config file is read and deserialized
var bytes = File.ReadAllBytes(args.ExportConfigFilePath);
XmlSerializer serializer = new(typeof(IFCExportConfig));
IFCExportConfig exportConfig = (IFCExportConfig)serializer.Deserialize(memStream);

// Line 19-23: SaveFileField override goes directly to OutputFile with no validation
if (!string.IsNullOrEmpty(args.FilePathOverride))
{
    exportConfig.OutputFile = args.FilePathOverride!;
}

// Line 25-34: OutputFile used directly after GetFullPath (normalization only) and Directory.CreateDirectory
if (string.IsNullOrEmpty(exportConfig.OutputFile))
{
    return Result.Text.Failed("Output file path is not set.");
}

exportConfig.OutputFile = Path.GetFullPath(exportConfig.OutputFile);
if (!exportConfig.OutputFile.ToUpper().EndsWith(".IFC"))
{
    exportConfig.OutputFile += ".ifc";
}
var directoryPath = Path.GetDirectoryName(exportConfig.OutputFile);
if (!Directory.Exists(directoryPath))
{
    Directory.CreateDirectory(directoryPath);
}
```

`src/Tekla/dotnet/IFCExport/IFCExportConfig.cs`:

```csharp
// Line 44-45: OutputFile is a plain string - XML content is user-editable
[XmlElement(ElementName="OutputFile")]
public string? OutputFile { get; set; }
```

The `IFCExportConfig` class is deserialized from an XML file that the user selects. While the user picks the file, the XML content is fully editable by the user (or by anyone who can place a file in the same directory). The config could contain:

```xml
<config>
    <OutputFile>../../../Windows/System32/malicious.ifc</OutputFile>
    ...
</config>
```

`Path.GetFullPath()` normalizes the path to an absolute path but does **not** prevent traversal. `Directory.CreateDirectory()` will create any missing parent directories. The Tekla API (`comp.Insert()`) then writes the export to that path.

## Impact

A user who runs IFCExport with a crafted XML config (or a config placed by an attacker in a shared directory) could cause Tekla to write IFC export files to arbitrary locations on the local filesystem. The actual IFC content is determined by Tekla's own export API, not by the config file, so the attacker cannot control the file content. However, writing to unexpected locations could:
- Overwrite existing files if the crafted path points to an existing file
- Create directories in unexpected locations
- Mislead users about where their exported files are stored

This is a local-only, single-user attack surface (no network component). The realistic vector is social engineering: presenting a user with a config file that appears legitimate but redirects exports.

What a fix involves:
- Validate the output path to ensure it stays within an expected base directory (e.g., a configured export root or the current working directory).
- Sanitize the filename by stripping invalid path characters (similar to the Revit `Regex.Replace(fileName, "[<>:\"/\\|?*]", "_")` pattern used in DWGExport/NWCExport).
- Consider adding a confirmation dialog for non-standard paths.
- Do not auto-create parent directories for paths that require traversal to reach them.

## References

- SEC-006: DaluxCloudDownload path traversal via server-supplied relative path (same category, different source)
- Comparison: Revit DWGExport (`src/Revit/dotnet/DWGExport/`) and NWCExport use `Regex.Replace(fileName, "[<>:\"/\\|?*]", "_")` for filename sanitization
- Inconsistency: Revit extensions sanitize output filenames but Tekla extensions do not
