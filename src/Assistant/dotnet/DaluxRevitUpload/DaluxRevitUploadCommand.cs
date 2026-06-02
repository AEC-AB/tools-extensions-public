
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
            var successMessage = await service.RunAutomationAsync(cancellationToken);

            // Step 5: Gather the audit log and return results
            var auditLog = service.GetAuditLogAsString();

            if (successMessage)
                return Result.Text.Succeeded(auditLog);
            else
                return Result.Text.Failed($"Dalux automation failed or was cancelled.\n\n{auditLog}");
        }
        catch (Exception ex)
        {
            return Result.Text.Failed($"Error during Dalux automation: {ex.Message}\n\nStack: {ex.StackTrace}");
        }
    }
}