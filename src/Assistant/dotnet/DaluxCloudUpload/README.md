# DaluxCloudUpload

An **Assistant** extension that uploads files directly to a Dalux project via Dalux API.

---

## Overview

The **DaluxCloudUpload** extension lets you upload a local file to a specific folder inside a Dalux Field project. It navigates the full folder hierarchy you provide, detects whether a previous version of the file already exists (and replaces it), and attaches optional metadata to the uploaded file.

---

## Prerequisites

- A valid **Dalux API Key** (contact your Dalux administrator or Dalux support).
- The **Base URL** for your Dalux API node (default: `https://node1.field.dalux.com/service/api`).
- The exact **Dalux project name** as it appears in Dalux Field.
- The destination folder must reside inside a file area of type **`files`** (not `shared` or `published`).

---

## Configuration

| Field | Required | Description |
|---|---|---|
| **API Key** | ✅ | Your Dalux API Identity key. |
| **API Base URL** | | Base URL for the Dalux API. Defaults to `https://node1.field.dalux.com/service/api`. |
| **Project Name** | ✅ | The exact name of the Dalux project to upload into. |
| **File to Upload** | ✅ | Path to the local file you want to upload. Use the browse button to select a file. |
| **Destination Folder Path** | ✅ | Full folder path inside Dalux, starting with the file area name (e.g., `Files/C07_Geometry/C07_K07/Model`). |
| **Metadata** | | Optional key-value pairs to attach to the uploaded file as metadata. |

---

## How to Use

1. Open an **Assistant** task and add the **DaluxCloudUpload** extension.
2. Fill in your **API Key** and, if necessary, a custom **API Base URL**.
3. Enter the **Project Name** exactly as it appears in Dalux Field (case-insensitive).
4. Use the browse button to select the **File to Upload**.
5. Enter the **Destination Folder Path** using forward slashes, starting with the file area name:
   ```
   Files/C07_Geometry/C07_K07/Model
   ```
6. Optionally add **Metadata** key-value pairs.
7. Run the task.

### Folder Path Format

```
<FileAreaName>/<Folder1>/<Folder2>/.../<DestinationFolder>
```

**Example:** `Files/C07_Geometry/C07_K07/Model`

- The first segment (`Files`) must match a file area of type `files` in your project.
- Each subsequent segment must match an existing subfolder at that level (exact name match).

---

## Behavior

- If a file with the **same name already exists** in the destination folder, the extension will upload a **new revision** of that file.
- If the file does **not exist**, it will be created as a new file.
- On success, the extension reports the file name, size, file ID, revision ID, and upload timestamp.

---

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `File not found` | The local file path does not exist. | Verify the file path or re-select it using the browse button. |
| `Project not found` | The project name does not match any project in Dalux. | Check the exact project name in Dalux Field (comparison is case-insensitive). |
| `File area not found` | The first path segment does not match any file area. | The error message lists available file areas. |
| `File area is not of type 'files'` | The target file area is `shared` or `published`. | Choose a folder inside a `files`-type file area. |
| `Folder not found under ...` | A path segment does not match any subfolder at that level. | The error message lists available subfolders at the failing level. |
| `Failed to upload file` | The Dalux API rejected the upload. | Check the error message for API details (status code, developer hint). |
