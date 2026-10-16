
namespace IdsValidator;

public class IdsValidatorCommand : IAssistantExtension<IdsValidatorArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, IdsValidatorArgs args, CancellationToken cancellationToken)
    {
        if (context.IsDryRun) return Result.Markdown.Succeeded("## IDS validation\n\n| Status | Result |\n|---|---|\n| Dry run | No validation performed and no reports written. |");
        try
        {
            await using var engine = new EngineSession();
            var report = await ValidationService.ValidateAsync(args.IfcPath, args.IdsPath, engine, cancellationToken);
            Reports.Write(report, args.OutputDirectory);
            context.SetVariableValue("IdsControlStatus", report.Status);
            context.SetVariableValue("IdsReportPath", report.JsonPath);
            context.SetVariableValue("IdsBcfPath", report.BcfPath);
            var markdown = Reports.Markdown(report);
            return report.Status == "passed" ? Result.Markdown.Succeeded(markdown) : Result.Markdown.Failed(markdown);
        }
        catch (System.IO.IOException ex) { return Failure(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Failure(ex.Message); }
        catch (ArgumentException ex) { return Failure(ex.Message); }
    }

    private static IExtensionResult Failure(string message) => Result.Markdown.Failed("## IDS validation\n\n| Status | Error |\n|---|---|\n| Technical error | " + Reports.Cell(message) + " |\n\nCheck the selected paths and report-folder permissions, then run validation again.");
}
