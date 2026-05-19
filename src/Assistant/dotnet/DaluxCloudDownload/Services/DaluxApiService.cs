using DaluxCloudDownload.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DaluxCloudDownload.Services;

public class DaluxApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public DaluxApiService(string apiKey, string baseUrl = "https://node1.field.dalux.com/service/api")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        _baseUrl = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
        _httpClient = new HttpClient();

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<DaluxApiResponse<List<DaluxProject>>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("5.1/projects", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return DaluxApiResponse<List<DaluxProject>>.Failed(await GetErrorMessageAsync(response, cancellationToken));
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiListResponse<DaluxProject>>(cancellationToken: cancellationToken);
            var projects = wrappedResponse?.Items?
                .Where(item => item.Data != null)
                .Select(item => item.Data!)
                .ToList() ?? new List<DaluxProject>();

            return DaluxApiResponse<List<DaluxProject>>.Success(projects);
        }
        catch (Exception ex)
        {
            return HandleException<List<DaluxProject>>(ex, "fetching projects");
        }
    }

    public async Task<DaluxApiResponse<DaluxFileAreaResponse>> GetFileAreasAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/file_areas", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return DaluxApiResponse<DaluxFileAreaResponse>.Failed(await GetErrorMessageAsync(response, cancellationToken));
            }

            var fileAreas = await response.Content.ReadFromJsonAsync<DaluxFileAreaResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFileAreaResponse>.Success(fileAreas);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFileAreaResponse>(ex, "fetching file areas");
        }
    }

    public async Task<DaluxApiResponse<DaluxFoldersResponse>> GetFoldersAsync(string projectId, string fileAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/file_areas/{fileAreaId}/folders", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return DaluxApiResponse<DaluxFoldersResponse>.Failed(await GetErrorMessageAsync(response, cancellationToken));
            }

            var folders = await response.Content.ReadFromJsonAsync<DaluxFoldersResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFoldersResponse>.Success(folders);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFoldersResponse>(ex, "fetching folders");
        }
    }

    public async Task<DaluxApiResponse<DaluxFile>> FindFileInFolderAsync(
        string projectId,
        string fileAreaId,
        string folderId,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? nextUrl = $"6.0/projects/{projectId}/file_areas/{fileAreaId}/files";

            while (nextUrl != null)
            {
                var response = await _httpClient.GetAsync(nextUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return DaluxApiResponse<DaluxFile>.Failed(await GetErrorMessageAsync(response, cancellationToken));
                }

                var page = await response.Content.ReadFromJsonAsync<DaluxFilesResponse>(cancellationToken: cancellationToken);
                if (page?.Items == null || page.Items.Count == 0)
                {
                    break;
                }

                var match = page.Items
                    .Select(item => item.Data)
                    .FirstOrDefault(file =>
                        file != null &&
                        !file.Deleted &&
                        file.FolderId == folderId &&
                        string.Equals(file.FileName, fileName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    return DaluxApiResponse<DaluxFile>.Success(match);
                }

                nextUrl = page.Links?
                    .FirstOrDefault(link => string.Equals(link.Rel, "nextPage", StringComparison.OrdinalIgnoreCase))?
                    .Href;
            }

            return DaluxApiResponse<DaluxFile>.Success(null);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFile>(ex, "finding file in folder");
        }
    }

    public async Task<DaluxApiResponse<byte[]>> GetFileContentAsync(
        string projectId,
        string fileAreaId,
        string fileId,
        string fileRevisionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"2.0/projects/{projectId}/file_areas/{fileAreaId}/files/{fileId}/revisions/{fileRevisionId}/content",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return DaluxApiResponse<byte[]>.Failed(await GetErrorMessageAsync(response, cancellationToken));
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return DaluxApiResponse<byte[]>.Success(content);
        }
        catch (Exception ex)
        {
            return HandleException<byte[]>(ex, "downloading file content");
        }
    }

    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                var parts = new List<string>
                {
                    $"API request failed: {response.StatusCode} ({(int)response.StatusCode})"
                };

                if (root.TryGetProperty("message", out var message) && message.ValueKind != JsonValueKind.Null)
                {
                    parts.Add($"Message: {message.GetString()}");
                }

                if (root.TryGetProperty("developerHint", out var hint) && hint.ValueKind != JsonValueKind.Null)
                {
                    parts.Add($"Developer Hint: {hint.GetString()}");
                }

                parts.Add($"Full Response: {content}");
                return string.Join("\n", parts);
            }
            catch
            {
                return $"API request failed: {response.StatusCode} ({(int)response.StatusCode}). Response: {content}";
            }
        }
        catch
        {
            return $"API request failed: {response.StatusCode} ({(int)response.StatusCode})";
        }
    }

    private DaluxApiResponse<T> HandleException<T>(Exception exception, string operation)
    {
        if (exception is HttpRequestException httpException)
        {
            return DaluxApiResponse<T>.Failed(
                $"Network error during {operation}: {httpException.Message}\n\n" +
                $"Base URL: {_baseUrl}");
        }

        if (exception is TaskCanceledException)
        {
            return DaluxApiResponse<T>.Failed($"Timeout during {operation}.");
        }

        return DaluxApiResponse<T>.Failed($"Error during {operation}: {exception.Message}");
    }

    public async Task<DaluxApiResponse<List<DaluxFile>>> GetAllFilesInFolderRecursiveAsync(
        string projectId,
        string fileAreaId,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allFiles = new List<DaluxFile>();
            var folderMap = new Dictionary<string, string>();

            // Get all folders
            var foldersResponse = await GetFoldersAsync(projectId, fileAreaId, cancellationToken);
            if (!foldersResponse.IsSuccess || foldersResponse.Data?.Items == null)
            {
                return DaluxApiResponse<List<DaluxFile>>.Failed("Failed to retrieve folders.");
            }

            var allFolders = foldersResponse.Data.Items
                .Where(item => item.Data != null && !item.Data.Deleted)
                .Select(item => item.Data!)
                .ToList();

            // Build folder name map
            foreach (var folder in allFolders)
            {
                folderMap[folder.FolderId] = folder.FolderName;
            }

            // Collect target folder and all its children
            var targetFolderIds = new List<string> { folderId };
            var toProcess = new Queue<string>(new[] { folderId });

            while (toProcess.Count > 0)
            {
                var currentId = toProcess.Dequeue();
                var childFolders = allFolders
                    .Where(f => f.ParentFolderId == currentId)
                    .ToList();

                foreach (var child in childFolders)
                {
                    targetFolderIds.Add(child.FolderId);
                    toProcess.Enqueue(child.FolderId);
                }
            }

            // Get all files and filter by target folders
            string? nextUrl = $"6.0/projects/{projectId}/file_areas/{fileAreaId}/files";

            while (nextUrl != null)
            {
                var response = await _httpClient.GetAsync(nextUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return DaluxApiResponse<List<DaluxFile>>.Failed(await GetErrorMessageAsync(response, cancellationToken));
                }

                var page = await response.Content.ReadFromJsonAsync<DaluxFilesResponse>(cancellationToken: cancellationToken);
                if (page?.Items == null || page.Items.Count == 0)
                {
                    break;
                }

                foreach (var fileItem in page.Items)
                {
                    var file = fileItem.Data;
                    if (file != null && !file.Deleted && file.FolderId != null && targetFolderIds.Contains(file.FolderId))
                    {
                        // Build relative path
                        var relativePath = BuildRelativePath(file.FolderId, folderId, allFolders, folderMap);
                        file.RelativePath = relativePath;
                        allFiles.Add(file);
                    }
                }

                nextUrl = page.Links?
                    .FirstOrDefault(link => string.Equals(link.Rel, "nextPage", StringComparison.OrdinalIgnoreCase))?
                    .Href;
            }

            return DaluxApiResponse<List<DaluxFile>>.Success(allFiles);
        }
        catch (Exception ex)
        {
            return HandleException<List<DaluxFile>>(ex, "getting files recursively");
        }
    }

    private string BuildRelativePath(string fileFolderId, string rootFolderId, List<DaluxFolder> allFolders, Dictionary<string, string> folderMap)
    {
        if (fileFolderId == rootFolderId)
        {
            return string.Empty;
        }

        var pathSegments = new Stack<string>();
        var current = fileFolderId;

        while (current != rootFolderId && !string.IsNullOrEmpty(current))
        {
            if (folderMap.TryGetValue(current, out var folderName))
            {
                pathSegments.Push(folderName);
            }

            var folder = allFolders.FirstOrDefault(f => f.FolderId == current);
            if (folder == null)
            {
                break;
            }

            current = folder.ParentFolderId ?? string.Empty;
        }

        return string.Join('/', pathSegments);
    }
}