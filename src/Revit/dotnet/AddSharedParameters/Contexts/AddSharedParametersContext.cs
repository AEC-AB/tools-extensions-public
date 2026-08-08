using System.Text;

namespace AddSharedParameters.Contexts;

public class AddSharedParametersContext(ExternalDefinition externalDefinition, CategoriesToBindResult categories) : SharedParameterContextBase(externalDefinition, categories)
{
    public override void AddSpesificDetails(StringBuilder sb)
    {
        sb.AppendLine($" - Parameter was added");

        sb.AppendLine($" - Added to categories:");
        foreach (Category category in CategoriesToBind.CategorySet)
            sb.AppendLine($"   * {category.Name}");
    }

    public void SetInternalDefinition(InternalDefinition internalDefinition)
    {
        _internalDefinition = internalDefinition;
    }

    internal AddSharedParametersResult GetResult()
    {
        var details = GetDetails();

        return new AddSharedParametersResult(ExternalDefinition.Name)
        {
            MergedParameters = MergedParameters,
            MergeParameterResult = MergeParameterResult,
            _details = details,
            Warning = GetWarning()
        };
    }
}