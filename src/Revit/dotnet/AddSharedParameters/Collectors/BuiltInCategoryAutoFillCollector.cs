namespace AddSharedParameters.Collectors;

public class BuiltInCategoryAutoFillCollector : IRevitAutoFillCollector<AddSharedParametersArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, AddSharedParametersArgs args)
    {
        var result = new Dictionary<string, string>();
        var document = uiApplication.ActiveUIDocument?.Document;

        if (document is null)
            return result;

        foreach (Category category in document.Settings.Categories)
        {
            if (!category.AllowsBoundParameters)
                continue;

#if R2024_OR_GREATER
            var builtInCategory = (BuiltInCategory)category.Id.Value;
#else
            var builtInCategory = (BuiltInCategory)category.Id.IntegerValue;
#endif

            if (!Enum.IsDefined(typeof(BuiltInCategory), builtInCategory))
                continue;

            var key = builtInCategory.ToString();

            if (!result.ContainsKey(key))
                result.Add(key, category.Name);
        }

        return result;
    }
}
