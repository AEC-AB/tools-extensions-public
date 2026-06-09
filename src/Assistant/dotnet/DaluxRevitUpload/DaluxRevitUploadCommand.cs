
namespace DaluxRevitUpload;

public class DaluxRevitUploadCommand : IAssistantExtension<DaluxRevitUploadArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, DaluxRevitUploadArgs args, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Validate input parameters
            if (string.IsNullOrWhiteSpace(args.RevitProcessId) || !int.TryParse(args.RevitProcessId, out var revitProcessId) || revitProcessId <= 0)
                return Result.Text.Failed("Revit Process ID is required and must be a valid positive integer. Run the Python script first to populate the 'revitprocessid' variable.");

            if (string.IsNullOrWhiteSpace(args.TargetFilename))
                return Result.Text.Failed("Target Filename is required.");

            // Step 2: Create the automation configuration from the arguments
            var config = new DaluxAutomationConfig
            {
                TargetFilename = args.TargetFilename,
                RevisionIncrement = args.RevisionIncrement,
                ActionButtonText = args.TriggerUpload ? "Upload" : string.Empty,
                RevitProcessId = revitProcessId,
                DebuggingPort = 0,
                WebSocketTimeout = 44000,
                AutoFreeCdpPort = true,
                RetryCount = 3
            };

            // Step 3: Validate and normalise column fields
            // Values that parse as a date are reformatted to "dd MMM yyyy" so handleDatePicker receives a consistent format.
            var columnFields = new Dictionary<string, string>();
            foreach (var kvp in args.ColumnFields ?? [])
            {
                if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                    return Result.Text.Failed("Column Fields contains empty keys or values. Please review your input.");

                var value = DateTime.TryParse(kvp.Value, out var parsedDate)
                    ? parsedDate.ToString("dd MMM yyyy")
                    : kvp.Value;
                columnFields[kvp.Key] = value;
            }
            config.ColumnFields = columnFields;

            // Step 4: Create and run the automation service
            using var service = new DaluxAutomationService(config);
            
            // Log that we're starting automation
            var success = await service.RunAutomationAsync(cancellationToken);

            // Step 5: Gather the audit log and return results
            var auditLog = service.GetAuditLogAsString();
            var summary = BuildSummary(config, columnFields, success, errorMessage: null);
            var message = $"{summary}\n\n## Log\n\n{auditLog}";

            if (success)
                return Result.Text.Succeeded(message);
            else
                return Result.Text.Failed(message);
        }
        catch (Exception ex)
        {
            return Result.Text.Failed(
                $"## Dalux Upload\n\n" +
                $"- **Status:** Error\n" +
                $"- **Error:** {ex.Message}\n\n" +
                $"## Log\n\n" +
                $"{ex.StackTrace}");
        }
    }

    private static string BuildSummary(
        DaluxAutomationConfig config,
        IReadOnlyDictionary<string, string> columnFields,
        bool success,
        string? errorMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Dalux Upload");
        sb.AppendLine();
        sb.AppendLine($"- **Status:** {(success ? "Succeeded" : "Failed")}");
        sb.AppendLine($"- **Target file:** {config.TargetFilename}");
        sb.AppendLine($"- **Revision increment:** {config.RevisionIncrement}");
        sb.AppendLine($"- **Upload triggered:** {(string.IsNullOrEmpty(config.ActionButtonText) ? "No (dry run)" : "Yes")}");
        sb.AppendLine($"- **Revit process id:** {config.RevitProcessId}");

        sb.AppendLine();
        if (columnFields.Count == 0)
        {
            sb.AppendLine("**Column fields:** _none provided_");
        }
        else
        {
            sb.AppendLine("**Column fields set:**");
            foreach (var kvp in columnFields)
                sb.AppendLine($"- `{kvp.Key}`: {kvp.Value}");
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {errorMessage}");
        }

        return sb.ToString().TrimEnd();
    }
}