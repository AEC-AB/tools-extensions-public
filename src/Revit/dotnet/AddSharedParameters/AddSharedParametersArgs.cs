using AddSharedParameters.Collectors;
using AddSharedParameters.Enums;

namespace AddSharedParameters;

public class AddSharedParametersArgs
{
    [Description("Shared Parameter file")]
    [ControlType(ControlType.Browse)]
    [FileExtension("txt")]
    [FileExtension(".*")]
    public string? SharedParameterPath { get; set; }

    [Description("Parameters to insert")]
    [ControlType(ControlType.ListBox)]
    [CustomRevitAutoFill(typeof(SharedParameterAutoFillCollector))]
    public List<string>? ParameterNames { get; set; }

#if R2024_OR_GREATER
    [Description("Parameter group")]
    [ControlType(ControlType.ComboBox)]
    [CustomRevitAutoFill(typeof(BuiltInParameterGroupAutoFillCollector))]
    public string ParameterGroup { get; set; } = GroupTypeId.IdentityData.TypeId;
#else
    [Description("Parameter group")]
    public BuiltInParameterGroup ParameterGroup { get; set; } = BuiltInParameterGroup.PG_IDENTITY_DATA;
#endif
    [Description("Change Parameter group")]
    [ControlData(ToolTip = "Change parameter group on exsisting parameters")]
    public bool ChangeParameterGroupOnExistingBindings { get; set; } = true;

    [Description("Binding type")]
    public BindingType BindingType { get; set; } = BindingType.Instance;

    [Description("Change binding type")]
    [ControlData(ToolTip = "Change binding type on exsisting parameters")]
    public bool CangeBindingTypeOnExistingBindings { get; set; } = false;

    [Description("Groups")]
    public VariesAcrossGroups VariesAcrossGroups { get; set; } = VariesAcrossGroups.Vary;

    [Description("Categories")]
    [ControlData(ToolTip = "Categories to include in parameters")]
    [ControlType(ControlType.ListBox)]
    [CustomRevitAutoFill(typeof(BuiltInCategoryAutoFillCollector))]
    public List<string>? CategoryNames { get; set; }

    [Description("Reset categories")]
    [ControlData(ToolTip = "Set Categories on exsisting parameter to this definition")]
    public bool ResetCategories { get; set; } = false;

    [Description("Remove categories")]
    [ControlData(ToolTip = "Categories to remove from parameters")]
    [ControlType(ControlType.ListBox)]
    [CustomRevitAutoFill(typeof(BuiltInCategoryAutoFillCollector))]
    public List<string>? CategoryNamesToRemove { get; set; }

    [Description("Replace parameter")]
    public List<ReplaceParameter>? ReplaceParameter { get; set; }

    [Description("Merge parameters")]
    [ControlData(ToolTip = "Merge parameters with same name")]
    public bool MergeParameters { get; set; }

    [Description("Reinsert parameter")]
    [ControlData(ToolTip = "Reinsert parameter if you have changed the definition in Shared Parameter file")]
    public bool ReInsertParameter { get; set; }

    [Description("Schedule name")]
    [ControlData(ToolTip = "If field out a schedule will be created with the shared parameters")]
    public string? ScheduleName { get; set; }
}
