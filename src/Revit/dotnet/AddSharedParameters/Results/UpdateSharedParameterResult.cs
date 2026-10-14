using AddSharedParameters.Helpers;

namespace AddSharedParameters.Results;

public record UpdateSharedParameterResult(string ParameterName) : AddSharedParametersResultBase(ParameterName)
{
    public required bool ParameterGroupUpdated { get; init; }
    public required bool CategoriesUpdated { get; init; }
    public required bool ParameterWasUpdated { get; init; }
    public required bool ParameterWasReplaced { get; init; }
    public required RestoreValuesResult? RestoreValuesResult { get; init; }
}
