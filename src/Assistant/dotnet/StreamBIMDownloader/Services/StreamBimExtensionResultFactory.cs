using System.Linq;
using System.Text;
using CW.Assistant.Extensions.Assistant;
using CW.Assistant.Extensions.Contracts;

namespace StreamBIMDownloader;

internal static class StreamBimExtensionResultFactory
{
    internal static IExtensionResult Create(StreamBimDownloadResult result)
    {
        if (result.TotalCount == 0)
        {
            return Result.Markdown.Failed("No files found.");
        }

        var message = ComposeMessage(result);
        if (result.SuccessfulCount > 0 && result.FailedFiles.Count == 0)
        {
            return Result.Markdown.Succeeded(message);
        }

        if (result.SuccessfulCount > 0)
        {
            return Result.Markdown.PartiallySucceeded(message);
        }

        return Result.Markdown.Failed(message);
    }

    private static string ComposeMessage(StreamBimDownloadResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## StreamBIM Download Result");
        builder.AppendLine();
        builder.AppendLine("| Result | Count |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Downloaded | {result.DownloadedFiles.Count} |");
        builder.AppendLine($"| Skipped | {result.SkippedFiles.Count} |");
        builder.AppendLine($"| Failed | {result.FailedFiles.Count} |");
        builder.AppendLine();

        if (result.DownloadedFiles.Count > 0)
        {
            AppendPathList(builder, "Downloaded Files", result.DownloadedFiles);
        }

        if (result.SkippedFiles.Count > 0)
        {
            AppendPathList(builder, "Skipped Files", result.SkippedFiles);
        }

        if (result.FailedFiles.Count > 0)
        {
            builder.AppendLine("## Failed Files");
            builder.AppendLine();
            builder.AppendLine("| File | Error |");
            builder.AppendLine("| --- | --- |");
            foreach (var failedFile in result.FailedFiles)
            {
                builder.AppendLine($"| {EscapeTableCell(failedFile.FileName)} | {EscapeTableCell(failedFile.ErrorMessage)} |");
            }

            builder.AppendLine();
        }

        if (result.LogEntries.Count > 0)
        {
            builder.AppendLine("## Execution Log");
            builder.AppendLine();
            builder.AppendLine("| Time (UTC) | Type | Message |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var entry in result.LogEntries.OrderBy(entry => entry.Timestamp))
            {
                builder.AppendLine($"| {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {EscapeTableCell(entry.EventType)} | {EscapeTableCell(entry.Message)} |");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendPathList(StringBuilder builder, string title, System.Collections.Generic.IReadOnlyList<string> paths)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        foreach (var path in paths)
        {
            builder.AppendLine($"- `{EscapeCodeSpan(path)}`");
        }

        builder.AppendLine();
    }

    private static string EscapeCodeSpan(string value)
    {
        return value.Replace("`", "'");
    }

    private static string EscapeTableCell(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("|", "\\|")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}
