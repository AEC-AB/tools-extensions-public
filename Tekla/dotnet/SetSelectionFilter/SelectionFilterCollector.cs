using Tekla.Structures.Dialog.UIControls;

namespace SelectObjectFromSelectionFilter;

internal class SelectionFilterCollector : ITeklaAutoFillCollector<SelectObjectFromSelectionFilterArgs>
{
    public Dictionary<string, string> Get(SelectObjectFromSelectionFilterArgs args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var model = new Model();
        if (!model.GetConnectionStatus())
        {
            return values;
        }

        var folders = EnvironmentFiles.GetStandardPropertyFileDirectories();
        var filters = EnvironmentFiles.GetMultiDirectoryFileList(folders, "SObjGrp");

        foreach (var filter in filters)
        {
            values[filter] = filter;
        }

        return values;
    }
}