using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamBIMDownloader;

internal sealed record UserCredentials(string UserName, string Password);

internal sealed record FailedFile(string FileName, string ErrorMessage);

internal sealed record StreamBimLogEntry(DateTimeOffset Timestamp, string EventType, string Message)
{
    internal static StreamBimLogEntry Error(string message)
    {
        return Create("error", message);
    }

    internal static StreamBimLogEntry Trace(string message)
    {
        return Create("trace", message);
    }

    internal static StreamBimLogEntry Warning(string message)
    {
        return Create("warning", message);
    }

    private static StreamBimLogEntry Create(string eventType, string message)
    {
        return new StreamBimLogEntry(DateTimeOffset.UtcNow, eventType, message);
    }
}

internal sealed record StreamBimSingleFileDownloadResult(
    string? DownloadedFile,
    string? SkippedFile,
    FailedFile? Failure,
    IReadOnlyList<StreamBimLogEntry> LogEntries)
{
    internal static StreamBimSingleFileDownloadResult Downloaded(string fileName, params string[] warnings)
    {
        var logEntries = new List<StreamBimLogEntry>
        {
            StreamBimLogEntry.Trace($"Downloaded '{fileName}'."),
        };

        logEntries.AddRange(warnings.Select(StreamBimLogEntry.Warning));
        return new StreamBimSingleFileDownloadResult(fileName, null, null, logEntries.ToArray());
    }

    internal static StreamBimSingleFileDownloadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimSingleFileDownloadResult(
            null,
            null,
            new FailedFile(fileName, errorMessage),
            [StreamBimLogEntry.Error($"Failed to download '{fileName}': {errorMessage}")]);
    }

    internal static StreamBimSingleFileDownloadResult Skipped(string fileName)
    {
        return new StreamBimSingleFileDownloadResult(
            null,
            fileName,
            null,
            [StreamBimLogEntry.Trace($"Skipped '{fileName}'.")]);
    }
}

internal sealed record StreamBimItemDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles,
    IReadOnlyList<StreamBimLogEntry> LogEntries)
{
    internal static StreamBimItemDownloadResult Empty { get; } = new StreamBimItemDownloadResult([], [], [], []);

    internal static StreamBimItemDownloadResult Failed(string fileName, string errorMessage)
    {
        return Failed(fileName, errorMessage, []);
    }

    internal static StreamBimItemDownloadResult Failed(string fileName, string errorMessage, IReadOnlyList<StreamBimLogEntry> logEntries)
    {
        return new StreamBimItemDownloadResult(
            [],
            [],
            [new FailedFile(fileName, errorMessage)],
            logEntries.Concat([StreamBimLogEntry.Error($"Failed to download '{fileName}': {errorMessage}")]).ToArray());
    }

    internal static StreamBimItemDownloadResult FromSingle(StreamBimSingleFileDownloadResult result)
    {
        return FromSingle(result, []);
    }

    internal static StreamBimItemDownloadResult FromSingle(StreamBimSingleFileDownloadResult result, IReadOnlyList<StreamBimLogEntry> logEntries)
    {
        var builder = new StreamBimDownloadOutcomeBuilder();
        builder.AddLogEntries(logEntries);
        builder.Add(result);
        return builder.BuildItemResult();
    }
}

internal sealed record StreamBimDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles,
    IReadOnlyList<StreamBimLogEntry> LogEntries)
{
    internal int SuccessfulCount => DownloadedFiles.Count + SkippedFiles.Count;

    internal int TotalCount => SuccessfulCount + FailedFiles.Count;
}

internal sealed class StreamBimDownloadOutcomeBuilder
{
    private readonly List<string> downloadedFiles = [];
    private readonly List<FailedFile> failedFiles = [];
    private readonly List<string> skippedFiles = [];
    private readonly List<StreamBimLogEntry> logEntries = [];

    internal void Add(StreamBimItemDownloadResult result)
    {
        downloadedFiles.AddRange(result.DownloadedFiles);
        skippedFiles.AddRange(result.SkippedFiles);
        failedFiles.AddRange(result.FailedFiles);
        logEntries.AddRange(result.LogEntries);
    }

    internal void Add(StreamBimSingleFileDownloadResult result)
    {
        if (result.DownloadedFile is not null)
        {
            downloadedFiles.Add(result.DownloadedFile);
        }

        if (result.SkippedFile is not null)
        {
            skippedFiles.Add(result.SkippedFile);
        }

        if (result.Failure is not null)
        {
            failedFiles.Add(result.Failure);
        }

        logEntries.AddRange(result.LogEntries);
    }

    internal void AddLogEntries(IReadOnlyList<StreamBimLogEntry> entries)
    {
        logEntries.AddRange(entries);
    }

    internal StreamBimDownloadResult BuildBatchResult()
    {
        return new StreamBimDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray(), logEntries.ToArray());
    }

    internal StreamBimItemDownloadResult BuildItemResult()
    {
        return new StreamBimItemDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray(), logEntries.ToArray());
    }
}
