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
            var commandResults = new List<CommandResult>();
            var commands = args.Commands?.Split('\n') ?? new string[] { };
            
            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command))
                    continue;
                    
                try
                {
                    RunCommand(command);
                    commandResults.Add(new CommandResult
                    {
                        Succeeded = true,
                        CommandResults = command
                    });
                }
                catch (System.Exception e)
                {
                    commandResults.Add(new CommandResult
                    {
                        Succeeded = false,
                        CommandResults = command,
                        ErrorMessage = e.Message
                    });
                }
            }

            var result = new RunCommandCommandResult
            {
                CommandResults = commandResults
            };
            
            // Determine overall result
            if (commandResults.All(x => x.Succeeded))
                result.Result = ExecutionResult.Succeeded;
            else if (commandResults.All(x => !x.Succeeded))
                result.Result = ExecutionResult.Failed;
            else
                result.Result = ExecutionResult.PartiallySucceeded;

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

    private static void RunCommand(string command)
    {
        dynamic acadApp = Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
        var thisDrawing = acadApp.ActiveDocument;
        thisDrawing.SendCommand(command);
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
