# SEC-002: Dalux API key handling - broken credential lookup in upload and minor HttpClient gaps

| | |
|---|---|
| Severity | High - DaluxCloudUpload bypasses credential lookup entirely, sending "Dalux API Key" as the API key; also, HttpClient lacks timeout and certificate validation callback |
| Status | Open |
| Category | Secrets / Auth |
| Location | `src/Assistant/dotnet/DaluxCloudUpload/DaluxCloudUploadCommand.cs` line 30, `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs` lines 20-24 and 118-143, `src/Assistant/dotnet/DaluxCloudDownload/Services/DaluxApiService.cs` lines 14-24 and 207-222 |
| First seen | 20260925-214417 at commit 4658b6b |
| Last verified | 20260925-214417 |
| Introduced | commit 4658b6b (initial commit of upload/download extensions) |
| Page | `Ai.Security/Findings/SEC-002-dalux-api-key-handling.md` |

## What

The Dalux Cloud Upload extension has a broken credential lookup: it reads the API key from Windows Credential Manager but then passes the unlooked-up argument value directly to the API service. In `DaluxCloudUploadCommand.cs`, the variable `apiKey` (line 13) holds the retrieved credential, is validated for null (line 14-17), and yet `DaluxApiService` is instantiated on line 30 with `args.ApiKey` instead of `apiKey`. The `args.ApiKey` property defaults to the literal string `"Dalux API Key"` (the credential name), so the upload always sends the credential-name string as the actual API key, causing every upload to fail with an authentication error. The Download extension (`DaluxCloudDownloadCommand.cs` line 35) correctly passes the looked-up `apiKey` variable to its DaluxApiService.

Additionally, both `DaluxApiService` implementations create a raw `new HttpClient()` with no timeout, no certificate validation callback, and no explicit handling of server certificate errors. The upload service's error handler (`HandleException`) includes the full base URL in error messages, which is a minor information disclosure.

## Evidence

**Broken credential lookup in upload command:**

```csharp
// src/Assistant/dotnet/DaluxCloudUpload/DaluxCloudUploadCommand.cs lines 13-30
var apiKey = GetApiKey(args.ApiKey);
if (apiKey is null)
{
    return Result.Text.Failed("API key not found.");
}
// ... validation continues ...
var daluxService = new DaluxApiService(args.ApiKey, args.BaseUrl);
```

Line 30 passes `args.ApiKey` (default: `"Dalux API Key"` — see below) instead of the looked-up `apiKey` variable. This is a copy-paste or refactoring bug.

**Args default value (the credential name, not a real key):**

```csharp
// src/Assistant/dotnet/DaluxCloudUpload/DaluxCloudUploadArgs.cs lines 10-14
[PasswordField(
    Label = "Dalux API Key",
    ToolTip = "Add organization name in the 'Name' field and Dalux API Key in the 'Password' field.")]
[Required(ErrorMessage = "Dalux API Key is required.")]
public string ApiKey { get; set; } = "Dalux API Key";
```

**Correct credential lookup in download command (for comparison):**

```csharp
// src/Assistant/dotnet/DaluxCloudDownload/DaluxCloudDownloadCommand.cs lines 14-35
var apiKey = GetApiKey(args.ApiKey);
if (apiKey is null)
{
    return Result.Text.Failed("API key not found.");
}
// ... validation ...
var daluxService = new DaluxApiService(apiKey, args.BaseUrl);
```

**HttpClient without timeout or certificate validation (both projects):**

```csharp
// src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs lines 20-24
public DaluxApiService(string apiKey, string baseUrl = "https://node1.field.dalux.com/service/api")
{
    _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    _baseUrl = baseUrl;
    if (!_baseUrl.EndsWith("/"))
    {
        _baseUrl += "/";
    }
    _httpClient = new HttpClient();
    ConfigureHttpClient();
}

private void ConfigureHttpClient()
{
    _httpClient.BaseAddress = new Uri(_baseUrl);
    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
}
```

No `_httpClient.Timeout` set (defaults to 100 seconds), no `ServerCertificateCustomValidationCallback` configured.

**Error messages include base URL (minor information disclosure):**

```csharp
// src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs lines 120-130
private DaluxApiResponse<T> HandleException<T>(Exception ex, string operation)
{
    if (ex is HttpRequestException httpEx)
    {
        return DaluxApiResponse<T>.Failed(
            $"Network error during {operation}: {httpEx.Message}\n\n" +
            $"Base URL: {_baseUrl}\n" +
            $"Please verify:\n" +
            $"- Network connectivity\n" +
            $"- Correct API endpoint (may be region-specific)\n" +
            $"- VPN/Proxy requirements\n" +
            $"- Firewall settings"
        );
    }
    // ...
}
```

**Git history check:** `git log -p -S "Dalux API Key\|apiKey\|X-API-Key" -- src/Assistant/dotnet/DaluxCloudUpload/ src/Assistant/dotnet/DaluxCloudDownload/` returned no results, confirming no hardcoded Dalux API keys have been committed.

**No logging of API keys:** Grepped all `.cs` files in both projects for logging patterns (`diagnostics.Log`, `Console.WriteLine`, etc.) — none found. API keys are not leaked via logs.

## Impact

**High severity:** The upload command's broken credential lookup is a functional bug that makes Dalux Cloud Upload completely non-functional — every upload attempt sends the literal string "Dalux API Key" as the API key, which will be rejected by the Dalux API. An attacker who can trigger an upload would not gain unauthorized access because the wrong credential is sent. However, the presence of this bug indicates a lack of code review on credential handling.

The HttpClient gaps are secondary: no timeout means the application can hang indefinitely on unresponsive servers (DoS potential in multi-user environments); no certificate validation callback means it trusts any certificate valid in the system's CA store, which is the default and generally acceptable. The base URL in error messages is a minor information disclosure that could help an attacker identify the Dalux deployment but provides limited value alone.

## What a fix involves

1. **Fix the credential lookup bug**: Change line 30 of `DaluxCloudUploadCommand.cs` from `new DaluxApiService(args.ApiKey, args.BaseUrl)` to `new DaluxApiService(apiKey, args.BaseUrl)` to use the looked-up credential.

2. **Add HttpClient timeout**: Set `_httpClient.Timeout = TimeSpan.FromSeconds(30);` (or similar) in `ConfigureHttpClient()` in both DaluxApiService implementations.

3. **Consider certificate validation callback**: Add `_httpClient.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;` only if a custom CA is used and `.DangerousAcceptAnyServerCertificateValidator` is inappropriate; otherwise keep the .NET default which trusts the system CA store.

4. **Remove base URL from error messages**: Remove or obfuscate the `_baseUrl` from the `HandleException` error string in `DaluxCloudUpload/Services/DaluxApiService.cs` to reduce information disclosure.

## References

- SEC-001 (StreamBIM credential storage) — similar credential manager pattern, correctly implemented there.
- Meziantou.Framework.Win32.CredentialManager — third-party library used for credential access in both projects.
- [Microsoft HttpClient best practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)