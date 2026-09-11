using System.IO;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Authentication;
using CW.Assistant.Extensions.Assistant.Collectors;
using FluentFTP;
using FluentFTP.Exceptions;

namespace StreamBIMUploader;

[SupportedOSPlatform("windows")]
internal sealed class StreamBIMFolderAutoFillCollector : IAsyncAutoFillCollector<StreamBIMUploaderArgs>
{
    private const int MaxSuggestionDepth = 2;
    private const string SelectProjectMessage = "Select a project and click Reload";
    private const string NoResultsMessage = "No folders found. Try changing the folder path and clicking Reload.";
    private const string InvalidFolderPathMessage = "Folder paths cannot contain '.' or '..' segments.";

    public async Task<Dictionary<string, string>> Get(StreamBIMUploaderArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SelectProjectMessage] = SelectProjectMessage,
            };
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(args.ApplicationName);
        if (credentials is null)
        {
            return [];
        }

        try
        {
            using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
            client.Config.DataConnectionType = FtpDataConnectionType.PASVEX;

            LookupContext lookupContext;
            try
            {
                lookupContext = CreateLookupContext(args.TargetFolder);
            }
            catch (ArgumentException)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InvalidFolderPathMessage] = InvalidFolderPathMessage,
                };
            }

            var folders = await GetFoldersAsync(
                client,
                StreamBimPathHelper.NormalizeProjectPath(args.Project),
                lookupContext.RelativeFolderPath,
                cancellationToken);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in folders)
            {
                if (!StreamBimPathHelper.GetLeafName(folder).StartsWith(lookupContext.NamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suggestion = folder + "/";
                result[suggestion] = suggestion;
            }

            return result.Count > 0
                ? result
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [NoResultsMessage] = NoResultsMessage,
                };
        }
        catch (FtpException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (SocketException)
        {
            return [];
        }
        catch (TimeoutException)
        {
            return [];
        }
        catch (AuthenticationException)
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<string>> GetFoldersAsync(
        AsyncFtpClient client,
        string projectPath,
        string relativeFolderPath,
        CancellationToken cancellationToken)
    {
        var folders = new List<string>();
        var foldersToVisit = new Queue<(string RelativePath, int Depth)>();
        foldersToVisit.Enqueue((relativeFolderPath, 0));

        while (foldersToVisit.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (currentRelativePath, depth) = foldersToVisit.Dequeue();
            var remotePath = StreamBimPathHelper.CombineFtpPath(projectPath, currentRelativePath);
            var listing = await client.GetListing(remotePath, cancellationToken);

            foreach (var item in listing.Where(item =>
                         item.Type == FtpObjectType.Directory &&
                         !string.IsNullOrWhiteSpace(item.Name) &&
                         !StreamBimPathHelper.IsIgnoredDirectoryName(item.Name)))
            {
                var descendantPath = StreamBimPathHelper.CombineRelativePath(currentRelativePath, item.Name);
                folders.Add(descendantPath);

                if (depth < MaxSuggestionDepth - 1)
                {
                    foldersToVisit.Enqueue((descendantPath, depth + 1));
                }
            }
        }

        return folders;
    }

    private static LookupContext CreateLookupContext(string? targetFolder)
    {
        var normalizedInput = targetFolder?
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/') ?? string.Empty;
        var endsWithSeparator = normalizedInput.EndsWith("/", StringComparison.Ordinal);
        var normalized = StreamBimPathHelper.NormalizeRelativePath(normalizedInput);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new LookupContext(string.Empty, string.Empty);
        }

        if (endsWithSeparator)
        {
            return new LookupContext(normalized, string.Empty);
        }

        var lastSeparatorIndex = normalized.LastIndexOf('/');
        return lastSeparatorIndex < 0
            ? new LookupContext(string.Empty, normalized)
            : new LookupContext(
                normalized[..lastSeparatorIndex].Trim('/'),
                normalized[(lastSeparatorIndex + 1)..]);
    }

    private sealed record LookupContext(string RelativeFolderPath, string NamePrefix);
}
