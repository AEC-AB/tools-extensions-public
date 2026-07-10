using AddSharedParameters.Extensions;
using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace AddSharedParameters.Helpers;

public class ParameterValueHelper
{
    private string GetMostCommonValue(List<string> value)
    {
        return value.GroupBy(v => v).OrderByDescending(g => g.Count()).First(x => !string.IsNullOrEmpty(x.Key)).Key;
    }

    public string GetParameterValue(Parameter parameter)
    {
        string? value = null;
        switch (parameter.StorageType)
        {
            case StorageType.None:
                value = string.Empty;
                break;
            case StorageType.Integer:
                value = parameter.AsInteger().ToString();
                break;
            case StorageType.Double:
                value = parameter.AsDouble().ToString();
                break;
            case StorageType.String:
                value = parameter.AsString();
                break;
            case StorageType.ElementId:
#if R2024_OR_GREATER
                value = parameter.AsElementId().Value.ToString();
#else
                value = parameter.AsElementId().IntegerValue.ToString();
#endif
                break;
        }

        return value ?? string.Empty;
    }

    private ElementFilter BuildCategoryFilter(CategorySet categories)
    {
        var categoryIds = new List<ElementId>();
        foreach (Category c in categories)
        {
            if (c != null)
                categoryIds.Add(c.Id);
        }
#if R2019_OR_GREATER
        return new ElementMulticategoryFilter(categoryIds);
#else
        var singleFilters = categoryIds
            .Select(id => new ElementCategoryFilter((BuiltInCategory)id.IntegerValue))
            .Cast<ElementFilter>()
            .ToList();
        return singleFilters.Count == 1
            ? singleFilters[0]
            : new LogicalOrFilter(singleFilters);
#endif
    }

    public RestoreValuesResult SetValuesOnTargetParameter(Document document, InternalDefinition targetParameterDefinition,
        Dictionary<ElementId, string> values, CategorySet categories)
    {
        var elementFilter = BuildCategoryFilter(categories);
        var result = new RestoreValuesResult();
        var updatedTypes = new List<ElementId>();

        foreach (var value in values)
        {
            using var element = document.GetElement(value.Key);

            if (element is null)
                continue;

            // Check if element matches the filter
            if (!elementFilter.PassesFilter(element))
                continue;

            var parameter = (element.get_Parameter(targetParameterDefinition) ??
                document.GetElement(element.GetTypeId())?.get_Parameter(targetParameterDefinition));
            
            if (parameter is null)
            {
                var parametersByName = element.GetParameters(targetParameterDefinition.Name);
                if (parametersByName.Count == 1)
                {
                    parameter = parametersByName[0];
                }
                else if (parametersByName.Count > 1)
                {
                    throw new AddSharedParameterFailedException($"Multiple parameters with the name {targetParameterDefinition.Name} found on element {element.Id}. Cannot determine which to update.");
                }
                else
                {
                    throw new AddSharedParameterFailedException($"Parameter {targetParameterDefinition.Name} not found on element {element.Id}");
                }
            }

            try
            {
                if (parameter.IsReadOnly)
                {
                    if (element is Instance instance &&
                        document.GetElement(instance.GetTypeId()) is ElementType typeOfInstance &&
                        typeOfInstance.get_Parameter(targetParameterDefinition) is { } parameterElement)
                    {
                        ApplyInstanceParameterToType(document, values, result, updatedTypes, instance, typeOfInstance, parameterElement);
                        continue;
                    }
                    else if (element is ElementType elementType)
                    {
                        ApplyValuesToInstances(document, targetParameterDefinition, result, value, elementType);
                        continue;
                    }
                    else
                    {
                        result.Failed.Add(element.GetIdIntegerValue());
                        continue;
                    }
                }

                UpdateValue(result, value.Value, element, parameter);
            }
            finally
            {
                parameter.Dispose();
            }
        }

        return result;
    }

