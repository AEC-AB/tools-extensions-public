using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;

namespace StreamBIMUploader;

internal static class StreamBimExtensionResultFactory
{
    internal static IExtensionResult Create(StreamBimUploadResult result)
    {
        if (result.TotalCount == 0)
        {
            return Result.Text.Failed("No files found.");
        }

        var message = ComposeMessage(result);
        if (result.SuccessfulCount > 0 && result.FailedFiles.Count == 0)
        {
            return Result.Text.Succeeded(message);
        }

        if (result.SuccessfulCount > 0)
        {
            return Result.Text.PartiallySucceeded(message);
        }

        return Result.Text.Failed(message);
    }

    private static string ComposeMessage(StreamBimUploadResult result)
    {
        var message = $"Uploaded {result.UploadedFiles.Count} files";
        if (result.UploadedFiles.Count > 0)
        {
            message += $"\n\n{string.Join("\n", result.UploadedFiles)}";
        }

        if (result.SkippedFiles.Count > 0)
        {
            message += $"\n\nSkipped {result.SkippedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.SkippedFiles)}";
        }

        if (result.FailedFiles.Count > 0)
        {
            message += $"\n\nFailed to upload {result.FailedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.FailedFiles.Select(x => x.FileName + ": " + x.ErrorMessage))}";
        }

        return message;
    }
}
