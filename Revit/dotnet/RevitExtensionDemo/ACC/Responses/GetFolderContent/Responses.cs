namespace RevitExtensionDemo.ACC.Responses.GetFolderContent;
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Attributes
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUserId { get; set; }
    public string CreateUserName { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public DateTime LastModifiedTimeRollup { get; set; }
    public string LastModifiedUserId { get; set; }
    public string LastModifiedUserName { get; set; }
    public string Path { get; set; }
    public int ObjectCount { get; set; }
    public bool Hidden { get; set; }
    public Extension Extension { get; set; }
    public bool? Reserved { get; set; }
    public DateTime? ReservedTime { get; set; }
    public string ReservedUserId { get; set; }
    public string ReservedUserName { get; set; }
    public int VersionNumber { get; set; }
    public string MimeType { get; set; }
}

public class Contents
{
    public Relations Links { get; set; }
}

public class Data
{
    public List<string> AllowedTypes { get; set; }
    public List<string> VisibleTypes { get; set; }
    public List<object> NamingStandardIds { get; set; }
    public string Type { get; set; }
    public string Id { get; set; }
    public object TempUrn { get; set; }
    public Properties Properties { get; set; }
    public string StorageUrn { get; set; }
    public string StorageType { get; set; }
    public string ConformingStatus { get; set; }
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

public class First
{
    public string Href { get; set; }
}

public class Included
{
    public string Type { get; set; }
    public string Id { get; set; }
    public Attributes Attributes { get; set; }
    public Relations Links { get; set; }
    public Relationships Relationships { get; set; }
}

public class Item
{
    public Relations Links { get; set; }
    public Data Data { get; set; }
}

public class Jsonapi
{
    public string Version { get; set; }
}

public class Link
{
    public string Href { get; set; }
}

public class Relations
{
    public Self Self { get; set; }
    public First First { get; set; }
    public Prev Prev { get; set; }
    public Next Next { get; set; }
    public WebView WebView { get; set; }
    public Related Related { get; set; }
    public Relations Links { get; set; }
}

public class Meta
{
    public Link Link { get; set; }
}

public class Next
{
    public string Href { get; set; }
}

public class Parent
{
    public Relations Links { get; set; }
    public Data Data { get; set; }
}

public class Prev
{
    public string Href { get; set; }
}

public class Properties
{
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
    public Tip Tip { get; set; }
    public Versions Versions { get; set; }
    public Item Item { get; set; }
    public Storage Storage { get; set; }
}

public class GetFolderContentResponse
{
    public Jsonapi Jsonapi { get; set; }
    public Relations Links { get; set; }
    public List<Data> Data { get; set; }
    public List<Included> Included { get; set; }
}

public class Schema
{
    public string Href { get; set; }
}

public class Self
{
    public string Href { get; set; }
}

public class Storage
{
    public Meta Meta { get; set; }
    public Data Data { get; set; }
}

public class Tip
{
    public Relations Links { get; set; }
    public Data Data { get; set; }
}

public class Versions
{
    public Relations Links { get; set; }
}

public class WebView
{
    public string Href { get; set; }
}