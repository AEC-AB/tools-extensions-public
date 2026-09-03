using System.IO;
using System.Runtime.Versioning;
using CW.Assistant.Extensions.Assistant.Collectors;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
internal class StreamBIMLocalFilesAutoFillCollector : IAsyncAutoFillCollector<StreamBIMUploaderArgs>
{
    private const string NoResultsMessage = "No results found. The folder may be empty or not exist. Try changing the folder and clicking reload.";
    private const string SelectUploadFolderMessage = "Select an upload folder and click reload";

    public Task<Dictionary<string, string>> Get(StreamBIMUploaderArgs args, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(args.UploadFolder))
        {
            result[SelectUploadFolderMessage] = SelectUploadFolderMessage;
            return Task.FromResult(result);
        }

        if (!Directory.Exists(args.UploadFolder))
        {
            result["folder_not_found"] = $"Upload folder not found: '{args.UploadFolder}'.";
            return Task.FromResult(result);
        }

        var lookupContexts = CreateLookupContexts(args.Files);
        foreach (var context in lookupContexts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchDirectory = Path.Combine(args.UploadFolder, context.RelativeFolderPath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(searchDirectory))
            {
                continue;
            }

            try
            {
                var entries = Directory.EnumerateFileSystemEntries(searchDirectory)
                    .Select(path => new
                    {
                        Name = Path.GetFileName(path),
                        FullName = Path.GetRelativePath(args.UploadFolder, path).Replace('\\', '/'),
                        IsDirectory = Directory.Exists(path)
                    })
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                    .Where(entry => !entry.IsDirectory || !StreamBimPathHelper.IsIgnoredDirectoryName(entry.Name))
                    .Where(entry => entry.Name.StartsWith(context.NamePrefix, StringComparison.OrdinalIgnoreCase));

                foreach (var entry in entries)
                {
                    var suggestion = entry.FullName;
                    if (entry.IsDirectory)
                    {
                        suggestion += "/";
                    }

                    result[suggestion] = suggestion;
                }
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
        }

        if (result.Count == 0)
        {
            result[NoResultsMessage] = NoResultsMessage;
        }

        return Task.FromResult(result);
    }

    private static IReadOnlyList<LookupContext> CreateLookupContexts(IReadOnlyList<string> configuredFiles)
    {
        if (configuredFiles.Count == 0)
        {
            return [new LookupContext(string.Empty, string.Empty)];
        }

        var contexts = new List<LookupContext>();
        foreach (var configuredFile in configuredFiles)
        {
            if (string.IsNullOrWhiteSpace(configuredFile))
            {
                contexts.Add(new LookupContext(string.Empty, string.Empty));
                continue;
            }

            var normalized = configuredFile.Replace('\\', '/');
            var lastSeparatorIndex = normalized.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
            {
                contexts.Add(new LookupContext(string.Empty, normalized));
            }
            else
            {
                var folderPath = normalized[..lastSeparatorIndex];
                var namePrefix = normalized[(lastSeparatorIndex + 1)..];
                contexts.Add(new LookupContext(folderPath, namePrefix));
            }
        }

        return contexts;
    }

    private sealed record LookupContext(string RelativeFolderPath, string NamePrefix);
}
