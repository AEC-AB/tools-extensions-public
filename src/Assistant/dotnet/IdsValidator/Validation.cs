
using System.IO;

using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using IdsLib;
using IdsLib.IdsSchema.IdsNodes;
using IdsLib.SchemaProviders;
using Microsoft.Extensions.Logging;

namespace IdsValidator;

public sealed record Diagnostic(string Code, string Message);
public sealed class Finding
{
    public string Status { get; set; } = "";
    public string RuleId { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Facet { get; set; } = "";
    public string? GlobalId { get; set; }
    public int? EntityLabel { get; set; }
    public string? IfcClass { get; set; }
    public object? Actual { get; set; }
    public object? Expected { get; set; }
    public string? ExpectedDescription { get; set; }
    public string Reason { get; set; } = "";
}
public sealed class ValidationReport
{
    public string FormatVersion { get; set; } = "1.0";
    public string Status { get; set; } = "technical_error";
    public bool Complete { get; set; }
    public DateTimeOffset StartedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string IfcPath { get; set; } = "";
    public string IdsPath { get; set; } = "";
    public string? IdsVersion { get; set; }
    public string? IdsVersionSource { get; set; }
    public string? DeclaredSchemaLocation { get; set; }
    public string? DocumentRevision { get; set; }
    public string[] SupportedIdsVersions { get; set; } = ["1.0"];
    public string? IfcSchema { get; set; }
    public object? Engine { get; set; }
    public string? IdsAuditStatus { get; set; }
    public string? IdsSha256Before { get; set; }
    public string? IdsSha256After { get; set; }
    public string? IfcSha256Before { get; set; }
    public string? IfcSha256After { get; set; }
    public string JsonPath { get; set; } = "";
    public string HtmlPath { get; set; } = "";
    public string MarkdownPath { get; set; } = "";
    public string? BcfPath { get; set; }
    public bool BcfSchemaValidated { get; set; }
    public int ElementsChecked { get; set; }
    public List<Finding> Findings { get; set; } = [];
    public List<JsonElement> Specifications { get; set; } = [];
    public List<Diagnostic> Errors { get; set; } = [];
    public List<Diagnostic> Warnings { get; set; } = [];
    public Finding[] Violations => Findings.Where(f => f.Status == "requirements_failed").ToArray();
    public int PassedChecks => Findings.Count(f => f.Status == "passed");
    public int FailedChecks => Violations.Length;
    public string[] Limitations { get; set; } = [
        "IDS 1.0 is the supported final standard. Explicit draft, future or unknown versions are rejected without conversion.",
        "Only requirements in the selected IDS are evaluated. No project filenames, classes, properties, tag patterns, MMI values, Reference View or extra delivery-quality rules are imposed.",
        "Source IDS text and model values are preserved. IFC schema metadata mismatches are reported without silently skipping specifications.",
        "Non-root resources may have no GlobalId; reports use their STEP ID. BCF topics never invent element identifiers. No geometric camera or snapshot is produced.",
        "Public conformance test results are evidence, not buildingSMART certification."
    ];
    public void Error(string code, string message) { Errors.Add(new(code, message)); Status = "technical_error"; Complete = false; }
}

public static class JsonConfig
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
}

