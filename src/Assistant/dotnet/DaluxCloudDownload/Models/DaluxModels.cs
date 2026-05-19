using System.Text.Json.Serialization;

namespace DaluxCloudDownload.Models;

public class DaluxApiResponse<T>
{
    public bool IsSuccess { get; set; }

    public T? Data { get; set; }

    public string? ErrorMessage { get; set; }

    public static DaluxApiResponse<T> Success(T? data)
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

public class DaluxApiListResponse<T>
{
    [JsonPropertyName("items")]
    public List<DaluxApiItemWrapper<T>>? Items { get; set; }
}

public class DaluxApiItemWrapper<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class DaluxApiLink
{
    [JsonPropertyName("rel")]
    public string? Rel { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

public class DaluxMetadata
{
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalRemainingItems")]
    public int TotalRemainingItems { get; set; }
}

public class DaluxProject
{
    [JsonPropertyName("projectId")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string Name { get; set; } = string.Empty;
}

public class DaluxFileAreaResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFileAreaItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxMetadata? Metadata { get; set; }
}

public class DaluxFileAreaItem
{
    [JsonPropertyName("data")]
    public DaluxFileArea? Data { get; set; }
}

public class DaluxFileArea
{
    [JsonPropertyName("fileAreaId")]
    public string FileAreaId { get; set; } = string.Empty;

    [JsonPropertyName("fileAreaName")]
    public string FileAreaName { get; set; } = string.Empty;
}

public class DaluxFoldersResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFolderItem>? Items { get; set; }

    [JsonPropertyName("metadata")]
    public DaluxMetadata? Metadata { get; set; }
}

public class DaluxFolderItem
{
    [JsonPropertyName("data")]
    public DaluxFolder? Data { get; set; }
}

public class DaluxFolder
{
    [JsonPropertyName("folderId")]
    public string FolderId { get; set; } = string.Empty;

    [JsonPropertyName("folderName")]
    public string FolderName { get; set; } = string.Empty;

    [JsonPropertyName("parentFolderId")]
    public string? ParentFolderId { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

public class DaluxFilesResponse
{
    [JsonPropertyName("links")]
    public List<DaluxApiLink>? Links { get; set; }

    [JsonPropertyName("items")]
    public List<DaluxFileItem>? Items { get; set; }
}

public class DaluxFileItem
{
    [JsonPropertyName("data")]
    public DaluxFile? Data { get; set; }
}

public class DaluxFile
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("fileRevisionId")]
    public string FileRevisionId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    // Not from API, used for tracking relative path in folder downloads
    [JsonIgnore]
    public string RelativePath { get; set; } = string.Empty;
}