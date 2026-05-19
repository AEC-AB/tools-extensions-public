using DaluxCloudUpload.Services;
using Meziantou.Framework.Win32;
using System.IO;

namespace DaluxCloudUpload;

public class DaluxCloudUploadCommand : IAssistantExtension<DaluxCloudUploadArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, DaluxCloudUploadArgs args, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = GetApiKey(args.ApiKey);
            if (apiKey is null)
            {
                return Result.Text.Failed("API key not found.");
            }
            // Validate file path
            if (!File.Exists(args.FilePath))
            {
                return Result.Text.Failed($"❌ File not found: {args.FilePath}");
            }

            // Validate folder path
            if (string.IsNullOrWhiteSpace(args.FolderPath))
            {
                return Result.Text.Failed($"❌ Folder path is required (e.g., 'Files/C07_Geometry/C07_K07/Model')");
            }

            var daluxService = new DaluxApiService(args.ApiKey, args.BaseUrl);


            var daluxProjects = await daluxService.GetProjectsAsync(cancellationToken);
            var project = daluxProjects.Data?.FirstOrDefault(p => p.Name.Equals(args.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (project == null)
            {
                return Result.Text.Failed($"❌ Project '{args.ProjectName}' not found.");
            }

            // Parse the folder path (e.g., "Files/C07_Geometry/C07_K07/Model")
            var pathParts = args.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0)
            {
                return Result.Text.Failed($"❌ Invalid folder path format. Expected format: 'FileAreaName/Folder1/Folder2/...'");
            }

            var fileAreaName = pathParts[0];
            var folderPathSegments = pathParts.Skip(1).ToList();

            // Get file areas to find the matching file area
            var fileAreasResponse = await daluxService.GetFileAreasAsync(project.Id, cancellationToken);
            if (!fileAreasResponse.IsSuccess || fileAreasResponse.Data?.Items == null)
            {
                return Result.Text.Failed($"❌ Failed to retrieve file areas: {fileAreasResponse.ErrorMessage}");
            }

            // Find the file area by name (case-insensitive)
            var fileArea = fileAreasResponse.Data.Items
                .FirstOrDefault(item => item.Data != null &&
                                       item.Data.FileAreaName.Equals(fileAreaName, StringComparison.OrdinalIgnoreCase));

            if (fileArea?.Data == null)
            {
                var availableAreas = string.Join(", ", fileAreasResponse.Data.Items
                    .Where(i => i.Data != null)
                    .Select(i => $"'{i.Data!.FileAreaName}'"));
                return Result.Text.Failed($"❌ File area '{fileAreaName}' not found. Available file areas: {availableAreas}");
            }

            var fileAreaId = fileArea.Data.FileAreaId;

            if (!fileArea.Data.FileAreaType.Equals("files", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Text.Failed($"❌ File area '{fileAreaName}' is of type '{fileArea.Data.FileAreaType}'. Only 'files' type file areas support uploads (not 'shared' or 'published').");
            }
            var foldersResponse = await daluxService.GetFoldersAsync(project.Id, fileAreaId, cancellationToken);
            if (!foldersResponse.IsSuccess || foldersResponse.Data?.Items == null)
            {
                return Result.Text.Failed($"❌ Failed to retrieve folders: {foldersResponse.ErrorMessage}");
            }

            // Build folder hierarchy map
            var allFolders = foldersResponse.Data.Items
                .Where(item => item.Data != null && !item.Data.Deleted)
                .Select(item => item.Data!)
                .ToList();

            // Step 1: Find the root folder (must have no parent and match file area name)
            var rootFolders = allFolders
                .Where(f => string.IsNullOrEmpty(f.ParentFolderId))
                .ToList();

            var rootFolder = rootFolders.FirstOrDefault(f =>
                f.FolderName.Equals(fileAreaName, StringComparison.OrdinalIgnoreCase));

            if (rootFolder == null)
            {
                var rootList = rootFolders.Any()
                    ? string.Join(", ", rootFolders.Select(f => $"'{f.FolderName}'"))
                    : "(no root folders)";
                return Result.Text.Failed($"❌ Root folder '{fileAreaName}' not found. Available root folders: {rootList}");
            }

            // Step 2: Navigate through each folder segment step-by-step
            string currentFolderId = rootFolder.FolderId;
            string currentPath = fileAreaName;

            foreach (var segment in folderPathSegments)
            {
                // Get subfolders of the current folder
                var subfolders = allFolders
                    .Where(f => f.ParentFolderId == currentFolderId)
                    .ToList();

                // Find the matching subfolder by normalizing both sides
                var matchingFolder = subfolders.FirstOrDefault(f =>
                    f.FolderName == segment);

                if (matchingFolder == null)
                {
                    var availableList = subfolders.Any()
                        ? string.Join(", ", subfolders.Select(f => $"'{f.FolderName}'"))
                        : "(no subfolders)";
                    return Result.Text.Failed($"❌ Folder '{segment}' not found under '{currentPath}'. Available subfolders: {availableList}");
                }

                // Move to the matching folder for next iteration
                currentFolderId = matchingFolder.FolderId;
                currentPath += "/" + segment;
            }

            // Get file info
            var fileInfo = new FileInfo(args.FilePath);
            var fileName = fileInfo.Name;
            var fileSizeMB = (fileInfo.Length / 1024.0 / 1024.0).ToString("F2");

            // Check if a file with the same name already exists in the destination folder
            string? existingFileId = null;
            var findFileResponse = await daluxService.FindFileInFolderAsync(project.Id, fileAreaId, currentFolderId, fileName, cancellationToken);
            if (findFileResponse.IsSuccess)
            {
                existingFileId = findFileResponse.Data?.FileId;
            }

            // Upload the file
            using var fileStream = File.OpenRead(args.FilePath);
            var uploadResult = await daluxService.UploadFileAsync(
                project.Id,
                fileAreaId,
                currentFolderId,
                fileStream,
                fileName,
                existingFileId,
                args.MetaData,
                cancellationToken
            );

            if (!uploadResult.IsSuccess)
            {
                return Result.Text.Failed($"❌ Failed to upload file: {uploadResult.ErrorMessage}");
            }

            var uploadedFile = uploadResult.Data;

            var message = $"✅ Successfully uploaded file to Dalux!\n\n" +
                         $"📁 Folder Path: {args.FolderPath}\n" +
                         $"📄 File Name: {fileName}\n" +
                         $"📊 File Size: {fileSizeMB} MB\n" +
                         $"🆔 File ID: {uploadedFile?.FileId ?? "N/A"}\n" +
                         $"🆔 File Revision ID: {uploadedFile?.FileRevisionId ?? "N/A"}\n" +
                         $"📅 Uploaded: {uploadedFile?.Uploaded?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

            return Result.Text.Succeeded(message);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed(
                $"❌ Unexpected error: {ex.Message}"
            );
        }
    }

    private static string? GetApiKey(string applicationName)
    {
        var creds = CredentialManager.ReadCredential(applicationName);
        if (!string.IsNullOrEmpty(creds?.Password))
        {
            return creds.Password;
        }

        return null;
    }
}