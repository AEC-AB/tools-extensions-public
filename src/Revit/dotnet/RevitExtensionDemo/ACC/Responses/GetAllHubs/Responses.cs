namespace RevitExtensionDemo.ACC.Responses.GetAllHubs;
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Attributes
{
    public string Name { get; set; }
    public Extension Extension { get; set; }
    public string Region { get; set; }
}

public class Data
{
    public string Type { get; set; }
    public string Id { get; set; }
    public Attributes Attributes { get; set; }
    public Links Links { get; set; }
    public Relationships Relationships { get; set; }
}

public class Extension
{
    public string Type { get; set; }
    public string Version { get; set; }
    public Schema Schema { get; set; }
    public Data Data { get; set; }
}

public class Jsonapi
{
    public string Version { get; set; }
}

public class Links
{
    public Self Self { get; set; }
    public Related Related { get; set; }
}

public class PimCollection
{
    public Data Data { get; set; }
}

public class Projects
{
    public Links Links { get; set; }
}

public class Related
{
    public string Href { get; set; }
}

public class Relationships
{
    public Projects Projects { get; set; }
    public PimCollection PimCollection { get; set; }
}

public class GetAllHubsResponse
{
    public Jsonapi Jsonapi { get; set; }
    public Links Links { get; set; }
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