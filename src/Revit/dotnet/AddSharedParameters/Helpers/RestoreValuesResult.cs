using System.Text;

namespace AddSharedParameters.Helpers;

public class RestoreValuesResult
{
    public List<long> Unchanged { get; } = [];
    public List<long> Updated { get; } = [];
    public List<long> Failed { get; } = [];
    

    internal void AppendDetails(StringBuilder sb)
    {
        sb.AppendLine($"    * Unchanged: {Unchanged.Count}, Updated: {Updated.Count}, Failed: {Failed.Count}");
    }

    private readonly List<ElementId> _parametersToRestore = [];

    internal void MarkParameterForRestoreVaryAcrossGroups(ElementId id)
    {
        _parametersToRestore.Add(id);
    }

    internal List<ElementId>? GetParameterIdsToRestoreVaryAcrossGroups()
    {
        if (_parametersToRestore.Count == 0)
            return null;

        return _parametersToRestore;
    }
}