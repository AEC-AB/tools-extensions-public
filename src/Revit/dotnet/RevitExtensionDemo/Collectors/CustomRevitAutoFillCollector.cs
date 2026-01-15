namespace RevitExtensionDemo.Collectors;

internal class CustomRevitAutoFillCollector : IRevitAutoFillCollector<RevitExtensionDemoArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, RevitExtensionDemoArgs args)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var document = uiApplication.ActiveUIDocument?.Document;

            if (document is null)
                return result;

            using var element = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_GenericModel).FirstElement();

            foreach (var parameter in element.GetOrderedParameters())
            {
                result.Add(parameter.Definition.Name, parameter.Definition.Name);
            }
        }
        catch
        {
            // ignore
        }

        return result;
    }
}