    private void ApplyInstanceParameterToType(Document document, Dictionary<ElementId, string> values, RestoreValuesResult result, List<ElementId> updatedTypes, Instance instance, ElementType typeOfInstance, Parameter parameterElement)
    {
        // Parameter has changed from instance to type
        if (updatedTypes.Contains(parameterElement.Id))
            return; // Type has already been updated

        var instancesOfType = new Dictionary<ElementId, string>();

        using var collector = new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategoryId(instance.Category.Id);

        using var iterator = collector.GetElementIterator();

        while (iterator.MoveNext())
        {
            var instanceElement = iterator.Current;

            if (instanceElement is null)
                continue;

            if (instanceElement.GetTypeId() != typeOfInstance.Id)
                continue;

            if (!values.TryGetValue(instanceElement.Id, out var instanceValue))
                continue;

            instancesOfType.Add(instanceElement.Id, instanceValue);
        }

        updatedTypes.Add(parameterElement.Id);

        var mostUsedValue = instancesOfType.GroupBy(x => x.Value).OrderByDescending(x => x.Count()).FirstOrDefault().Key;
        if (mostUsedValue is null)
        {
            result.Failed.Add(instance.GetIdIntegerValue());
            return;
        }

        UpdateValue(result, mostUsedValue, typeOfInstance, parameterElement);
    }

    private void ApplyValuesToInstances(Document document, InternalDefinition targetParameterDefinition, RestoreValuesResult result, KeyValuePair<ElementId, string> value, ElementType elementType)
    {
        var instancesOfType = new Dictionary<ElementId, string>();
        using var collector = new FilteredElementCollector(document)
                                    .WhereElementIsNotElementType()
                                    .OfCategoryId(elementType.Category.Id);

        using var iterator = collector.GetElementIterator();
        while (iterator.MoveNext())
        {
            var instanceElement = iterator.Current;

            if (instanceElement is null)
                continue;

            if (instanceElement.GetTypeId() != elementType.Id)
                continue;

            using var instanceParameter = instanceElement.get_Parameter(targetParameterDefinition);

            if (instanceParameter is null)
                continue;

            UpdateValue(result, value.Value, instanceElement, instanceParameter);
        }
    }

    private void UpdateValue(RestoreValuesResult result, string value, Element element, Parameter parameter)
    {
        var existingValue = GetParameterValue(parameter);

        if (existingValue == value)
        {
            result.Unchanged.Add(element.GetIdIntegerValue());
            return;
        }

        if (SetParameterValue(result, parameter, value))
            result.Updated.Add(element.GetIdIntegerValue());
        else
            result.Failed.Add(element.GetIdIntegerValue());
    }

    public bool SetParameterValue(RestoreValuesResult result, Parameter parameter, string value)
    {
        if (parameter.IsReadOnly)
            return false;

        var interalDefinition = (InternalDefinition)parameter.Definition;
        var element = parameter.Element;
        var document = element.Document;

        if (element.GroupId != ElementId.InvalidElementId && !interalDefinition.VariesAcrossGroups)
        {
            try
            {
                interalDefinition.SetAllowVaryBetweenGroups(document, true);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("This parameter does not support the specified value of allowVaryBetweenGroups."))
                    throw new AddSharedParameterFailedException($"Parameter {interalDefinition.Name} does not support variations in values across different group instances. " +
                        $"Failed to set value '{value}' to element '{element.Id}'");
                else
                    throw;
            }
            result.MarkParameterForRestoreVaryAcrossGroups(parameter.Id);
        }

        if (document.IsWorkshared && WorksharingUtils.GetCheckoutStatus(document, parameter.Element.Id) == CheckoutStatus.OwnedByOtherUser)
            throw new AddSharedParameterFailedException("Element is owned by anyone else, sync with central and try again");

