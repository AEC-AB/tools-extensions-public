using System.Text.RegularExpressions;

namespace PrintPDF;

public class PrintPDFSelector
{
    private readonly Document _document;
    private readonly PrintPDFArgs _args;
    private readonly SimpleLogger? _logger;
    private readonly Telemetry? _telemetry;

    public PrintPDFSelector(Document document, PrintPDFArgs args, SimpleLogger? logger = null, Telemetry? telemetry = null)
    {
        _document = document;
        _args = args;
        _logger = logger;
        _telemetry = telemetry;
    }

    public bool IsRegexValid(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;
        _logger?.Info($"Validating regex pattern: '{pattern}'");
        try
        {
            _ = Regex.Match("", pattern);
            _logger?.Info("Regex pattern is valid");
            return true;
        }
        catch (ArgumentException)
        {
            _logger?.Error($"Invalid regex pattern: '{pattern}'");
            _telemetry?.IncrementErrors();
            return false;
        }
    }

    // Return a list of (sheets, viewSetName) tuples — one per matched view set.
    public List<(List<ViewSheet> Sheets, string Name)> CollectByViewSetRegex(string pattern)
    {
        var results = new List<(List<ViewSheet> Sheets, string Name)>();

        if (string.IsNullOrEmpty(pattern))
            return results;

        _logger?.Info($"Collecting view sets matching regex: '{pattern}'");

        var viewSets = new FilteredElementCollector(_document).OfClass(typeof(ViewSheetSet)).OfType<ViewSheetSet>();
        var matchedNames = new List<string>();

        foreach (var viewSet in viewSets)
        {
            if (!Regex.IsMatch(viewSet.Name, pattern))
                continue;

            matchedNames.Add(viewSet.Name);
            _logger?.Info($"Matched view set: '{viewSet.Name}'");

            var sheetsForSet = new List<ViewSheet>();

            // Try to use explicit ordered list when available (R2023+)
#if R2023_OR_GREATER
            try
            {
                var pm = _document.PrintManager;
                var vss = pm.ViewSheetSetting.CurrentViewSheetSet;
                if (vss != null && !vss.IsAutomatic)
                {
                    foreach (var v in vss.OrderedViewList)
                    {
                        if (v is ViewSheet sheet)
                            sheetsForSet.Add(sheet);
                    }
                    _telemetry?.MarkOrderedViewListUsed();
                    _telemetry?.MarkViewSetProcessed();
                }
                else
                {
                    foreach (var viewSheet in viewSet.Views)
                    {
                        if (viewSheet is ViewSheet sheet)
                            sheetsForSet.Add(sheet);
                    }
                    _telemetry?.MarkViewSetProcessed();
                }
            }
            catch
            {
                foreach (var viewSheet in viewSet.Views)
                {
                    if (viewSheet is ViewSheet sheet)
                        sheetsForSet.Add(sheet);
                }
                _telemetry?.MarkViewSetProcessed();
            }
#else
            foreach (var viewSheet in viewSet.Views)
            {
                if (viewSheet is ViewSheet sheet)
                    sheetsForSet.Add(sheet);
            }
            _telemetry?.MarkViewSetProcessed();
#endif

            // Preserve the order of sheets as provided by the ViewSheetSet(s).
            results.Add((sheetsForSet, viewSet.Name));
            _logger?.Info($"Collected {sheetsForSet.Count} sheets from view set '{viewSet.Name}'");
            _telemetry?.IncrementSheetsRequested(sheetsForSet.Count);
        }

        if (matchedNames.Count > 1)
        {
            _logger?.Info($"Multiple view sets matched pattern '{pattern}': {string.Join(", ", matchedNames)}. Returning results for each matched view set.");
        }

        return results;
    }

    public List<ViewSheet> CollectAllSheets()
    {
        _logger?.Info($"Collecting all sheets");
        var allSheets = new FilteredElementCollector(_document).OfClass(typeof(ViewSheet)).WhereElementIsNotElementType().OfType<ViewSheet>().ToList();
        _logger?.Info($"Collected {allSheets.Count} sheets");
        _telemetry?.IncrementSheetsRequested(allSheets.Count);
        return allSheets;
    }

