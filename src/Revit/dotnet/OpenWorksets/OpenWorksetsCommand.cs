using System.Text;
using System.Text.RegularExpressions;

namespace OpenWorksets;

public class OpenWorksetsCommand : IRevitExtension<OpenWorksetsArgs>
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    public IExtensionResult Run(IRevitExtensionContext context, OpenWorksetsArgs args, CancellationToken cancellationToken)
    {
        var uiDocument = context.UIApplication.ActiveUIDocument;
        var document = uiDocument?.Document;
        if (uiDocument is null || document is null)
            return Result.Text.Failed("Revit has no active document. Open the model that contains the worksets and run the extension again.");

        if (!document.IsWorkshared)
            return Result.Text.Failed($"'{document.Title}' is not workshared, so it has no worksets to open. Run the extension on a workshared model.");

        var names = args.WorksetNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        var patternTexts = args.WorksetNamePatterns.Where(pattern => !string.IsNullOrWhiteSpace(pattern)).ToList();
        if (names.Count == 0 && patternTexts.Count == 0)
            return Result.Text.Failed("No worksets are configured. Select at least one workset or enter a workset name pattern.");

        if (!TryCreatePatterns(patternTexts, out var patterns, out var patternError))
            return Result.Text.Failed(patternError);

        WorksetSelection selection;
        try
        {
            selection = WorksetSelection.Resolve(document, names, patterns);
        }
        catch (RegexMatchTimeoutException exception)
        {
            return Result.Text.Failed($"The workset name pattern '{exception.Pattern}' took too long to evaluate. Simplify the pattern and run the extension again.");
        }

        if (selection.Closed.Count == 0)
            return NothingToOpen(selection, patternTexts.Count > 0);

        if (context.IsDryRun)
            return Result.Markdown.Succeeded(BuildDryRunReport(document, selection, args.ReloadUnloadedLinks));

        if (cancellationToken.IsCancellationRequested)
            return Result.Text.Failed("Cancelled before any worksets were opened.");

        var linksToIsolate = LinkLoader.FindLinks(document, selection.Closed, includeLinkInstances: false);

        var outcome = WorksetOpener.Open(uiDocument, selection.Closed, linksToIsolate, cancellationToken);
        switch (outcome)
        {
            case WorksetOpener.Outcome.NoThreeDimensionalViewType:
                return Result.Text.Failed($"'{document.Title}' has no 3D view type, so the temporary view used to open worksets cannot be created. Create a 3D view type in the model and run the extension again.");
            case WorksetOpener.Outcome.Cancelled:
                return Result.Text.Failed("Cancelled while opening worksets. No changes were made.");
        }

        var worksetTable = document.GetWorksetTable();
        var opened = selection.Closed.Where(workset => worksetTable.GetWorkset(workset.Id).IsOpen).ToList();
        var stillClosed = selection.Closed.Except(opened).ToList();

        var linkOutcomes = args.ReloadUnloadedLinks
            ? LinkLoader.LoadUnloaded(LinkLoader.FindLinks(document, opened, includeLinkInstances: true), cancellationToken)
            : [];

        var report = BuildReport(selection, opened, stillClosed, linkOutcomes, cancellationToken.IsCancellationRequested);

        if (opened.Count == 0)
            return Result.Markdown.Failed(report);

        if (stillClosed.Count > 0 || selection.NotFound.Count > 0 || linkOutcomes.Any(link => !link.Loaded) || cancellationToken.IsCancellationRequested)
            return Result.Markdown.PartiallySucceeded(report);

        return Result.Markdown.Succeeded(report);
    }

    private static bool TryCreatePatterns(IEnumerable<string> patternTexts, out List<Regex> patterns, out string error)
    {
        patterns = [];
        error = string.Empty;

        foreach (var text in patternTexts)
        {
            try
            {
                patterns.Add(new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, PatternTimeout));
            }
            catch (ArgumentException exception)
            {
                error = $"The workset name pattern '{text}' is not a valid regular expression: {exception.Message} Correct the pattern and run the extension again.";
                return false;
            }
        }

        return true;
    }

    private static IExtensionResult NothingToOpen(WorksetSelection selection, bool hasPatterns)
    {
        var report = new StringBuilder("No closed worksets to open.");
        if (hasPatterns)
            report.Append(" No closed workset matched the workset name patterns.");
        report.AppendLine();

        AppendSkipped(report, selection);

        return selection.NotFound.Count > 0
            ? Result.Markdown.Failed(report.ToString())
            : Result.Markdown.Succeeded(report.ToString());
    }

    private static string BuildDryRunReport(Document document, WorksetSelection selection, bool reloadUnloadedLinks)
    {
        var report = new StringBuilder();
        report.AppendLine("Dry run: no changes were made.");
        report.AppendLine();
        report.AppendLine($"## Worksets that would be opened ({selection.Closed.Count})");
        AppendList(report, selection.Closed.Select(workset => workset.Name));
        AppendSkipped(report, selection);

        var links = reloadUnloadedLinks
            ? LinkLoader.FindLinks(document, selection.Closed, includeLinkInstances: true)
            : [];
        if (links.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("## Links that would be loaded if still unloaded after opening");
            AppendList(report, links.Select(link => link.IsNestedLink ? $"{link.Name} (nested, skipped)" : link.Name));
        }

        return report.ToString();
    }

    private static string BuildReport(
        WorksetSelection selection,
        IReadOnlyCollection<Workset> opened,
        IReadOnlyCollection<Workset> stillClosed,
        IReadOnlyCollection<LinkLoader.LinkLoadOutcome> linkOutcomes,
        bool cancelled)
    {
        var report = new StringBuilder();

        report.AppendLine($"## Worksets opened ({opened.Count})");
        AppendList(report, opened.Select(workset => workset.Name));

        if (stillClosed.Count > 0)
        {
            report.AppendLine();
            report.AppendLine($"## Worksets still closed ({stillClosed.Count})");
            report.AppendLine("Revit did not open these worksets. Check that you can access them in the Worksets dialog, then open them there or run the extension again.");
            AppendList(report, stillClosed.Select(workset => workset.Name));
        }

        AppendSkipped(report, selection);

        if (linkOutcomes.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("## Links");
            report.AppendLine("| Link | Result | Details |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var link in linkOutcomes)
                report.AppendLine($"| {EscapeCell(link.Name)} | {(link.Loaded ? "Loaded" : "Not loaded")} | {EscapeCell(link.Detail)} |");

            if (linkOutcomes.Any(link => !link.Loaded))
                report.AppendLine().AppendLine("Open **Manage Links** to check the path of links that were not loaded and reload them there.");
        }

        if (cancelled)
        {
            report.AppendLine();
            report.AppendLine("Cancelled while loading links. Links not listed above were not loaded.");
        }

        return report.ToString();
    }

    private static void AppendSkipped(StringBuilder report, WorksetSelection selection)
    {
        if (selection.AlreadyOpen.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("## Already open");
            AppendList(report, selection.AlreadyOpen.Select(workset => workset.Name));
        }

        if (selection.NotFound.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("## Not found in the model");
            report.AppendLine("Check the spelling, or reselect these worksets from the list.");
            AppendList(report, selection.NotFound);
        }
    }

    private static void AppendList(StringBuilder report, IEnumerable<string> items)
    {
        foreach (var item in items)
            report.AppendLine($"- {item}");
    }

    private static string EscapeCell(string text) => text.Replace("|", "\\|");
}
