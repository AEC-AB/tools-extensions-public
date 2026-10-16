using System.Text.Json;
using System.Xml.Linq;
using IdsValidator;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project tests/IdsValidator -- <cases directory> <report directory>");
    return 2;
}
var casesRoot = Path.GetFullPath(args[0]);
await OfficialCases.EnsureAsync(casesRoot);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var tests = Directory.EnumerateFiles(casesRoot, "*.ids", SearchOption.AllDirectories).Order()
    .Select(p => new Case(Path.GetRelativePath(casesRoot, p).Replace('\\', '/'), p, Path.ChangeExtension(p, ".ifc"),
        Path.GetFileName(p).Split('-')[0] switch { "pass" => "passed", "fail" => "requirements_failed", "invalid" => "technical_error", _ => throw new InvalidDataException(p) }, true)).ToList();
if (tests.Count != 334) throw new InvalidDataException("Expected exactly 334 official cases");
var original = tests.Single(t => t.Id == "attribute/pass-an_optional_attribute_passes_if_null.ids");
var fixtureDirectory = Path.Join(output, "fixtures");
Directory.CreateDirectory(fixtureDirectory);
XNamespace idsNs = "http://standards.buildingsmart.org/IDS", xsi = "http://www.w3.org/2001/XMLSchema-instance";
void Fixture(string id, string expected, Action<XDocument> change, string? ifc = null, string? warning = null, string? error = null)
{
    var doc = XDocument.Load(original.Ids);
    change(doc);
    var path = Path.Join(fixtureDirectory, Path.GetFileName(id) + ".ids");
    doc.Save(path);
    tests.Add(new(id, path, ifc ?? original.Ifc, expected, false, warning, error));
}
Fixture("explicit-1.0", "passed", _ => { });
Fixture("explicit-1.0.0", "passed", d => d.Root!.Attribute(xsi + "schemaLocation")!.Value = "http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/1.0.0/ids.xsd");
Fixture("https-schema", "passed", d => d.Root!.Attribute(xsi + "schemaLocation")!.Value = "http://standards.buildingsmart.org/IDS https://standards.buildingsmart.org/IDS/1.0/ids.xsd");
Fixture("author-revision", "passed", d => d.Root!.Element(idsNs + "info")!.Element(idsNs + "title")!.AddAfterSelf(new XElement(idsNs + "version", "2026.42")));
Fixture("no-schema-hint", "passed", d => d.Root!.Attribute(xsi + "schemaLocation")!.Remove(), warning: "IDS_VERSION_INFERRED");
foreach (var version in new[] { "2.0", "0.9.7" })
    Fixture("unsupported-" + version, "technical_error", d => d.Root!.Attribute(xsi + "schemaLocation")!.Value = $"http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/{version}/ids.xsd", error: "IDS_VERSION_UNSUPPORTED");
