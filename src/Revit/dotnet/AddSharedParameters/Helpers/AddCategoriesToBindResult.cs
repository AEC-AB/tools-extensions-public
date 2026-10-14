namespace AddSharedParameters;

public class CategoriesToBindResult : IDisposable
{
    private bool _disposed;

    public CategoriesToBindResult(CategorySet categorySet)
    {
        CategorySet = categorySet;
    }

    public bool HasChanges { get; set; }
    public bool HasUnValidCategory { get; set; }
    public CategorySet CategorySet { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        CategorySet?.Dispose();
        _disposed = true;
    }
}