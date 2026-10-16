using System.IO;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;

namespace IdsValidator;

internal static class Bcf
{
    private static readonly ConcurrentDictionary<string, XmlSchemaSet> Schemas = new();
    private static void Validate(XDocument doc, string name)
    {
        var set = Schemas.GetOrAdd(name, static name =>
        {
            var schemas = new XmlSchemaSet { XmlResolver = null };
            using var stream = typeof(Bcf).Assembly.GetManifestResourceStream("IdsValidator.Schemas." + name) ?? throw new IOException("Missing BCF schema: " + name);
            using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null });
            schemas.Add(null, reader); schemas.Compile(); return schemas;
        });
        doc.Validate(set, (_, e) => throw new XmlSchemaException(e.Message));
    }
    public static void Write(ValidationReport report)
    {
        var target = report.BcfPath!;
        var violations = report.Violations;
        var globalIds = violations.Where(v => v.GlobalId is not null).Select(v => v.GlobalId!).ToHashSet(StringComparer.Ordinal);
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
            {
                Add(zip, "bcf.version", new XDocument(new XElement("Version", new XAttribute("VersionId", "2.1"))), "version.xsd");
                foreach (var f in violations)
                {
                    var topic = Guid.NewGuid().ToString(); var view = Guid.NewGuid().ToString();
                    var description = $"Rule ID: {f.RuleId}\nSpecification: {f.Specification}\nFacet: {f.Facet}\nIFC file: {report.IfcPath}\nIFC GlobalId: {f.GlobalId}\nSTEP ID: #{f.EntityLabel}\nActual: {JsonSerializer.Serialize(f.Actual)}\nExpected: {JsonSerializer.Serialize(f.Expected)}\nExpected description: {f.ExpectedDescription}\nReason: {f.Reason}";
                    var root = new XElement("Markup", new XElement("Header", new XElement("File", new XAttribute("isExternal", "true"), new XElement("Filename", Path.GetFileName(report.IfcPath)), new XElement("Reference", new Uri(report.IfcPath).AbsoluteUri))),
                        new XElement("Topic", new XAttribute("Guid", topic), new XAttribute("TopicType", "IDS violation"), new XAttribute("TopicStatus", "Open"), new XElement("Title", f.RuleId + " " + f.Facet),
                            new XElement("Labels", f.RuleId), new XElement("CreationDate", report.StartedUtc.UtcDateTime.ToString("o")), new XElement("CreationAuthor", "IDS Validator"), new XElement("Description", description)));
                    if (f.GlobalId is not null && Regex.IsMatch(f.GlobalId, @"\A[0-3][0-9A-Za-z_$]{21}\z"))
                    {
                        root.Add(new XElement("Viewpoints", new XAttribute("Guid", view), new XElement("Viewpoint", "viewpoint.bcfv")));
                        Add(zip, topic + "/viewpoint.bcfv", new XDocument(new XElement("VisualizationInfo", new XAttribute("Guid", view), new XElement("Components",
                            new XElement("Selection", new XElement("Component", new XAttribute("IfcGuid", f.GlobalId), new XElement("OriginatingSystem", "IDS Validator"), new XElement("AuthoringToolId", "#" + f.EntityLabel))),
                            new XElement("Visibility", new XAttribute("DefaultVisibility", "true"))))), "visinfo.xsd");
                    }
                    Add(zip, topic + "/markup.bcf", new XDocument(root), "markup.xsd");
                }
            }
            using (var zip = ZipFile.OpenRead(temp))
            {
                if (zip.Entries.Count(e => e.FullName.EndsWith("/markup.bcf")) != violations.Length) throw new IOException("BCF topic count does not match the violations.");
                foreach (var entry in zip.Entries)
                { using var stream = entry.Open(); var doc = XDocument.Load(stream); Validate(doc, entry.FullName == "bcf.version" ? "version.xsd" : entry.FullName.EndsWith(".bcfv") ? "visinfo.xsd" : "markup.xsd");
                    foreach (var component in doc.Descendants("Component"))
                        if (!globalIds.Contains((string?)component.Attribute("IfcGuid") ?? "")) throw new IOException("BCF references an element that is not in the findings.");
                }
            }
            File.Move(temp, target, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static void Add(ZipArchive zip, string path, XDocument doc, string schema)
    { Validate(doc, schema); using var stream = zip.CreateEntry(path).Open(); doc.Save(stream); }
}
