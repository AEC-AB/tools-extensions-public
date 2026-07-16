namespace RenameProjectEntities.Renamers;

/// <summary>
/// Contract for a renamer that targets a specific category of Revit entities.
/// </summary>
public interface IEntityRenamer
{
    string Category { get; }

    /// <summary>
    /// Performs renaming in the document under the given arguments.
    /// When <paramref name="previewOnly"/> is true, no modifications are committed.
    /// </summary>
    IEnumerable<RenameResult> Rename(
        Document document,
        RenameProjectEntitiesArgs args,
        bool previewOnly,
        CancellationToken cancellationToken);
}
