using System.Runtime.Versioning;
using FluentFTP;

namespace StreamBim;

[SupportedOSPlatform("windows")]
internal static class StreamBimProjectRootFolderCollector
{
    internal static async Task<Dictionary<string, string>> CollectAsync(
        string applicationName,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var credentials = StreamBimCredentialProvider.TryGetUserCredentials(applicationName);
        if (credentials is null)
        {
            result["auth_message"] = "Enter username and password and click Reload to see available projects.";
            return result;
        }

        using var client = await StreamBimFtpClientFactory.CreateAndConnectClientAsync(credentials, cancellationToken);
        var listing = await client.GetListing("/", cancellationToken);
        foreach (var item in listing)
        {
            if (item.Type != FtpObjectType.Directory)
            {
                continue;
            }

            var name = item.Name;
            if (string.IsNullOrWhiteSpace(name)
                || StreamBimPathHelper.IsIgnoredDirectoryName(name))
            {
                continue;
            }

            result[name] = name;
        }

        return result;
    }
}
