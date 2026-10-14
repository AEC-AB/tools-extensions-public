using AddSharedParameters.Helpers;

namespace AddSharedParameters.Results;

public abstract record AddSharedParametersResultBase(string ParameterName)
{
    public bool Succeeded => string.IsNullOrEmpty(Warning);

    public string? Warning { get; set; }

    public required bool MergedParameters { get; init; }
    public required RestoreValuesResult? MergeParameterResult { get; init; }

    internal string? _details;

    public string GetDetails()
    {
        return _details ?? string.Empty;
    }
}
