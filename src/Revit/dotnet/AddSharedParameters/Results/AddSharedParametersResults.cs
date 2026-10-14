using AddSharedParameters.Extensions;
using System.Text;

namespace AddSharedParameters.Results;

public class AddSharedParametersResults : IExtensionResult
{
    public List<AddSharedParametersResultBase> Results { get; } = [];

    public ExecutionResult Result
    {
        get
        {
            if (!Results.Any())
                return ExecutionResult.Failed;

            if (Results.All(x => x.Succeeded))
            {
                return ExecutionResult.Succeeded;
            }

            return ExecutionResult.PartiallySucceeded;
        }

        set => Result = value;
    }

    public string? AsText()
    {
        var sb = new StringBuilder();

        sb.AppendLine(GetHeading());

        if (Results.Any())
            sb.AppendLine("--- Parameters ---");

        foreach (var result in Results)
        {
            sb.AppendLine(result.GetDetails());
        }

        return sb.ToString();
    }

    private string GetHeading()
    {
        return Result switch
        {
            ExecutionResult.Succeeded => GetSucceededText() ?? "Succeeded",
            ExecutionResult.Failed when !Results.Any() => "Failed to add any shared parameters.",
            ExecutionResult.PartiallySucceeded => GetPartiallySucceededText(),
            _ => throw new ArgumentOutOfRangeException("Failed to get heading for AddSharedParameterResults, result was not recognized.")
        };
    }

    private string? GetSucceededText()
    {
        var sb = new StringBuilder();

        var succededResults = Results.GetSucceeded().ToList();

        var numberOfAdded = succededResults.GetAdded().Count();
        var numberOfUpdated = succededResults.GetUpdated().Count();

        if (numberOfAdded == 0 && numberOfUpdated == 0)
            return null; ;

        sb.Append("Succeeded to ");

        if (numberOfAdded > 0)
            sb.Append($"add {numberOfAdded} ");

        if (numberOfUpdated > 0)
            sb.Append($"update {numberOfUpdated} ");

        sb.Append("shared parameter(s).");

        return sb.ToString();
    }

    private string GetPartiallySucceededText()
    {
        var sb = new StringBuilder();

        if (GetSucceededText() is { } succeededText)
            sb.AppendLine(succeededText);
        sb.Append(GetFailedText());

        return sb.ToString();
    }

    private string GetFailedText()
    {
        var sb = new StringBuilder();

        var failedResults = Results.GetFailed().ToList();

        var numberOfAdded = failedResults.GetAdded().Count();
        var numberOfUpdated = failedResults.GetUpdated().Count();

        sb.Append("Partially succeeded to ");

        if (numberOfAdded > 0)
            sb.Append($"add {numberOfAdded} ");

        if (numberOfUpdated > 0)
            sb.Append($"update {numberOfUpdated} ");

        sb.AppendLine("shared parameter(s).");

        sb.AppendLine();
        sb.AppendLine("--- Warnings: ---");
        foreach (var result in failedResults)
        {
            sb.Append("  - ");
            sb.Append(result.ParameterName);
            sb.Append(": ");
            sb.AppendLine(result.Warning);
        }

        return sb.ToString();
    }

    internal void Add(AddSharedParametersResultBase result)
    {
        Results.Add(result);
    }
}