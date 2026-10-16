using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xbim.Common;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace IdsValidator;

public sealed class EngineSession : IAsyncDisposable
{
    private readonly ServiceProvider services = CreateServices();

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection().AddLogging().AddIdsValidation();
        services.AddTransient<StandardEntityBinder>();
        services.AddTransient<StandardPropertyBinder>();
        services.AddTransient<StandardAttributeBinder>();
        services.Configure<IdsBinderFactoryOptions>(o =>
        {
            o.DefaultBinderMappings[typeof(Xbim.InformationSpecifications.IfcTypeFacet)] = typeof(StandardEntityBinder);
            o.DefaultBinderMappings[typeof(Xbim.InformationSpecifications.IfcPropertyFacet)] = typeof(StandardPropertyBinder);
            o.DefaultBinderMappings[typeof(Xbim.InformationSpecifications.AttributeFacet)] = typeof(StandardAttributeBinder);
        });
        return services.BuildServiceProvider();
    }

    public async Task<ValidationReport> ValidateAsync(string ifc, string ids, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var report = new ValidationReport();
        using var scope = services.CreateScope();
        using var model = IfcStore.Open(ifc, ifcDatabaseSizeThreshHold: -1, accessMode: XbimDBAccess.Read);
        token.ThrowIfCancellationRequested();
        var validator = scope.ServiceProvider.GetRequiredService<IIdsModelValidator>();
        var options = new VerificationOptions
        {
            OutputFullEntity = true,
            IncludeSubtypes = false,
            AllowDerivedAttributes = false,
            PerformInPlaceSchemaUpgrade = false,
            SkipIncompatibleSpecification = false,
            PermittedIdsAuditStatuses = VerificationOptions.Strict
        };
        var outcome = await validator.ValidateAgainstIdsAsync(model, ids, NullLogger.Instance, verificationOptions: options, token: token);
        token.ThrowIfCancellationRequested();
        var xml = XDocument.Load(ids);
        XNamespace ns = "http://standards.buildingsmart.org/IDS";
        var originalSpecs = xml.Root!.Element(ns + "specifications")!.Elements(ns + "specification").ToArray();
        report.IfcSchema = model.Header.FileSchema.Schemas.FirstOrDefault();
        report.Engine = new { name = "Xbim.IDS.Validator.Core", version = "1.0.187", managed = true };
        if (outcome.Status == ValidationStatus.Error || outcome.ExecutedRequirements.Count != originalSpecs.Length)
        {
            report.Error("ENGINE_ERROR", outcome.Message ?? "The engine did not execute every IDS specification.");
            return report;
        }
        var checkedEntities = new HashSet<int>();
        for (var si = 0; si < originalSpecs.Length; si++)
        {
            token.ThrowIfCancellationRequested();
            var spec = outcome.ExecutedRequirements[si];
            var source = originalSpecs[si];
            var identifier = (string?)source.Attribute("identifier");
            var id = $"S{si + 1:D4}" + (identifier is null ? "" : ":" + identifier);
            var name = (string?)source.Attribute("name") ?? id;
            var applicability = source.Element(ns + "applicability")!;
            var min = (string?)applicability.Attribute("minOccurs") ?? "1";
            var max = (string?)applicability.Attribute("maxOccurs") ?? "unbounded";
            var optionalEmpty = spec.ApplicableResults.Count == 0 && min == "0";
            var passed = spec.Status == ValidationStatus.Pass || optionalEmpty || (max == "0" && spec.ApplicableResults.Count == 0);
            if (spec.Status == ValidationStatus.Error || spec.Status == ValidationStatus.Skipped || (spec.Status == ValidationStatus.Inconclusive && !passed))
                report.Error("ENGINE_SPECIFICATION_ERROR", id + ": " + spec.Status);
            var versions = ((string?)source.Attribute("ifcVersion") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!versions.Contains(report.IfcSchema)) report.Warnings.Add(new("IFC_VERSION_METADATA", id + " lists " + string.Join(", ", versions) + "; model schema is " + report.IfcSchema + ". All requirements were evaluated."));
            report.Specifications.Add(JsonSerializer.SerializeToElement(new { specificationId = id, identifier, name, applicable = spec.ApplicableResults.Count,
                passedElements = spec.PassedResults.Count(), failedElements = spec.FailedResults.Count(), status = passed ? "passed" : "requirements_failed", minOccurs = min, maxOccurs = max, ifcVersion = versions }, JsonConfig.Options));
            if (!passed && spec.ApplicableResults.Count == 0 && spec.Status == ValidationStatus.Fail)
                report.Findings.Add(new Finding { Status = "requirements_failed", RuleId = id + "/applicability", Specification = name, Facet = "Applicability",
                    Actual = new { applicableCount = 0 }, Expected = new { minOccurs = min, maxOccurs = max }, Reason = "The required specification has no matching entities." });
            var facets = source.Element(ns + "requirements")?.Elements().ToArray() ?? [];
            foreach (var entityResult in spec.ApplicableResults)
            {
                var entity = entityResult.FullEntity as IPersistEntity;
                if (entityResult.Entity is int label) checkedEntities.Add(label);
                if (entityResult.ValidationStatus is ValidationStatus.Error or ValidationStatus.Skipped or ValidationStatus.Inconclusive)
                    report.Error("ENGINE_ENTITY_ERROR", id + ": " + entityResult.ValidationStatus + " for #" + entityResult.Entity);
                if (max == "0")
                {
                    report.Findings.Add(Find(entity, entityResult.Entity, id + "/applicability", name, "Applicability", "requirements_failed",
                        new { entityLabel = entityResult.Entity, ifcClass = entity?.ExpressType.Name }, new { applicableCount = 0 }, "The specification prohibits matching entities."));
                    continue;
                }
                var engineFacets = spec.Specification.Requirement?.Facets.ToArray() ?? [];
                foreach (var group in entityResult.Messages.GroupBy(m => m.Clause, ReferenceEqualityComparer.Instance))
                {
                    var messages = group.ToArray();
                    if (messages.Any(m => m.Status != ValidationStatus.Pass && m.Status != ValidationStatus.Fail))
                    { report.Error("ENGINE_CHECK_ERROR", id + ": " + string.Join("; ", messages.Select(m => m.ToString()))); continue; }
                    var ordinal = Array.FindIndex(engineFacets, f => ReferenceEquals(f, group.Key)) + 1;
                    var originalFacet = ordinal > 0 && ordinal <= facets.Length ? facets[ordinal - 1] : null;
                    var facetName = group.Key?.GetType().Name ?? "Applicability";
                    var finding = Find(entity, entityResult.Entity, id + (ordinal == 0 ? "/applicability" : $"/R{ordinal:D3}"), name,
                        originalFacet?.Name.LocalName ?? facetName, messages.All(m => m.Status == ValidationStatus.Pass) ? "passed" : "requirements_failed",
                        new { engineEvidence = new { actual = messages.Select(m => new { status = m.Status.ToString(), field = m.ValidatedField, value = Value(m.ActualResult), formatted = m.FormatedActualResult, affectedEntity = Value(m.EntityAffected) }).ToArray() } },
                        new { constraintXml = originalFacet?.ToString(SaveOptions.DisableFormatting), fields = messages.Select(m => new { field = m.ValidatedField, value = Value(m.ExpectedResult), cardinality = m.Expectation.ToString() }).ToArray() }, string.Join("; ", messages.Select(m => m.ToString())));
                    finding.ExpectedDescription = string.Join("; ", messages.Select(m => m.Expectation + " " + m.ValidatedField + ": " + (Value(m.ExpectedResult)?.ToString() ?? "a matching value")).Distinct());
                    report.Findings.Add(finding);
                }
            }
        }
        report.ElementsChecked = checkedEntities.Count;
        if (report.Errors.Count == 0)
        {
            report.Status = report.FailedChecks > 0 || outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Fail) ? "requirements_failed" : "passed";
            report.Complete = true;
        }
        return report;
    }

    private static object? Value(object? value) => value switch
    {
        null => null,
        string or bool or int or long or short or byte or double or float or decimal => value,
        IPersistEntity entity => new { entityLabel = entity.EntityLabel, ifcClass = entity.ExpressType.Name, globalId = (entity as IIfcRoot)?.GlobalId.ToString() },
        System.Collections.IEnumerable items => items.Cast<object?>().Select(Value).ToArray(),
        _ => value.ToString()
    };

    private static Finding Find(IPersistEntity? entity, int? label, string id, string spec, string facet, string status, object? actual, object? expected, string reason) => new()
    { Status = status, RuleId = id, Specification = spec, Facet = facet, EntityLabel = label, IfcClass = entity?.ExpressType.Name,
        GlobalId = (entity as IIfcRoot)?.GlobalId.ToString(), Actual = actual, Expected = expected, ExpectedDescription = Reports.Text(expected), Reason = reason };

    public ValueTask DisposeAsync() => services.DisposeAsync();
}
