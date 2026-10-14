namespace AddSharedParameters.Results;

public class RemovedParameterBackup(Dictionary<ElementId, string> values)
{
    public Dictionary<ElementId, string> Values { get; } = values;
}