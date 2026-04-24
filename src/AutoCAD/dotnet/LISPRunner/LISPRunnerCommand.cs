namespace LISPRunner;

public class LISPRunnerCommand : IAutoCADExtension<LISPRunnerArgs>
{
    public IExtensionResult Run(IAutoCADExtensionContext context, LISPRunnerArgs args, CancellationToken cancellationToken)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return Result.Text.Failed("AutoCAD has no active model open.");
        }

        return args.Mode switch
        {
            LispRunnerMode.Inline => RunInlineScript(doc, args),
            _ => RunScriptFile(doc, args),
        };
    }

    private static IExtensionResult RunScriptFile(Autodesk.AutoCAD.ApplicationServices.Document doc, LISPRunnerArgs args)
    {
        var scriptPath = args.LispScriptPath?.Trim();
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return Result.Text.Failed("A Lisp script file must be selected in SelectFile mode.");
        }

        var fullScriptPath = System.IO.Path.GetFullPath(scriptPath);
        if (!System.IO.File.Exists(fullScriptPath))
        {
            return Result.Text.Failed($"The selected Lisp script was not found: {fullScriptPath}");
        }

        var lispLoadCommand = BuildLoadCommand(fullScriptPath);
        doc.SendStringToExecute(lispLoadCommand, activate: true, wrapUpInactiveDoc: false, echoCommand: false);

        return Result.Text.Succeeded($"Queued Lisp script file for execution: {fullScriptPath}");
    }

    private static IExtensionResult RunInlineScript(Autodesk.AutoCAD.ApplicationServices.Document doc, LISPRunnerArgs args)
    {
        var inlineScript = args.InlineScript?.Trim();
        if (string.IsNullOrWhiteSpace(inlineScript))
        {
            return Result.Text.Failed("Inline Lisp script content is required in Inline mode.");
        }

        doc.SendStringToExecute(inlineScript + " ", activate: true, wrapUpInactiveDoc: false, echoCommand: false);

        return Result.Text.Succeeded("Queued inline Lisp script for execution.");
    }

    private static string BuildLoadCommand(string scriptPath)
    {
        var normalizedPath = scriptPath.Replace('\\', '/').Replace("\"", "\\\"");
        return $"(load \"{normalizedPath}\") ";
    }
}