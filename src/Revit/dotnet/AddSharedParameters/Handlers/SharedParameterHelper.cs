using AddSharedParameters.Enums;
using AddSharedParameters.Extensions;
using AddSharedParameters.Helpers;

namespace AddSharedParameters.Handlers;

public class SharedParameterHelper
{
    private readonly ParameterValueHelper _parameterValueHelper;
    private readonly ScheduleHelper _scheduleHelper;

    // Constructor for shared instances
    public SharedParameterHelper(ParameterValueHelper parameterValueHelper, ScheduleHelper scheduleHelper)
    {
        _parameterValueHelper = parameterValueHelper;
        _scheduleHelper = scheduleHelper;
    }

    // Default constructor for backward compatibility
    public SharedParameterHelper()
    {
        _parameterValueHelper = new ParameterValueHelper();
        _scheduleHelper = new ScheduleHelper();
    }

    public ExternalDefinition? GetExternalDefinition(SharedParameterFileDefinition parameterDefinition, DefinitionFile definitionFile)
    {
        return definitionFile.Groups.SelectMany(x => x.Definitions.OfType<ExternalDefinition>()).FirstOrDefault(x => x.GUID.Equals(parameterDefinition.Guid));
    }

    public IEnumerable<SharedParameterElement> GetSharedParameterElements(Document document)
    {
        using var collector = new FilteredElementCollector(document)
            .OfClass(typeof(SharedParameterElement));

        return collector.OfType<SharedParameterElement>().ToList();
    }

    internal RemovedParameterBackup DeleteAndInsertParameter(Document document, UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        var parameterGroup = GetParameterGroup(context, args);
        var backupValues = _parameterValueHelper.FetchBackupValues(document, context.InternalDefinition);

        if (context.ChangeBindingType)
        {
            var elementBinding = CreateBinding(document, context.CategoriesToBind.CategorySet, args.BindingType);
            context.UpdateElementBinding(elementBinding);
        }
        else if (context.CategoriesToBind.HasChanges)
        {
            context.ElementBinding.Categories = context.CategoriesToBind.CategorySet;
        }

        context.SheduleBackup = _scheduleHelper.ScheduleBackup(document, context.InternalDefinition);

        var removed = document.ParameterBindings.Remove(context.InternalDefinition);

        if (!removed)
            throw new AddSharedParameterFailedException($"Could not remove parameter: '{context.ExternalDefinition.Name}'");

        try
        {
            document.Delete(context.InternalDefinition.Id);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Could not delete parameter: '{context.ExternalDefinition.Name}'", e);
        }

        var inserted = document.ParameterBindings.Insert(context.ExternalDefinition, context.ElementBinding, parameterGroup);

        if (!inserted)
            throw new AddSharedParameterFailedException($"Could not insert parameter: '{context.ExternalDefinition.Name}'");

        var internalDefinition = GetInternalDefinition(document, context.ExternalDefinition)
            ?? throw new AddSharedParameterFailedException($"Parameter deleted and inserted but could not get internal definition: '{context.ExternalDefinition.Name}'");

        context.UpdateInternalDefinition(internalDefinition);

        if (context.SheduleBackup is not null)
            _scheduleHelper.RestoreSchedule(document, context);

        context.ParameterGroupUpdated = context.ChangeParameterGroup;
        context.CategoriesUpdated = context.CategoriesToBind.HasChanges;
        context.ParameterWasReplaced = true;

        return new RemovedParameterBackup(backupValues);
    }

