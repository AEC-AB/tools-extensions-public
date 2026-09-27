# SEC-001: StreamBIM credential storage and FTP transfer security

| | |
|---|---|
| Severity | Low - Minor defense-in-depth gaps in FTP certificate validation and password-in-memory handling |
| Status | Open |
| Category | Secrets / Config |
| Location | `src/Assistant/dotnet/StreamBim/Services/StreamBimCredentialProvider.cs` lines 1-51, `src/Assistant/dotnet/StreamBim/Services/StreamBimFtpClientFactory.cs` lines 1-42, `src/Assistant/dotnet/StreamBim/Models/UserCredentials.cs` line 3 |
| First seen | run 20260925-214417 at commit 4658b6b |
| Last verified | run 20260925-214417 |
| Introduced | commit 183a206, 2026-09-15 (initial commit of StreamBIM uploader/downloader) |
| Page | `Ai.Security/Findings/SEC-001-streambim-credential-storage.md` |

## What

The StreamBIM uploader and downloader components (`StreamBIMUploader`, `StreamBIMDownloader`) use Windows Credential Manager to store and retrieve FTP credentials. This is the recommended approach for Windows applications. The FTP connection uses TLS 1.2 with Explicit encryption mode. The `StreamBimCredentialProvider` reads credentials from the Windows Credential Manager using `CredentialManager.ReadCredential()` and wraps them in a plain `UserCredentials` record. No credentials appear in logs or command-line arguments. However, there are two minor defense-in-depth observations: (1) the FTP `AsyncFtpClient` does not set an explicit `CertificateValidationCallback`, relying on .NET's default trust-store validation; and (2) password values are held as plaintext strings in memory in the `UserCredentials` record for the duration of the FTP operation, which is inherent to any application that must use credentials but is noted for completeness.

## Evidence

**StreamBimCredentialProvider.cs** - credentials read from Windows Credential Manager, not hardcoded:

```csharp
// src/Assistant/dotnet/StreamBim/Services/StreamBimCredentialProvider.cs line 41-51
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
```

**StreamBimFtpClientFactory.cs** - FTP client with TLS 1.2 Explicit encryption, no explicit certificate validation callback:

```csharp
// src/Assistant/dotnet/StreamBim/Services/StreamBimFtpClientFactory.cs line 14-21
var profile = new FtpProfile
{
    Host = "ftp.streambim.com",
    Encoding = Encoding.UTF8,
    Encryption = FtpEncryptionMode.Explicit,
    Protocols = System.Security.Authentication.SslProtocols.Tls12,
    Credentials = new NetworkCredential(credentials.UserName, credentials.Password),
};
```

No `CertificateValidationCallback` is set on the client. FluentFTP defaults to .NET's certificate validation, which trusts the system CA store.

**UserCredentials.cs** - plaintext password in memory:

```csharp
// src/Assistant/dotnet/StreamBim/Models/UserCredentials.cs line 3
internal sealed record UserCredentials(string UserName, string Password);
```

**Args with PasswordField attribute** (prevents UI display of credentials):

```csharp
// src/Assistant/dotnet/StreamBIMUploader/StreamBIMUploaderArgs.cs lines 5-8
[PasswordField(
    Label = "StreamBIM Credentials",
    ToolTip = "Enter your StreamBIM credentials.")]
[Required(ErrorMessage = "StreamBIM Credentials are required.")]
public UserCredentials? Credentials { get; init; }
```

**No credential leakage in diagnostics** - verified by grepping all `diagnostics.Log()` calls across `StreamBimFileTransferService.cs` and `StreamBimUploadDiagnostics.cs`: no password, credential, or username values are logged.

**Git history** - `git log -p -S "password|secret|apiKey|Bearer" -- src/Assistant/dotnet/StreamBim/` returned no results, confirming no hardcoded secrets in the commit history.

## Impact

**Low impact**: The current implementation is security-sound. The credential storage in Windows Credential Manager (DPAPI-protected) is a best practice. The FTP connection uses TLS 1.2 Explicit encryption, which provides transport security. The absence of an explicit `CertificateValidationCallback` means the connection trusts any certificate valid in the system's CA store, which is the default and acceptable behavior for most deployments. It would only be a risk if an attacker could install a rogue CA certificate on the user's machine, which is already a high-bar privilege escalation. The plaintext-in-memory password is inherent to any application that must use credentials to perform operations; there is no practical way to avoid this. The overall implementation is a good example of credential handling in a Windows application, with the noted items being observations for defense-in-depth rather than exploitable weaknesses.

## What a fix involves

No fix is required. If the organization has a strict defense-in-depth policy, the following low-effort enhancements could be considered:

1. **Certificate validation callback**: Set `client.Config.CertificateValidationCallback` to reject certificates that do not match expected characteristics (e.g., a specific issuer or SAN). This prevents MITM even if a rogue CA is installed. Note: this is only useful if the organization controls the CA or uses certificate pinning.

2. **SecureString for password**: Change `UserCredentials.Password` from `string` to `SecureString` to prevent the password from lingering in garbage-collected memory. This adds complexity as all FluentFTP APIs expect a plain `string`, so a conversion layer would be needed. Evaluate against the threat model before investing effort.

3. **Document the security model**: Add a comment to `UserCredentials.cs` and `StreamBimFtpClientFactory.cs` explaining the security choices (Credential Manager, TLS 1.2, no certificate pinning) so future developers understand the rationale.

## References

- FluentFTP documentation: `FtpEncryptionMode.Explicit`, `SslProtocols.Tls12`, `CertificateValidationCallback`
- Windows Credential Manager: DPAPI-based credential storage via `System.Security.Cryptography.CredentialManager`
- OWASP Secure Storage of Sensitive Data: [https://cheatsheetseries.owasp.org/cheatsheets/Secure_Storage_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Secure_Storage_Cheat_Sheet.html)