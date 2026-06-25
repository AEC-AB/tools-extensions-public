using System;
using System.Collections.Generic;

namespace StreamBIMDownloader;

internal sealed record UserCredentials(string UserName, string Password);

internal sealed record FailedFile(string FileName, string ErrorMessage);

internal sealed record StreamBimSingleFileDownloadResult(
    string? DownloadedFile,
    string? SkippedFile,
    FailedFile? Failure,
    IReadOnlyList<string> Warnings)
{
    internal static StreamBimSingleFileDownloadResult Downloaded(string fileName, params string[] warnings)
    {
        return new StreamBimSingleFileDownloadResult(fileName, null, null, warnings);
    }

    internal static StreamBimSingleFileDownloadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimSingleFileDownloadResult(null, null, new FailedFile(fileName, errorMessage), []);
    }

    internal static StreamBimSingleFileDownloadResult Skipped(string fileName)
    {
        return new StreamBimSingleFileDownloadResult(null, fileName, null, []);
    }
}

internal sealed record StreamBimItemDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles,
    IReadOnlyList<string> Warnings)
{
    internal static StreamBimItemDownloadResult Empty { get; } = new StreamBimItemDownloadResult([], [], [], []);

    internal static StreamBimItemDownloadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimItemDownloadResult([], [], [new FailedFile(fileName, errorMessage)], []);
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
    IReadOnlyList<FailedFile> FailedFiles,
    IReadOnlyList<string> Warnings)
{
    internal int SuccessfulCount => DownloadedFiles.Count + SkippedFiles.Count;

    internal int TotalCount => SuccessfulCount + FailedFiles.Count;
}

internal sealed class StreamBimDownloadOutcomeBuilder
{
    private readonly List<string> downloadedFiles = [];
    private readonly List<FailedFile> failedFiles = [];
    private readonly List<string> skippedFiles = [];
    private readonly List<string> warnings = [];

    internal void Add(StreamBimItemDownloadResult result)
    {
        downloadedFiles.AddRange(result.DownloadedFiles);
        skippedFiles.AddRange(result.SkippedFiles);
        failedFiles.AddRange(result.FailedFiles);
        warnings.AddRange(result.Warnings);
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

        warnings.AddRange(result.Warnings);
    }

    internal StreamBimDownloadResult BuildBatchResult()
    {
        return new StreamBimDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray(), warnings.ToArray());
    }

    internal StreamBimItemDownloadResult BuildItemResult()
    {
        return new StreamBimItemDownloadResult(downloadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray(), warnings.ToArray());
    }
}
