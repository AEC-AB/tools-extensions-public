using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IdsValidator;

public static class Reports
{
    public static void Write(ValidationReport report, string output)
    {
        output = Path.GetFullPath(output);
        var jsonPath = Path.Join(output, "report.json");
        var htmlPath = Path.Join(output, "report.html");
        var markdownPath = Path.Join(output, "result.md");
        var bcfPath = Path.Join(output, "deviations.bcfzip");
        var targets = new[] { jsonPath, htmlPath, markdownPath, bcfPath };
        if (targets.Any(p => p.Equals(report.IfcPath, StringComparison.OrdinalIgnoreCase) || p.Equals(report.IdsPath, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("A report filename would overwrite an input file. Select a different report folder.");
        Directory.CreateDirectory(output);
        report.JsonPath = jsonPath; report.HtmlPath = htmlPath; report.MarkdownPath = markdownPath;
        try { report.BcfPath = bcfPath; Bcf.Write(report); report.BcfSchemaValidated = true; }
        catch (IOException ex) { report.Error("BCF_WRITE_ERROR", ex.Message); report.BcfPath = null; }
        catch (System.Xml.Schema.XmlSchemaException ex) { report.Error("BCF_SCHEMA_ERROR", ex.Message); report.BcfPath = null; }
        File.WriteAllText(report.JsonPath, JsonSerializer.Serialize(report, JsonConfig.Options), new UTF8Encoding(false));
        File.WriteAllText(report.MarkdownPath, Markdown(report), new UTF8Encoding(false));
        var b = new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><title>IDS validation report</title><style>body{font:15px system-ui;margin:24px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccc;padding:8px;vertical-align:top}pre{white-space:pre-wrap;overflow-wrap:anywhere}.fail{background:#fff0ed}</style><h1>IDS validation report</h1>");
        b.Append("<table><tr><th>Status</th><th>Complete</th><th>IDS standard</th><th>Elements checked</th><th>Passed checks</th><th>Violations</th><th>Technical errors</th></tr><tr>");
        foreach (var v in new object?[] { report.Status, report.Complete, report.IdsVersion, report.ElementsChecked, report.PassedChecks, report.FailedChecks, report.Errors.Count }) b.Append("<td>" + E(v) + "</td>");
        b.Append("</tr></table><h2>Inputs and version selection</h2><pre>" + E(new { report.IfcPath, report.IdsPath, report.IdsVersionSource, report.DeclaredSchemaLocation, report.DocumentRevision, report.IfcSchema, report.Engine, report.IdsAuditStatus }) + "</pre>");
        b.Append("<h2>Diagnostics</h2><pre>" + E(new { report.Errors, report.Warnings }) + "</pre><h2>Specifications</h2><pre>" + E(report.Specifications) + "</pre><h2>All checks</h2><table><tr><th>Status / Rule ID</th><th>IFC GlobalId / STEP ID</th><th>Specification / Facet</th><th>Actual</th><th>Expected</th><th>Reason</th></tr>");
        foreach (var f in report.Findings) b.Append($"<tr class='{(f.Status == "passed" ? "pass" : "fail")}'><td>{E(f.Status)}<br>{E(f.RuleId)}</td><td>{E(f.GlobalId)}<br>#{f.EntityLabel}</td><td>{E(f.Specification)}<br>{E(f.Facet)}</td><td><pre>{E(f.Actual)}</pre></td><td><pre>{E(f.Expected)}</pre></td><td>{E(f.Reason)}</td></tr>");
        b.Append("</table><h2>Source integrity</h2><pre>" + E(new { report.IfcSha256Before, report.IfcSha256After, report.IdsSha256Before, report.IdsSha256After }) + "</pre><h2>Report files</h2><pre>" + E(new { report.JsonPath, report.HtmlPath, report.MarkdownPath, report.BcfPath, report.BcfSchemaValidated }) + "</pre><h2>Scope</h2><ul>");
        foreach (var l in report.Limitations) b.Append("<li>" + E(l) + "</li>");
        File.WriteAllText(report.HtmlPath, b.Append("</ul></html>").ToString(), new UTF8Encoding(false));
    }

    public static string Markdown(ValidationReport r)
    {
        var b = new StringBuilder("## IDS validation\n\n");
        Table(b, ["Summary", "Result"], [
            ["Validation status", r.Status == "passed" ? "Passed" : r.Status == "requirements_failed" ? "Failed: requirement violations" : "Technical error"],
            ["Validation complete", r.Complete ? "Yes" : "No"], ["IDS standard", r.IdsVersion ?? "Not identified"],
            ["Version selection", r.IdsVersionSource ?? "Not resolved"], ["IDS document revision", r.DocumentRevision ?? "Not specified"],
            ["Elements checked", r.ElementsChecked.ToString()], ["Passed checks", r.PassedChecks.ToString()],
            ["Requirement violations", r.FailedChecks.ToString()], ["Technical errors", r.Errors.Count.ToString()]
        ]);
        Table(b, ["Input", "File"], [["IFC", r.IfcPath], ["IDS", r.IdsPath]]);
        if (r.Specifications.Count > 0)
        {
            b.Append("### Specifications\n\n");
            Table(b, ["ID", "Name", "Applicable", "Result"], r.Specifications.Select(s => new[] { s.GetProperty("specificationId").GetString()!, s.GetProperty("name").GetString()!, s.GetProperty("applicable").ToString(), s.GetProperty("status").GetString()! }));
        }
        b.Append("### Requirement violations\n\n");
        if (r.Violations.Length == 0) b.Append(r.Complete ? "No requirement violations found.\n\n" : "No requirement violations recorded. Validation is incomplete.\n\n");
        else Table(b, ["Rule ID", "IFC GlobalId / STEP ID", "Facet", "Actual", "Expected"], r.Violations.Take(50).Select(f => new[] { f.RuleId, f.GlobalId ?? (f.EntityLabel is null ? "Specification-level issue" : "#" + f.EntityLabel), f.Facet, ShortActual(f), f.ExpectedDescription ?? Text(f.Expected) }));
        if (r.Violations.Length > 50) b.Append($"Showing 50 of {r.Violations.Length} violations. Full details are included in the reports.\n\n");
        if (r.Errors.Count > 0) { b.Append("### Technical errors\n\n"); Table(b, ["Code", "Message"], r.Errors.Select(e => new[] { e.Code, e.Message })); }
        if (r.Warnings.Count > 0) { b.Append("### Warnings\n\n"); Table(b, ["Code", "Message"], r.Warnings.Select(e => new[] { e.Code, e.Message })); }
        b.Append("### Report files\n\n");
        Table(b, ["Report", "File path"], [["JSON", r.JsonPath], ["HTML", r.HtmlPath], ["Markdown", r.MarkdownPath], ["BCF 2.1", r.BcfPath ?? "Not created"]]);
        b.Append("Only requirements from the selected IDS are evaluated. No project-specific delivery checks are added.\n");
        return b.ToString();
    }
    private static string ShortActual(Finding f)
    {
        var actual = JsonSerializer.SerializeToElement(f.Actual, JsonConfig.Options);
        if (actual.ValueKind == JsonValueKind.Object)
        {
            if (actual.TryGetProperty("engineEvidence", out var evidence) && evidence.ValueKind == JsonValueKind.Object)
            {
                if (evidence.TryGetProperty("actual", out var v))
                {
                    if (v.ValueKind == JsonValueKind.Array)
                    {
                        var fields = v.EnumerateArray().Where(x => x.TryGetProperty("status", out var s) && s.GetString() == "Fail").ToArray();
                        return string.Join("; ", fields.Select(x => x.GetProperty("field").GetString() + ": " + Text(x.GetProperty("value"))));
                    }
                    return Text(v);
                }
                if (evidence.TryGetProperty("type", out var t) && new[] { "NOPSET", "NOVALUE" }.Contains(t.GetString())) return "Missing matching value";
            }
            if (actual.TryGetProperty("observed", out var observed)) return Text(observed);
        }
        return Text(f.Actual);
    }
    public static string Text(object? v) => v is null ? "null" : v is string s ? s : JsonSerializer.Serialize(v);
    private static string E(object? v) => WebUtility.HtmlEncode(v is string s ? s : JsonSerializer.Serialize(v, JsonConfig.Options));
    public static string Cell(string v) => v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("|", "&#124;").Replace("\\", "&#92;").Replace("_", "&#95;").Replace("*", "&#42;").Replace("`", "&#96;").Replace("[", "&#91;").Replace("]", "&#93;").Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
    private static void Table(StringBuilder b, string[] columns, IEnumerable<string[]> rows)
    { b.Append("| ").AppendJoin(" | ", columns.Select(Cell)).Append(" |\n|").AppendJoin('|', columns.Select(_ => "---")).Append("|\n"); foreach (var row in rows) b.Append("| ").AppendJoin(" | ", row.Select(Cell)).Append(" |\n"); b.Append('\n'); }
}
