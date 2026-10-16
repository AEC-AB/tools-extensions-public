using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace IdsValidator;

// Copyright xbim Ltd and contributors. Adapted under AGPL-3.0-only; see DEVELOPMENT.md for provenance.
public sealed class StandardEntityBinder(ILogger<IfcTypeFacetBinder> logger) : IfcTypeFacetBinder(logger)
{
    public override void ValidateEntity(IPersistEntity item, IfcTypeFacet facet, Cardinality cardinality, IdsValidationResult result)
    {
        if (Model.SchemaVersion != XbimSchemaVersion.Ifc2X3 || item is not IIfcObject occurrence)
        { base.ValidateEntity(item, facet, cardinality, result); return; }
        var mapped = SchemaTypeMap.Ifc2x3TypeMap.FirstOrDefault(m => facet.IfcType!.IsSatisfiedBy(m.Key) &&
            item.ExpressType.ExpressNameUpper == m.Value.ElementType.Name.ToUpperInvariant() &&
            occurrence.IsTypedBy.Any(r => r.RelatingType.ExpressType.ExpressNameUpper == m.Value.DefiningType.Name.ToUpperInvariant()));
        if (mapped.Key is null) { base.ValidateEntity(item, facet, cardinality, result); return; }
        var native = new IdsValidationResult(item, null, true);
        base.ValidateEntity(item, facet, cardinality, native);
        var context = CreateValidationContext(cardinality, facet);
        foreach (var message in native.Messages)
        {
            var current = message;
            if (message.ValidatedField == nameof(IfcTypeFacet.IfcType))
                current = facet.IfcType!.ExpectationIsSatisifedBy(mapped.Key, context, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance)
                    ? ValidationMessage.Success(context, f => f.IfcType!, mapped.Key, "IFC2X3 occurrence/type mapping matched", item)
                    : ValidationMessage.Failure(context, f => f.IfcType!, mapped.Key, "IFC2X3 occurrence/type mapping prohibited", item);
            switch (current.Status)
            {
                case ValidationStatus.Pass: result.MarkSatisified(current); break;
                case ValidationStatus.Fail: result.Fail(current); break;
                default: result.FailWithError(current); break;
            }
        }
    }
}

public sealed class StandardPropertyBinder(ILogger<PsetFacetBinder> logger, IValueMapper mapper) : PsetFacetBinder(logger, mapper)
{
    private static bool IsResource(IPersistEntity entity) => entity is IIfcMaterialDefinition or IIfcProfileDef or Xbim.Ifc2x3.MaterialResource.IfcMaterial;

    public override Expression BindSelectionExpression(Expression expression, IfcPropertyFacet facet)
    {
        var ordinary = base.BindSelectionExpression(expression, facet);
        var resources = Model.Instances.Where(IsResource).Where(e => Matches(e, facet));
        return Expression.Call(typeof(Enumerable), nameof(Enumerable.Concat), [typeof(IPersistEntity)],
            Expression.Convert(ordinary, typeof(IEnumerable<IPersistEntity>)), Expression.Constant(resources, typeof(IEnumerable<IPersistEntity>)));
    }

    public override Expression BindWhereExpression(Expression expression, IfcPropertyFacet facet) =>
        Expression.Call(Expression.Constant(this), nameof(FilterCandidates), Type.EmptyTypes,
            Expression.Convert(expression, typeof(IEnumerable<IPersistEntity>)), Expression.Constant(facet));

    public IEnumerable<IPersistEntity> FilterCandidates(IEnumerable<IPersistEntity> candidates, IfcPropertyFacet facet) => candidates.Where(e => Matches(e, facet));

    private bool Matches(IPersistEntity entity, IfcPropertyFacet facet)
    {
        var result = new IdsValidationResult(entity, null);
        ValidateEntity(entity, facet, Cardinality.Expected, result);
        return result.ValidationStatus == ValidationStatus.Pass;
    }

