# DaluxCloudDownload

An Assistant extension that downloads one or more files or folders from a Dalux project into a local folder, preserving folder structure.

## Configuration

| Field | Required | Description |
|---|---|---|
| API Key | Yes | Your Dalux API Identity key. |
| API Base URL | Yes | Base URL for the Dalux API. |
| Project Name | Yes | The Dalux project name to search in. |
| Download Files/Folders | Yes | List of Dalux file or folder paths. Examples: `Files/Path/file.ifc` (single file) or `Files/Path/Folder` (entire folder). |
| Output Folder | Yes | The local folder where files will be written. Existing files are overwritten. |

## Behavior

1. Resolves the Dalux project by name.
2. For each path in the list:
   - If the path ends with a file extension (e.g., `.ifc`), it downloads that specific file.
   - If the path is a folder, it downloads all files in that folder and all subfolders, preserving the folder structure locally.
3. Creates subdirectories locally as needed to match Dalux folder structure.
4. Reports a summary at the end with success and failure counts.
5. If any downloads fail, the task is marked as partially succeeded (some succeed) or failed (all fail).

## Notes

- The file path must use forward slashes.
- The first path segment must be the Dalux file area name.
- The local output file name is always the Dalux file name.