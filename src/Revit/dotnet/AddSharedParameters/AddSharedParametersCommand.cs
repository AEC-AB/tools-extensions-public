using AddSharedParameters.Contexts;
using AddSharedParameters.Enums;
using AddSharedParameters.Extensions;
using AddSharedParameters.Handlers;
using AddSharedParameters.Helpers;
using AddSharedParameters.Results;
using System.IO;

namespace AddSharedParameters;

public class AddSharedParametersCommand : IRevitExtension<AddSharedParametersArgs>
{
    private readonly ScheduleHelper _scheduleHelper = new();
    private readonly CategoryHelper _categoryHelper = new();
    private readonly ParameterValueHelper _parameterValueHelper = new();
    private readonly SharedParameterHelper _sharedParameterHelper;

    public AddSharedParametersCommand()
    {
        // Share helper instances to avoid duplication
        _sharedParameterHelper = new SharedParameterHelper(_parameterValueHelper, _scheduleHelper);
    }

    public IExtensionResult Run(IRevitExtensionContext context, AddSharedParametersArgs args, CancellationToken cancellationToken)
    {
        if (args.SharedParameterPath is null || string.IsNullOrEmpty(args.SharedParameterPath))
            return Result.Text.Failed("No shared parameter file path is set");

        var parameterNames = args.GetNormalizedParameterNames().ToList();

        if (!parameterNames.Any())
            return Result.Text.Failed("No parameters to insert is set");

        var sharedParameterPath = Environment.ExpandEnvironmentVariables(args.SharedParameterPath);

        if (!File.Exists(sharedParameterPath))
            return Result.Text.Failed($"Could not find shared parameter file: {args.SharedParameterPath}");

        var document = context.UIApplication.ActiveUIDocument?.Document;

        if (document is null)
            return Result.Text.Failed("No active Revit model is open");

        var application = context.UIApplication.Application;

        using var sharedParameterFileHandler = new SharedParameterFileHandler(application);
        var definitionFile = sharedParameterFileHandler.OpenSharedParameterFile(sharedParameterPath);
        var selectedParameters = new List<SharedParameterFileDefinition>();
        var parametersInFile = _sharedParameterHelper.GetAllParametersInSharedParameterFile(definitionFile);

        // Convert to dictionary for O(1) lookup instead of O(n) per iteration
        var parameterLookup = parametersInFile.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        var results = new AddSharedParametersResults();

        foreach (var parameterName in parameterNames)
        {
            if (parameterLookup.TryGetValue(parameterName, out var parameter))
                selectedParameters.Add(parameter);
            else
                results.Add(AddSharedParametersResult.CreateWarning(parameterName, $"Could not find parameter in shared parameter file"));
        }

        if (!selectedParameters.Any())
            return Result.Text.Failed("No parameters to insert is found in the shared parameter file");

        using var transaction = new Transaction(document, "Adding shared parameters");
        transaction.Start();

        var externalDefinitions = new List<ExternalDefinition>();

        try
        {
            foreach (var selectedParameter in selectedParameters)
            {
                if (_sharedParameterHelper.GetExternalDefinition(selectedParameter, definitionFile) is not ExternalDefinition externalDefinition)
                    throw new AddSharedParameterFailedException($"Could not find External ExternalDefinition for {selectedParameter.Name}");

                externalDefinitions.Add(externalDefinition);
            }

            foreach (var externalDefinition in externalDefinitions)
            {
                var result = AddSharedParameter(externalDefinition, document, args, definitionFile);
                results.Add(result);
            }

            if (args.ScheduleName is not null && !string.IsNullOrEmpty(args.ScheduleName))
            {
                // Avoid creating intermediate list - pass IEnumerable directly
                var internalDefinitions = externalDefinitions.Select(externalDefinition => _sharedParameterHelper.GetInternalDefinition(document, externalDefinition));
                _scheduleHelper.CreateSchedule(args.ScheduleName, internalDefinitions, document, args.CategoryNames ?? []);
            }

            if (args.MergeParameters)
            {
                foreach (var externalDefinition in externalDefinitions)
                {
                    if (_sharedParameterHelper.HasDuplicateParameters(document, externalDefinition, out var _))
                        throw new AddSharedParameterFailedException($"Failed to merge parameters, duplicate parameters found");
                }
            }

            transaction.Commit();

            return results;
        }
        catch (AddSharedParameterFailedException failedException)
        {
            return Result.Text.Failed(failedException.Message);
        }
        catch (AddSharedParameterPartiallySucceededException partialSuccessException)
        {
            return Result.Text.PartiallySucceeded(partialSuccessException.Message);
        }
    }

