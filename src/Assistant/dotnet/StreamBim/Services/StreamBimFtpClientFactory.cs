using System.Net;
using System.Text;
using FluentFTP;

namespace StreamBim;

internal static class StreamBimFtpClientFactory
{
    internal static async Task<AsyncFtpClient> CreateAndConnectClientAsync(UserCredentials credentials, CancellationToken cancellationToken)
    {
        var client = new AsyncFtpClient();
        client.Config.RetryAttempts = 5;

        var profile = new FtpProfile
        {
            Host = "ftp.streambim.com",
            Encoding = Encoding.UTF8,
            Encryption = FtpEncryptionMode.Explicit,
            Protocols = System.Security.Authentication.SslProtocols.Tls12,
            Credentials = new NetworkCredential(credentials.UserName, credentials.Password),
        };

        Exception? lastException = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await client.Connect(profile, cancellationToken);
                return client;
            }
            catch (Exception exception) when (attempt < 2 && StreamBimExceptionHelper.IsTransientFtpFailure(exception))
            {
                lastException = exception;
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to connect to StreamBIM.", lastException);
    }
}
