namespace AddSharedParameters.Extensions;

public static class AddSharedParameterResultsExtensions
{
    public static IEnumerable<AddSharedParametersResult> GetAdded(this IEnumerable<AddSharedParametersResultBase> results)
    {
        return results.OfType<AddSharedParametersResult>();
    }

    public static IEnumerable<UpdateSharedParameterResult> GetUpdated(this IEnumerable<AddSharedParametersResultBase> results)
    {
        return results.OfType<UpdateSharedParameterResult>();
    }

    public static IEnumerable<AddSharedParametersResultBase> GetFailed(this IEnumerable<AddSharedParametersResultBase> results)
    {
        return results.Where(x => !x.Succeeded);
    }

    public static IEnumerable<AddSharedParametersResultBase> GetSucceeded(this IEnumerable<AddSharedParametersResultBase> results)
    {
        return results.Where(x => x.Succeeded);
    }
}
