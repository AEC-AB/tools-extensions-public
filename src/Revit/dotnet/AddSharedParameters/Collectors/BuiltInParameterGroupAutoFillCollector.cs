#if R2024_OR_GREATER

namespace AddSharedParameters;

internal class BuiltInParameterGroupAutoFillCollector : IRevitAutoFillCollector<AddSharedParametersArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, AddSharedParametersArgs args)
    {
        var result = new Dictionary<string, string>();
        var groups = ParameterUtils.GetAllBuiltInGroups();

        var noneGroup = new ForgeTypeId(string.Empty);
        groups.Add(noneGroup);

        foreach (var group in groups)
        {
            var label = LabelUtils.GetLabelForGroup(group);
            result.Add(group.TypeId, label);
        }

        return result;
    }
}
#endif