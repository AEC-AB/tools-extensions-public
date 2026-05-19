using DaluxCloudUpload.Models;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DaluxCloudUpload.Services;

/// <summary>
/// Service for interacting with the Dalux Field API v5.1
/// </summary>
public class DaluxApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public DaluxApiService(string apiKey, string baseUrl = "https://node1.field.dalux.com/service/api")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl;
        // Ensure base URL ends with / for proper path concatenation
        if (!_baseUrl.EndsWith("/"))
        {
            _baseUrl += "/";
        }
        _httpClient = new HttpClient();

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    private static bool IsNoAvailableMetadataPropertyError(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return false;

        return errorMessage.Contains("No available metadata property with ID", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            string content;
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch (InvalidOperationException)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                content = System.Text.Encoding.UTF8.GetString(bytes);
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var parts = new List<string>
                {
                    $"API request failed: {response.StatusCode} ({(int)response.StatusCode})"
                };

                if (root.TryGetProperty("message", out var message) && message.ValueKind != JsonValueKind.Null)
                    parts.Add($"Message: {message.GetString()}");

                if (root.TryGetProperty("developerHint", out var hint) && hint.ValueKind != JsonValueKind.Null)
                    parts.Add($"Developer Hint: {hint.GetString()}");

                if (root.TryGetProperty("errorCodeMessage", out var codeMsg) && codeMsg.ValueKind != JsonValueKind.Null)
                    parts.Add($"Error Code: {codeMsg.GetString()}");

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                {
                    var fieldErrors = new List<string>();
                    foreach (var error in errors.EnumerateArray())
                    {
                        var propName = error.TryGetProperty("propertyName", out var p) ? p.GetString() : "?";
                        var errMsg = error.TryGetProperty("errorMessage", out var e) ? e.GetString() : "unknown error";
                        var inputVal = error.TryGetProperty("inputValue", out var iv) && iv.ValueKind != JsonValueKind.Null ? iv.GetString() : null;
                        var devHint = error.TryGetProperty("developerHint", out var dh) && dh.ValueKind != JsonValueKind.Null ? dh.GetString() : null;

                        var detail = $"  [{propName}]: {errMsg}";
                        if (inputVal != null) detail += $" (value: '{inputVal}')";
                        if (devHint != null) detail += $" — {devHint}";
                        fieldErrors.Add(detail);
                    }
                    if (fieldErrors.Count > 0)
                        parts.Add("Field Errors:\n" + string.Join("\n", fieldErrors));
                }

                parts.Add($"Full Response: {content}");
                return string.Join("\n", parts);
            }
            catch
            {
                // Not JSON or couldn't parse, return as-is
            }

            return $"API request failed: {response.StatusCode} ({(int)response.StatusCode}). Response: {content}";
        }
        catch
        {
            return $"API request failed: {response.StatusCode} ({(int)response.StatusCode})";
        }
    }

    private DaluxApiResponse<T> HandleException<T>(Exception ex, string operation)
    {
        if (ex is HttpRequestException httpEx)
        {
            return DaluxApiResponse<T>.Failed(
                $"Network error during {operation}: {httpEx.Message}\n\n" +
                $"Base URL: {_baseUrl}\n" +
                $"Please verify:\n" +
                $"- Network connectivity\n" +
                $"- Correct API endpoint (may be region-specific)\n" +
                $"- VPN/Proxy requirements\n" +
                $"- Firewall settings"
            );
        }
        else if (ex is TaskCanceledException)
        {
            return DaluxApiResponse<T>.Failed(
                $"Timeout during {operation}. The API did not respond in time.\n" +
                $"Consider increasing the timeout or checking the API status."
            );
        }
        else
        {
            return DaluxApiResponse<T>.Failed($"Error during {operation}: {ex.Message}");
        }
    }

    #region Projects

    /// <summary>
    /// Get all projects accessible to the API identity
    /// </summary>
    public async Task<DaluxApiResponse<List<DaluxProject>>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("5.1/projects", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxProject>>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiListResponse<DaluxProject>>(cancellationToken: cancellationToken);

            // Extract projects from the wrapped response
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

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    public async Task<DaluxApiResponse<DaluxProject>> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.0/projects/{projectId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxProject>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiObjectResponse<DaluxProject>>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxProject>.Success(wrappedResponse?.Data);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxProject>(ex, "fetching project");
        }
    }

    #endregion

    #region Files (Box - Document Management)

    /// <summary>
    /// Search for documents and drawings across folders
    /// </summary>
    public async Task<DaluxApiResponse<List<DaluxDocument>>> GetFilesAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/files", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxDocument>>.Failed(errorMessage);
            }

            var documents = await response.Content.ReadFromJsonAsync<List<DaluxDocument>>(cancellationToken: cancellationToken);
            return DaluxApiResponse<List<DaluxDocument>>.Success(documents);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<List<DaluxDocument>>.Failed($"Error fetching files: {ex.Message}");
        }
    }

    /// <summary>
    /// Get files from a specific file area
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFilesResponse>> GetFilesAsync(string projectId, string fileAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"6.0/projects/{projectId}/file_areas/{fileAreaId}/files", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFilesResponse>.Failed(errorMessage);
            }

            var documents = await response.Content.ReadFromJsonAsync<DaluxFilesResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFilesResponse>.Success(documents);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<DaluxFilesResponse>.Failed($"Error fetching files from file area: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for a file by name inside a specific folder, paginating through all results.
    /// Returns the matched <see cref="DaluxFile"/> in <c>Data</c>, or <c>null</c> in <c>Data</c> when no match is found.
    /// </summary>
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
                    var errorMessage = await GetErrorMessageAsync(response);
                    return DaluxApiResponse<DaluxFile>.Failed(errorMessage);
                }

                var page = await response.Content.ReadFromJsonAsync<DaluxFilesResponse>(cancellationToken: cancellationToken);

                if (page?.Items == null || !page.Items.Any())
                    break;

                var match = page.Items
                    .Select(item => item.Data)
                    .FirstOrDefault(f =>
                        f != null &&
                        !f.Deleted &&
                        f.FolderId == folderId &&
                        string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return DaluxApiResponse<DaluxFile>.Success(match);

                nextUrl = page.Links?
                    .FirstOrDefault(l => string.Equals(l.Rel, "nextPage", StringComparison.OrdinalIgnoreCase))
                    ?.Href;
            }

            return DaluxApiResponse<DaluxFile>.Success(null);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFile>(ex, "finding file in folder");
        }
    }

    /// <summary>
    /// Get a specific file by ID
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFile>> GetFileAsync(string projectId, string fileAreaId, string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.0/projects/{projectId}/file_areas/{fileAreaId}/files/{fileId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFile>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiObjectResponse<DaluxFile>>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFile>.Success(wrappedResponse?.Data);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<DaluxFile>.Failed($"Error fetching file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file areas by project ID
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFileAreaResponse>> GetFileAreasAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/file_areas", cancellationToken); 
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFileAreaResponse>.Failed(errorMessage);
            }

            var fileAreaResponse = await response.Content.ReadFromJsonAsync<DaluxFileAreaResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFileAreaResponse>.Success(fileAreaResponse);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFileAreaResponse>(ex, "fetching file areas");
        }
    }

    /// <summary>
    /// Download the content of a specific file revision
    /// </summary>
    public async Task<DaluxApiResponse<byte[]>> GetFileContentAsync(string projectId, string fileAreaId, string fileId, string fileRevisionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"2.0/projects/{projectId}/file_areas/{fileAreaId}/files/{fileId}/revisions/{fileRevisionId}/content", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<byte[]>.Failed(errorMessage);
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return DaluxApiResponse<byte[]>.Success(content);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<byte[]>.Failed($"Error downloading file content: {ex.Message}");
        }
    }

    /// <summary>
    /// Get folders in a file area
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFoldersResponse>> GetFoldersAsync(string projectId, string fileAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/file_areas/{fileAreaId}/folders", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFoldersResponse>.Failed(errorMessage);
            }

            var folders = await response.Content.ReadFromJsonAsync<DaluxFoldersResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFoldersResponse>.Success(folders);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFoldersResponse>(ex, "fetching folders");
        }
    }

    /// <summary>
    /// Get all properties for each file type in a specific folder
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFilePropertiesResponse>> GetFolderFilePropertiesAsync(
        string projectId, 
        string fileAreaId, 
        string folderId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"1.0/projects/{projectId}/file_areas/{fileAreaId}/folders/{folderId}/files/properties/1.0/mappings", 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFilePropertiesResponse>.Failed(errorMessage);
            }

            var propertiesResponse = await response.Content.ReadFromJsonAsync<DaluxFilePropertiesResponse>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFilePropertiesResponse>.Success(propertiesResponse);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFilePropertiesResponse>(ex, "fetching folder file properties");
        }
    }

    /// <summary>
    /// Step 1: Create a new upload slot and return a GUID pointing to that slot
    /// </summary>
    public async Task<DaluxApiResponse<DaluxUploadSlotResponse>> CreateUploadSlotAsync(
        string projectId, 
        string fileAreaId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"1.0/projects/{projectId}/file_areas/{fileAreaId}/upload", 
                null, 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxUploadSlotResponse>.Failed(errorMessage);
            }

            var uploadSlotResponse = await response.Content.ReadFromJsonAsync<DaluxUploadSlotResponse>(cancellationToken: cancellationToken);
            var uploadGuid = uploadSlotResponse?.Data?.UploadGuid;

            if (string.IsNullOrEmpty(uploadGuid))
            {
                return DaluxApiResponse<DaluxUploadSlotResponse>.Failed("Failed to retrieve upload GUID from response");
            }

            return DaluxApiResponse<DaluxUploadSlotResponse>.Success(uploadSlotResponse);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxUploadSlotResponse>(ex, "creating upload slot");
        }
    }

    /// <summary>
    /// Step 2: Upload file content to the upload slot
    /// </summary>
    public async Task<DaluxApiResponse<long>> UploadFileContentAsync(
        string projectId, 
        string fileAreaId, 
        string uploadGuid, 
        Stream fileContent, 
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new StreamContent(fileContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Get file size for Content-Range header
            long fileSize = 0;
            if (fileContent.CanSeek)
            {
                fileSize = fileContent.Length;
                content.Headers.ContentLength = fileSize;
            }
            else
            {
                return DaluxApiResponse<long>.Failed("Stream must support seeking to determine file size for upload");
            }

            // Set Content-Disposition header with form-data format and URI-encoded filename
            var encodedFileName = Uri.EscapeDataString(fileName);
            var dispositionHeader = $"form-data; filename=\"{fileName}\"; filename*=UTF-8''{encodedFileName}";
            content.Headers.TryAddWithoutValidation("Content-Disposition", dispositionHeader);

            // Set Content-Range header (bytes 0-<end>/<total>)
            var contentRangeHeader = $"bytes 0-{fileSize - 1}/{fileSize}";
            content.Headers.TryAddWithoutValidation("Content-Range", contentRangeHeader);

            var response = await _httpClient.PostAsync(
                $"1.0/projects/{projectId}/file_areas/{fileAreaId}/upload/{uploadGuid}", 
                content, 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<long>.Failed(
                    $"Upload content failed with status {response.StatusCode}.\n" +
                    $"{errorMessage}\n" +
                    $"Expected: 202 Accepted"
                );
            }

            // API should return 202 for successful part upload
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                return DaluxApiResponse<long>.Failed(
                    $"⚠️ Warning: Expected status 202 (Accepted) but got {response.StatusCode}. " +
                    $"Upload may not have completed correctly."
                );
            }

            // Return file size so it can be used in finalize
            return DaluxApiResponse<long>.Success(fileSize);
        }
        catch (Exception ex)
        {
            return HandleException<long>(ex, "uploading file content");
        }
    }

    /// <summary>
    /// Step 3: Finalize the upload with file metadata
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFile>> FinalizeUploadAsync(
        string projectId, 
        string fileAreaId, 
        string uploadGuid, 
        DaluxFileUploadMetadata metadata, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Configure JSON serialization to ignore null values (API is sensitive to null properties)
            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            // Serialize to string for debugging
            var jsonBody = JsonSerializer.Serialize(metadata, jsonOptions);

            var jsonContent = JsonContent.Create(metadata, options: jsonOptions);

            var response = await _httpClient.PostAsync(
                $"2.0/projects/{projectId}/file_areas/{fileAreaId}/upload/{uploadGuid}/finalize",
                jsonContent,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.LoadIntoBufferAsync();
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxFile>.Failed(
                    $"{errorMessage}\n\n" +
                    $"Request Details:\n" +
                    $"Final URL: {response.RequestMessage?.RequestUri}\n" +
                    $"Upload GUID: {uploadGuid}\n" +
                    $"Request Body: {jsonBody}\n\n" +
                    $"Metadata Values:\n" +
                    $"- File Name: {metadata.FileName}\n" +
                    $"- File Type: {metadata.FileType}\n" +
                    $"- File ID: {metadata.FileId ?? "(null)"}\n" +
                    $"- Properties: {(metadata.Properties == null ? "(null)" : $"{metadata.Properties.Count} items")}"
                );
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiObjectResponse<DaluxFile>>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxFile>.Success(wrappedResponse?.Data);
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFile>(ex, "finalizing upload");
        }
    }

    /// <summary>
    /// Complete file upload workflow: creates upload slot, uploads content, and finalizes with metadata.
    /// If <paramref name="existingFileId"/> is provided the upload is treated as a new revision of that file;
    /// otherwise the file is created in <paramref name="folderId"/>.
    /// </summary>
    public async Task<DaluxApiResponse<DaluxFile>> UploadFileAsync(
        string projectId, 
        string fileAreaId, 
        string folderId, 
        Stream fileContent, 
        string fileName,
        string? existingFileId = null,
        Dictionary<string, string> metaDataInput = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createSlotResult = await CreateUploadSlotAsync(projectId, fileAreaId, cancellationToken);
            if (!createSlotResult.IsSuccess)
            {
                return DaluxApiResponse<DaluxFile>.Failed($"Failed to create upload slot: {createSlotResult.ErrorMessage}");
            }

            var uploadGuid = createSlotResult.Data?.Data?.UploadGuid;
            if( uploadGuid == null) {
                return DaluxApiResponse<DaluxFile>.Failed("Upload GUID is null after creating upload slot");
            }

            var uploadContentResult = await UploadFileContentAsync(projectId, fileAreaId, uploadGuid, fileContent, fileName, cancellationToken);
            if (!uploadContentResult.IsSuccess)
            {
                return DaluxApiResponse<DaluxFile>.Failed($"Failed to upload file content: {uploadContentResult.ErrorMessage}");
            }

            DaluxFileUploadMetadata metadata;

            if (existingFileId != null)
            {
                var existingFileResult = await GetFileAsync(projectId, fileAreaId, existingFileId, cancellationToken);
                if (!existingFileResult.IsSuccess || existingFileResult.Data == null)
                {
                    return DaluxApiResponse<DaluxFile>.Failed($"Failed to retrieve existing file metadata: {existingFileResult.ErrorMessage}");
                }

                var existingFile = existingFileResult.Data;
                var fileType = existingFile.FileType ?? "model";

                // Get valid properties for this file type from the folder configuration
                HashSet<string>? validPropertyKeys = null;
                Dictionary<string, string>? propertyKeyToTitle = null;
                Dictionary<string, string>? propertyKeyToValueType = null;
                var effectiveFolderId = existingFile.FolderId ?? folderId;

                if (!string.IsNullOrEmpty(effectiveFolderId))
                {
                    var folderPropsResult = await GetFolderFilePropertiesAsync(projectId, fileAreaId, effectiveFolderId, cancellationToken);
                    if (folderPropsResult.IsSuccess && folderPropsResult.Data?.Items != null)
                    {
                        // Build a set of property keys, titles and valueTypes that are valid for this specific file type
                        validPropertyKeys = new HashSet<string>();
                        propertyKeyToTitle = new Dictionary<string, string>();
                        propertyKeyToValueType = new Dictionary<string, string>();
                        foreach (var item in folderPropsResult.Data.Items)
                        {
                            if (item.Data?.Key == null) continue;

                            var prop = item.Data;
                            if (prop.Key == "170682181460")
                                continue; 
                            bool isValid = fileType.ToLowerInvariant() switch
                            {
                                "model" => prop.RequiredOnModels || (!prop.RequiredOnDocuments && !prop.RequiredOnDrawings),
                                "drawing" => prop.RequiredOnDrawings || (!prop.RequiredOnDocuments && !prop.RequiredOnModels),
                                "document" => prop.RequiredOnDocuments || (!prop.RequiredOnDrawings && !prop.RequiredOnModels),
                                _ => true
                            };

                            if (isValid)
                            {
                                validPropertyKeys.Add(prop.Key);
                                if (!string.IsNullOrEmpty(prop.Title))
                                    propertyKeyToTitle[prop.Key] = prop.Title;
                                if (!string.IsNullOrEmpty(prop.ValueType))
                                    propertyKeyToValueType[prop.Key] = prop.ValueType;
                            }
                        }
                    }
                }

                // Map metadataInput with property titles and build properties list
                List<DaluxFileProperty>? properties = null;
                if (metaDataInput != null && propertyKeyToTitle != null && propertyKeyToTitle.Count > 0)
                {
                    properties = new List<DaluxFileProperty>();
                    foreach (var kvp in metaDataInput)
                    {
                        // Find the property key by matching the title
                        var matchingKey = propertyKeyToTitle.FirstOrDefault(p => 
                            string.Equals(p.Value, kvp.Key, StringComparison.OrdinalIgnoreCase)).Key;

                        if (!string.IsNullOrEmpty(matchingKey) && validPropertyKeys != null && validPropertyKeys.Contains(matchingKey))
                        {
                            var valueType = propertyKeyToValueType != null && propertyKeyToValueType.TryGetValue(matchingKey, out var vt) ? vt : "text";
                            properties.Add(new DaluxFileProperty
                            {
                                Key = matchingKey,
                                Name = kvp.Key,
                                Values = new List<DaluxFilePropertyValue>
                                {
                                    new DaluxFilePropertyValue { ValueType = valueType, Text = kvp.Value }
                                }
                            });
                        }
                    }
                }

                metadata = new DaluxFileUploadMetadata
                {
                    FileId = existingFileId,
                    FileName = existingFile.FileName,
                    FileType = fileType,
                    Properties = properties
                };       
            }
            else
            {
                metadata = new DaluxFileUploadMetadata
                {
                    FolderId = folderId,
                    FileName = fileName
                };
            }

            var finalizeResult = await FinalizeUploadAsync(projectId, fileAreaId, uploadGuid, metadata, cancellationToken);
            return finalizeResult;
        }
        catch (Exception ex)
        {
            return HandleException<DaluxFile>(ex, "uploading file");
        }
    }

    #endregion

    #region Tasks

    /// <summary>
    /// Get tasks for a specific project with optional OData filtering
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="filter">Optional: OData filter expression on typeId, e.g. "data/type/typeId eq '177352982697'"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DaluxApiResponse<List<DaluxTask>>> GetTasksAsync(
        string projectId, 
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = !string.IsNullOrEmpty(filter) ? $"?$filter={Uri.EscapeDataString(filter)}" : "";
            var response = await _httpClient.GetAsync($"5.2/projects/{projectId}/tasks{queryString}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxTask>>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxApiListResponse<DaluxTask>>(cancellationToken: cancellationToken);
            var tasks = wrappedResponse?.Items?
                .Where(item => item.Data != null)
                .Select(item => item.Data!)
                .ToList() ?? new List<DaluxTask>();
            return DaluxApiResponse<List<DaluxTask>>.Success(tasks);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<List<DaluxTask>>.Failed($"Error fetching tasks: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new task in a project (e.g., from external audit tool)
    /// </summary>
    public async Task<DaluxApiResponse<DaluxTask>> CreateTaskAsync(string projectId, DaluxTaskCreate taskData, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"5.1/projects/{projectId}/tasks", taskData, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxTask>.Failed(errorMessage);
            }

            var task = await response.Content.ReadFromJsonAsync<DaluxTask>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxTask>.Success(task);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<DaluxTask>.Failed($"Error creating task: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing task using PATCH (as per Dalux API spec - no PUT support)
    /// </summary>
    public async Task<DaluxApiResponse<DaluxTask>> UpdateTaskAsync(string projectId, string taskId, DaluxTaskUpdate taskData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"5.1/projects/{projectId}/tasks/{taskId}")
            {
                Content = JsonContent.Create(taskData)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxTask>.Failed(errorMessage);
            }

            var task = await response.Content.ReadFromJsonAsync<DaluxTask>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxTask>.Success(task);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<DaluxTask>.Failed($"Error updating task: {ex.Message}");
        }
    }

    #endregion

    #region Users

    /// <summary>
    /// Get users for a specific project (for mapping IDs in task assignments)
    /// </summary>
    public async Task<DaluxApiResponse<List<DaluxUser>>> GetProjectUsersAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"1.2/projects/{projectId}/users", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxUser>>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxDirectListResponse<DaluxUser>>(cancellationToken: cancellationToken);
            var users = wrappedResponse?.Items ?? new List<DaluxUser>();
            return DaluxApiResponse<List<DaluxUser>>.Success(users);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<List<DaluxUser>>.Failed($"Error fetching users: {ex.Message}");
        }
    }

    #endregion

    #region Checklists & Inspection Plans

    /// <summary>
    /// Get checklists for quality control data
    /// </summary>
    public async Task<DaluxApiResponse<List<DaluxChecklist>>> GetChecklistsAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"5.1/projects/{projectId}/checklists", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxChecklist>>.Failed(errorMessage);
            }

            var checklists = await response.Content.ReadFromJsonAsync<List<DaluxChecklist>>(cancellationToken: cancellationToken);
            return DaluxApiResponse<List<DaluxChecklist>>.Success(checklists);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<List<DaluxChecklist>>.Failed($"Error fetching checklists: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a checklist item (close points or add comments)
    /// </summary>
    public async Task<DaluxApiResponse<DaluxChecklist>> UpdateChecklistAsync(string projectId, string checklistId, DaluxChecklistUpdate checklistData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"5.1/projects/{projectId}/checklists/{checklistId}")
            {
                Content = JsonContent.Create(checklistData)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<DaluxChecklist>.Failed(errorMessage);
            }

            var checklist = await response.Content.ReadFromJsonAsync<DaluxChecklist>(cancellationToken: cancellationToken);
            return DaluxApiResponse<DaluxChecklist>.Success(checklist);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<DaluxChecklist>.Failed($"Error updating checklist: {ex.Message}");
        }
    }

    #endregion

    #region Work Packages

    /// <summary>
    /// Get work packages for a project (for ERP sync - costs/procurement)
    /// </summary>
    public async Task<DaluxApiResponse<List<DaluxWorkPackage>>> GetWorkPackagesAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"1.0/projects/{projectId}/workpackages", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                return DaluxApiResponse<List<DaluxWorkPackage>>.Failed(errorMessage);
            }

            var wrappedResponse = await response.Content.ReadFromJsonAsync<DaluxDirectListResponse<DaluxWorkPackage>>(cancellationToken: cancellationToken);
            var workPackages = wrappedResponse?.Items ?? new List<DaluxWorkPackage>();
            return DaluxApiResponse<List<DaluxWorkPackage>>.Success(workPackages);
        }
        catch (Exception ex)
        {
            return DaluxApiResponse<List<DaluxWorkPackage>>.Failed($"Error fetching work packages: {ex.Message}");
        }
    }

    #endregion
}
