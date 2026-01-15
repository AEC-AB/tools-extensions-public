using CW.Assistant.Extensions.Tekla.Helpers;
using Tekla.Structures.Model.History;

namespace TeklaReadIn;

public class TeklaReadInCommand : ITeklaExtension<TeklaReadInArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, TeklaReadInArgs args, CancellationToken cancellationToken)
    {
        var model = new Model();
        var localChanges = ModelHistory.GetLocalChanges();
        var localIds = new HashSet<Guid>();
        while (localChanges.Modified.MoveNext())
        {
            var localObject = localChanges.Modified.Current;
            localIds.Add(localObject.Identifier.GUID);
        }
        while (localChanges.Deleted.MoveNext())
        {
            var localObject = localChanges.Deleted.Current;
            localIds.Add(localObject.Identifier.GUID);
        }
        var stamp = Guid.NewGuid().ToString();
        ModelHistory.UpdateModificationStampToLatest(stamp);

        var macroRunner = new TeklaMacroBuilderHelper();
        macroRunner.MacroBuilder.Callback("acmdRunPluginMethod", "SharingToolsFeature;Tool.SharingAutomation;r0.00:00:00", "main_frame");
        macroRunner.MacroBuilder.Run();
        macroRunner.ClearMacroDirectory();

        var modification = ModelHistory.TakeModifications(stamp);
        var readInIds = new HashSet<Guid>();
        while (modification.Modified.MoveNext())
        {
            var readInObject = modification.Modified.Current;
            readInIds.Add(readInObject.Identifier.GUID);
        }
        while (modification.Deleted.MoveNext())
        {
            var readInObject = modification.Deleted.Current;
            readInIds.Add(readInObject.Identifier.GUID);
        }
        var commonIds = localIds.Intersect(readInIds);
        int commonCount = commonIds.Count();
        if (commonCount > 0)
        {
            var message = $"There were {commonCount} model object conflicts after read in.";
            if (args.FailTask)
                return Result.Text.Failed(message);
            return Result.Text.PartiallySucceeded(message);
        }


        if (!args.Save)
            return Result.Text.Succeeded("Read in completed successfully without conflicts.");

        var modelHandler = new ModelHandler();
        modelHandler.Save();
        return Result.Text.Succeeded("Read in completed successfully and model saved.");
    }
}