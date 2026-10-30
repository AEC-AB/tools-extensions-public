# SEC-007: Base URL exposed in API error messages

| | |
|---|---|
| Severity | Low - Information disclosure of infrastructure endpoint. Not directly exploitable since authentication is via X-API-Key header (not embedded in error messages), and the base URL is a public endpoint URL. |
| Status | Open |
| Category | Data protection |
| Location | `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs` lines 122-130 |
| First seen | run 20260926-215844 at commit unknown |
| Last verified | run 20260926-215844 |
| Introduced | Not determined |

## What

When a network error occurs (e.g., HTTP request fails), the error response returned to the caller includes the full base URL of the Dalux API service (`https://node1.field.dalux.com/service/api`). This is exposed in the `HandleException<T>` method on lines 122-130 of `DaluxApiService.cs`. The base URL is also potentially exposed in other error messages if `_baseUrl` is logged or printed elsewhere.

## Evidence

File: `src/Assistant/dotnet/DaluxCloudUpload/Services/DaluxApiService.cs` (identical code in `DaluxCloudDownload/Services/DaluxApiService.cs`)

Line 122-130:
```csharp
return DaluxApiResponse<T>.Failed(
    $"Network error during {operation}: {httpEx.Message}\n\n" +
    $"Base URL: {_baseUrl}\n" +
    $"Please verify:\n" +
    $"- Network connectivity\n" +
    $"- Correct API endpoint (may be region-specific)\n" +
    $"- VPN/Proxy requirements\n" +
    $"- Firewall settings"
);
```

Default constructor value (line 20): `_baseUrl = "https://node1.field.dalux.com/service/api"`

## Impact

An attacker who can trigger a network error (e.g., via a malformed request or by exhausting connection resources) would learn the exact Dalux API base URL. The URL itself is publicly discoverable (standard Dalux API), so the disclosure is limited. No credentials, tokens, or internal network topology are revealed. The X-API-Key header used for authentication is not embedded in the error message.

## What a fix involves

Remove or sanitize the `_baseUrl` from the returned error text. Lines 124 should be deleted or replaced with a generic message that does not reference the infrastructure endpoint. Apply the same fix to `DaluxCloudDownload/Services/DaluxApiService.cs`.

## References

Related: StreamBIM credential handling uses Windows Credential Manager (SEC-001).
