using System;
using System.Collections.Generic;

namespace ReLoadRevitLinkFrom.ACC.Responses;

public class GetTopFoldersResponse
{
    public record Root(List<Data> Data);
    public record Attributes(string Name);
    public record Data(string Id, Attributes Attributes);
}
