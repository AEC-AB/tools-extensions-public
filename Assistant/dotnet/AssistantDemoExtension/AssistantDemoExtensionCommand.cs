
namespace AssistantDemoExtension;

public class AssistantDemoExtensionCommand : IAssistantExtension<AssistantDemoExtensionArgs>
{
    public async Task<IExtensionResult> RunAsync(IAssistantExtensionContext context, AssistantDemoExtensionArgs args, CancellationToken cancellationToken)
    {
        // Create a message with the input text
        var message = $"This is the result";

        // Await some delay to simulate async work
        await Task.Delay(300, cancellationToken);

        // Return a result with the message
        return Result.Text.Succeeded(message);
    }
}