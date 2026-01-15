using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PrintPDF;

public sealed class Telemetry
{
    private int _sheetsRequested;
    private int _sheetsExported;
    private int _errors;
    private int _orderedViewListUsed;
    private int _viewSetsProcessed;
    private bool _combinedExported;
    private readonly List<string> _exportedSheetNames = new();
    private readonly object _sync = new();

    public void IncrementSheetsRequested(int n)
    {
        Interlocked.Add(ref _sheetsRequested, n);
    }

    public void IncrementSheetsExported()
    {
        Interlocked.Increment(ref _sheetsExported);
    }

    public void IncrementErrors()
    {
        Interlocked.Increment(ref _errors);
    }

    public void MarkOrderedViewListUsed()
    {
        Interlocked.Increment(ref _orderedViewListUsed);
    }

    public void MarkViewSetProcessed()
    {
        Interlocked.Increment(ref _viewSetsProcessed);
    }

    public void MarkCombinedExport()
    {
        _combinedExported = true;
    }

    public void AddExportedSheetName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_sync)
            _exportedSheetNames.Add(name);
    }

    public string ToSummaryString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("PrintPDF Telemetry Summary");
        sb.AppendLine($"Sheets requested: {_sheetsRequested}");
        sb.AppendLine($"Sheets exported: {_sheetsExported}");
        sb.AppendLine($"Errors: {_errors}");
        sb.AppendLine($"View sets processed: {_viewSetsProcessed}");
        sb.AppendLine($"Ordered view lists used (count): {_orderedViewListUsed}");
        sb.AppendLine($"Combined export used: {_combinedExported}");
        if (_exportedSheetNames.Count > 0)
        {
            sb.AppendLine("Exported sheet names:");
            lock (_sync)
            {
                foreach (var n in _exportedSheetNames)
                    sb.AppendLine(" - " + n);
            }
        }
        return sb.ToString();
    }
}
