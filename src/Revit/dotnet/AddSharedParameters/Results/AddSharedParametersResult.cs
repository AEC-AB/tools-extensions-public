namespace AddSharedParameters.Results;

public record AddSharedParametersResult(string ParameterName) : AddSharedParametersResultBase(ParameterName)
{
    internal static AddSharedParametersResult CreateWarning(string parameterName, string message)
    {
        var result = new AddSharedParametersResult(parameterName)
        {
            Warning = message,
            MergedParameters = false,
            MergeParameterResult = null
        };

        return result;
    }
}