using System.Linq;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;

namespace StreamBIMDownloader;

internal static class StreamBimExtensionResultFactory
{
    internal static IExtensionResult Create(StreamBimDownloadResult result)
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

    private static string ComposeMessage(StreamBimDownloadResult result)
    {
        var message = $"Downloaded {result.DownloadedFiles.Count} files";
        if (result.DownloadedFiles.Count > 0)
        {
            message += $"\n\n{string.Join("\n", result.DownloadedFiles)}";
        }

        if (result.SkippedFiles.Count > 0)
        {
            message += $"\n\nSkipped {result.SkippedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.SkippedFiles)}";
        }

        if (result.Warnings.Count > 0)
        {
            message += $"\n\nWarnings";
            message += $"\n\n{string.Join("\n", result.Warnings)}";
        }

        if (result.FailedFiles.Count > 0)
        {
            message += $"\n\nFailed to download {result.FailedFiles.Count} files";
            message += $"\n\n{string.Join("\n", result.FailedFiles.Select(x => x.FileName + ": " + x.ErrorMessage))}";
        }

        return message;
    }
}
