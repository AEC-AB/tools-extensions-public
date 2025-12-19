namespace RevitExtensionDemo.Collectors;

public class CustomAsyncAutoFillCollector : IRevitAutoFillCollector<RevitExtensionDemoArgs>
{

    public Dictionary<string, string> Get(UIApplication uiApplication, RevitExtensionDemoArgs args)
    {
        var options = new Dictionary<string, string>
        {
            { "OptionA", "Option A" },
            { "OptionB", "Option B" },
            { "OptionC", "Option C" }
        };

        return options;
    }
}