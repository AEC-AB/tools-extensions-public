namespace RunCommand;

public class RunCommandCommand : IAutoCADExtension<RunCommandArgs>
{
    public IExtensionResult Run(IAutoCADExtensionContext context, RunCommandArgs args, CancellationToken cancellationToken)
    {
        var doc = Application.DocumentManager?.MdiActiveDocument;
        if (doc is null)
        {
            return new RunCommandCommandResult
            {
                Result = ExecutionResult.Failed,
                CommandResults = [new CommandResult { Succeeded = false, ErrorMessage = "Running a command requires an open drawing." }]
            };
        }

        if (string.IsNullOrEmpty(args.Commands))
        {
            return new RunCommandCommandResult
            {
                Result = ExecutionResult.Failed,
                CommandResults = [new CommandResult { Succeeded = false, ErrorMessage = "Commands cannot be empty" }]
            };
        }

        try
        {
            var normalizedCommands = NormalizeCommands(args.Commands);
            var commandResults = normalizedCommands
                .Split('\n')
                .Where(command => !string.IsNullOrWhiteSpace(command))
                .Select(command => new CommandResult
                {
                    Succeeded = true,
                    CommandResults = command
                })
                .ToList();

            if (!commandResults.Any())
            {
                return new RunCommandCommandResult
                {
                    Result = ExecutionResult.Failed,
                    CommandResults = [new CommandResult { Succeeded = false, ErrorMessage = "Commands cannot be empty" }]
                };
            }

            QueueCommands(doc, normalizedCommands);

            var result = new RunCommandCommandResult
            {
                CommandResults = commandResults
            };

            result.Result = ExecutionResult.Succeeded;

            return result;
        }
        catch (System.Exception e)
        {
            return new RunCommandCommandResult
            {
                Result = ExecutionResult.Failed,
                CommandResults = [new CommandResult { Succeeded = false, ErrorMessage = e.Message }]
            };
        }
    }

    private static string NormalizeCommands(string? commands)
    {
        return commands?
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim() ?? string.Empty;
    }

    private static void QueueCommands(Autodesk.AutoCAD.ApplicationServices.Document doc, string commands)
    {
        doc.SendStringToExecute(commands + "\n", activate: true, wrapUpInactiveDoc: false, echoCommand: false);
    }
}


public class RunCommandCommandResult : IExtensionResult
{
    public ExecutionResult Result { get; set; }
    
    public List<CommandResult> CommandResults { get; set; } = [];
    
    public string? AsText()
    {   
        if (!CommandResults.Any())
        {
            return "No commands were executed.";
        }

        var resultLines = new List<string>();
        
        foreach (var result in CommandResults)
        {
            var status = result.Succeeded ? "Succeeded" : "Failed";
            var line = $"{status}\t{result.CommandResults}";
            
            if (!result.Succeeded && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                line += $"\t{result.ErrorMessage}";
            }
            
            resultLines.Add(line);
        }

        return "\n" + string.Join("\n", resultLines);
    }
}
public class CommandResult
{
    public bool Succeeded { get; set; }
    public string? CommandResults { get; set; }
    public string? ErrorMessage { get; set; }
}