    internal bool HasDuplicateParameters(Document document, ExternalDefinition externalDefinition, out List<ParameterElement> collection)
    {
        collection = [];

        using var collector = new FilteredElementCollector(document).OfClass(typeof(ParameterElement));
        using var iterator = collector.GetEnumerator();

        while (iterator.MoveNext())
        {
            var current = iterator.Current;

            if (current is not ParameterElement parameterElement)
            {
                current?.Dispose();
                continue;
            }

            if (parameterElement.GetDefinition().BuiltInParameter != BuiltInParameter.INVALID)
            {
                current.Dispose();
                continue;
            }

            if (current is SharedParameterElement sharedParameterElement && sharedParameterElement.GuidValue.Equals(externalDefinition.GUID))
            {
                current.Dispose();
                continue;
            }

            if (!parameterElement.Name.Equals(externalDefinition.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                current.Dispose();
                continue;
            }

            collection.Add(parameterElement);
        }

        return collection.Count > 0;
    }

    // Check if a shared parameter is only present at family level (i.e. not bound in the project document)
    public bool IsFamilyLevelBinding(Document document, ExternalDefinition externalDefinition)
    {
        // If the document has no binding for the external definition, we treat it as family-level only.
        var binding = document.ParameterBindings.get_Item(externalDefinition);
        if (binding is null)
            return true;

        return false;
    }

    internal RemovedParameterBackup? ReInsertParameter(Document document, UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        var parameterGroup = GetParameterGroup(context, args);

        if (context.CategoriesToBind.HasChanges)
            context.ElementBinding.Categories = context.CategoriesToBind.CategorySet;

        if (document.GetElement(context.InternalDefinition.Id) is not ParameterElement parameterElement)
            throw new AddSharedParameterFailedException($"Could not get parameter: '{context.ExternalDefinition.Name}'");

        var backupValues = _parameterValueHelper.FetchValues(parameterElement);

        // Must reinsert to get it updated: http://thebuildingcoder.typepad.com/blog/2009/09/adding-a-category-to-a-parameter-binding.html
        var result = document.ParameterBindings.ReInsert(context.ExternalDefinition, context.ElementBinding, parameterGroup);

        if (!result)
            throw new AddSharedParameterFailedException($"Could not reinsert parameter: '{context.ExternalDefinition.Name}'");

        var internalDefinition = GetInternalDefinition(document, context.ExternalDefinition)
            ?? throw new AddSharedParameterFailedException($"Parameter reinserted but could not get internal definition: '{context.ExternalDefinition.Name}'");

        context.UpdateInternalDefinition(internalDefinition);

        context.ParameterGroupUpdated = context.ChangeParameterGroup;
        context.CategoriesUpdated = context.CategoriesToBind.HasChanges;
        context.ParameterWasUpdated = true;

        return new RemovedParameterBackup(backupValues);
    }

#if R2024_OR_GREATER
    private ForgeTypeId GetParameterGroup(UpdateSharedParameterContext context, AddSharedParametersArgs args)
#else
        private BuiltInParameterGroup GetParameterGroup(UpdateSharedParameterContext context, AddSharedParametersArgs args)
#endif
    {
        if (context.ChangeParameterGroup)
            return args.GetParameterGroup();

#if R2024_OR_GREATER
        return context.InternalDefinition.GetParameterGroup();
#else
        return context.InternalDefinition.ParameterGroup;
#endif
    }

    internal InternalDefinition GetInternalDefinition(Document document, string parameterName)
    {
        if (Guid.TryParse(parameterName, out var guid))
            return GetInternalDefinitionByGuid(document, guid);

        return GetInternalDefinitionByName(document, parameterName);
    }

    private InternalDefinition GetInternalDefinitionByName(Document document, string parameterName)
    {
        using (var collector = new FilteredElementCollector(document).OfClass(typeof(SharedParameterElement)))
        {
            foreach (SharedParameterElement sharedParameterElement in collector)
            {
                if (sharedParameterElement.Name.Equals(parameterName))
                    return sharedParameterElement.GetDefinition();
            }
        }

        throw new AddSharedParameterFailedException($"Could not find parameter {parameterName} in document {document.Title}");
    }

    internal InternalDefinition GetInternalDefinition(Document document, ExternalDefinition definition)
    {
        return GetInternalDefinitionByGuid(document, definition.GUID);
    }

    private InternalDefinition GetInternalDefinitionByGuid(Document document, Guid guid)
    {
        using (var collector = new FilteredElementCollector(document).OfClass(typeof(SharedParameterElement)))
        {
            foreach (SharedParameterElement sharedParameterElement in collector)
            {
                if (sharedParameterElement.GuidValue.Equals(guid))
                    return sharedParameterElement.GetDefinition();
            }
        }

        throw new AddSharedParameterFailedException($"Could not find parameter {guid} in document {document.Title}");
    }

    internal void SetVariesAcrossGroups(Document document, SharedParameterContextBase context, VariesAcrossGroups variesAcrossGroups)
    {
        if (context.InternalDefinition.VariesAcrossGroups.Equals(variesAcrossGroups == VariesAcrossGroups.Vary))
            return;

        try
        {
            context.InternalDefinition.SetAllowVaryBetweenGroups(document, variesAcrossGroups.Equals(VariesAcrossGroups.Vary));
            context.VariesAcrossGroupsUpdated = true;
        }
        catch (Exception e)
        {
            if (e.Message.Contains("This parameter does not support the specified value of allowVaryBetweenGroups."))
                context.AddMessage("This parameter does not support variations in values across different group instances.");
            else
                throw new AddSharedParameterFailedException($"Could not change values can vary by group for parameter: '{context.InternalDefinition.Name}'", e);
        }
        
    }

    public List<SharedParameterFileDefinition> GetAllParametersInSharedParameterFile(DefinitionFile file)
    {
        var result = new List<SharedParameterFileDefinition>();

        foreach (var group in file.Groups)
        {
            var groupName = group.Name;

            foreach (var definition in group.Definitions)
            {
                if (definition is not ExternalDefinition externalDefinition) continue;

                var parameterType = string.Empty;

#if R2022_OR_GREATER
                var forgeTypeId = externalDefinition.GetDataType();
                parameterType = LabelUtils.GetLabelForSpec(forgeTypeId);
#else
                parameterType = LabelUtils.GetLabelFor(definition.ParameterType);
#endif
                result.Add(new SharedParameterFileDefinition(externalDefinition.GUID, definition.Name, groupName, parameterType));
            }
        }

        return result;
    }

    public ElementBinding CreateBinding(Document document, CategorySet categories, BindingType typeBinding)
    {
        return typeBinding switch
        {
            BindingType.Type => document.Application.Create.NewTypeBinding(categories),
            BindingType.Instance => document.Application.Create.NewInstanceBinding(categories),
            _ => throw new ArgumentOutOfRangeException(nameof(typeBinding), typeBinding, null)
        };
    }

    internal void ResetVariesAcrossGroups(Document document, List<ElementId> parameterIdsToReset)
    {
        foreach (var parameterId in parameterIdsToReset)
        {
            using var parameter = document.GetElement(parameterId) as ParameterElement ??
                throw new NullReferenceException("Failed to restore VaryBetweenGroups, parameter was not found");

            var internalDefinition = parameter.GetDefinition();
            internalDefinition.SetAllowVaryBetweenGroups(document, false);
        }
    }
}