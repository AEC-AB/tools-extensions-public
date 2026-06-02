//-----------------------------------------------------------------------------
// CdpClient.cs
//
// Chrome DevTools Protocol (CDP) client for communicating with the Dalux WebView.
// Handles WebSocket connections and command/response management.
//
//-----------------------------------------------------------------------------

namespace DaluxRevitUpload;

/// <summary>
/// Represents a CDP command response
/// </summary>
public class CdpResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public JsonElement Result { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public JsonElement? Error { get; set; }
}

/// <summary>
/// Information about a debuggable tab
/// </summary>
public class DebugTab
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("webSocketDebuggerUrl")]
    public string WebSocketDebuggerUrl { get; set; } = string.Empty;
}

/// <summary>
/// Client for communicating with Chrome DevTools Protocol (CDP) via WebSocket.
/// Used to inject and execute JavaScript in the Dalux WebView.
/// </summary>
public class CdpClient : IDisposable
{
    private ClientWebSocket? _webSocket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private int _commandId = 1;
    private readonly int _webSocketTimeoutMs;

    public CdpClient(int webSocketTimeoutMs = 44000)
    {
        _webSocketTimeoutMs = webSocketTimeoutMs;
    }

    /// <summary>
    /// Gets the WebSocket URL for the Dalux tab by querying the CDP debugger endpoint
    /// </summary>
    /// <param name="port">The debugging port (typically 9222)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebSocket URL or null if not found</returns>
    public static async Task<string?> GetWebSocketUrlAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            // Use 127.0.0.1, not "localhost": Chromium's CDP debugger binds to 127.0.0.1
            // only by default. On Windows 11 "localhost" can resolve to ::1 first and
            // hang until the HttpClient timeout when the server isn't on IPv6.
            var response = await client.GetStringAsync($"http://127.0.0.1:{port}/json", cancellationToken);

            var tabs = JsonSerializer.Deserialize<List<DebugTab>>(response);
            if (tabs == null || tabs.Count == 0)
                return null;

            // First try to find a tab explicitly labeled "Dalux"
            var daluxTab = tabs.FirstOrDefault(t =>
                (!string.IsNullOrEmpty(t.Url) && t.Url.ToLower().Contains("dalux")) ||
                (!string.IsNullOrEmpty(t.Title) && t.Title.ToLower().Contains("dalux")));

            if (daluxTab != null)
                return daluxTab.WebSocketDebuggerUrl;

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting WebSocket URL: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Result of probing the CDP endpoint's /json/version URL. Distinguishes "port silent"
    /// (TCP refused / HTTP error) from "CDP is up" (got a non-empty JSON response body).
    /// When CDP is up, <see cref="VersionJson"/> contains the raw body for diagnostics.
    /// </summary>
    public record CdpVersionProbe(bool CdpUp, string? VersionJson, string? Error);

