using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CW.Assistant.Extensions.Contracts.Browsing;

namespace OnlineBrowserExample;

public sealed class ExampleFilesBrowser : IOnlineBrowser<OnlineBrowserExampleArgs>
{
    private const int PageSize = 2;
    private static readonly Entry[] Catalog =
    [
        new("models", null, "Models", "Models", OnlineBrowserItemKind.Folder),
        new("documents", null, "Documents", "Documents", OnlineBrowserItemKind.Folder),
        new("archive", null, "Archive", "Archive", OnlineBrowserItemKind.Folder, false),
        new("architecture", "models", "Architecture.ifc", "Models/Architecture.ifc", OnlineBrowserItemKind.File),
        new("structure", "models", "Structure.ifc", "Models/Structure.ifc", OnlineBrowserItemKind.File),
        new("services", "models", "Services.ifc", "Models/Services.ifc", OnlineBrowserItemKind.File),
        new("review", "documents", "Review", "Documents/Review", OnlineBrowserItemKind.Folder),
        new("checklist", "review", "Checklist.pdf", "Documents/Review/Checklist.pdf", OnlineBrowserItemKind.File),
        new("previous", "archive", "Previous model.ifc", "Archive/Previous model.ifc", OnlineBrowserItemKind.File),
    ];

    public Task<OnlineBrowserPage> GetChildrenAsync(
        OnlineBrowserExampleArgs args, OnlineBrowserPageRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ParentId is not null && !Catalog.Any(item => item.Id == request.ParentId && item.Kind == OnlineBrowserItemKind.Folder))
            throw new InvalidOperationException("This folder is not available. Reopen the browser to refresh the listing.");
        Entry[] children = Catalog.Where(item => item.ParentId == request.ParentId).ToArray();
        string tokenPrefix = $"{args.Project}/{request.ParentId ?? "root"}/";
        int offset = ReadOffset(request.ContinuationToken, tokenPrefix, children.Length);
        int next = offset + PageSize;
        return Task.FromResult(new OnlineBrowserPage
        {
            Items = children.Skip(offset).Take(PageSize).Select(item => ToItem(item, args.Project)).ToArray(),
            ContinuationToken = next < children.Length ? tokenPrefix + next.ToString(CultureInfo.InvariantCulture) : null,
        });
    }

    public Task<OnlineBrowserLocation> GetLocationAsync(
        OnlineBrowserExampleArgs args, OnlineBrowserItemReference reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reference.Provider != OnlineBrowserProvider.Custom ||
            (reference.Scope is not null && reference.Scope != args.Project.ToString()))
            return Task.FromResult(new OnlineBrowserLocation());
        Entry? item = string.IsNullOrWhiteSpace(reference.Id)
            ? Catalog.FirstOrDefault(candidate => candidate.Value == reference.Value)
            : Catalog.FirstOrDefault(candidate => candidate.Id == reference.Id);
        if (item is null)
            return Task.FromResult(new OnlineBrowserLocation());

        var folders = new List<OnlineBrowserItem>();
        Entry? ancestor = item.Kind == OnlineBrowserItemKind.Folder ? item : FindParent(item);
        while (ancestor is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            folders.Insert(0, ToItem(ancestor, args.Project));
            ancestor = FindParent(ancestor);
        }
        return Task.FromResult(new OnlineBrowserLocation { Item = ToItem(item, args.Project), Folders = folders.ToArray() });
    }

    private static Entry? FindParent(Entry item) => Catalog.FirstOrDefault(candidate => candidate.Id == item.ParentId);

    private static int ReadOffset(string? token, string prefix, int count)
    {
        if (token is null)
            return 0;
        if (!token.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(token.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int offset) ||
            offset < 0 || offset > count || offset % PageSize != 0)
            throw new InvalidOperationException("This listing token is invalid. Reopen the browser to refresh the listing.");
        return offset;
    }

    private static OnlineBrowserItem ToItem(Entry item, ExampleProject project) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Value = item.Value,
        Kind = item.Kind,
        Provider = OnlineBrowserProvider.Custom,
        Scope = project.ToString(),
        CanSelect = item.CanSelect,
        SelectionDisabledReason = item.CanSelect ? null : "Browse into this folder and choose one of its files.",
    };

    private sealed record Entry(string Id, string? ParentId, string Name, string Value, OnlineBrowserItemKind Kind, bool CanSelect = true);
}
