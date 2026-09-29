using System.Net.Http;
using System.Text.Json;
using System.Net;
using CW.Assistant.Extensions;
using ReLoadRevitLinkFrom.ACC.Responses;

namespace ReLoadRevitLinkFrom.ACC;

public class AccClient(IExtensionHttpClient client)
{
    private readonly IExtensionHttpClient _client = client;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetFolderContentResponse.Root GetFolderContents(string projectId, string folderId)
    {
        var encodedFolderId = WebUtility.UrlEncode(folderId);
        var request = new HttpRequestMessage(HttpMethod.Get, $"data/v1/projects/{projectId}/folders/{encodedFolderId}/contents");

        var response = _client.Send(request);
        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"ACC folder contents request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {json}");

        var result = JsonSerializer.Deserialize<GetFolderContentResponse.Root>(json, _jsonOptions);
        return result ?? throw new Exception("Result is null");
    }

    public GetTopFoldersResponse.Root GetTopFolders(string hubId, string projectId)
    {
        var response = _client.GetAsJson<GetTopFoldersResponse.Root>($"project/v1/hubs/{hubId}/projects/{projectId}/topFolders");
        return response.GetResult();
    }
}