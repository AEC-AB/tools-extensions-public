namespace AddSharedParameters.Models;

public class SharedParameterFileDefinition(Guid guid, string name, string groupName, string unit)
{
    public string Key => Guid.ToString();
    public Guid Guid { get; } = guid;
    public string Name { get; } = name;
    public string GroupName { get; } = groupName;
    public string TypeId { get; } = unit;

    public override string ToString()
    {
        return $"{Name} ({GroupName}) [{TypeId}]";
    }
}
