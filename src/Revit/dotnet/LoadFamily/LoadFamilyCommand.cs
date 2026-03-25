namespace LoadFamily;

public class LoadFamilyCommand : IRevitExtension<LoadFamilyArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, LoadFamilyArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        if (document is null)
            return Result.Text.Failed("No active document found");

        if (cancellationToken.IsCancellationRequested)
            return Result.Text.PartiallySucceeded("Operation cancelled");

        var familyPaths = ResolveFamilyPaths(args);

        if (familyPaths.Count == 0)
        {
            return args.Mode == LoadMode.LoadFromDirectory
                ? Result.Text.Failed($"No .rfa files found in '{args.FamilyDirectory}'")
                : Result.Text.Failed("No family files were specified");
        }

        var loadedNames = new List<string>();
        var skippedNames = new List<string>();
        var failedNames = new List<string>();

        using var transactionGroup = new TransactionGroup(document, "Load Families");
        transactionGroup.Start();

        foreach (var path in familyPaths)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var name = Path.GetFileNameWithoutExtension(path);

            if (!File.Exists(path))
            {
                failedNames.Add($"{name} (file not found)");
                continue;
            }

            var result = LoadSingleFamily(document, path, args);
            switch (result)
            {
                case FamilyLoadResult.Loaded:
                    loadedNames.Add(name);
                    break;
                case FamilyLoadResult.Skipped:
                    skippedNames.Add(name);
                    break;
                case FamilyLoadResult.Failed:
                    failedNames.Add(name);
                    break;
            }
        }

        if (cancellationToken.IsCancellationRequested && loadedNames.Count == 0 && skippedNames.Count == 0)
        {
            transactionGroup.RollBack();
            return Result.Text.PartiallySucceeded("Operation cancelled before any families were loaded");
        }

        if (failedNames.Count > 0 && loadedNames.Count == 0 && skippedNames.Count == 0)
        {
            transactionGroup.RollBack();
            return Result.Text.Failed(BuildSummary(loadedNames, skippedNames, failedNames));
        }

        transactionGroup.Assimilate();

        if (failedNames.Count > 0)
            return Result.Text.PartiallySucceeded(BuildSummary(loadedNames, skippedNames, failedNames));

        return Result.Text.Succeeded(BuildSummary(loadedNames, skippedNames, failedNames));
    }

    private static List<string> ResolveFamilyPaths(LoadFamilyArgs args)
    {
        if (args.Mode == LoadMode.LoadFromDirectory)
        {
            if (string.IsNullOrWhiteSpace(args.FamilyDirectory) || !Directory.Exists(args.FamilyDirectory))
                return [];

            var searchOption = args.IncludeSubDirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            return [.. Directory.GetFiles(args.FamilyDirectory, "*.rfa", searchOption)];
        }

        return args.FamilyPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    private static FamilyLoadResult LoadSingleFamily(Document document, string path, LoadFamilyArgs args)
    {
        var familyName = Path.GetFileNameWithoutExtension(path);

        // Revit's IFamilyLoadOptions.OnFamilyFound is not reliably invoked in all Revit versions.
        // Pre-check existence and usage via FilteredElementCollector so control flow never depends
        // on the callback being called.
        var existingFamily = new FilteredElementCollector(document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));

        if (existingFamily is not null)
        {
            if (!args.OverwriteIfFound)
                return FamilyLoadResult.Skipped;

            if (!args.OverwriteIfInUse && IsFamilyInUse(document, existingFamily))
                return FamilyLoadResult.Skipped;
        }

        using var transaction = new Transaction(document, $"Load Family: {familyName}");
        transaction.Start();

        // FamilyLoadOptions is still passed so Revit versions that do invoke the callback
        // can apply OverwriteParameterValues correctly.
        var familyLoadArgs = new FamilyLoadArgs(args.OverwriteIfFound, args.OverwriteIfInUse, args.OverwriteParameterValues);
        var options = new FamilyLoadOptions(familyLoadArgs);
        document.LoadFamily(path, options, out var family);

        // For overwrites, Revit may return false and skip the callback entirely, but the
        // overwrite is still applied inside the transaction. Use our pre-check as the
        // authoritative signal rather than the return value.
        if (existingFamily is not null)
        {
            ActivateFamilySymbols(document, family ?? existingFamily);
            var r = transaction.Commit();
            return r == TransactionStatus.Committed ? FamilyLoadResult.Loaded : FamilyLoadResult.Failed;
        }

        // New family — family must be non-null for a successful load.
        if (family is null)
        {
            transaction.RollBack();
            return FamilyLoadResult.Failed;
        }

        ActivateFamilySymbols(document, family);
        var transactionResult = transaction.Commit();
        return transactionResult == TransactionStatus.Committed ? FamilyLoadResult.Loaded : FamilyLoadResult.Failed;
    }

    private static bool IsFamilyInUse(Document document, Family family)
    {
        foreach (var symbolId in family.GetFamilySymbolIds())
        {
            if (new FilteredElementCollector(document)
                    .WherePasses(new FamilyInstanceFilter(document, symbolId))
                    .GetElementCount() > 0)
                return true;
        }
        return false;
    }

    private static void ActivateFamilySymbols(Document document, Family family)
    {
        foreach (var id in family.GetFamilySymbolIds())
        {
            var symbol = document.GetElement(id) as FamilySymbol;
            if (symbol?.IsActive == false)
                symbol.Activate();
        }
    }

    private static string BuildSummary(List<string> loaded, List<string> skipped, List<string> failed)
    {
        var headline = new List<string>();
        if (loaded.Count > 0) headline.Add($"{loaded.Count} loaded");
        if (skipped.Count > 0) headline.Add($"{skipped.Count} skipped");
        if (failed.Count > 0) headline.Add($"{failed.Count} failed");

        var lines = new List<string> { string.Join(" · ", headline) };

        if (loaded.Count > 0)
            lines.Add($"  Loaded:  {string.Join(", ", loaded)}");

        if (skipped.Count > 0)
            lines.Add($"  Skipped (no changes):  {string.Join(", ", skipped)}");

        if (failed.Count > 0)
            lines.Add($"  Failed:  {string.Join(", ", failed)}");

        return string.Join("\n", lines);
    }

    private enum FamilyLoadResult
    {
        Loaded,
        Skipped,
        Failed
    }
}
