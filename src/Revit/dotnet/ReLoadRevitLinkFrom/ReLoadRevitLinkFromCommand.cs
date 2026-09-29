using System.IO;
using System.Text;
using ReLoadRevitLinkFrom.ACC;
using ReLoadRevitLinkFrom.Utils;

#if !R2022_OR_GREATER
using Autodesk.Revit.DB.ExternalService;
#endif

namespace ReLoadRevitLinkFrom;


public class ReLoadRevitLinkFromCommand : IRevitExtension<ReLoadRevitLinkFromArgs>
{
    private string? _hubId;
    private string? _projectId;
    private CancellationToken? _cancellationToken;

#if !R2022_OR_GREATER
    private static IExternalResourceServer? _bim360Server;
#endif

    public IExtensionResult Run(IRevitExtensionContext context, ReLoadRevitLinkFromArgs args, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        var document = context.UIApplication.ActiveUIDocument?.Document;

        if (document is null)
            return Result.Text.Failed("Revit has no active model open");

        var message = string.Empty;

        var client = new AccClient(args.AutodeskClient!);

#if R2022_OR_GREATER
        _projectId = document.GetProjectId();
        _hubId = document.GetHubId();
#else
        _bim360Server ??= GetBim360Server();
        _projectId = args.ProjectId;
        _hubId = args.HubId;
#endif

        var allLinks = new FilteredElementCollector(document).OfClass(typeof(RevitLinkType)).OfType<RevitLinkType>().Where(x => LinkPassesFilter(x, args)).ToList();

        try
        {
            var linkReferences = GetLinkReferences(args, client, context);

            var linkLoadResults = new ReloadLinkResults();

            var linksToReplace = args.ReloadMode == ReloadMode.List ? allLinks.Where(x => args.Links.Contains(LinksAutoFillCollector.GetLinkName(x))).ToList() : allLinks;

            linkLoadResults.Links.AddRange(ReloadFromList(document, linksToReplace, linkReferences, args.UnloadLinks));

            if (!linkLoadResults.Any())
                if (args.ReloadMode == ReloadMode.List)
                    return Result.Text.Failed("No links matching the given names");
                else
                    return Result.Text.Succeeded("All links are reloaded");

            return linkLoadResults;
        }
        catch (OperationCanceledException cancelled)
        {
            return Result.Text.Failed(cancelled.Message);
        }
        catch (InvalidOperationException invalidOperation)
        {
            return Result.Text.Failed(invalidOperation.Message);
        }
    }

    private bool LinkPassesFilter(RevitLinkType revitLinkType, ReLoadRevitLinkFromArgs args)
    {
        if (!revitLinkType.IsFromLocalPath() && !args.ReLoadCloudLinks)
            return false;

        if (revitLinkType.IsNestedLink)
            return false;

        if (revitLinkType.Name.EndsWith(".ifc", StringComparison.CurrentCultureIgnoreCase))
            return false;

        return true;
    }

    private List<LinkReference> GetLinkReferences(ReLoadRevitLinkFromArgs args, AccClient client, IRevitExtensionContext context)
    {
        if (_projectId is null)
            throw new InvalidOperationException("Project Id is null");

        switch (args.ReloadOption)
        {
            case ReloadOptionEnum.FolderId:
                if (string.IsNullOrEmpty(args.FolderId))
                    throw new InvalidOperationException("Please enter the Folder ID");

                return GetLinkReferencesFromFolderId(args.FolderId!, client, _projectId);

            case ReloadOptionEnum.FolderPath:
                if (string.IsNullOrEmpty(args.FolderPath))
                    throw new InvalidOperationException("Please enter Reload From location");
                var folderId = GetEndFolderId(args, client);

                if (folderId is null)
                    throw new InvalidOperationException("Folder not found");

                return GetLinkReferencesFromFolderId(folderId, client, _projectId);

            case ReloadOptionEnum.Variable:

                var value = context.GetVariableValue("Files");

                if (value is null)
                    throw new InvalidOperationException("Required 'Files' variable not found");

                if (string.IsNullOrEmpty(value))
                    throw new InvalidOperationException("Variable 'Files' is empty");

                var lines = value.Split('\n').ToList();

                return lines.Select(x =>
                {
                    var parts = x.Split(';');
                    if (parts.Length != 3)
                        throw new InvalidOperationException("Invalid format in 'Files' variable");

                    var fileName = Path.GetFileName(parts[0]);

                    if (!Guid.TryParse(parts[1], out var guidValue))
                        throw new InvalidOperationException("Invalid Guid in 'Files' variable");

#if R2022_OR_GREATER
                    return new LinkReference(fileName, guidValue);
#else
                    return new LinkReference(fileName, guidValue, parts[2], _projectId!);
#endif
                }).ToList();

            default:
                throw new InvalidOperationException("Invalid Reload Option");
        }
    }

