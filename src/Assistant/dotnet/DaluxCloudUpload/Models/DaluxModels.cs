using System.Text.Json.Serialization;

namespace DaluxCloudUpload.Models;

#region Response Wrapper

/// <summary>
/// Generic response wrapper for Dalux API calls
/// </summary>
public class DaluxApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static DaluxApiResponse<T> Success(T data)
    {
        return new DaluxApiResponse<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static DaluxApiResponse<T> Failed(string errorMessage)
    {
        return new DaluxApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

#endregion

#region API Response Wrappers

/// <summary>
/// Wrapper for API responses that return items in an array, where each item has a 'data' property
/// </summary>
public class DaluxApiListResponse<T>
{
    [JsonPropertyName("items")]
    public List<DaluxApiItemWrapper<T>>? Items { get; set; }
}

/// <summary>
/// Wrapper for individual items that have a 'data' property
/// </summary>
public class DaluxApiItemWrapper<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Wrapper for API responses that return items directly in an array (no 'data' wrapper per item)
/// </summary>
public class DaluxDirectListResponse<T>
{
    [JsonPropertyName("items")]
    public List<T>? Items { get; set; }
}

/// <summary>
/// Wrapper for API responses that return a single object in a 'data' property
/// </summary>
public class DaluxApiObjectResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

#endregion

#region Project Models

public class DaluxProject
{
    [JsonPropertyName("projectId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("number")]
    public string? ProjectNumber { get; set; }

    [JsonPropertyName("created")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("modified")]
    public DateTime? ModifiedDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("modules")]
    public List<DaluxModule>? Modules { get; set; }
}

public class DaluxModule
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

#endregion

#region Document Models

public class DaluxDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("uploadDate")]
    public DateTime? UploadDate { get; set; }

    [JsonPropertyName("uploadedBy")]
    public string? UploadedBy { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

#endregion

#region FileArea Models

/// <summary>
/// Represents a file area response with metadata
/// </summary>
public class DaluxFileAreaResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFileAreaItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxMetadata? Metadata { get; set; }
}

/// <summary>
/// Wrapper for individual file area items
/// </summary>
public class DaluxFileAreaItem
{
    [JsonPropertyName("data")]
    public DaluxFileArea? Data { get; set; }

    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }
}

/// <summary>
/// File area data model
/// </summary>
public class DaluxFileArea
{
    [JsonPropertyName("fileAreaId")]
    public string FileAreaId { get; set; } = string.Empty;

    [JsonPropertyName("fileAreaName")]
    public string FileAreaName { get; set; } = string.Empty;

    [JsonPropertyName("fileAreaType")]
    public string FileAreaType { get; set; } = string.Empty;
}

/// <summary>
/// API link representation
/// </summary>
public class DaluxApiLink
{
    [JsonPropertyName("rel")]
    public string? Rel { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

/// <summary>
/// Metadata for paginated responses
/// </summary>
public class DaluxMetadata
{
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalRemainingItems")]
    public int TotalRemainingItems { get; set; }
}

#endregion

#region File Models

/// <summary>
/// Represents a file list response with metadata
/// </summary>
public class DaluxFilesResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFileItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxFileMetadata? Metadata { get; set; }
}

/// <summary>
/// Wrapper for individual file items
/// </summary>
public class DaluxFileItem
{
    [JsonPropertyName("data")]
    public DaluxFile? Data { get; set; }

    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }
}

/// <summary>
/// File data model
/// </summary>
public class DaluxFile
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("fileRevisionId")]
    public string FileRevisionId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileAreaId")]
    public string FileAreaId { get; set; } = string.Empty;

    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    [JsonPropertyName("uploadedByUserId")]
    public string? UploadedByUserId { get; set; }

    [JsonPropertyName("uploaded")]
    public DateTime? Uploaded { get; set; }

    [JsonPropertyName("lastModifiedByUserId")]
    public string? LastModifiedByUserId { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("contentHash")]
    public string? ContentHash { get; set; }

    [JsonPropertyName("downloadLink")]
    public string? DownloadLink { get; set; }

    [JsonPropertyName("properties")]
    public List<DaluxFileProperty>? Properties { get; set; }
}

/// <summary>
/// File property model
/// </summary>
public class DaluxFileProperty
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("values")]
    public List<DaluxFilePropertyValue>? Values { get; set; }
}

