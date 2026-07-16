namespace RenameProjectEntities;

/// <summary>
/// Provides step-by-step diagnostic logging to an in-memory StringBuilder.
/// Log content is returned in the extension result so it survives even if Revit crashes.
/// </summary>
public sealed class RenamerLogger
{
    private readonly System.Text.StringBuilder _sb;
    private readonly object _lock = new();

    public static RenamerLogger Create()
    {
        return new RenamerLogger();
    }

    private RenamerLogger()
    {
        _sb = new System.Text.StringBuilder();
        WriteLine($"Log created: {DateTime.Now:O}");
    }

    public void Step(string message)
    {
        WriteLine($"[STEP] {DateTime.Now:HH:mm:ss.fff} | {message}");
    }

    public void Info(string message)
    {
        WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} | {message}");
    }

    public void Warn(string message)
    {
        WriteLine($"[WARN] {DateTime.Now:HH:mm:ss.fff} | {message}");
    }

    public void Error(string message)
    {
        WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} | {message}");
    }

    public string GetContent()
    {
        lock (_lock)
        {
            return _sb.ToString();
        }
    }

    private void WriteLine(string line)
    {
        lock (_lock)
        {
            _sb.AppendLine(line);
        }
    }
}