    private AddSharedParametersResultBase AddSharedParameter(ExternalDefinition externalDefinition, Document document, AddSharedParametersArgs args, DefinitionFile definitionFile)
    {
        if (document.ParameterBindings.get_Item(externalDefinition) is ElementBinding elementBinding)
            return UpdateParameterBinding(externalDefinition, document, args, elementBinding);

        return InsertParameterBinding(externalDefinition, document, args);
    }

    private UpdateSharedParameterResult UpdateParameterBinding(ExternalDefinition externalDefinition, Document document, AddSharedParametersArgs args, ElementBinding elementBinding)
    {
        if (_sharedParameterHelper.GetInternalDefinition(document, externalDefinition) is not InternalDefinition internalDefinition)
            throw new InvalidOperationException($"Document don't contain a internal definition for parameter {externalDefinition.Name}");

        // Skip parameters bound only at the family level
        if (_sharedParameterHelper.IsFamilyLevelBinding(document, externalDefinition))
        {
            // Return a warning result without attempting any updates
            var skippedResult = new UpdateSharedParameterResult(ParameterName: externalDefinition.Name)
            {
                ParameterGroupUpdated = false,
                CategoriesUpdated = false,
                ParameterWasUpdated = false,
                ParameterWasReplaced = false,
                RestoreValuesResult = null,
                MergedParameters = false,
                MergeParameterResult = null,
                Warning = $"Parameter '{externalDefinition.Name}' is bound only at the family level and was skipped.",
                _details = $"** Parameter: {externalDefinition.Name} **\n - Skipped because it is bound only at the family level"
            };
            return skippedResult;
        }

        var categories = _categoryHelper.CollectCategoriesToBind(document, elementBinding.Categories, args);
        using var context = new UpdateSharedParameterContext(externalDefinition, categories, elementBinding, internalDefinition);

        CheckParameterNameOrTypeChange(context, args);
        CheckForChangedBindingType(context, args);
        CheckForChangedParameterGroup(context, args);

        if (context.RequiresDeleteAndInsert(args))
            context.RemovedParameterBackup = _sharedParameterHelper.DeleteAndInsertParameter(document, context, args);
        else if (context.RequiresReinsertion())
            context.RemovedParameterBackup = _sharedParameterHelper.ReInsertParameter(document, context, args);

        if (context.ElementBinding is InstanceBinding)
            _sharedParameterHelper.SetVariesAcrossGroups(document, context, args.VariesAcrossGroups);

        if (args.MergeParameters && _sharedParameterHelper.HasDuplicateParameters(document, context.ExternalDefinition, out var duplicatedParameters))
        {
            context.AddDuplicatedParameterRange(duplicatedParameters);

            var origionalValues = context.RemovedParameterBackup is null ?
                _parameterValueHelper.FetchBackupValues(document, context.InternalDefinition) :
                context.RemovedParameterBackup.Values;

            context.MergeParameterResult = _parameterValueHelper.MergeParameters(document, context, context.ElementBinding.Categories, origionalValues);

            if (context.MergeParameterResult?.GetParameterIdsToRestoreVaryAcrossGroups() is { } mergedResultParameterIdsToRestore)
                _sharedParameterHelper.ResetVariesAcrossGroups(document, mergedResultParameterIdsToRestore);

            context.MergedParameters = true;
            return context.GetResult();
        }

        if (context.RemovedParameterBackup is null)
            return context.GetResult();

        context.RestoreValuesResult = _parameterValueHelper.SetValuesOnTargetParameter(document, context.InternalDefinition, context.RemovedParameterBackup.Values, context.ElementBinding.Categories);

        if (context.RestoreValuesResult.GetParameterIdsToRestoreVaryAcrossGroups() is { } parameterIdsToReset)
            _sharedParameterHelper.ResetVariesAcrossGroups(document, parameterIdsToReset);

        return context.GetResult();
    }

