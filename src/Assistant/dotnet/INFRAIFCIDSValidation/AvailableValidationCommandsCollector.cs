using CW.Assistant.Extensions.Assistant.Collectors;

namespace INFRAIFCIDSValidation;

[SupportedOSPlatform("windows")]
public class AvailableValidationCommandsCollector : IAsyncAutoFillCollector<INFRAIFCIDSValidationArgs>
{
    public Task<Dictionary<string, string>> Get(INFRAIFCIDSValidationArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(InfraCommand.IFC_CHECK)] = nameof(InfraCommand.IFC_CHECK),
            [nameof(InfraCommand.STEP_SYNTAX)] = nameof(InfraCommand.STEP_SYNTAX),
            [nameof(InfraCommand.IFC_SCHEMA)] = nameof(InfraCommand.IFC_SCHEMA),
            [nameof(InfraCommand.IDS_VALIDATION)] = nameof(InfraCommand.IDS_VALIDATION),
        };

        return Task.FromResult(options);
    }
}
