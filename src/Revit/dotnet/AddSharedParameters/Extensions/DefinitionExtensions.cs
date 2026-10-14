namespace AddSharedParameters.Extensions;

internal static class DefinitionExtensions
{
#if R2024_OR_GREATER
    public static ForgeTypeId GetParameterGroup(this Definition definition)
    {
        return definition.GetGroupTypeId();
    }
#else
    public static BuiltInParameterGroup GetParameterGroup(this Definition definition)
    {
        return definition.ParameterGroup;
    }
#endif


    internal static bool HasDifferentNameThen(this Definition definition, Definition otherDefinition)
    {
        return !definition.Name.Equals(otherDefinition.Name);
    }

    internal static bool HasDifferentParameterTypeThen(this Definition definitionA, Definition definitionB)
    {
#if R2022_OR_GREATER
        var parameterAType = definitionA.GetDataType();
        var parameterBType = definitionB.GetDataType();
#else
        var parameterAType = definitionA.ParameterType;
        var parameterBType = definitionB.ParameterType;
#endif

        return parameterAType != parameterBType;
    }

    public static bool HasDifferentParameterGroupThen(this InternalDefinition internalDefinition, string otherParameterGroup)
    {
#if R2024_OR_GREATER
        var parameterGroup = internalDefinition.GetParameterGroup().TypeId;
#else
        var parameterGroup = internalDefinition.ParameterGroup.ToString();
#endif

        return !parameterGroup.Equals(otherParameterGroup);
    }
}