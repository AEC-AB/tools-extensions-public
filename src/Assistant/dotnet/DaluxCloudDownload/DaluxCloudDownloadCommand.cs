using DaluxCloudDownload.Models;
using DaluxCloudDownload.Services;
using System.IO;

namespace DaluxCloudDownload;

public class DaluxCloudDownloadCommand : IAssistantExtension<DaluxCloudDownloadArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, DaluxCloudDownloadArgs args, CancellationToken cancellationToken)
    {
        try
        {
            if (args.FilePaths == null || args.FilePaths.Count == 0)
            {
                return Result.Text.Failed("At least one Dalux File Path is required.");
            }

            if (string.IsNullOrWhiteSpace(args.OutputFolder))
            {
                return Result.Text.Failed("Output Folder is required.");
            }

            if (!Directory.Exists(args.OutputFolder))
            {
                return Result.Text.Failed($"Output folder not found: {args.OutputFolder}");
            }

            var daluxService = new DaluxApiService(args.ApiKey, args.BaseUrl);

            var projectResponse = await daluxService.GetProjectsAsync(cancellationToken);
            if (!projectResponse.IsSuccess || projectResponse.Data == null)
            {
                return Result.Text.Failed($"Failed to retrieve projects: {projectResponse.ErrorMessage}");
            }

            var project = projectResponse.Data.FirstOrDefault(p => p.Name.Equals(args.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (project == null)
            {
                return Result.Text.Failed($"Project '{args.ProjectName}' not found.");
            }

            var successCount = 0;
            var failureCount = 0;
            var failures = new List<(string FilePath, string Error)>();

            foreach (var filePath in args.FilePaths)
            {
                var result = await DownloadSingleFileAsync(
                    daluxService,
                    project.Id,
                    args.OutputFolder,
                    filePath,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                    failures.Add((filePath, result.ErrorMessage ?? "Unknown error"));
                }
            }

            var summary = $"Download Summary for Project '{project.Name}':\n\n" +
                          $"✅ Successful: {successCount}\n" +
                          $"❌ Failed: {failureCount}\n" +
                          $"📁 Output Folder: {args.OutputFolder}";

            if (failures.Count > 0)
            {
                summary += "\n\nFailures:\n";
                foreach (var (fp, error) in failures)
                {
                    summary += $"  • {fp}: {error}\n";
                }

                return failureCount == args.FilePaths.Count
                    ? Result.Text.Failed(summary)
                    : Result.Text.PartiallySucceeded(summary);
            }

            return Result.Text.Succeeded(summary);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Unexpected error: {ex.Message}");
        }
    }

    private static DaluxApiResponse<ParsedDaluxFilePath> ParseDaluxFilePath(string filePath)
    {
        var trimmedPath = filePath.Trim();
        // Normalize both forward and backslashes to forward slashes
        var normalizedPath = trimmedPath.Replace('\\', '/');
        var pathParts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (pathParts.Length < 2)
        {
            return DaluxApiResponse<ParsedDaluxFilePath>.Failed(
                "Invalid Dalux file path format. Expected format: 'FileAreaName/Folder1/.../FileName.ext'.");
        }

        var fileName = pathParts[^1];
        var folderPath = string.Join('/', pathParts.Take(pathParts.Length - 1));

        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(folderPath))
        {
            return DaluxApiResponse<ParsedDaluxFilePath>.Failed(
                "Invalid Dalux file path format. Expected format: 'FileAreaName/Folder1/.../FileName.ext'.");
        }

        return DaluxApiResponse<ParsedDaluxFilePath>.Success(new ParsedDaluxFilePath
        {
            FolderPath = folderPath,
            FileName = fileName
        });
    }

    private static async Task<DaluxApiResponse<ResolvedDaluxFolder>> ResolveFolderAsync(
        DaluxApiService daluxService,
        string projectId,
        string folderPath,
        CancellationToken cancellationToken)
    {
        var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathParts.Length == 0)
        {
            return DaluxApiResponse<ResolvedDaluxFolder>.Failed("Invalid folder path format. Expected format: 'FileAreaName/Folder1/Folder2/...'.");
        }

        var fileAreaName = pathParts[0];
        var folderSegments = pathParts.Skip(1).ToList();

        var fileAreasResponse = await daluxService.GetFileAreasAsync(projectId, cancellationToken);
        if (!fileAreasResponse.IsSuccess || fileAreasResponse.Data?.Items == null)
        {
            return DaluxApiResponse<ResolvedDaluxFolder>.Failed($"Failed to retrieve file areas: {fileAreasResponse.ErrorMessage}");
        }

        var fileArea = fileAreasResponse.Data.Items
            .FirstOrDefault(item => item.Data != null &&
                                    item.Data.FileAreaName.Equals(fileAreaName, StringComparison.OrdinalIgnoreCase));

        if (fileArea?.Data == null)
        {
            var availableAreas = string.Join(", ", fileAreasResponse.Data.Items
                .Where(item => item.Data != null)
                .Select(item => $"'{item.Data!.FileAreaName}'"));

            return DaluxApiResponse<ResolvedDaluxFolder>.Failed(
                $"File area '{fileAreaName}' not found. Available file areas: {availableAreas}");
        }

        var foldersResponse = await daluxService.GetFoldersAsync(projectId, fileArea.Data.FileAreaId, cancellationToken);
        if (!foldersResponse.IsSuccess || foldersResponse.Data?.Items == null)
        {
            return DaluxApiResponse<ResolvedDaluxFolder>.Failed($"Failed to retrieve folders: {foldersResponse.ErrorMessage}");
        }

        var allFolders = foldersResponse.Data.Items
            .Where(item => item.Data != null && !item.Data.Deleted)
            .Select(item => item.Data!)
            .ToList();

        var rootFolders = allFolders
            .Where(folder => string.IsNullOrEmpty(folder.ParentFolderId))
            .ToList();

        var currentFolder = rootFolders.FirstOrDefault(folder =>
            folder.FolderName.Equals(fileAreaName, StringComparison.OrdinalIgnoreCase));

        if (currentFolder == null)
        {
            var availableRoots = rootFolders.Any()
                ? string.Join(", ", rootFolders.Select(folder => $"'{folder.FolderName}'"))
                : "(no root folders)";

            return DaluxApiResponse<ResolvedDaluxFolder>.Failed(
                $"Root folder '{fileAreaName}' not found. Available root folders: {availableRoots}");
        }

        var currentPath = fileAreaName;

        foreach (var segment in folderSegments)
        {
            var subfolders = allFolders
                .Where(folder => folder.ParentFolderId == currentFolder.FolderId)
                .ToList();

            var nextFolder = subfolders.FirstOrDefault(folder =>
                folder.FolderName.Equals(segment, StringComparison.Ordinal));

            if (nextFolder == null)
            {
                var availableSubfolders = subfolders.Any()
                    ? string.Join(", ", subfolders.Select(folder => $"'{folder.FolderName}'"))
                    : "(no subfolders)";

                return DaluxApiResponse<ResolvedDaluxFolder>.Failed(
                    $"Folder '{segment}' not found under '{currentPath}'. Available subfolders: {availableSubfolders}");
            }

            currentFolder = nextFolder;
            currentPath += "/" + segment;
        }

        return DaluxApiResponse<ResolvedDaluxFolder>.Success(new ResolvedDaluxFolder
        {
            FileAreaId = fileArea.Data.FileAreaId,
            FolderId = currentFolder.FolderId
        });
    }

    private sealed class ResolvedDaluxFolder
    {
        public string FileAreaId { get; set; } = string.Empty;

        public string FolderId { get; set; } = string.Empty;
    }

    private sealed class ParsedDaluxFilePath
    {
        public string FolderPath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }

    private async Task<DaluxApiResponse<string>> DownloadSingleFileAsync(
        DaluxApiService daluxService,
        string projectId,
        string outputFolder,
        string filePath,
        CancellationToken cancellationToken)
    {
        var trimmedPath = filePath.Trim().Replace('\\', '/');
        
        // Try as file first
        var fileResult = await TryDownloadAsFileAsync(daluxService, projectId, outputFolder, trimmedPath, cancellationToken);
        if (fileResult.IsSuccess)
        {
            return fileResult;
        }

        // If file not found, try as folder
        return await DownloadFolderContentsAsync(daluxService, projectId, outputFolder, trimmedPath, cancellationToken);
    }

    private async Task<DaluxApiResponse<string>> TryDownloadAsFileAsync(
        DaluxApiService daluxService,
        string projectId,
        string outputFolder,
        string filePath,
        CancellationToken cancellationToken)
    {
        var pathParts = filePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathParts.Length < 2)
        {
            return DaluxApiResponse<string>.Failed("Invalid path format.");
        }

        var fileName = pathParts[^1];
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
        {
            // No extension, likely a folder, not a file
            return DaluxApiResponse<string>.Failed("Not a file (no extension found).");
        }

        var folderPath = string.Join('/', pathParts.Take(pathParts.Length - 1));
        var folderResolve = await ResolveFolderAsync(daluxService, projectId, folderPath, cancellationToken);
        if (!folderResolve.IsSuccess || folderResolve.Data == null)
        {
            return DaluxApiResponse<string>.Failed(folderResolve.ErrorMessage ?? "Failed to resolve folder.");
        }

        var target = folderResolve.Data;
        var findFile = await daluxService.FindFileInFolderAsync(
            projectId,
            target.FileAreaId,
            target.FolderId,
            fileName,
            cancellationToken);

        if (!findFile.IsSuccess || findFile.Data == null)
        {
            return DaluxApiResponse<string>.Failed("File not found.");
        }

        if (string.IsNullOrWhiteSpace(findFile.Data.FileRevisionId))
        {
            return DaluxApiResponse<string>.Failed("File has no downloadable revision.");
        }

        var content = await daluxService.GetFileContentAsync(
            projectId,
            target.FileAreaId,
            findFile.Data.FileId,
            findFile.Data.FileRevisionId,
            cancellationToken);

        if (!content.IsSuccess || content.Data == null)
        {
            return DaluxApiResponse<string>.Failed($"Failed to download: {content.ErrorMessage}");
        }

        var outputPath = Path.Combine(outputFolder, findFile.Data.FileName);
        await File.WriteAllBytesAsync(outputPath, content.Data, cancellationToken);
        return DaluxApiResponse<string>.Success(outputPath);
    }

    private async Task<DaluxApiResponse<string>> DownloadFolderContentsAsync(
        DaluxApiService daluxService,
        string projectId,
        string outputFolder,
        string folderPath,
        CancellationToken cancellationToken)
    {
        var folderResolve = await ResolveFolderAsync(daluxService, projectId, folderPath, cancellationToken);
        if (!folderResolve.IsSuccess || folderResolve.Data == null)
        {
            return DaluxApiResponse<string>.Failed(folderResolve.ErrorMessage ?? "Failed to resolve folder.");
        }

        var target = folderResolve.Data;
        var files = await daluxService.GetAllFilesInFolderRecursiveAsync(
            projectId,
            target.FileAreaId,
            target.FolderId,
            cancellationToken);

        if (!files.IsSuccess || files.Data == null || files.Data.Count == 0)
        {
            return DaluxApiResponse<string>.Failed("No files found in folder.");
        }

        var downloadedCount = 0;
        var failedCount = 0;

        foreach (var file in files.Data)
        {
            if (string.IsNullOrWhiteSpace(file.FileRevisionId))
            {
                failedCount++;
                continue;
            }

            var content = await daluxService.GetFileContentAsync(
                projectId,
                target.FileAreaId,
                file.FileId,
                file.FileRevisionId,
                cancellationToken);

            if (!content.IsSuccess || content.Data == null)
            {
                failedCount++;
                continue;
            }

            var relativePath = string.IsNullOrEmpty(file.RelativePath) ? file.FileName : $"{file.RelativePath}/{file.FileName}";
            var outputPath = Path.Combine(outputFolder, relativePath);
            var outputDir = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            await File.WriteAllBytesAsync(outputPath, content.Data, cancellationToken);
            downloadedCount++;
        }

        if (downloadedCount == 0)
        {
            return DaluxApiResponse<string>.Failed($"Failed to download any files ({failedCount} had errors).");
        }

        return DaluxApiResponse<string>.Success($"Downloaded {downloadedCount} file(s)");
    }
}