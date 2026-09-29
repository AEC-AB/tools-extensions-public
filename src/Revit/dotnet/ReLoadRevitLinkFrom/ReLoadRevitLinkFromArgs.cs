using CW.Assistant.Extensions;
using System.Text.RegularExpressions;

namespace ReLoadRevitLinkFrom;

/// <summary>
/// Represents the inputs to an Assistant extension.
/// This class is used for defining the inputs required by the extension.
/// The properties in this class are parsed into UI elements in the Extension Task configuration in Assistant.
/// </summary>
public class ReLoadRevitLinkFromArgs
{
    [Authorization(Login.Autodesk)]
    [BaseUrl("https://developer.api.autodesk.com/")]
    public IExtensionHttpClient? AutodeskClient { get; set; }

#if R2021_OR_LESS
    [Description("Hub ID")]
    [ControlData(ToolTip = """
        BIM 360 Hub Id 
        Required fied if 'Folder Path' is used, only for Revit 2021 or older.
        Example: b.a1eefaf7-effc-433f-873d-16b2cacccff4
        """)] 
    public string? HubId { get; set; }

    [Description("Project ID")]
    [ControlData(ToolTip = """
        BIM 360 Project Id
        Required field for Revit 2021 or older.
        Example: b.12f70284-3d6f-4dc0-9cd5-e870479f6430
        """)]
    public string? ProjectId { get; set; }
#endif

    [Description("Reload Mode")]
    [ControlData(ToolTip = """
        If list mode is seleced only the links in the list will be reloaded.
        If available mode is selected all available links in the model will be reloaded.
        """)]
    public ReloadMode ReloadMode { get; set; } = ReloadMode.List;

    [Description("Links")]
    [CustomRevitAutoFill(typeof(LinksAutoFillCollector))]
    public List<string> Links { get; set; } = new();

    [Description("Reload Option")]
    [ControlData(ToolTip = """
        Select the method to reload the link.

        When 'Files' variable is selected:
        Define a variable named Files and the value should be a comma-seperated list with the full path of the *.rvt file the model guid and model id
        Example:
        Project/Folder/File.rvt;;33ae5b8e-b8d2-4cda-8c11-d0a840f25487;urn:adsk.wipemea:dm.lineage:QFG-TgvNQUGHl790h2swjw
        """)]
    public ReloadOptionEnum ReloadOption { get; set; }

    [Description("Folder ID")]
    [ControlData(ToolTip = """
        BIM 360 Folder Id
        Required fied if 'Folder ID' is used.
        Example: urn:adsk.wipemea:fs.folder:co.3JQR7J6QTZnJ6Q1J7J6QTZaJ6Q1
        """)]
    public string? FolderId { get; set; }

    [Description("Folder Path")]
    [ControlData(ToolTip = "eg. Project Files/02_WIP/Mechanical")]
    public string? FolderPath { get; set; }

    [Description("Unload links")]
    [ControlData(ToolTip = "Unload links after reloading, this will reduce memory usage")]
    public bool UnloadLinks { get; set; }

    [Description("Re-load cloud links")]
    [ControlData(ToolTip = "Also reload cloud links")]
    public bool ReLoadCloudLinks { get; set; }
}

internal class LinksAutoFillCollector : IRevitAutoFillCollector<ReLoadRevitLinkFromArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, ReLoadRevitLinkFromArgs args)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;
            if (document is null)
                return result;

            using var collector = new FilteredElementCollector(document);
            var allLinks = collector.OfClass(typeof(RevitLinkType)).OfType<RevitLinkType>().ToList();

            foreach (var link in allLinks)
            {
                var linkName = GetLinkName(link);
                result.Add(link.UniqueId, linkName);
            }

        }
        catch { }
        return result;
    }

    public static string GetLinkName(RevitLinkType link)
    {
        return Regex.Split(link.LookupParameter("Type Name").AsString(), ".rvt").First();
    }
}

public enum ReloadOptionEnum
{
    [Description("Folder ID")]
    FolderId,
    [Description("Folder Path")]
    FolderPath,
    [Description("From 'Files' variable")]
    Variable
}

public enum ReloadMode
{
    [Description("From list of links")]
    List,
    [Description("From available links")]
    Available
}