    private static List<LinkReference> GetLinkReferencesFromFolderId(string folderId, AccClient client, string projectId)
    {
        var subContents = client.GetFolderContents(projectId, folderId);
        if (subContents.Included is null)
            throw new Exception("No links found in the folder");

        var included = subContents.Included;
        return included.Where(IsValidLinkReference)
#if R2022_OR_GREATER
            .Select(x => new LinkReference(x.Attributes.DisplayName, x.Attributes.Extension.Data.ModelGuid!.Value))
#else
            .Select(x => new LinkReference(x.Attributes.DisplayName, x.Attributes.Extension.Data.ModelGuid!.Value, x.Attributes.Extension.Data.OriginalItemUrn!, projectId))
#endif
            .ToList();
    }

    private static bool IsValidLinkReference(ACC.Responses.GetFolderContentResponse.Included included)
    {
        return included.Attributes.Extension.Data.ModelGuid.HasValue
            && included.Attributes.DisplayName is not null
#if !R2022_OR_GREATER
            && included.Attributes.Extension.Data.OriginalItemUrn is not null
#endif
            ;
    }

    private List<ReloadLinkResult> ReloadFromList(Document document, List<RevitLinkType> linksToReplace, List<LinkReference> linkReferences, bool unloadLink)
    {
        var linkLoadResults = new List<ReloadLinkResult>();

        foreach (var linkToReplace in linksToReplace)
        {
            _cancellationToken?.ThrowIfCancellationRequested();

            var linkName = linkToReplace.LookupParameter("Type Name").AsString();
            var linkReference = linkReferences.Where(x => x.DisplayName == linkName).FirstOrDefault();

            if (linkReference is null)
            {
                linkLoadResults.Add(new ReloadLinkResult { LinkName = linkName, Success = false, ErrorMessage = "Link not found in the list" });
                continue;
            }

            try
            {
                var result = ReloadLink(document, linkToReplace, linkReference, unloadLink);
                linkLoadResults.Add(new ReloadLinkResult { LinkName = linkReference.DisplayName, Success = result == LinkLoadResultType.LinkLoaded });
            }
            catch (Exception e)
            {
                linkLoadResults.Add(new ReloadLinkResult { LinkName = linkReference.DisplayName, Success = false, ErrorMessage = e.Message });
            }
        }

        return linkLoadResults;
    }

    private string? GetEndFolderId(ReLoadRevitLinkFromArgs args, AccClient client)
    {
        var subFolders = args.FolderPath!.Replace("/", "\\").Split('\\').Select(x => x.ToLower()).ToList();

        if (_projectId is null)
            throw new Exception("Failed to get folder id, Project Id is null");

        if (_hubId is null)
            throw new Exception("Failed to get folder id, Hub Id is null");

        var topFolders = client.GetTopFolders(_hubId, _projectId);
        var projectFilesFolder = topFolders.Data.FirstOrDefault(x => NameEquals(x.Attributes.Name, subFolders.ElementAt(0)));

        if (projectFilesFolder is null)
            return null;

        var folderContent = client.GetFolderContents(_projectId, projectFilesFolder.Id);

        var firstSubFolder = folderContent.Data.FirstOrDefault(x => NameEquals(x.Attributes.Name, subFolders.ElementAt(1)));

        if (firstSubFolder is null)
            return string.Empty;

        var i = 2;
        var endFolder = GetFolderId(subFolders, i, client, _projectId, firstSubFolder);
        return endFolder?.Id;
    }

    private bool NameEquals(string name, string value)
    {
        if (name.ToLower() == value)
            return true;

        return false;
    }

    public ACC.Responses.GetFolderContentResponse.Data? GetFolderId(List<string> subFolders, int i, AccClient client, string projectId, ACC.Responses.GetFolderContentResponse.Data topFolder)
    {
        _cancellationToken?.ThrowIfCancellationRequested();

        if (i < subFolders.Count)
        {
            var folderContents = client.GetFolderContents(projectId, topFolder.Id);
            var endFolder = folderContents.Data.Where(x => NameEquals(x.Attributes.Name, subFolders.ElementAt(i))).FirstOrDefault();
            if (endFolder == null)
                return null;
            i++;
            return GetFolderId(subFolders, i, client, projectId, endFolder);
        }
        else
        {
            return topFolder;
        }
    }

