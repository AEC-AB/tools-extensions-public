using CW.Assistant.Extensions.Tekla.Helpers;

namespace TeklaWriteOut;

public class TeklaWriteOutCommand : ITeklaExtension<TeklaWriteOutArgs>
{
    public IExtensionResult Run(ITeklaExtensionContext context, TeklaWriteOutArgs args, CancellationToken cancellationToken)
    {
        var macroRunner = new TeklaMacroBuilderHelper();
        macroRunner.MacroBuilder.Callback("acmdRunPluginMethod", "SharingToolsFeature;Tool.SharingAutomation;w", "main_frame");
        macroRunner.MacroBuilder.Run();
        macroRunner.ClearMacroDirectory();

        // Return a result with the message
        return Result.Empty.Succeeded();
    }
}