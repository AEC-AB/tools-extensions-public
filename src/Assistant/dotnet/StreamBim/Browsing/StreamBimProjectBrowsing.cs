using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Contracts.Browsing;
using FluentFTP;

namespace StreamBim;

[SupportedOSPlatform("windows")]
internal static class StreamBimProjectBrowsing
{
    internal static async Task<OnlineBrowserLocation> GetLocationAsync(
        string applicationName,
        OnlineBrowserItemReference reference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(reference.Value))
        {
            return new();
        }

        var projects = await GetChildrenAsync(applicationName, new(), cancellationToken);
        var project = projects.Items.FirstOrDefault(item => item.Value == reference.Value)
            ?? throw new InvalidOperationException($"Saved StreamBIM project '{reference.Value}' was not found.");
        return new OnlineBrowserLocation { Item = project, Folders = [project] };
    }

    internal static async Task<OnlineBrowserPage> GetChildrenAsync(
        string applicationName,
        OnlineBrowserPageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(request.ContinuationToken))
        {
            throw new InvalidOperationException("StreamBIM project browsing does not support continuation tokens.");
        }
        if (!string.IsNullOrWhiteSpace(request.ParentId))
        {
            return new();
        }

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(applicationName)
            ?? throw new InvalidOperationException("StreamBIM credentials were not found.");

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing("/", cancellationToken);

        var items = listing
            .Where(item => item.Type == FtpObjectType.Directory)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Where(item => !StreamBimPathHelper.IsIgnoredDirectoryName(item.Name))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new OnlineBrowserItem
            {
                Id = item.Name,
                Name = item.Name,
                Value = item.Name,
                Kind = OnlineBrowserItemKind.Folder,
                Provider = OnlineBrowserProvider.Custom,
                Scope = applicationName,
            })
            .ToArray();
        return new OnlineBrowserPage { Items = items };
    }
}