    public override void ValidateEntity(IPersistEntity item, IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result)
    {
        if (!IsResource(item)) { base.ValidateEntity(item, facet, cardinality, result); return; }
        var context = CreateValidationContext(cardinality, facet);
        var sets = PropertySets(item).Where(s => facet.PropertySetName!.IsSatisfiedBy(s.Name)).ToArray();
        if (sets.Length == 0)
        {
            var absent = cardinality == Cardinality.Expected
                ? ValidationMessage.Failure(context, f => f.PropertySetName!, null, "No matching resource property set", item)
                : ValidationMessage.Success(context, f => f.PropertySetName!, null, "Optional or prohibited resource property set absent", item);
            Mark(result, absent); return;
        }
        foreach (var set in sets)
        {
            var properties = set.Properties.Where(p => facet.PropertyName!.IsSatisfiedBy(p.Name.ToString())).ToArray();
            if (properties.Length == 0)
            {
                Mark(result, cardinality == Cardinality.Expected
                    ? ValidationMessage.Failure(context, f => f.PropertyName!, null, "No matching property in " + set.Name, item)
                    : ValidationMessage.Success(context, f => f.PropertyName!, null, "Optional or prohibited resource property absent", item));
                continue;
            }
            foreach (var property in properties)
            {
                var values = Values(property).Where(v => v is not null).ToArray();
                if (values.Length == 0 && cardinality is Cardinality.Optional or Cardinality.Prohibited)
                { Mark(result, ValidationMessage.Success(context, f => f.PropertyName!, null, "Optional or prohibited null property absent", property)); continue; }
                bool populated = false, matched = false;
                foreach (var value in values)
                {
                    var actual = GetNormalisedValue(value);
                    var valueExpected = cardinality != Cardinality.Prohibited || !facet.PropertyValue.IsNullOrEmpty();
                    populated |= IsValueRelevant(actual) == valueExpected;
                    if (ValueSatifiesConstraint(facet, actual, context))
                    {
                        matched = true;
                        if (string.IsNullOrEmpty(facet.DataType) || ValidateDataType(context, result, value, facet.DataType)) break;
                    }
                }
                Mark(result, populated
                    ? ValidationMessage.Success(context, f => f.PropertyName!, property.Name, "Resource property provided in " + set.Name, property)
                    : ValidationMessage.Failure(context, f => f.PropertyName!, property.Name, "Resource property absent or prohibited in " + set.Name, property));
                var actualValues = string.Join(",", values.Select(GetNormalisedValue));
                Mark(result, matched
                    ? ValidationMessage.Success(context, f => f.PropertyValue!, actualValues, "Resource property value matched", property)
                    : ValidationMessage.Failure(context, f => f.PropertyValue!, actualValues, "Resource property value did not match", property));
            }
        }
    }

    private IEnumerable<(string Name, IEnumerable<IIfcSimpleProperty> Properties)> PropertySets(IPersistEntity entity)
    {
        if (Model.SchemaVersion == XbimSchemaVersion.Ifc2X3 && entity is Xbim.Ifc2x3.MaterialResource.IfcMaterial material)
        {
            foreach (var p in Model.Instances.OfType<Xbim.Ifc2x3.MaterialPropertyResource.IfcExtendedMaterialProperties>().Where(p => p.Material == material))
                yield return (p.Name.ToString(), p.ExtendedProperties.OfType<IIfcSimpleProperty>());
        }
        else if (entity is IIfcMaterialDefinition definition)
        {
            foreach (var p in definition.HasProperties) yield return (p.Name.ToString() ?? "", p.Properties.OfType<IIfcSimpleProperty>());
        }
        else if (entity is IIfcProfileDef profile)
        {
            foreach (var p in Model.Instances.OfType<IIfcProfileProperties>().Where(p => p.ProfileDefinition == profile))
                yield return (p.Name.ToString() ?? "", p.Properties.OfType<IIfcSimpleProperty>());
        }
    }

    private static IEnumerable<IIfcValue> Values(IIfcSimpleProperty property) => property switch
    {
        IIfcPropertySingleValue p => p.NominalValue is null ? [] : [p.NominalValue],
        IIfcPropertyEnumeratedValue p => p.EnumerationValues,
        IIfcPropertyListValue p => p.ListValues,
        IIfcPropertyTableValue p => p.DefinedValues.Concat(p.DefiningValues),
        IIfcPropertyBoundedValue p => new[] { p.LowerBoundValue, p.UpperBoundValue, p.SetPointValue }.OfType<IIfcValue>(),
        _ => []
    };
    private static void Mark(IdsValidationResult result, ValidationMessage message)
    { if (message.Status == ValidationStatus.Pass) result.MarkSatisified(message); else result.Fail(message); }
}

public sealed class StandardAttributeBinder(ILogger<AttributeFacetBinder> logger, IValueMapper mapper) : AttributeFacetBinder(logger, mapper)
{
    public override Expression BindSelectionExpression(Expression expression, AttributeFacet facet) => Model.SchemaVersion == XbimSchemaVersion.Ifc2X3
        ? Expression.Constant(FilterCandidates(Model.Instances, facet), typeof(IEnumerable<IPersistEntity>))
        : base.BindSelectionExpression(expression, facet);

    public override Expression BindWhereExpression(Expression expression, AttributeFacet facet) =>
        Expression.Call(Expression.Constant(this), nameof(FilterCandidates), Type.EmptyTypes,
            Expression.Convert(expression, typeof(IEnumerable<IPersistEntity>)), Expression.Constant(facet));

    public IEnumerable<IPersistEntity> FilterCandidates(IEnumerable<IPersistEntity> candidates, AttributeFacet facet) => candidates.Where(e =>
    {
        var result = new IdsValidationResult(e, null);
        ValidateEntity(e, facet, Cardinality.Expected, result);
        return result.ValidationStatus == ValidationStatus.Pass;
    });
}
