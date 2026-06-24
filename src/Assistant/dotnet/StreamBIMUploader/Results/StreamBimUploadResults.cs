namespace StreamBIMUploader;

internal sealed record StreamBimSingleFileUploadResult(string? UploadedFile, string? SkippedFile, FailedFile? Failure)
{
    internal static StreamBimSingleFileUploadResult Uploaded(string fileName)
    {
        return new StreamBimSingleFileUploadResult(fileName, null, null);
    }

    internal static StreamBimSingleFileUploadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimSingleFileUploadResult(null, null, new FailedFile(fileName, errorMessage));
    }

    internal static StreamBimSingleFileUploadResult Skipped(string fileName)
    {
        return new StreamBimSingleFileUploadResult(null, fileName, null);
    }
}

internal sealed record StreamBimItemUploadResult(
    IReadOnlyList<string> UploadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles)
{
    internal static StreamBimItemUploadResult Empty { get; } = new StreamBimItemUploadResult([], [], []);

    internal static StreamBimItemUploadResult Failed(string fileName, string errorMessage)
    {
        return new StreamBimItemUploadResult([], [], [new FailedFile(fileName, errorMessage)]);
    }

    internal static StreamBimItemUploadResult FromSingle(StreamBimSingleFileUploadResult result)
    {
        var builder = new StreamBimUploadOutcomeBuilder();
        builder.Add(result);
        return builder.BuildItemResult();
    }
}

internal sealed record StreamBimUploadResult(
    IReadOnlyList<string> UploadedFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<FailedFile> FailedFiles)
{
    internal int SuccessfulCount => UploadedFiles.Count + SkippedFiles.Count;

    internal int TotalCount => SuccessfulCount + FailedFiles.Count;
}

internal sealed class StreamBimUploadOutcomeBuilder
{
    private readonly List<string> uploadedFiles = [];
    private readonly List<FailedFile> failedFiles = [];
    private readonly List<string> skippedFiles = [];

    internal void Add(StreamBimItemUploadResult result)
    {
        uploadedFiles.AddRange(result.UploadedFiles);
        skippedFiles.AddRange(result.SkippedFiles);
        failedFiles.AddRange(result.FailedFiles);
    }

    internal void Add(StreamBimSingleFileUploadResult result)
    {
        if (result.UploadedFile is not null)
        {
            uploadedFiles.Add(result.UploadedFile);
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

    internal StreamBimUploadResult BuildBatchResult()
    {
        return new StreamBimUploadResult(uploadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray());
    }

    internal StreamBimItemUploadResult BuildItemResult()
    {
        return new StreamBimItemUploadResult(uploadedFiles.ToArray(), skippedFiles.ToArray(), failedFiles.ToArray());
    }
}
