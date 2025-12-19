namespace RevitExtensionDemo.ACC.Responses.GetTopFolders;

public class Attributes
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUserId { get; set; }
    public string CreateUserName { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public string LastModifiedUserId { get; set; }
    public string LastModifiedUserName { get; set; }
    public DateTime LastModifiedTimeRollup { get; set; }
    public int ObjectCount { get; set; }
    public bool Hidden { get; set; }
    public Extension Extension { get; set; }
}

public class Contents
{
    public Relations Links { get; set; }
}

public class Data
{
    public List<string> AllowedTypes { get; set; }
    public List<string> VisibleTypes { get; set; }
    public bool IsRoot { get; set; }
    public string FolderType { get; set; }
    public List<FolderParent> FolderParents { get; set; }
    public List<object> NamingStandardIds { get; set; }
    public string Type { get; set; }
    public string Id { get; set; }
    public Attributes Attributes { get; set; }
    public Relations Links { get; set; }
    public Relationships Relationships { get; set; }
}

public class Extension
{
    public string Type { get; set; }
    public string Version { get; set; }
    public Schema Schema { get; set; }
    public Data Data { get; set; }
}

public class FolderParent
{
    public string Urn { get; set; }
    public bool IsRoot { get; set; }
    public string Title { get; set; }
    public string ParentUrn { get; set; }
}

public class Jsonapi
{
    public string Version { get; set; }
}

public class Relations
{
    public Self Self { get; set; }
    public WebView WebView { get; set; }
    public Related Related { get; set; }
    public Relations Links { get; set; }
}

public class Parent
{
    public Relations Links { get; set; }
    public Data Data { get; set; }
}

public class Refs
{
    public Relations Links { get; set; }
}

public class Related
{
    public string Href { get; set; }
}

public class Relationships
{
    public Parent Parent { get; set; }
    public Refs Refs { get; set; }
    public Relations Links { get; set; }
    public Contents Contents { get; set; }
}

public class GetTopFoldersResponse
{
    public Jsonapi Jsonapi { get; set; }
    public Relations Links { get; set; }
    public List<Data> Data { get; set; }
}

public class Schema
{
    public string Href { get; set; }
}

public class Self
{
    public string Href { get; set; }
}

public class WebView
{
    public string Href { get; set; }
}