    /// <summary>
    /// Fetches the full list of debuggable tabs from /json. Returns an empty list on any failure.
    /// Used for diagnostic dumps when no Dalux-matching tab is found.
    /// </summary>
    public static async Task<List<DebugTab>> GetAllTabsAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetStringAsync($"http://127.0.0.1:{port}/json", cancellationToken);
            return JsonSerializer.Deserialize<List<DebugTab>>(response) ?? new List<DebugTab>();
        }
        catch
        {
            return new List<DebugTab>();
        }
    }

    /// <summary>
    /// Probes http://localhost:{port}/json/version. This endpoint responds as soon as the
    /// CDP debugger is up, regardless of whether any debuggable pages exist — which lets
    /// callers distinguish "port dead" from "port up, no Dalux target yet".
    /// </summary>
    public static async Task<CdpVersionProbe> ProbeVersionAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var body = await client.GetStringAsync($"http://127.0.0.1:{port}/json/version", cancellationToken);
            return new CdpVersionProbe(true, body, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CdpVersionProbe(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Connects to the Dalux WebView via WebSocket
    /// </summary>
    /// <param name="webSocketUrl">The WebSocket URL from the CDP debugger</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConnectAsync(string webSocketUrl, CancellationToken cancellationToken)
    {
        _webSocket = new ClientWebSocket();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_webSocketTimeoutMs);
            await _webSocket.ConnectAsync(new Uri(webSocketUrl), timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _webSocket?.Dispose();
            throw new TimeoutException(
                $"WebSocket handshake timed out after {_webSocketTimeoutMs}ms. URL: {webSocketUrl}. " +
                "The Dalux WebView2 CDP endpoint accepted the TCP connection but did not complete the WebSocket upgrade. " +
                "This usually means the popup is still initialising — the outer retry loop will reconnect automatically.");
        }
        catch
        {
            _webSocket?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sends a CDP command and returns the result
    /// </summary>
    /// <param name="method">The CDP method name (e.g., "Runtime.evaluate")</param>
    /// <param name="parameters">The CDP method parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The CDP response</returns>
    public async Task<CdpResponse> SendCommandAsync(string method, JsonElement? parameters = null, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException(
                $"Cannot send CDP command '{method}': WebSocket is not open " +
                $"(state={_webSocket?.State.ToString() ?? "null"}). " +
                "The Dalux popup may have closed or navigated away.");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var cmdId = Interlocked.Increment(ref _commandId);

            var commandObject = new
            {
                id = cmdId,
                method = method,
                @params = parameters ?? JsonDocument.Parse("{}").RootElement
            };

            var json = JsonSerializer.Serialize(commandObject);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            // Loop, discarding CDP event frames that don't match our command id.
            while (true)
            {
                var responseJson = await ReceiveFullMessageAsync(_webSocket!, cancellationToken);
                var response = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.TryGetProperty("id", out var idElement) && idElement.GetInt32() == cmdId)
                {
                    return new CdpResponse
                    {
                        Id = cmdId,
                        Result = response.TryGetProperty("result", out var resultElement) ? resultElement : default,
                        Error = response.TryGetProperty("error", out var errorElement) ? errorElement : null
                    };
                }
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Reads a complete WebSocket message, accumulating frames until EndOfMessage is true.
    /// Fixes the previous 1 MB single-read that silently truncated larger CDP responses
    /// (e.g. long automation result strings or verbose timeout diagnostics).
    /// </summary>
    private static async Task<string> ReceiveFullMessageAsync(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[64 * 1024];
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new InvalidOperationException(
                    $"WebSocket closed by remote: status={result.CloseStatus}, " +
                    $"description=\"{result.CloseStatusDescription}\". " +
                    "The Dalux popup was closed or navigated away while waiting for a CDP response.");
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    /// <summary>
    /// Evaluates JavaScript in the Dalux WebView
    /// </summary>
    /// <param name="script">The JavaScript code to execute</param>
    /// <param name="awaitPromise">Whether to wait for Promise resolution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation result</returns>
    public async Task<JsonElement> EvaluateAsync(string script, bool awaitPromise = true, CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            expression = script,
            returnByValue = true,
            awaitPromise = awaitPromise
        };

        var paramJson = JsonDocument.Parse(JsonSerializer.Serialize(parameters));
        var response = await SendCommandAsync("Runtime.evaluate", paramJson.RootElement, cancellationToken);

        if (response.Error.HasValue)
        {
            var errorMsg = response.Error.Value.GetRawText();
            throw new InvalidOperationException(
                $"CDP Runtime.evaluate failed (protocol-level error, not a JS exception): {errorMsg}. " +
                "Common cause: the DevTools target was destroyed — the popup closed or navigated " +
                "while the script was running. The -32000 code means 'context not found'.");
        }

        return response.Result;
    }

    /// <summary>
    /// Disposes the current WebSocket and creates a fresh one, ready for a new connection attempt.
    /// Required because ClientWebSocket cannot be reused after a failed connect.
    /// </summary>
    public void ResetConnection()
    {
        _webSocket?.Dispose();
        _webSocket = null;
    }

    /// <summary>
    /// Closes the WebSocket connection
    /// </summary>
    public async Task CloseAsync()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None);
        }
    }

    /// <summary>
    /// Disposes the WebSocket and related resources
    /// </summary>
    public void Dispose()
    {
        _webSocket?.Dispose();
        _sendLock?.Dispose();
    }
}
