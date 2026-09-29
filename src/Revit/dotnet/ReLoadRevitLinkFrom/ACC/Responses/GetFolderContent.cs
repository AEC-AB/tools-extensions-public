using System;
using System.Collections.Generic;

namespace ReLoadRevitLinkFrom.ACC.Responses;

public class GetFolderContentResponse
{
    public record Root(JsonApi JsonApi, RootLinks Links, List<Data> Data, List<Included>? Included);

    public record JsonApi(string Version);

    public record RootLinks(LinkSelf Self);

    public record LinkSelf(string Href);

    public record Data(string Type, string Id, Attributes Attributes);

    public record Included(string Type, string Id, Attributes Attributes);

    public record Attributes(string Name, string? DisplayName, Extension Extension);

    public record Extension(string Type, string Version, Schema Schema, ExtensionDataContainer Data);

    public record Schema(string Href);

    public record ExtensionDataContainer(List<string>? VisibleTypes, List<string>? Actions, List<string>? AllowedTypes, List<string>? NamingStandardIds,
        Guid? ModelGuid, string? OriginalItemUrn);
}
