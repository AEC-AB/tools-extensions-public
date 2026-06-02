using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using Meziantou.Framework.Win32;

namespace StreamBIMDownloader;

[SupportedOSPlatform("windows")]
internal static class StreamBimCredentialProvider
{
    internal static UserCredentials? TryGetUserCredentials(string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return null;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
        {
            return null;
        }

        try
        {
            return ReadUserCredentials(applicationName);
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows5.1.2600")]
    private static UserCredentials? ReadUserCredentials(string applicationName)
    {
        var credentials = CredentialManager.ReadCredential(applicationName);
        if (credentials is null ||
            string.IsNullOrWhiteSpace(credentials.UserName) ||
            string.IsNullOrWhiteSpace(credentials.Password))
        {
            return null;
        }

        return new UserCredentials(credentials.UserName, credentials.Password);
    }
}