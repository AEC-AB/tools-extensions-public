namespace RevitExtensionDemo.ACC.Responses.GetAllProjects;
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attributes
    {
        public string Name { get; set; }
        public List<string> Scopes { get; set; }
        public Extension Extension { get; set; }
    }

    public class Checklists
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Cost
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Data
    {
        public string ProjectType { get; set; }
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

    public class Hub
    {
        public Data Data { get; set; }
        public Links Links { get; set; }
    }

    public class Issues
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Jsonapi
    {
        public string Version { get; set; }
    }

    public class Link
    {
        public string Href { get; set; }
    }

    public class Links
    {
        public Self Self { get; set; }
        public WebView WebView { get; set; }
        public Related Related { get; set; }
    }

    public class Locations
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Markups
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Meta
    {
        public Link Link { get; set; }
    }

    public class Related
    {
        public string Href { get; set; }
    }

    public class Relationships
    {
        public Hub Hub { get; set; }
        public RootFolder RootFolder { get; set; }
        public TopFolders TopFolders { get; set; }
        public Issues Issues { get; set; }
        public Submittals Submittals { get; set; }
        public Rfis Rfis { get; set; }
        public Markups Markups { get; set; }
        public Checklists Checklists { get; set; }
        public Cost Cost { get; set; }
        public Locations Locations { get; set; }
    }

    public class Rfis
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class GetAllProjectsResponse
    {
        public Jsonapi Jsonapi { get; set; }
        public Links Links { get; set; }
        public List<Data> Data { get; set; }
    }

    public class RootFolder
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Schema
    {
        public string Href { get; set; }
    }

    public class Self
    {
        public string Href { get; set; }
    }

    public class Submittals
    {
        public Data Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class TopFolders
    {
        public Links Links { get; set; }
    }

    public class WebView
    {
        public string Href { get; set; }
    }