Fixture("malformed-pair", "technical_error", d => d.Root!.Attribute(xsi + "schemaLocation")!.Value = "http://standards.buildingsmart.org/IDS", error: "IDS_VERSION_AMBIGUOUS");
Fixture("duplicate-hint", "technical_error", d => d.Root!.Attribute(xsi + "schemaLocation")!.Value += " " + d.Root.Attribute(xsi + "schemaLocation")!.Value, error: "IDS_VERSION_AMBIGUOUS");
Fixture("wrong-namespace", "technical_error", d => { d.Root!.Name = XName.Get("ids", "urn:other"); d.Root.SetAttributeValue("xmlns", "urn:other"); }, error: "IDS_DOCUMENT_EXPECTED");
foreach (var fixture in new[] { ("malformed-xml", "<ids>", "IDS_XML_INVALID"), ("xsd-input", "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", "IDS_DOCUMENT_EXPECTED") })
{
    var path = Path.Join(fixtureDirectory, Path.GetFileName(fixture.Item1) + ".ids"); File.WriteAllText(path, fixture.Item2);
    tests.Add(new(fixture.Item1, path, original.Ifc, "technical_error", false, Error: fixture.Item3));
}
var ifc4x3 = Path.Join(fixtureDirectory, "ifc4x3.ifc");
File.WriteAllText(ifc4x3, File.ReadAllText(original.Ifc).Replace("'IFC4'", "'IFC4X3_ADD2'"));
Fixture("ifc4x3-pass", "passed", d => d.Descendants(idsNs + "specification").Single().SetAttributeValue("ifcVersion", "IFC4X3_ADD2"), ifc4x3);
Fixture("ifc4x3-fail", "requirements_failed", d => { d.Descendants(idsNs + "specification").Single().SetAttributeValue("ifcVersion", "IFC4X3_ADD2"); d.Descendants(idsNs + "attribute").Single().SetAttributeValue("cardinality", "required"); }, ifc4x3);
Fixture("ifc4x3-metadata-warning", "passed", _ => { }, ifc4x3, warning: "IFC_VERSION_METADATA");
foreach (var pair in new[] { ("invalid-ifc", "not an IFC"), ("empty-ifc", "") })
{
    var path = Path.Join(fixtureDirectory, Path.GetFileName(pair.Item1) + ".ifc"); File.WriteAllText(path, pair.Item2);
    tests.Add(new(pair.Item1, original.Ids, path, "technical_error", false, Error: "IO_ERROR"));
}
tests.Add(new("missing-ifc", original.Ids, Path.Combine(fixtureDirectory, "absent.ifc"), "technical_error", false, Error: "IFC_FILE_MISSING"));
tests.Add(new("missing-ids", Path.Combine(fixtureDirectory, "absent.ids"), original.Ifc, "technical_error", false, Error: "IDS_FILE_MISSING"));
void ResourceFixture(string id, string sourceId, string expected, Action<XDocument> change)
{
    var source = tests.Single(t => t.Id == sourceId);
    var document = XDocument.Load(source.Ids);
    change(document);
    var path = Path.Join(fixtureDirectory, Path.GetFileName(id) + ".ids"); document.Save(path);
    tests.Add(new(id, path, source.Ifc, expected, false));
}
const string material2x3 = "property/pass-material_properties_are_supported_under_ifc2x3_via_extendedmaterialproperties.ids";
const string material4 = "property/pass-material_properties_are_supported_under_ifc4_via_ifcmaterialproperties.ids";
foreach (var sourceId in new[] { material2x3, material4 })
{
    var suffix = sourceId == material2x3 ? "2x3" : "4";
    ResourceFixture("material-wrong-value-" + suffix, sourceId, "requirements_failed", d =>
        d.Descendants(idsNs + "requirements").Single().Element(idsNs + "property")!.Add(new XElement(idsNs + "value", new XElement(idsNs + "simpleValue", "Wrong value"))));
    ResourceFixture("material-wrong-datatype-" + suffix, sourceId, "requirements_failed", d =>
        d.Descendants(idsNs + "requirements").Single().Element(idsNs + "property")!.SetAttributeValue("dataType", "IFCBOOLEAN"));
    ResourceFixture("material-property-applicability-" + suffix, sourceId, "passed", d =>
    {
        var property = new XElement(d.Descendants(idsNs + "requirements").Single().Element(idsNs + "property")!);
        property.Attribute("cardinality")?.Remove();
        d.Descendants(idsNs + "applicability").Single().ReplaceNodes(property);
    });
}
ResourceFixture("mapped-type-wrong-class", "entity/pass-in_ifc2x3_an_airterminal_predefined_type_resolves_via_the_type_mapping_table_1_2.ids", "requirements_failed", d =>
{
    var entity = d.Descendants(idsNs + "requirements").Single().Element(idsNs + "entity")!;
    entity.Element(idsNs + "name")!.Element(idsNs + "simpleValue")!.Value = "IFCCOLUMN";
    entity.Element(idsNs + "predefinedType")?.Remove();
    d.Descendants(idsNs + "applicability").Single().ReplaceNodes(new XElement(idsNs + "attribute", new XElement(idsNs + "name", new XElement(idsNs + "simpleValue", "GlobalId"))));
});
var results = new List<object>();
int mismatches = 0;
await using var engine = new EngineSession();
foreach (var test in tests)
{
    var report = await ValidationService.ValidateAsync(test.Ifc, test.Ids, engine, CancellationToken.None);
    Reports.Write(report, Path.Combine(output, "cases", (results.Count + 1).ToString("D3")));
    var matched = report.Status == test.Expected && report.BcfSchemaValidated &&
        report.IfcSha256Before == report.IfcSha256After && report.IdsSha256Before == report.IdsSha256After &&
        (test.Warning is null || report.Warnings.Any(w => w.Code == test.Warning)) &&
        (test.Error is null || report.Errors.Any(e => e.Code == test.Error)) &&
        (!test.Official || test.Expected != "technical_error" || report.Errors.Any(e => e.Code.StartsWith("IDS_")));
    if (test.Id == "author-revision") matched &= report.DocumentRevision == "2026.42" && report.IdsVersion == "1.0";
    if (!matched) { mismatches++; Console.Error.WriteLine($"MISMATCH {test.Id}: expected {test.Expected}, actual {report.Status}; {JsonSerializer.Serialize(report.Errors)}"); }
    results.Add(new { test.Id, test.Official, expected = test.Expected, actual = report.Status, matched, report.FailedChecks });
}
File.WriteAllText(Path.Combine(output, "test-results.json"), JsonSerializer.Serialize(new { total = tests.Count, officialCases = 334, integrationCases = tests.Count - 334, matched = tests.Count - mismatches, mismatches, results }, JsonConfig.Options));
Console.WriteLine($"{tests.Count - mismatches}/{tests.Count} expected outcomes matched; {mismatches} mismatches.");
return mismatches == 0 ? 0 : 1;

record Case(string Id, string Ids, string Ifc, string Expected, bool Official, string? Warning = null, string? Error = null);
