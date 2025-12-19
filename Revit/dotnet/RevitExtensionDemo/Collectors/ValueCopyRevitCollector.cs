namespace RevitExtensionDemo.Collectors;

public class ValueCopyRevitCollector : IValueCopyRevitCollector<RevitExtensionDemoArgs>
{
    public ValueCopyRevitSources GetSources(IValueCopyRevitContext context, RevitExtensionDemoArgs args)
    {  
        if (args.FilterControl is not null)
        {
            return new ValueCopyRevitSources(args.FilterControl);
        }

        var filter = new FilteredElementCollector(context.Document).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsElementType();
        return new ValueCopyRevitSources(filter);
    }

    public ValueCopyRevitTargets GetTargets(IValueCopyRevitContext context, RevitExtensionDemoArgs args)
    {
        if (args.FilterControlWithSelectedCategories is not null)
        {
            return new ValueCopyRevitTargets(args.FilterControlWithSelectedCategories);
        }

        var filter = new FilteredElementCollector(context.Document).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsElementType();
        return new ValueCopyRevitTargets(filter);
    }
}