        switch (parameter.StorageType)
        {
            case StorageType.None:
                throw new AddSharedParameterFailedException($"Failed to set value {value} on duplicatedParameter {parameter.Definition.Name} on instance {parameter.Element.Id}");
            case StorageType.Double:
                if (value.GetType().Equals(typeof(string)))
                {
                    return parameter.Set(double.Parse(value));
                }
                else
                {
                    return parameter.Set(Convert.ToDouble(value));
                }
            case StorageType.Integer:
                if (value.GetType().Equals(typeof(string)))
                {
                    return parameter.Set(int.Parse(value));
                }
                else
                {
                    return parameter.Set(Convert.ToInt32(value));
                }
            case StorageType.ElementId:
                if (value.GetType().Equals(typeof(string)))
                {
#if R2024_OR_GREATER
                    return parameter.Set(new ElementId(long.Parse(value)));
#else
                    return parameter.Set(new ElementId(int.Parse(value)));
#endif
                }
                else
                {
#if R2024_OR_GREATER
                    return parameter.Set(new ElementId(Convert.ToInt64(value)));
#else
                    return parameter.Set(new ElementId(Convert.ToInt32(value)));
#endif
                }
            case StorageType.String:
                return parameter.Set(value.ToString());
        }

        throw new AddSharedParameterFailedException($"Failed to set value {value} on duplicatedParameter {parameter.Definition.Name} on instance {parameter.Element.Id}");
    }

    internal RestoreValuesResult MergeParameters(Document document, SharedParameterContextBase context, CategorySet categories,
        Dictionary<ElementId, string>? origionalValues = null)
    {
        var allValues = new Dictionary<ElementId, List<string>>();

        foreach (var duplicatedParameter in context.GetDuplicatedParameters())
        {
            var backupParameterValues = FetchValues(duplicatedParameter);

            foreach (var value in backupParameterValues)
            {
                if (allValues.ContainsKey(value.Key))
                    allValues[value.Key].Add(value.Value);
                else
                    allValues.Add(value.Key, [value.Value]);
            }
        }

        origionalValues = GetMostUsedValueOrDefault(allValues, origionalValues);

        var mergeResult = SetValuesOnTargetParameter(document, context.InternalDefinition, origionalValues, categories);
        document.Delete(context.GetDuplicatedParameters().Select(x => x.Id).ToList());

        context.DeletedDuplicatedParameters = true;

        return mergeResult;
    }

    private Dictionary<ElementId, string> GetMostUsedValueOrDefault(Dictionary<ElementId, List<string>> allValues, Dictionary<ElementId, string>? origionalValues)
    {
        var result = new Dictionary<ElementId, string>();

        foreach (var item in allValues)
        {
            if (origionalValues != null && origionalValues.ContainsKey(item.Key))
            {
                result.Add(item.Key, origionalValues[item.Key]);
                continue;
            }

            result.Add(item.Key, GetMostCommonValue(item.Value));
        }

        return result;
    }

    public Dictionary<ElementId, string> FetchValues(ParameterElement parameter)
    {
        return FetchBackupValues(parameter.Document, parameter.GetDefinition());
    }

    public Dictionary<ElementId, string> FetchBackupValues(Document document, InternalDefinition internalDefinition)
    {
#if R2019
        var filterRule = ParameterFilterRuleFactory.CreateSharedParameterApplicableRule(internalDefinition.Name);
#else
        var filterRule = ParameterFilterRuleFactory.CreateHasValueParameterRule(internalDefinition.Id);
#endif
        using var collector = new FilteredElementCollector(document).WherePasses(new ElementParameterFilter(filterRule));

        return GetValuesFromElements(collector, internalDefinition);
    }

    private Dictionary<ElementId, string> GetValuesFromElements(FilteredElementCollector collector, InternalDefinition definition)
    {
        var result = new Dictionary<ElementId, string>();

        using var iterator = collector.GetElementIterator();

        while (iterator.MoveNext())
        {
            using var element = iterator.Current;

            if (element is null)
                continue;

            var document = element.Document;

            using var parameter = element.get_Parameter(definition) ??
                document.GetElement(element.GetTypeId())?.get_Parameter(definition);

            if (parameter is null)
                continue;

            var value = GetParameterValue(parameter);

            if (string.IsNullOrEmpty(value))
                continue;

            result.Add(element.Id, value);
        }

        return result;
    }
}