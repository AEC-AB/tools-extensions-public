using PrintPDF.Enums;

namespace PrintPDF;

public class PrintPDFCommand : IRevitExtension<PrintPDFArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, PrintPDFArgs Args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        if (document == null)
            return Result.Text.Failed("No active document found");

        if (string.IsNullOrEmpty(Args.DestinationDirectory))
            return Result.Text.Failed("Please select a destination directory");

        if (cancellationToken.IsCancellationRequested)
            return Result.Text.PartiallySucceeded("Operation cancelled");

        var logger = new SimpleLogger();
        var telemetry = new Telemetry();

        try
        {
            // Ensure destination directory exists and is writable
            if (!System.IO.Directory.Exists(Args.DestinationDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(Args.DestinationDirectory!);
                }
                catch (Exception ex)
                {
                    return Result.Text.Failed($"Failed to access destination directory: {ex.Message}.");
                }
            }

            var worker = new PrintPDFWorker(document, Args, logger, telemetry, cancellationToken);
            var selector = new PrintPDFSelector(document, Args, logger, telemetry);

            switch (Args.ExportOption)
            {
                case ExportOptions.ActiveView:
                    if (document.ActiveView is not ViewSheet activeSheet)
                        return Result.Text.Failed("Active view is not a sheet");
                    {
                        var sheets = new List<ViewSheet> { activeSheet };
                        worker.PrintPDF(sheets);
                    }
                    break;
                case ExportOptions.AllSheets:
                    {
                        var sheetsToPrint = selector.CollectAllSheets();
                        if (sheetsToPrint == null || !sheetsToPrint.Any())
                            return Result.Text.PartiallySucceeded("No sheets found in the model");
                        worker.PrintPDF(sheetsToPrint);
                        break;
                    }
                case ExportOptions.CustomFilter:
                    if (Args.CustomFilter == null)
                        return Result.Text.Failed("Please define a custom filter");
                    {
                        var sheetsToPrint = Args.CustomFilter.OfType<ViewSheet>().ToList();
                        if (sheetsToPrint == null || !sheetsToPrint.Any())
                            return Result.Text.PartiallySucceeded("No sheets found matching the custom filter");
                        worker.PrintPDF(sheetsToPrint);
                        break;
                    }
                case ExportOptions.SheetSet:
                    if (Args.ViewSet == null)
                        return Result.Text.Failed("Please Select a View/Sheet set");
                    {
                        var (sheetsToPrint, name) = selector.CollectByViewSet(Args.ViewSet ?? string.Empty);
                        if (sheetsToPrint == null || !sheetsToPrint.Any())
                            return Result.Text.PartiallySucceeded($"No sheets found in view set '{name}'");
                        worker.PrintPDF(sheetsToPrint, name ?? string.Empty, OrderingTechnique.PreserveSourceOrder);
                    }
                    break;

                case ExportOptions.UseRegexInSheetSet:
                    if (!selector.IsRegexValid(Args.RegexPattern))
                        return Result.Text.PartiallySucceeded("Invalid Regex pattern");
                    {
                        var viewSetResults = selector.CollectByViewSetRegex(Args.RegexPattern!);
                        if (viewSetResults == null || !viewSetResults.Any())
                            return Result.Text.PartiallySucceeded("No sheet set found matching the regex pattern");

                        foreach (var (sheetsToPrint, name) in viewSetResults)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            if (sheetsToPrint == null || !sheetsToPrint.Any())
                                continue;
                            worker.PrintPDF(sheetsToPrint, name ?? string.Empty, OrderingTechnique.PreserveSourceOrder);
                        }
                    }
                    break;
#if R2025_OR_GREATER
                case ExportOptions.SheetCollection:
                    if (Args.ViewCollection == null)
                        return Result.Text.Failed("Please Select a Sheet collection");
                    {
                        var (sheetsToPrint, name) = selector.CollectByViewCollection(Args.ViewCollection ?? string.Empty);
                        if (sheetsToPrint == null || !sheetsToPrint.Any())
                            return Result.Text.PartiallySucceeded($"No sheets found in view collection '{name}'");
                        worker.PrintPDF(sheetsToPrint, name ?? string.Empty, OrderingTechnique.PreserveSourceOrder);
                    }
                    break;
                case ExportOptions.UseRegexInSheetCollections:
                    if (!selector.IsRegexValid(Args.RegexPattern))
                        return Result.Text.Failed("Invalid Regex pattern");
                    {
                        var collectionResults = selector.CollectByViewCollectionRegex(Args.RegexPattern!);
                        if (collectionResults == null || !collectionResults.Any())
                            return Result.Text.PartiallySucceeded("No view collection found matching the regex pattern");

                        // For each matched collection, export its sheets as a combined PDF (worker will apply prefix/suffix when configured)
                        foreach (var (sheetsToPrint, name) in collectionResults)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            if (sheetsToPrint == null || !sheetsToPrint.Any())
                                continue;
                            worker.PrintPDF(sheetsToPrint, name ?? string.Empty, OrderingTechnique.PreserveSourceOrder);
                        }
                    }
                    break;
#endif
                default:
                    break;
            }

            var summary = telemetry.ToSummaryString();
            var msg = summary + "\n" + logger.ToString();

            if (cancellationToken.IsCancellationRequested)
            {
                var cancelledMsg = "Operation cancelled.";
                if (!string.IsNullOrWhiteSpace(msg))
                    cancelledMsg += "\n" + msg;
                return Result.Text.PartiallySucceeded(cancelledMsg);
            }

            if (string.IsNullOrWhiteSpace(msg))
                return Result.Empty.Succeeded();
            return Result.Text.Succeeded(msg);
        }
        catch (OperationCanceledException)
        {
            var summary = telemetry.ToSummaryString();
            var msg = summary + "\n" + logger.ToString();
            var cancelledMsg = "Operation cancelled.";
            if (!string.IsNullOrWhiteSpace(msg))
                cancelledMsg += "\n" + msg;
            return Result.Text.PartiallySucceeded(cancelledMsg);
        }
        catch (Exception exception)
        {
            return Result.Text.Failed(exception.Message + "\n" + exception.StackTrace);
        }
    }
}