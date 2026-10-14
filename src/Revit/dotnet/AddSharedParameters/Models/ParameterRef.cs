namespace AddSharedParameters;

public class ParameterRef
{
    public Guid? GuidValue { get; }
    public string Name { get; }

    public ParameterRef(ParameterElement parameter)
    {
        Name = parameter.Name;
        if (parameter is SharedParameterElement sharedParameterElement)
            GuidValue = sharedParameterElement.GuidValue;
       
    }

    public bool IsShared() => GuidValue.HasValue;
}