using AddSharedParameters.Helpers;
using System.Text;

namespace AddSharedParameters.Contexts;

public class UpdateSharedParameterContext : SharedParameterContextBase
{
    public UpdateSharedParameterContext(ExternalDefinition externalDefinition,
        CategoriesToBindResult categories,
        ElementBinding elementBinding,
        InternalDefinition internalDefinition
        ) : base(externalDefinition, categories)
    {
        ElementBinding = elementBinding;
        _internalDefinition = internalDefinition;
    }

    public RemovedParameterBackup? RemovedParameterBackup { get; set; }
    public ElementBinding ElementBinding { get; private set; }

    public bool ChangeParameterName { get; set; }
    public bool ChangeParameterType { get; set; }
    public bool ChangeBindingType { get; set; }
    public bool ChangeParameterGroup { get; set; }

    public void UpdateInternalDefinition(InternalDefinition internalDefinition)
    {
        _internalDefinition = internalDefinition;
    }
    public void UpdateElementBinding(ElementBinding elementBinding)
    {
        ElementBinding = elementBinding;
    }

    internal RestoreValuesResult? RestoreValuesResult { get; set; }
    public ScheduleBackups? SheduleBackup { get; set; }

    public bool ParameterGroupUpdated { get; set; }
    public bool ParameterWasUpdated { get; set; }
    public bool ParameterWasReplaced { get; set; }
    public bool CategoriesUpdated { get; set; }
    public bool RestoredSchedules { get; set; }

    internal UpdateSharedParameterResult GetResult()
    {
        var details = GetDetails();

        return new UpdateSharedParameterResult(ExternalDefinition.Name)
        {
            ParameterGroupUpdated = ParameterGroupUpdated,
            CategoriesUpdated = CategoriesUpdated,
            ParameterWasUpdated = ParameterWasUpdated,
            ParameterWasReplaced = ParameterWasReplaced,
            RestoreValuesResult = RestoreValuesResult,
            MergedParameters = MergedParameters,
            MergeParameterResult = MergeParameterResult,
            _details = details,
            Warning = GetWarning()
        };
    }

    internal bool RequiresDeleteAndInsert(AddSharedParametersArgs args)
    {
        return args.ReInsertParameter || ChangeParameterName || ChangeParameterType || ChangeBindingType;
    }

    internal bool RequiresReinsertion()
    {
        return CategoriesToBind.HasChanges || ChangeParameterGroup;
    }

    public override void AddSpesificDetails(StringBuilder sb)
    {
        if (ParameterWasReplaced)
        {
            sb.AppendLine($" - Parameter was replaced");

            if (ChangeParameterName)
                sb.AppendLine($"   * Because of name was changed");

            if (ChangeParameterType)
                sb.AppendLine($"   * Because of type was changed");

            if (ChangeBindingType)
                sb.AppendLine($"   * Because of binding type was changed");

            if (RestoreValuesResult is not null)
            {
                sb.AppendLine($" - Parameter values was restored");
                RestoreValuesResult?.AppendDetails(sb);
            }
        }
        else if (ParameterWasUpdated)
            sb.AppendLine($"- Parameter was updated");

        if (CategoriesUpdated)
        { 
            sb.AppendLine($"- Categories was updated");
            foreach (Category category in CategoriesToBind.CategorySet)
                sb.AppendLine($"   * {category.Name}");
        }

        if (ParameterGroupUpdated)
            sb.AppendLine($"- Parameter group was updated");

        if (RestoredSchedules)
            sb.AppendLine($" - Schedules was restored");
    }
}