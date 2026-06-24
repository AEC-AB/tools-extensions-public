using System;
using System.Collections.Generic;

namespace StreamBIMDownloader;

internal sealed record StreamBimSingleFileDownloadResult(string? DownloadedFile, string? SkippedFile, FailedFile? Failure)
{
    internal static StreamBimSingleFileDownloadResult Downloaded(string fileName)
    {
        return new StreamBimSingleFileDownloadResult(fileName, null, null);
    }

    internal static StreamBimSingleFileDownloadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimSingleFileDownloadResult(null, null, new FailedFile(fileName, errorMessage));
    }

    internal static StreamBimSingleFileDownloadResult Skipped(string fileName)
    {
        return new StreamBimSingleFileDownloadResult(null, fileName, null);
    }
}

internal sealed record StreamBimItemDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles)
{
    internal static StreamBimItemDownloadResult Empty { get; } = new StreamBimItemDownloadResult([], [], []);

    internal static StreamBimItemDownloadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimItemDownloadResult([], [], [new FailedFile(fileName, errorMessage)]);
    }

    internal static StreamBimItemDownloadResult FromSingle(StreamBimSingleFileDownloadResult result)
    {
        var builder = new StreamBimDownloadOutcomeBuilder();
        builder.Add(result);
        return builder.BuildItemResult();
    }
}

internal sealed record StreamBimDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles)
{
    internal int SuccessfulCount => DownloadedFiles.Count + SkippedFiles.Count;

    internal int TotalCount => SuccessfulCount + FailedFiles.Count;
}

internal sealed class StreamBimDownloadOutcomeBuilder
{
    private readonly List<string> downloadedFiles = [];
    private readonly List<FailedFile> failedFiles = [];
    private readonly List<string> skippedFiles = [];

    internal void Add(StreamBimItemDownloadResult result)
    {
        downloadedFiles.AddRange(result.DownloadedFiles);
        skippedFiles.AddRange(result.SkippedFiles);
        failedFiles.AddRange(result.FailedFiles);
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
    }

    internal StreamBimDownloadResult BuildBatchResult()
    {
        return new StreamBimDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray());
    }

    internal StreamBimItemDownloadResult BuildItemResult()
    {
        return new StreamBimItemDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray());
    }
}