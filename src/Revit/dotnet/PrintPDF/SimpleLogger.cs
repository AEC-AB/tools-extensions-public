using System.Text;

namespace PrintPDF;

public sealed class SimpleLogger
{
    private readonly StringBuilder _sb = new();
    private readonly object _sync = new();

    public void Info(string message)
    {
        if (message == null) return;
        lock (_sync)
        {
            _sb.AppendLine($"[I] {message}");
        }
    }

    public void Error(string message)
    {
        if (message == null) return;
        lock (_sync)
        {
            _sb.AppendLine($"[E] {message}");
        }
    }

    public override string ToString()
    {
        lock (_sync)
        {
            return _sb.ToString();
        }
    }
}
