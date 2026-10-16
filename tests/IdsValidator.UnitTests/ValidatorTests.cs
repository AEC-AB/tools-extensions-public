using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using IdsValidator;
using Microsoft.Extensions.Logging.Abstractions;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.Ifc;
using Xbim.InformationSpecifications;
using Xunit;

namespace IdsValidatorTests;

// Authored fixtures are independent of the downloaded conformance corpus.
public sealed class ValidatorTests : IDisposable
{
    private const string GlobalId = "1hqIFTRjfV6AWq_bMtnZwI";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ids-unit-tests", Guid.NewGuid().ToString("N"));
    private readonly string ifc;
    private readonly string ids;

    public ValidatorTests()
    {
        Directory.CreateDirectory(directory);
        ifc = Path.Combine(directory, "model.ifc");
        ids = Path.Combine(directory, "rules.ids");
        File.WriteAllText(ifc, """
            ISO-10303-21;
            HEADER;
            FILE_DESCRIPTION(('ViewDefinition [CoordinationView]'),'2;1');
            FILE_NAME('model.ifc','2026-10-16T00:00:00',(),(),'Unit tests','Unit tests','');
            FILE_SCHEMA(('IFC4'));
            ENDSEC;
            DATA;
            #1=IFCWALL('1hqIFTRjfV6AWq_bMtnZwI',$,'Test wall',$,$,$,$,$,$);
            ENDSEC;
            END-ISO-10303-21;
            """);
        File.WriteAllText(ids, """
            <ids xmlns="http://standards.buildingsmart.org/IDS"
                 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                 xsi:schemaLocation="http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/1.0/ids.xsd">
              <info><title>Local unit test</title></info>
              <specifications>
                <specification name="Wall name" identifier="WALL-NAME" ifcVersion="IFC4">
                  <applicability minOccurs="1" maxOccurs="unbounded">
                    <entity><name><simpleValue>IFCWALL</simpleValue></name></entity>
                  </applicability>
                  <requirements>
                    <attribute cardinality="required">
                      <name><simpleValue>Name</simpleValue></name>
                      <value><simpleValue>Test wall</simpleValue></value>
                    </attribute>
                  </requirements>
                </specification>
              </specifications>
            </ids>
            """);
    }

    private void ChangeIds(Action<XDocument> change)
    {
        var doc = XDocument.Load(ids);
        change(doc);
        doc.Save(ids);
    }

    private static XName Name(string local) => XName.Get(local, "http://standards.buildingsmart.org/IDS");
    private async Task<ValidationReport> Validate()
    {
        await using var engine = new EngineSession();
        return await ValidationService.ValidateAsync(ifc, ids, engine, CancellationToken.None);
    }

