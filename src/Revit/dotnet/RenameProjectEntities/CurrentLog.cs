namespace RenameProjectEntities;

/// <summary>
/// Provides a static reference to the active logger so renamers can log
/// without changing the IEntityRenamer interface.
/// </summary>
public static class CurrentLog
{
    public static RenamerLogger? Logger { get; set; }

    public static void Step(string message) => Logger?.Step(message);
    public static void Info(string message) => Logger?.Info(message);
    public static void Warn(string message) => Logger?.Warn(message);
    public static void Error(string message) => Logger?.Error(message);
}
