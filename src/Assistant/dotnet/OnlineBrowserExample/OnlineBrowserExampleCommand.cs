using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;

namespace OnlineBrowserExample;

public sealed class OnlineBrowserExampleCommand : IAssistantExtension<OnlineBrowserExampleArgs>
{
    public Task<IExtensionResult> RunAsync(
        IAssistantExtensionContext context, OnlineBrowserExampleArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string selected = args.File.SelectedItem?.Value ?? "No file selected";
        string folder = args.Folder.SelectedItem?.Value ?? "No folder selected";
        string items = string.Join(", ", args.Items.Items.Select(item => item.Value));
        string downloads = string.Join("\n", args.Downloads.Targets.Select(target => $"{target.Source.Value} to {target.Destination}"));
        string uploads = string.Join("\n", args.Uploads.Targets.Select(target => $"{target.Source.Value} to {target.Destination}"));
        IExtensionResult result = Result.Text.Succeeded(
            $"Project: {args.Project}\nFile: {selected}\nFolder: {folder}\nRemote items: {items}\n" +
            $"Download rows:\n{downloads}\nUpload rows:\n{uploads}\nExample only: no files were read, written, uploaded, or downloaded.");
        return Task.FromResult(result);
    }
}
