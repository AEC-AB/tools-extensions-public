using AddSharedParameters.Handlers;
using AddSharedParameters.Helpers;
using System.IO;

namespace AddSharedParameters.Collectors;

public class SharedParameterAutoFillCollector : IRevitAutoFillCollector<AddSharedParametersArgs>
{
    private readonly SharedParameterHelper _sharedParameterHelper = new();

    public Dictionary<string, string> Get(UIApplication uiApplication, AddSharedParametersArgs args)
    {
        var logMessages = new List<string>();
        var result = new Dictionary<string, string>();

        if (args.SharedParameterPath is null)
        {
            result = new Dictionary<string, string> { { string.Empty, "Shared parameter file path is empty" } };
            return result;
        }

        try
        {
            var sharedParameterPath = Environment.ExpandEnvironmentVariables(args.SharedParameterPath);

            if (!File.Exists(sharedParameterPath))
            {
                result = new Dictionary<string, string> { { string.Empty, $"Could not find shared parameter file: {args.SharedParameterPath}" } };
                return result;
            }

            using var sharedParameterFileHandler = new SharedParameterFileHandler(uiApplication.Application);
            var definitionFile = sharedParameterFileHandler.OpenSharedParameterFile(args.SharedParameterPath);
            
            var parameters = _sharedParameterHelper.GetAllParametersInSharedParameterFile(definitionFile);

            foreach (var sharedParameterFileDefinition in parameters.Where(sharedParameterFileDefinition => !result.ContainsKey(sharedParameterFileDefinition.Key)))
            {
                result.Add(sharedParameterFileDefinition.Key, sharedParameterFileDefinition.ToString());
            }
        }
        catch(Exception e)
        {
            result = new Dictionary<string, string> { { string.Empty, $"Failed to get auto fill: {e.Message}" } };
        }
        return result;
    }

    private string ExpandSpecialEnvironmentVariables(string sharedParameterPath)
    {
        if (string.IsNullOrEmpty(sharedParameterPath))
            return string.Empty;

        return Environment.ExpandEnvironmentVariables(sharedParameterPath);
    }
}