    private void CheckForChangedParameterGroup(UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        if (!args.ChangeParameterGroupOnExistingBindings)
            return;
#if R2024_OR_GREATER
        context.ChangeParameterGroup = context.InternalDefinition.HasDifferentParameterGroupThen(args.ParameterGroup);
#else
        context.ChangeParameterGroup = context.InternalDefinition.HasDifferentParameterGroupThen(args.ParameterGroup.ToString());
#endif
    }

    private AddSharedParametersResult InsertParameterBinding(ExternalDefinition externalDefinition, Document document, AddSharedParametersArgs args)
    {
        var categories = _categoryHelper.CollectCategoriesToBind(document, document.Application.Create.NewCategorySet(), args);
        using var context = new AddSharedParametersContext(externalDefinition, categories);

        if (args.CategoryNames is null || args.CategoryNames.Count == 0)
            throw new AddSharedParameterFailedException("Trying to add a new shared parameter without any categories");

        var binding = _sharedParameterHelper.CreateBinding(document, context.CategoriesToBind.CategorySet, args.BindingType);

        try
        {
            document.ParameterBindings.Insert(context.ExternalDefinition, binding, args.GetParameterGroup());
            var internalDefinition = _sharedParameterHelper.GetInternalDefinition(document, context.ExternalDefinition);

            context.SetInternalDefinition(internalDefinition);

            if (binding is InstanceBinding)
                _sharedParameterHelper.SetVariesAcrossGroups(document, context, args.VariesAcrossGroups);

            if (args.MergeParameters && _sharedParameterHelper.HasDuplicateParameters(document, context.ExternalDefinition, out var parameterElements))
            {
                context.AddDuplicatedParameterRange(parameterElements);
                context.MergeParameterResult = _parameterValueHelper.MergeParameters(document, context, context.CategoriesToBind.CategorySet);

                if (context.MergeParameterResult?.GetParameterIdsToRestoreVaryAcrossGroups() is { } mergedResultParameterIdsToRestore)
                    _sharedParameterHelper.ResetVariesAcrossGroups(document, mergedResultParameterIdsToRestore);

                context.MergedParameters = true;
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Could not bind shared parameter: '{context.ExternalDefinition.Name}'", e);
        }

        return context.GetResult();
    }

    private void CheckParameterNameOrTypeChange(UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        if (args.ReplaceParameter is null || !args.ReplaceParameter.Any())
            return;

        if (args.ReplaceParameter.Contains(ReplaceParameter.Name))
            context.ChangeParameterName = context.InternalDefinition.HasDifferentNameThen(context.ExternalDefinition);

        if (args.ReplaceParameter.Contains(ReplaceParameter.Type))
            context.ChangeParameterType = context.InternalDefinition.HasDifferentParameterTypeThen(context.ExternalDefinition);
    }

    private void CheckForChangedBindingType(UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        if (!args.CangeBindingTypeOnExistingBindings)
            return;

        context.ChangeBindingType = BindingTypeHasChanged(context, args);
    }

    private bool BindingTypeHasChanged(UpdateSharedParameterContext context, AddSharedParametersArgs args)
    {
        var typeBinding = args.BindingType.Equals(BindingType.Type);

        return args.BindingType switch
        {
            BindingType.Type => context.ElementBinding is not TypeBinding,
            BindingType.Instance => context.ElementBinding is not InstanceBinding,
            _ => throw new ArgumentOutOfRangeException("Failed to check if binding type has changed, binding type is not supported", nameof(args.BindingType))
        };
    }
}