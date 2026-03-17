namespace SaveModel;

public class SaveModelCommand : IAutoCADExtension<SaveModelArgs>
{
    private BackgroundWorker backgroundWorker = new BackgroundWorker();
    public IExtensionResult Run(IAutoCADExtensionContext context, SaveModelArgs args, CancellationToken cancellationToken)
    {
        // Here we connect to the active AutoCAD Document
        var doc = Application.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return Result.Text.Failed("AutoCAD has no active model open");

        var filePath = args.SaveWithNewName ? args.SavePath : doc.Database.Filename;

        using var documentLock = doc.LockDocument();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            if (args.SaveWithNewName)
                return Result.Text.Failed("Save path is required when saving with a new name");
            else
                return Result.Text.Failed("This drawing has not been saved before. Please save it first or specify a save path");
        }

        if (args.SaveWithNewName)
        {
            doc.Database.SaveAs(filePath, true, DwgVersion.Current, doc.Database.SecurityParameters);
            return Result.Text.Succeeded($"Saved model to {args.SavePath}");
        }
        else
        {
            doc.SendStringToExecute("_qsave ", false, false, true);
            return Result.Text.Succeeded($"Saved successfully");
        }
    }
}