    private LinkLoadResultType ReloadLink(Document document, RevitLinkType linkType, LinkReference linkReference, bool unloadLink)
    {
        var cloudPath = document.GetCloudModelPath();
#if R2021_OR_GREATER
        var region = cloudPath.Region;
#else
        var region = "US";
#endif
        var projectGuid = cloudPath.GetProjectGUID();
        Workset? linkWorkset = null;

        if (document.IsWorkshared)
        {
            linkWorkset = GetWorkset(document, linkType);

            if (!linkWorkset.IsOpen)
                WorksetUtils.OpenWorkset(document, linkWorkset, _cancellationToken);
        }

        var result = ReloadLink(region, projectGuid, linkReference, linkType);
        var linkFileStatus = linkType.GetLinkedFileStatus();

        if (linkWorkset is not null && unloadLink && linkFileStatus != LinkedFileStatus.LocallyUnloaded)
        {
            linkType.UnloadLocally(null);
        }
        else if (unloadLink && linkFileStatus != LinkedFileStatus.Unloaded)
        {
            linkType.Unload(null);
        }

        return result;
    }

    private static Workset GetWorkset(Document document, RevitLinkType linkType)
    {
        var worksetId = linkType.WorksetId;
        var worksetTable = document.GetWorksetTable();
        return worksetTable.GetWorkset(worksetId);
    }


#if R2022_OR_GREATER
    private static LinkLoadResultType ReloadLink(string region, Guid projectGuid, LinkReference linkReference, RevitLinkType linkType)
    {
        var linkModelPath = ModelPathUtils.ConvertCloudGUIDsToCloudPath(region, projectGuid, linkReference.ModelGuid);

        var result = linkType.LoadFrom(linkModelPath, null);
        return result.LoadResult;
    }
#else
    private static LinkLoadResultType ReloadLink(string region, Guid projectGuid, LinkReference linkReference, RevitLinkType linkType)
    {
        Dictionary<string, string> Dictionary_ExternalResource = new()
        {
            {"ForgeDmItemUrn", linkReference.ItemUrn },
            {"ForgeDmProjectId", linkReference.ProjectId },
            {"LinkedModelModelId", linkReference.ModelGuid.ToString()},
            {"LinkedModelProjectId", projectGuid.ToString()},
            {"LinkedModelRegion", region }
        };

        if (_bim360Server is null)
            throw new InvalidOperationException("BIM 360 Server was not set before reloading the link");

        var externalResourceReference = new ExternalResourceReference(_bim360Server.GetServerId(), Dictionary_ExternalResource, string.Empty, string.Empty);
        var result = linkType.LoadFrom(externalResourceReference, null);
        return result.LoadResult;
    }

    private static IExternalResourceServer GetBim360Server()
    {
        var externalResourceService = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.ExternalResourceService) ??
            throw new NullReferenceException("Failed to get ExternalResourceService");

        var server_ids = externalResourceService.GetRegisteredServerIds();

        foreach (var server_id in server_ids)
        {
            var server = externalResourceService.GetServer(server_id);

            if (server is not IExternalResourceServer resourceServer)
                continue;

            if (resourceServer.GetName() == "BIM 360")
                return resourceServer;

        }

        throw new Exception($"Failed to get BIM 360 server");
    }
#endif
}

#if R2022_OR_GREATER
internal record LinkReference(string DisplayName, Guid ModelGuid);
#else
internal record LinkReference(string DisplayName, Guid ModelGuid, string ItemUrn, string ProjectId);
#endif

public class ReloadLinkResults : IExtensionResult
{
    public List<ReloadLinkResult> Links { get; set; } = new();
    public ExecutionResult Result
    {
        get
        {
            if (Links.All(x => !x.Success))
                return ExecutionResult.Failed;

            return Links.Any(x => !x.Success) ? ExecutionResult.PartiallySucceeded : ExecutionResult.Succeeded;
        }
        set { }
    }

    public string? AsText()
    {
        return CreateSucceededMessage(Links);
    }

    private string CreateSucceededMessage(List<ReloadLinkResult> linkLoadResults)
    {
        var loaded = linkLoadResults.Where(x => x.Success).ToList();
        var failed = linkLoadResults.Where(x => !x.Success).ToList();
        var sb = new StringBuilder();
        if (loaded.Any())
        {
            sb.AppendLine($"Successfully reloaded links:");

            foreach (var load in loaded)
                sb.AppendLine(load.LinkName);
        }
        if (failed.Any())
        {
            sb.AppendLine($"Failed to reload links:");

            foreach (var fail in failed)
                sb.AppendLine($"{fail.LinkName}: {fail.ErrorMessage}");
        }

        return sb.ToString();
    }

    internal bool Any()
    {
        return Links.Any();
    }
}

public class ReloadLinkResult
{
    public string? LinkName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}