/// <summary>
/// File property value model
/// </summary>
public class DaluxFilePropertyValue
{
    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Metadata for file list responses
/// </summary>
public class DaluxFileMetadata
{
    [JsonPropertyName("totalRemainingItems")]
    public int TotalRemainingItems { get; set; }
}

/// <summary>
/// Metadata for finalizing file upload - only includes properties accepted by the API
/// According to Dalux API docs: "Only the properties fileId, folderId, properties, fileType and fileName are considered in the metadata"
/// Either fileId or folderId can be set, but not both
/// </summary>
public class DaluxFileUploadMetadata
{
    /// <summary>
    /// Set this to upload a new version of an existing file
    /// </summary>
    [JsonPropertyName("fileId")]
    public string? FileId { get; set; }

    /// <summary>
    ///  Set this to upload a new file
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    /// <summary>
    /// Set this to upload a new file to a specific folder
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }

    [JsonPropertyName("properties")]
    public List<DaluxFileProperty>? Properties { get; set; }
}

/// <summary>
/// Response from creating an upload slot
/// </summary>
public class DaluxUploadSlotResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("data")]
    public DaluxUploadSlotData? Data { get; set; }
}

/// <summary>
/// Upload slot data containing the upload GUID
/// </summary>
public class DaluxUploadSlotData
{
    [JsonPropertyName("uploadGuid")]
    public string UploadGuid { get; set; } = string.Empty;
}

#endregion

#region Folder Models

/// <summary>
/// Represents a folder response with metadata
/// </summary>
public class DaluxFoldersResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFolderItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxMetadata? Metadata { get; set; }
}

/// <summary>
/// Wrapper for individual folder items
/// </summary>
public class DaluxFolderItem
{
    [JsonPropertyName("data")]
    public DaluxFolder? Data { get; set; }

    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }
}

/// <summary>
/// Folder data model
/// </summary>
public class DaluxFolder
{
    [JsonPropertyName("folderId")]
    public string FolderId { get; set; } = string.Empty;

    [JsonPropertyName("folderName")]
    public string FolderName { get; set; } = string.Empty;

    [JsonPropertyName("parentFolderId")]
    public string? ParentFolderId { get; set; }

    [JsonPropertyName("fileAreaId")]
    public string FileAreaId { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

#endregion

#region File Properties Models

/// <summary>
/// Response for file properties mappings in a folder
/// </summary>
public class DaluxFilePropertiesResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFilePropertiesMappingItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxFilePropertiesMetadata? Metadata { get; set; }
}

/// <summary>
/// Wrapper for individual file properties mapping items
/// </summary>
public class DaluxFilePropertiesMappingItem
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("data")]
    public DaluxPropertyDefinition? Data { get; set; }
}

/// <summary>
/// Property definition for files
/// </summary>
public class DaluxPropertyDefinition
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    [JsonPropertyName("requiredOnDocuments")]
    public bool RequiredOnDocuments { get; set; }

    [JsonPropertyName("requiredOnDrawings")]
    public bool RequiredOnDrawings { get; set; }

    [JsonPropertyName("requiredOnModels")]
    public bool RequiredOnModels { get; set; }
}

/// <summary>
/// Metadata for file properties response
/// </summary>
public class DaluxFilePropertiesMetadata
{
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalRemainingItems")]
    public int TotalRemainingItems { get; set; }
}

#endregion

#region Task Models

public class DaluxTask
{
    [JsonPropertyName("taskId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("created")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("completedDate")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class DaluxTaskCreate
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class DaluxTaskUpdate
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("completedDate")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

#endregion

#region User Models

public class DaluxUser
{
    [JsonPropertyName("userId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }
}

#endregion

#region Issue Models

public class DaluxIssue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("reportedBy")]
    public string? ReportedBy { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("resolvedDate")]
    public DateTime? ResolvedDate { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("attachments")]
    public List<string>? Attachments { get; set; }
}

public class DaluxIssueCreate
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

#endregion

#region Checklist Models

public class DaluxChecklist
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";

    [JsonPropertyName("templateName")]
    public string? TemplateName { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("inspector")]
    public string? Inspector { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("completedDate")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxChecklistItem>? Items { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }
}

public class DaluxChecklistItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("imageUrls")]
    public List<string>? ImageUrls { get; set; }
}

public class DaluxChecklistUpdate
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxChecklistItemUpdate>? Items { get; set; }
}

public class DaluxChecklistItemUpdate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

#endregion

#region WorkPackage Models

public class DaluxWorkPackage
{
    [JsonPropertyName("workpackageId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

#endregion