    public List<ViewSheet> CollectBySheetNameRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return new List<ViewSheet>();
        _logger?.Info($"Collecting sheets with name matching regex: '{pattern}'");
        var allSheets = new FilteredElementCollector(_document).OfClass(typeof(ViewSheet)).WhereElementIsNotElementType().OfType<ViewSheet>().ToList();
        var matches = allSheets.Where(s => Regex.IsMatch(s.Name, pattern));
        var ordered = OrderingHelper.GetOrderedSheets(matches);
        _logger?.Info($"Collected {ordered.Count} sheets matching name regex '{pattern}'");
        _telemetry?.IncrementSheetsRequested(ordered.Count);
        return ordered;
    }

    public List<ViewSheet> CollectBySheetNumberRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return new List<ViewSheet>();
        _logger?.Info($"Collecting sheets with number matching regex: '{pattern}'");
        var allSheets = new FilteredElementCollector(_document).OfClass(typeof(ViewSheet)).WhereElementIsNotElementType().OfType<ViewSheet>().ToList();
        var matches = allSheets.Where(s => Regex.IsMatch(s.SheetNumber, pattern));
        var ordered = OrderingHelper.GetOrderedSheets(matches);
        _logger?.Info($"Collected {ordered.Count} sheets matching number regex '{pattern}'");
        _telemetry?.IncrementSheetsRequested(ordered.Count);
        return ordered;
    }

    

    public (List<ViewSheet> Sheets, string Name) CollectByViewSet(string viewSetName)
    {
        var viewSet = new FilteredElementCollector(_document).OfClass(typeof(ViewSheetSet)).Where(x => x.Name == viewSetName).OfType<ViewSheetSet>().FirstOrDefault();
        var sheets = new List<ViewSheet>();
        if (viewSet != null)
        {
            _logger?.Info($"Collecting view set named: '{viewSetName}'");
            // Try to use explicit ordered list when available (R2025+)
#if R2025_OR_GREATER
            try
            {
                var pm = _document.PrintManager;
                var vss = pm.ViewSheetSetting.CurrentViewSheetSet;
                if (vss != null && !vss.IsAutomatic)
                {
                    _logger?.Info($"Using ordered view list for view set '{viewSetName}'");
                    _telemetry?.MarkOrderedViewListUsed();
                    _telemetry?.MarkViewSetProcessed();
                    foreach (var v in vss.OrderedViewList)
                    {
                        if (v is ViewSheet sheet)
                            sheets.Add(sheet);
                    }
                }
                else
                {
                    foreach (var viewSheet in viewSet.Views)
                    {
                        if (viewSheet is ViewSheet sheet)
                            sheets.Add(sheet);
                    }
                }
            }
            catch
            {
                foreach (var viewSheet in viewSet.Views)
                {
                    if (viewSheet is ViewSheet sheet)
                        sheets.Add(sheet);
                }
            }
#else
            foreach (var viewSheet in viewSet.Views)
            {
                if (viewSheet is ViewSheet sheet)
                    sheets.Add(sheet);
            }
#endif
        }
        _logger?.Info($"Collected {sheets.Count} sheets from view set '{viewSetName}'");
        _telemetry?.IncrementSheetsRequested(sheets.Count);
        // Preserve view set order as returned by the ViewSheetSet
        return (sheets, viewSetName);
    }

#if R2025_OR_GREATER

    public (List<ViewSheet> sheetsToPrint, string? name) CollectByViewCollection(string viewCollectionName)
    {
        var results = (new List<ViewSheet>(), (string?)null);
        _logger?.Info($"Collecting view collections named: '{viewCollectionName}'");

        var collection = new FilteredElementCollector(_document)
            .OfCategory(BuiltInCategory.OST_SheetCollections)
            .WhereElementIsNotElementType()
            .Where(x => x.Name == viewCollectionName)
            .FirstOrDefault();

        if (collection == null)
        {
            _logger?.Info($"No view collection found with name: '{viewCollectionName}'");
            _telemetry?.IncrementErrors();
            return (new List<ViewSheet>(), viewCollectionName);
        }

        _logger?.Info($"Matched view collection: '{collection.Name}' (Id={collection.Id})");

        // Build a filter for sheets that belong to this collection (SHEET_COLLECTION == col.Id)
        var rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.SHEET_COLLECTION), collection.Id);
        var filter = new ElementParameterFilter(rule);

        var sheetsForCollection = new FilteredElementCollector(_document)
            .OfClass(typeof(ViewSheet))
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .OfType<ViewSheet>()
            .ToList();

        // Sort alphanumerically using OrderingHelper
        var ordered = OrderingHelper.GetOrderedSheets(sheetsForCollection);
        _logger?.Info($"Collected {ordered.Count} sheets from view collection '{collection.Name}'");
        _telemetry?.IncrementSheetsRequested(ordered.Count);
        
        results = (ordered, collection.Name);
        return results;
    }

    // Return a list of (sheets, collectionName) tuples — one per matched collection.
    public List<(List<ViewSheet> Sheets, string Name)> CollectByViewCollectionRegex(string pattern)
    {
        var results = new List<(List<ViewSheet> Sheets, string Name)>();

        if (string.IsNullOrEmpty(pattern))
            return results;

        _logger?.Info($"Collecting view collections matching regex: '{pattern}'");

        // Get all view collections (sheet collections)
        var collections = new FilteredElementCollector(_document)
            .OfCategory(BuiltInCategory.OST_SheetCollections)
            .WhereElementIsNotElementType()
            .ToElements();

        var matchedNames = new List<string>();

        foreach (var col in collections)
        {
            if (!Regex.IsMatch(col.Name, pattern))
                continue;

            _logger?.Info($"Matched view collection: '{col.Name}' (Id={col.Id})");
            matchedNames.Add(col.Name);

            // Build a filter for sheets that belong to this collection (SHEET_COLLECTION == col.Id)
            var rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.SHEET_COLLECTION), col.Id);
            var filter = new ElementParameterFilter(rule);

            var sheetsForCollection = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewSheet))
                .WhereElementIsNotElementType()
                .WherePasses(filter)
                .OfType<ViewSheet>()
                .ToList();

            // Sort alphanumerically using OrderingHelper
            var ordered = OrderingHelper.GetOrderedSheets(sheetsForCollection);
            _logger?.Info($"Collected {ordered.Count} sheets from view collection '{col.Name}'");
            _telemetry?.IncrementSheetsRequested(ordered.Count);

            results.Add((ordered, col.Name));
        }

        if (matchedNames.Count > 1)
        {
            _logger?.Info($"Multiple view collections matched pattern '{pattern}': {string.Join(", ", matchedNames)}. Returning results for each matched collection.");
        }

        return results;
    }


#endif
    private List<ViewSheet> GetSheetsMatchingRule(FilterRule rule)
    {
        var filter = new ElementParameterFilter(rule);
        var result = new FilteredElementCollector(_document)
            .OfClass(typeof(ViewSheet))
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .OfType<ViewSheet>()
            .ToList();

        return OrderingHelper.GetOrderedSheets(result);
    }  
}
