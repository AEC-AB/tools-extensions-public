using RevitExtensionDemo.Forma.Responses.GetAllHubs;
using RevitExtensionDemo.Forma.Responses.GetAllProjects;
using RevitExtensionDemo.Forma.Responses.GetFolderContent;
using RevitExtensionDemo.Forma.Responses.GetTopFolders;

namespace RevitExtensionDemo.Forma;

public class FormaClient
{
    private readonly IExtensionHttpClient _client;

    public FormaClient(IExtensionHttpClient client)
    {
        _client = client;
    }

    private void ThrowIfError<T>(RequestResult<T> response)
    {
        if ((int)response.StatusCode > 299)
            throw new Exception(response.ReasonPhrase);
    }

    public GetAllHubsResponse GetHubs()
    {
        var response = _client.GetAsJson<GetAllHubsResponse>("project/v1/hubs");
        ThrowIfError(response);
        return response.Result;
    }

    public GetAllProjectsResponse GetProjects(string hubId)
    {
        var response = _client.GetAsJson<GetAllProjectsResponse>($"project/v1/hubs/{hubId}/projects");
        ThrowIfError(response);
        return response.Result;
    }

    public GetFolderContentResponse GetFolderContents(string projectId, string folderId)
    {
        var response = _client.GetAsJson<GetFolderContentResponse>($"data/v1/projects/{projectId}/folders/{folderId}/contents");
        ThrowIfError(response);
        return response.Result;
    }

    public GetTopFoldersResponse GetTopFolders(string hubId, string projectId)
    {
        var response = _client.GetAsJson<GetTopFoldersResponse>($"project/v1/hubs/{hubId}/projects/{projectId}/topFolders");
        ThrowIfError(response);
        return response.Result;
    }
}