public static class ValidationService
{
    public static async Task<ValidationReport> ValidateAsync(string ifcPath, string idsPath, EngineSession engine, CancellationToken token)
    {
        var report = new ValidationReport { IfcPath = ifcPath, IdsPath = idsPath };
        try
        {
            token.ThrowIfCancellationRequested();
            report.IfcPath = Path.GetFullPath(ifcPath); report.IdsPath = Path.GetFullPath(idsPath);
            if (!File.Exists(report.IdsPath)) { report.Error("IDS_FILE_MISSING", "Selected IDS file does not exist: " + report.IdsPath); return report; }
            report.IdsSha256Before = Hash(report.IdsPath);
            DetectVersion(report);
            if (report.Errors.Count > 0) return report;
            using (var stream = File.OpenRead(report.IdsPath))
            {
                var logger = new AuditLogger();
                var options = new SingleAuditOptions { IdsVersion = IdsVersion.Ids1_0, SchemaProvider = new FixedVersionSchemaProvider(IdsVersion.Ids1_0),
                    OmitIdsSchemaAudit = false, OmitIdsContentAudit = false, XmlWarningAction = AuditProcessOptions.XmlWarningBehaviour.ReportAsWarning };
                var result = Audit.Run(stream, options, logger);
                report.IdsAuditStatus = result.ToString();
                if (((int)result & ~256) != 0)
                { report.Error("IDS_AUDIT_FAILED", "The IDS failed schema or semantic auditing: " + result + ". " + string.Join("; ", logger.Messages)); return report; }
                foreach (var message in logger.Warnings) report.Warnings.Add(new("IDS_AUDIT_WARNING", message));
            }
            if (!File.Exists(report.IfcPath)) { report.Error("IFC_FILE_MISSING", "Selected IFC file does not exist: " + report.IfcPath); return report; }
            report.IfcSha256Before = Hash(report.IfcPath);
            var evaluated = await engine.ValidateAsync(report.IfcPath, report.IdsPath, token);
            report.Status = evaluated.Status; report.Complete = evaluated.Complete; report.Engine = evaluated.Engine; report.IfcSchema = evaluated.IfcSchema;
            report.Findings = evaluated.Findings; report.Specifications = evaluated.Specifications; report.ElementsChecked = evaluated.ElementsChecked;
            report.Errors.AddRange(evaluated.Errors); report.Warnings.AddRange(evaluated.Warnings);
        }
        catch (XmlException ex) { report.Error("IDS_XML_INVALID", ex.Message); }
        catch (IOException ex) { report.Error("IO_ERROR", ex.Message); }
        catch (UnauthorizedAccessException ex) { report.Error("ACCESS_DENIED", ex.Message); }
        catch (ArgumentException ex) { report.Error("INPUT_ERROR", ex.Message); }
        catch (JsonException ex) { report.Error("ENGINE_PROTOCOL_ERROR", ex.Message); }
        catch (Xbim.Common.Exceptions.XbimException ex) { report.Error("ENGINE_ERROR", ex.Message); }
        catch (FormatException ex) { report.Error("ENGINE_ERROR", ex.Message); }
        catch (InvalidOperationException ex) { report.Error("ENGINE_ERROR", ex.Message); }
        catch (NotSupportedException ex) { report.Error("ENGINE_ERROR", ex.Message); }
        finally
        {
            if (report.IdsSha256Before is not null) report.IdsSha256After = FinalHash(report.IdsPath, report);
            if (report.IfcSha256Before is not null) report.IfcSha256After = FinalHash(report.IfcPath, report);
            if (report.IdsSha256Before != report.IdsSha256After || report.IfcSha256Before != report.IfcSha256After)
                report.Error("INPUT_CHANGED", "An input file changed while validation was running.");
        }
        return report;
    }

    public static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }

    private static string? FinalHash(string path, ValidationReport report)
    {
        try { return Hash(path); }
        catch (IOException ex) { report.Error("INPUT_INTEGRITY_ERROR", ex.Message); return null; }
        catch (UnauthorizedAccessException ex) { report.Error("INPUT_INTEGRITY_ERROR", ex.Message); return null; }
    }

    private static void DetectVersion(ValidationReport report)
    {
        const string ns = "http://standards.buildingsmart.org/IDS";
        using var reader = XmlReader.Create(report.IdsPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 64 * 1024 * 1024 });
        var doc = XDocument.Load(reader);
        if (doc.Root?.Name != XName.Get("ids", ns)) { report.Error("IDS_DOCUMENT_EXPECTED", "Select an IDS requirements document with an ids root element, not an XSD schema."); return; }
        report.DocumentRevision = doc.Root.Element(XName.Get("info", ns))?.Element(XName.Get("version", ns))?.Value;
        var hint = (string?)doc.Root.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"));
        report.DeclaredSchemaLocation = hint;
        if (hint is null)
        { report.IdsVersion = "1.0"; report.IdsVersionSource = "inferred by schema validation"; report.Warnings.Add(new("IDS_VERSION_INFERRED", "No schemaLocation was declared. The document is validated against supported IDS 1.0. info/version is document metadata.")); return; }
        var tokens = hint.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length % 2 != 0) { report.Error("IDS_VERSION_AMBIGUOUS", "schemaLocation must contain namespace/location pairs."); return; }
        var locations = Enumerable.Range(0, tokens.Length / 2).Where(i => tokens[2 * i] == ns).Select(i => tokens[2 * i + 1]).ToArray();
        if (locations.Length != 1) { report.Error("IDS_VERSION_AMBIGUOUS", "schemaLocation must declare exactly one IDS schema location."); return; }
        var location = locations[0];
        if (!Uri.TryCreate(location, UriKind.Absolute, out var uri) || uri.Host != "standards.buildingsmart.org" || (uri.Scheme != "http" && uri.Scheme != "https") ||
            (uri.AbsolutePath != "/IDS/1.0/ids.xsd" && uri.AbsolutePath != "/IDS/1.0.0/ids.xsd") || uri.Query.Length > 0 || uri.Fragment.Length > 0)
        { report.Error("IDS_VERSION_UNSUPPORTED", "Unsupported or unrecognised IDS schema declaration: " + location + ". Supported final standard: IDS 1.0. No conversion or remote schema download was performed."); return; }
        report.IdsVersion = "1.0"; report.IdsVersionSource = "xsi:schemaLocation";
    }

    private sealed class AuditLogger : ILogger
    {
        public List<string> Messages { get; } = [];
        public List<string> Warnings { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, System.Exception? exception, Func<TState, System.Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;
            var message = formatter(state, exception);
            if (state is IEnumerable<KeyValuePair<string, object>> fields)
                message += " " + string.Join("; ", fields.Where(f => f.Key != "{OriginalFormat}").Select(f => f.Key + "=" + f.Value));
            Messages.Add(message); if (level == LogLevel.Warning) Warnings.Add(message);
        }
    }
}
