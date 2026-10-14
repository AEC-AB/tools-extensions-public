//-----------------------------------------------------------------------------
// RenameProjectEntitiesCommand.cs
//
// This file contains the main implementation of the RenameProjectEntities,
// which executes within the Autodesk Revit environment to automate tasks.
// 
// The command class implements the IRevitExtension interface, which defines
// the contract for all Revit extensions in the Assistant platform.
//
// DEVELOPER GUIDE:
// 1. Implement your core extension logic in the Run method
// 2. Always use transactions for any model modifications
// 3. Check for null or invalid inputs before operations
// 4. Return appropriate success/failure results with informative messages
//-----------------------------------------------------------------------------

namespace RenameProjectEntities;

using RenameProjectEntities.Renamers;

/// <summary>
/// Main command class for the RenameProjectEntities extension.
/// Executes a find-and-replace across project entities based on user configuration.
/// </summary>
public class RenameProjectEntitiesCommand : IRevitExtension<RenameProjectEntitiesArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, RenameProjectEntitiesArgs args, CancellationToken cancellationToken)
    {
        var logger = RenamerLogger.Create();
        CurrentLog.Logger = logger;

        logger.Step("Command started");
        logger.Info($"Find='{args.Find}', Replace='{args.Replace}', Scope={args.SearchScope}, Mode={args.MatchMode}, MatchCase={args.MatchCase}, UseRegex={args.UseRegex}, PreviewOnly={args.PreviewMode}");

        var document = context.UIApplication.ActiveUIDocument?.Document;
        if (document is null)
        {
            logger.Error("No active document");
            return Result.Text.Failed($"Revit has no active model open.\n\n{logger.GetContent()}");
        }

        logger.Info($"Document: {document.PathName ?? "Untitled"}");

        if (string.IsNullOrWhiteSpace(args.Find))
        {
            logger.Error("Find text is empty");
            return Result.Text.Failed($"Find text cannot be empty.\n\n{logger.GetContent()}");
        }

        if (!args.UseRegex && string.Equals(args.Find, args.Replace, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            logger.Warn("Find and Replace are identical");
            return Result.Text.PartiallySucceeded($"Find and Replace are identical. No changes were made.\n\n{logger.GetContent()}");
        }

        if (args.UseRegex)
        {
            logger.Step("Validating regex");
            try
            {
                _ = new System.Text.RegularExpressions.Regex(args.Find);
                logger.Info("Regex valid");
            }
            catch (Exception ex)
            {
                logger.Error($"Invalid regex: {ex.Message}");
                return Result.Text.Failed($"Invalid regular expression: {ex.Message}\n\n{logger.GetContent()}");
            }
        }

        var renamers = ResolveRenamers(args);
        logger.Info($"Resolved {renamers.Count} renamer(s): {string.Join(", ", renamers.Select(r => r.Category))}");

        if (renamers.Count == 0)
        {
            return Result.Text.Succeeded($"No categories selected for renaming.\n\n{logger.GetContent()}");
        }

        var report = new RenameReport();

        if (args.PreviewMode)
        {
            logger.Step("Starting PREVIEW mode (no transaction)");
            RunRenamers(document, args, renamers, previewOnly: true, report, cancellationToken, logger);
            logger.Step("Preview complete");
            report.LogContent = logger.GetContent();
            return report.ToExtensionResult();
        }

        logger.Step("Starting transaction");
        using var transaction = new Transaction(document, "Rename Project Entities");
        transaction.Start();
        logger.Info("Transaction started");

        try
        {
            RunRenamers(document, args, renamers, previewOnly: false, report, cancellationToken, logger);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.Step("Cancellation requested — rolling back");
                transaction.RollBack();
                logger.Info("Rolled back");
                report.LogContent = logger.GetContent();
                return Result.Text.Failed($"Operation cancelled by user.\n\n{logger.GetContent()}");
            }

            logger.Step("Committing transaction");
            transaction.Commit();
            logger.Info("Transaction committed");
            report.LogContent = logger.GetContent();
            return report.ToExtensionResult();
        }
        catch (Exception ex)
        {
            logger.Error($"Unhandled exception: {ex}");
            if (transaction.HasStarted() && !transaction.HasEnded())
                transaction.RollBack();
            report.LogContent = logger.GetContent();
            return Result.Text.Failed($"Error during rename operation: {ex.Message}\n\n{logger.GetContent()}");
        }
    }

    private static void RunRenamers(Document document, RenameProjectEntitiesArgs args,
        List<IEntityRenamer> renamers, bool previewOnly, RenameReport report,
        CancellationToken cancellationToken, RenamerLogger logger)
    {
        foreach (var renamer in renamers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.Step("Cancellation detected between renamers");
                break;
            }

            logger.Step($"Running renamer: {renamer.Category}");
            int beforeCount = report.Results.Count;

            try
            {
                foreach (var result in renamer.Rename(document, args, previewOnly, cancellationToken))
                {
                    report.Add(result);
                    if (report.Results.Count % 500 == 0)
                        logger.Info($"  ...processed {report.Results.Count - beforeCount} results in {renamer.Category}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"CRASH in renamer '{renamer.Category}': {ex}");
            }

            int afterCount = report.Results.Count;
            logger.Info($"Renamer '{renamer.Category}' produced {afterCount - beforeCount} result(s)");
        }
    }

    private static List<IEntityRenamer> ResolveRenamers(RenameProjectEntitiesArgs args)
    {
        var renamers = new List<IEntityRenamer>();

        switch (args.SearchScope)
        {
            case SearchScope.Everything:
                renamers.Add(new ElementNameRenamer());
                renamers.Add(new ParameterValueRenamer());
                renamers.Add(new ParameterNameRenamer());
                renamers.Add(new ViewRenamer());
                renamers.Add(new FamilyRenamer());
                renamers.Add(new MaterialRenamer());
                renamers.Add(new ProjectInfoRenamer());
                renamers.Add(new LevelGridRenamer());
                renamers.Add(new WorksetRenamer());
                renamers.Add(new PhaseRenamer());
                break;

            case SearchScope.NamesOnly:
                renamers.Add(new ElementNameRenamer());
                break;

            case SearchScope.ParameterValuesOnly:
                renamers.Add(new ParameterValueRenamer());
                break;

            case SearchScope.ParameterNamesOnly:
                renamers.Add(new ParameterNameRenamer());
                break;

            case SearchScope.Custom:
                if (args.IncludeElementNames)
                    renamers.Add(new ElementNameRenamer());
                if (args.IncludeParameterValues)
                    renamers.Add(new ParameterValueRenamer());
                if (args.IncludeParameterNames)
                    renamers.Add(new ParameterNameRenamer());
                if (args.IncludeViews)
                    renamers.Add(new ViewRenamer());
                if (args.IncludeFamilies)
                    renamers.Add(new FamilyRenamer());
                if (args.IncludeMaterials)
                    renamers.Add(new MaterialRenamer());
                if (args.IncludeProjectInfo)
                    renamers.Add(new ProjectInfoRenamer());
                if (args.IncludeLevelsGrids)
                    renamers.Add(new LevelGridRenamer());
                if (args.IncludeWorksets)
                    renamers.Add(new WorksetRenamer());
                if (args.IncludePhases)
                    renamers.Add(new PhaseRenamer());
                break;
        }

        return renamers;
    }
}