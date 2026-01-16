using NWCExport.Enums;
using System.Text.RegularExpressions;

namespace NWCExport;

public partial class NWCExportCommand : IRevitExtension<NWCExportArgs>
{
    private NWCExportArgs? _args;
    private NavisworksExportOptions? _nwcOptions;
    private int _exportedCount = 0;
    private int _failedCount = 0;
    private readonly List<string> _failureLog = new();

    private void LogFailure(string reason)
    {
        _failedCount += 1;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _failureLog.Add($"[{timestamp}] {reason}");
    }

    private string FormatFailureLog()
    {
        if (_failureLog.Count == 0)
            return string.Empty;

        return "\n\nFailures:\n" + string.Join("\n", _failureLog.Select((x, i) => $"{i + 1}. {x}"));
    }

    public IExtensionResult Run(IRevitExtensionContext context, NWCExportArgs args, CancellationToken cancellationToken)
    {
        if (context?.UIApplication?.ActiveUIDocument == null)
            return Result.Text.Failed("No active Revit document.");

        if (!OptionalFunctionalityUtils.IsNavisworksExporterAvailable())
            return Result.Text.Failed($"Navisworks Exporter missing. Please install it and try again!");

        if (string.IsNullOrWhiteSpace(args.DestinationDirectory))
            return Result.Text.Failed($"Destination directory is required.");

        var activeUiDocument = context.UIApplication.ActiveUIDocument;
        var document = activeUiDocument.Document;

        _args = args;
        _exportedCount = 0;
        _failedCount = 0;
        _failureLog.Clear();
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return Result.Text.Failed($"Operation cancelled. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

            // Ensure destination directory exists
            if (!System.IO.Directory.Exists(_args.DestinationDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_args.DestinationDirectory);
                }
                catch (Exception ex)
                {
                    LogFailure($"Failed to access destination directory '{_args.DestinationDirectory}': {ex.Message}");
                    return Result.Text.Failed($"Failed to access destination directory.{FormatFailureLog()}");
                }
            }

            _nwcOptions = CreateNwcOptions();

            switch (_args.ExportOption)
            {
                case ExportOptions.UseActiveView:
                    if (activeUiDocument.ActiveView == null)
                    {
                        LogFailure("No active view available.");
                        return Result.Text.Failed($"No active view available. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }
                    if (activeUiDocument.ActiveView is not View3D)
                    {
                        LogFailure($"Active view '{activeUiDocument.ActiveView.Name}' is not a 3D view.");
                        return Result.Text.Failed($"Active view is not a 3D view. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }
                    ExportViewToNWC(activeUiDocument.ActiveView, cancellationToken);
                    break;

                case ExportOptions.AllViews:
                    var allViews = new FilteredElementCollector(document)
                        .OfClass(typeof(View3D)).WhereElementIsNotElementType().OfType<View>();
                    foreach (var view in allViews)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return Result.Text.Failed($"Operation cancelled. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

                        ExportViewToNWC(view, cancellationToken);
                    }
                    break;

                case ExportOptions.ViewSet:
                    if (string.IsNullOrWhiteSpace(_args.ViewSet))
                    {
                        LogFailure("View set name is required.");
                        return Result.Text.Failed($"View set name is required. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }

                    var viewSet = new FilteredElementCollector(document)
                        .OfClass(typeof(ViewSheetSet))
                        .WhereElementIsNotElementType()
                        .Where(v => v.Name.Equals(_args.ViewSet, StringComparison.OrdinalIgnoreCase)).OfType<ViewSheetSet>().FirstOrDefault();

                    if (viewSet == null)
                    {
                        LogFailure($"View set '{_args.ViewSet}' not found.");
                        return Result.Text.Failed($"View set '{_args.ViewSet}' not found. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }

                    if (viewSet.Views == null || viewSet.Views.Size == 0)
                    {
                        LogFailure($"View set '{_args.ViewSet}' contains no views.");
                        return Result.Text.Failed($"View set '{_args.ViewSet}' contains no views. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }

                    ExportViewSetToNWC(viewSet, cancellationToken);
                    break;

                case ExportOptions.UseRegexInViewSet:
                    var regex = _args.RegexPattern;
                    if (string.IsNullOrWhiteSpace(regex))
                    {
                        LogFailure("Regex pattern is empty.");
                        return Result.Text.Failed($"Regex pattern is empty.{FormatFailureLog()}");
                    }

                    if (!IsRegexValid(regex))
                    {
                        LogFailure($"Invalid Regex pattern: '{regex}'.");
                        return Result.Text.Failed($"Invalid Regex pattern.{FormatFailureLog()}");
                    }

                    var allViewSets = new FilteredElementCollector(document)
                        .OfClass(typeof(ViewSheetSet))
                        .GetElementIterator();

                    while (allViewSets.MoveNext())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return Result.Text.Failed($"Operation cancelled. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

                        if (allViewSets.Current is not ViewSheetSet viewSetRegex)
                            continue;

                        if (!Regex.IsMatch(viewSetRegex.Name, regex))
                            continue;

                        foreach (View view in viewSetRegex.Views)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return Result.Text.Failed($"Operation cancelled. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

                            if (view == null || view.ViewType != ViewType.ThreeD)
                                continue;

                            ExportViewToNWC(view, cancellationToken);
                        }
                    }

                    if (_exportedCount == 0)
                    {
                        LogFailure($"No view set matches the regex pattern '{regex}'.");
                        return Result.Text.Failed($"No view set matches the regex pattern.{FormatFailureLog()}");
                    }

                    break;

                case ExportOptions.CustomFilter:
                    if (_args.ViewFilterControl == null)
                    {
                        LogFailure("Custom filter is not configured.");
                        return Result.Text.Failed($"Custom filter is not configured. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
                    }

                    var views = _args.ViewFilterControl.OfClass(typeof(View3D)).WhereElementIsNotElementType().OfType<View>();
                    foreach (var view in views)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return Result.Text.Failed($"Operation cancelled. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

                        ExportViewToNWC(view, cancellationToken);
                    }
                    break;
                default:
                    LogFailure($"Unsupported export option '{_args.ExportOption}'.");
                    return Result.Text.Failed($"Unsupported export option. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
            }

            if (_failedCount > 0)
                return Result.Text.PartiallySucceeded($"NWC export done. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");

            return Result.Text.Succeeded($"NWC export done. Exported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
        }
        catch (Exception exception)
        {
            LogFailure($"Unhandled exception: {exception.Message}");
            return Result.Text.Failed(exception.Message + "\n" + exception.StackTrace + $"\nExported: {_exportedCount}, Failed: {_failedCount}{FormatFailureLog()}");
        }
    }

    private void ExportViewSetToNWC(ViewSheetSet viewSet, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;
#if R2023_OR_GREATER
        foreach(var view in viewSet.OrderedViewList)
        {
            if (cancellationToken.IsCancellationRequested)        
                return;         
            ExportViewToNWC(view, cancellationToken);
        }
#else
        var enumerator = viewSet.Views.GetEnumerator();
        while (enumerator.MoveNext()) {
            if (cancellationToken.IsCancellationRequested)
                return;
            var view = enumerator.Current as View;
            if (view != null)
                ExportViewToNWC(view, cancellationToken);
        }
#endif

    }

    private static bool IsRegexValid(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            _ = Regex.Match(string.Empty, pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    private void ExportViewToNWC(View view, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (view is not View3D)
        {
            LogFailure($"Skipped view '{view?.Name ?? "<null>"}': not a 3D view.");
            return;
        }

        if (_args == null || _nwcOptions == null)
            throw new InvalidOperationException("Exporter is not initialized.");

        var doc = view.Document;

        _nwcOptions.ViewId = view.Id;
        var nwcName = CreateNwcName(view);
        try
        {
            doc.Export(_args.DestinationDirectory, nwcName, _nwcOptions);
            _exportedCount += 1;
        }
        catch (Exception ex)
        {
            LogFailure($"Export failed for view '{view.Name}' -> '{nwcName}.nwc': {ex.Message}");
        }
    }

    private NavisworksExportOptions CreateNwcOptions()
    {
        return new NavisworksExportOptions
        {
            ConvertElementProperties = _args.ConvertElementProperties,
            Coordinates = _args.Coordinate,
            DivideFileIntoLevels = _args.DivideFileIntoLevels,
            ExportElementIds = _args.ExportElementIds,
            ExportLinks = _args.ExportLinks,
            ExportParts = _args.ExportParts,
            ExportRoomAsAttribute = _args.ExportRoomAsAttribute,
            ExportRoomGeometry = _args.ExportRoomGeometry,
            ExportScope = NavisworksExportScope.View,
            ExportUrls = _args.ExportUrls,
            FindMissingMaterials = _args.FindMissingMaterials,
            Parameters = _args.Parameters,
#if R2020_OR_GREATER
            FacetingFactor = _args.FacetingFactor,
            ConvertLights = _args.ConvertLights,
            ConvertLinkedCADFormats = _args.ConvertLinkedCADFormats
#endif
        };
    }

    public static string FullPath(Document document)
    {
        if (document.IsLinked || !document.IsWorkshared)
            return document.PathName;
        var modelPath = document.GetWorksharingCentralModelPath();
        var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
        return string.IsNullOrEmpty(path) ? document.PathName : path;
    }

    private string CreateNwcName(View view)
    {
        if (_args == null)
            throw new InvalidOperationException("Arguments must be initialized before generating NWC names.");

        string fileName;
        var fullPath = FullPath(view.Document);
        var docName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
        var separator = _args.SeparatorInFileName;
        switch (_args.NamingOptions)
        {
            case NamingOptions.ViewNameOnly:
                fileName = view.Name;
                break;
            case NamingOptions.ModelNameOnly:
                fileName = docName;
                break;
            case NamingOptions.ModelNameAndViewName:
                fileName = $"{docName}{separator}{view.Name}";
                break;
            case NamingOptions.ViewNameAndModelName:
                fileName = $"{view.Name}{separator}{docName}";
                break;
            case NamingOptions.CustomNamingConvention:
                var customFileName = _args.CustomNamingConvention;
                if (string.IsNullOrEmpty(customFileName))
                {
                    fileName = view.Name;
                    break;
                }
                customFileName = customFileName!.Replace("{ViewName}", view.Name);
                customFileName = customFileName.Replace("{ModelName}", docName);
                if (customFileName.Contains("{"))
                    customFileName = ReplaceRevitParameterVariables(customFileName, view);

                fileName = customFileName;
                break;
            default:
                fileName = view.Name;
                break;
        }

        if (!string.IsNullOrEmpty(_args.PrefixFileName))
            fileName = _args.PrefixFileName + fileName;

        if (!string.IsNullOrEmpty(_args.SuffixFileName))
            fileName += _args.SuffixFileName;

        fileName = Regex.Replace(fileName, @"[<>:""/\\|?*]", "_");

        return fileName;
    }

    private static string ReplaceRevitParameterVariables(string customFileName, View view)
    {
        var parameters = view.Parameters;
        foreach (Parameter param in parameters)
        {
            var paramValue = param.AsValueString();
            if (string.IsNullOrEmpty(paramValue))
                continue;
            var paramName = param.Definition.Name;
            customFileName = customFileName.Replace("{" + paramName + "}", paramValue);
        }
        return customFileName;
    }

}
