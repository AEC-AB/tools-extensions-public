namespace TeklaSaveModel;

public class TeklaSaveModelCommand : ITeklaExtension<TeklaSaveModelArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, TeklaSaveModelArgs args, CancellationToken cancellationToken)
    {
        var wasSaved = new ModelHandler().Save();

        var message = wasSaved
            ? "Model was saved"
            : "No changes to save";

        // Return a result with the message
        return Result.Text.Succeeded(message);
    }
}