    [Theory]
    [InlineData(false, "X", "Y", "requirements_failed")]
    [InlineData(false, "Y", "X", "requirements_failed")]
    [InlineData(true, "X", "Y", "requirements_failed")]
    [InlineData(true, "Y", "X", "requirements_failed")]
    [InlineData(false, "Y", "Z", "passed")]
    [InlineData(true, "Y", "Z", "passed")]
    public void Prohibited_resource_list_checks_all_values(bool profile, string first, string second, string expected)
    {
        ResourceList(profile, first, second, "prohibited", "IFCLABEL");
        // Strict IDS auditing rejects prohibited value constraints. Test the binder
        // directly so its collection semantics remain correct independently of auditing.
        using var model = IfcStore.Open(ifc, ifcDatabaseSizeThreshHold: -1);
        var binder = new StandardPropertyBinder(NullLogger<PsetFacetBinder>.Instance,
            new IdsValueMapper([new IdsValueMapProvider()]));
        binder.Initialise(new BinderContext { Model = model });
        var facet = new IfcPropertyFacet { PropertySetName = "TestProperties", PropertyName = "Code", PropertyValue = "X" };
        var result = new IdsValidationResult(model.Instances[1], null);
        binder.ValidateEntity(model.Instances[1], facet, RequirementCardinalityOptions.Cardinality.Prohibited, result);
        Assert.Equal(expected == "passed" ? ValidationStatus.Pass : ValidationStatus.Fail, result.ValidationStatus);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Prohibited_value_constraints_remain_rejected_by_strict_audit(bool includeDataType)
    {
        ResourceList(false, "X", "Y", "prohibited", "IFCLABEL");
        if (includeDataType) ChangeIds(d => d.Descendants(Name("property")).Single().SetAttributeValue("dataType", "IFCLABEL"));
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.Contains(result.Errors, e => e.Code == "IDS_AUDIT_FAILED");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Resource_list_preserves_datatype_validation(bool profile)
    {
        ResourceList(profile, "X", "Y", "required", "IFCTEXT");
        var result = await Validate();
        Assert.Empty(result.Errors);
        Assert.Equal("requirements_failed", result.Status);
        Assert.Contains("IFCLABEL", JsonSerializer.Serialize(Assert.Single(result.Violations).Actual));
    }

    private void ResourceList(bool profile, string first, string second, string cardinality, string dataType)
    {
        var text = File.ReadAllText(ifc);
        var start = text.IndexOf("#1=", StringComparison.Ordinal);
        var end = text.IndexOf("ENDSEC;", start, StringComparison.Ordinal);
        var resource = profile ? "IFCRECTANGLEPROFILEDEF(.AREA.,'Test profile',$,1.,1.)" : "IFCMATERIAL('Test material',$,$)";
        var propertySet = profile ? "IFCPROFILEPROPERTIES" : "IFCMATERIALPROPERTIES";
        File.WriteAllText(ifc, text[..start] + $"#1={resource};\n#2=IFCPROPERTYLISTVALUE('Code',$,(IFCLABEL('{first}'),IFCLABEL('{second}')),$);\n#3={propertySet}('TestProperties',$,(#2),#1);\n" + text[end..]);
        ChangeIds(d => {
            d.Descendants(Name("entity")).Single().Descendants(Name("simpleValue")).Single().Value = profile ? "IFCRECTANGLEPROFILEDEF" : "IFCMATERIAL";
            d.Descendants(Name("requirements")).Single().ReplaceNodes(
                new XElement(Name("property"), new XAttribute("cardinality", cardinality),
                    cardinality == "prohibited" ? null : new XAttribute("dataType", dataType),
                    new XElement(Name("propertySet"), new XElement(Name("simpleValue"), "TestProperties")),
                    new XElement(Name("baseName"), new XElement(Name("simpleValue"), "Code")),
                    new XElement(Name("value"), new XElement(Name("simpleValue"), "X"))));
        });
    }

    [Fact]
    public async Task Matching_requirement_passes_without_changing_inputs()
    {
        var ifcHash = SHA256.HashData(File.ReadAllBytes(ifc));
        var idsHash = SHA256.HashData(File.ReadAllBytes(ids));
        var result = await Validate();
        Assert.Equal("passed", result.Status);
        Assert.True(result.Complete);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Violations);
        Assert.Equal(1, result.PassedChecks);
        Assert.Equal(ifcHash, SHA256.HashData(File.ReadAllBytes(ifc)));
        Assert.Equal(idsHash, SHA256.HashData(File.ReadAllBytes(ids)));
        Assert.Equal(result.IfcSha256Before, result.IfcSha256After);
        Assert.Equal(result.IdsSha256Before, result.IdsSha256After);
    }

    [Fact]
    public async Task One_failed_value_fails_validation_and_preserves_evidence()
    {
        ChangeIds(d => d.Descendants(Name("value")).Single().Element(Name("simpleValue"))!.Value = "Expected wall");
        var result = await Validate();
        Assert.Equal("requirements_failed", result.Status);
        Assert.True(result.Complete);
        Assert.Empty(result.Errors);
        var violation = Assert.Single(result.Violations);
        Assert.Equal(GlobalId, violation.GlobalId);
        Assert.Contains("WALL-NAME", violation.RuleId);
        Assert.Contains("Test wall", JsonSerializer.Serialize(violation.Actual));
        Assert.Contains("Expected wall", JsonSerializer.Serialize(violation.Expected));
    }

    [Fact]
    public async Task Passing_values_do_not_mask_a_failed_value()
    {
        ChangeIds(d => d.Descendants(Name("requirements")).Single().Add(
            new XElement(Name("attribute"), new XAttribute("cardinality", "required"),
                new XElement(Name("name"), new XElement(Name("simpleValue"), "Description")),
                new XElement(Name("value"), new XElement(Name("simpleValue"), "Required description")))));
        var result = await Validate();
        Assert.Equal("requirements_failed", result.Status);
        Assert.True(result.Complete);
        Assert.Equal(1, result.PassedChecks);
        Assert.Equal(1, result.FailedChecks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Duplicate_version_declarations_are_rejected()
    {
        ChangeIds(d => {
            var location = d.Root!.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"))!;
            location.Value += " " + location.Value;
        });
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.Contains(result.Errors, e => e.Code == "IDS_VERSION_AMBIGUOUS");
    }

    [Fact]
    public async Task Prohibited_applicability_fails_when_an_element_matches()
    {
        ChangeIds(d => {
            var applicability = d.Descendants(Name("applicability")).Single();
            applicability.SetAttributeValue("minOccurs", "0");
            applicability.SetAttributeValue("maxOccurs", "0");
            d.Descendants(Name("requirements")).Single().Remove();
        });
        var result = await Validate();
        Assert.Equal("requirements_failed", result.Status);
        Assert.True(result.Complete);
        Assert.Equal(GlobalId, Assert.Single(result.Violations).GlobalId);
    }

    [Theory]
    [InlineData("1.0", "http")]
    [InlineData("1.0.0", "http")]
    [InlineData("1.0", "https")]
    public async Task Supported_schema_locations_select_standard_1_0(string version, string scheme)
    {
        ChangeIds(d => d.Root!.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"))!.Value =
            $"http://standards.buildingsmart.org/IDS {scheme}://standards.buildingsmart.org/IDS/{version}/ids.xsd");
        var result = await Validate();
        Assert.Equal("passed", result.Status);
        Assert.Equal("1.0", result.IdsVersion);
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("0.9.7")]
    public async Task Unsupported_versions_are_technical_errors(string version)
    {
        File.WriteAllText(ids, File.ReadAllText(ids).Replace("/1.0/ids.xsd", $"/{version}/ids.xsd"));
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.False(result.Complete);
        Assert.Contains(result.Errors, e => e.Code == "IDS_VERSION_UNSUPPORTED");
    }

    [Fact]
    public async Task Document_revision_does_not_select_the_standard()
    {
        ChangeIds(d => d.Descendants(Name("title")).Single().AddAfterSelf(new XElement(Name("version"), "2026.42")));
        var result = await Validate();
        Assert.Equal("passed", result.Status);
        Assert.Equal("2026.42", result.DocumentRevision);
        Assert.Equal("1.0", result.IdsVersion);
    }

    [Fact]
    public async Task Missing_schema_location_is_audited_with_warning()
    {
        ChangeIds(d => d.Root!.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"))!.Remove());
        var result = await Validate();
        Assert.Equal("passed", result.Status);
        Assert.Contains(result.Warnings, w => w.Code == "IDS_VERSION_INFERRED");
    }

    [Theory]
    [InlineData("<ids>", "IDS_XML_INVALID")]
    [InlineData("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", "IDS_DOCUMENT_EXPECTED")]
    [InlineData("<ids xmlns='http://standards.buildingsmart.org/IDS'><info/></ids>", "IDS_AUDIT_FAILED")]
    public async Task Invalid_ids_is_a_technical_error(string xml, string code)
    {
        File.WriteAllText(ids, xml);
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.False(result.Complete);
        Assert.Contains(result.Errors, e => e.Code == code);
    }

    [Theory]
    [InlineData(true, "IFC_FILE_MISSING")]
    [InlineData(false, "IDS_FILE_MISSING")]
    public async Task Missing_inputs_are_technical_errors(bool removeIfc, string code)
    {
        File.Delete(removeIfc ? ifc : ids);
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.False(result.Complete);
        Assert.Contains(result.Errors, e => e.Code == code);
    }

    [Fact]
    public async Task Invalid_ifc_is_a_technical_error()
    {
        File.WriteAllText(ifc, "This is not an IFC file");
        var result = await Validate();
        Assert.Equal("technical_error", result.Status);
        Assert.False(result.Complete);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("0", "unbounded", "passed")]
    [InlineData("1", "unbounded", "requirements_failed")]
    public async Task Empty_applicability_obeys_minimum_cardinality(string min, string max, string expected)
    {
        ChangeIds(d => {
            var applicability = d.Descendants(Name("applicability")).Single();
            applicability.SetAttributeValue("minOccurs", min);
            applicability.SetAttributeValue("maxOccurs", max);
            applicability.Descendants(Name("simpleValue")).Single().Value = "IFCDOOR";
        });
        var result = await Validate();
        Assert.Equal(expected, result.Status);
        Assert.True(result.Complete);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("optional", "passed")]
    [InlineData("required", "requirements_failed")]
    public async Task Null_attribute_obeys_requirement_cardinality(string cardinality, string expected)
    {
        ChangeIds(d => {
            var attribute = d.Descendants(Name("attribute")).Single();
            attribute.SetAttributeValue("cardinality", cardinality);
            attribute.Element(Name("name"))!.Element(Name("simpleValue"))!.Value = "Description";
        });
        Assert.Equal(expected, (await Validate()).Status);
    }

    [Fact]
    public async Task Reports_include_failed_rule_values_and_bcf_element_selection()
    {
        ChangeIds(d => d.Descendants(Name("value")).Single().Element(Name("simpleValue"))!.Value = "Expected wall");
        var result = await Validate();
        Reports.Write(result, Path.Combine(directory, "reports"));
        Assert.True(result.BcfSchemaValidated);
        Assert.Equal("requirements_failed", result.Status);
        var violation = Assert.Single(result.Violations);
        var markdown = WebUtility.HtmlDecode(File.ReadAllText(result.MarkdownPath));
        Assert.Contains("| Summary | Result |", markdown);
        Assert.Contains("Failed: requirement violations", markdown);
        Assert.Contains(violation.RuleId, markdown);
        Assert.Contains(GlobalId, markdown);
        Assert.Contains("Test wall", markdown);
        Assert.Contains("Expected wall", markdown);
        Assert.Contains("<table>", File.ReadAllText(result.HtmlPath));
        using var json = JsonDocument.Parse(File.ReadAllText(result.JsonPath));
        Assert.Equal("requirements_failed", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(result.BcfPath, json.RootElement.GetProperty("bcfPath").GetString());
        using var archive = ZipFile.OpenRead(result.BcfPath!);
        var markupEntry = Assert.Single(archive.Entries, e => e.FullName.EndsWith("markup.bcf"));
        using var markupStream = markupEntry.Open();
        var markup = XDocument.Load(markupStream);
        var description = markup.Descendants("Description").Single().Value;
        Assert.Contains(violation.RuleId, description);
        Assert.Contains("Test wall", description);
        Assert.Contains("Expected wall", description);
        var viewEntry = Assert.Single(archive.Entries, e => e.FullName.EndsWith(".bcfv"));
        using var viewStream = viewEntry.Open();
        var view = XDocument.Load(viewStream);
        Assert.Contains(view.Descendants("Component"), e => (string?)e.Attribute("IfcGuid") == GlobalId);
    }

    [Fact]
    public async Task Passed_report_has_no_bcf_issues()
    {
        var result = await Validate();
        Reports.Write(result, Path.Combine(directory, "reports"));
        Assert.Equal("passed", result.Status);
        Assert.True(result.BcfSchemaValidated);
        using var archive = ZipFile.OpenRead(result.BcfPath!);
        Assert.DoesNotContain(archive.Entries, e => e.FullName.EndsWith("markup.bcf"));
    }

    [Fact]
    public void Bcf_preserves_multiple_topics_with_shared_and_missing_global_ids()
    {
        var result = new ValidationReport { IfcPath = ifc, IdsPath = ids, Status = "requirements_failed", Complete = true };
        for (var index = 0; index < 3; index++)
            result.Findings.Add(new Finding { Status = "requirements_failed", RuleId = $"Rule-{index}",
                GlobalId = index < 2 ? GlobalId : null, EntityLabel = index + 1,
                Actual = "Actual value", Expected = "Expected value", Facet = "Property" });
        var output = Path.Join(directory, "reports") + Path.DirectorySeparatorChar;
        Reports.Write(result, output);
        Assert.Equal("requirements_failed", result.Status);
        Assert.True(result.BcfSchemaValidated);
        foreach (var path in new[] { result.JsonPath, result.HtmlPath, result.MarkdownPath, result.BcfPath! })
            Assert.Equal(Path.TrimEndingDirectorySeparator(output), Path.GetDirectoryName(path));
        using var archive = ZipFile.OpenRead(result.BcfPath!);
        Assert.Equal(3, archive.Entries.Count(e => e.FullName.EndsWith("markup.bcf")));
        var views = archive.Entries.Where(e => e.FullName.EndsWith(".bcfv")).ToArray();
        Assert.Equal(2, views.Length);
        foreach (var entry in views)
        {
            using var stream = entry.Open();
            var component = Assert.Single(XDocument.Load(stream).Descendants("Component"));
            Assert.Equal(GlobalId, (string?)component.Attribute("IfcGuid"));
        }
    }

    [Fact]
    public void Report_output_cannot_overwrite_an_input()
    {
        var source = Path.Combine(directory, "report.json");
        File.WriteAllText(source, "source input");
        var result = new ValidationReport { IfcPath = source, IdsPath = ids };
        Assert.Throws<ArgumentException>(() => Reports.Write(result, directory));
        Assert.Equal("source input", File.ReadAllText(source));
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        await using var engine = new EngineSession();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ValidationService.ValidateAsync(ifc, ids, engine, cancellation.Token));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
