namespace RenameProjectEntities;

/// <summary>
/// Describes a single rename operation result.
/// </summary>
public sealed class RenameResult
{
    public string Category { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Collects rename results and builds a human-readable summary.
/// </summary>
public sealed class RenameReport
{
    private readonly List<RenameResult> _results = [];

    public string? LogContent { get; set; }

    public void Add(RenameResult result) => _results.Add(result);
    public void AddRange(IEnumerable<RenameResult> results) => _results.AddRange(results);
    public IReadOnlyList<RenameResult> Results => _results;
    public int TotalChanges => _results.Count(r => r.Success);
    public int TotalFailures => _results.Count(r => !r.Success);

    public IExtensionResult ToExtensionResult()
    {
        var sb = new System.Text.StringBuilder();

        // --- Primary message ---
        if (_results.Count == 0)
        {
            sb.AppendLine("No matches found. Nothing was renamed.");
        }
        else
        {
            sb.AppendLine($"Rename Report: {TotalChanges} changed, {TotalFailures} failed.");
            sb.AppendLine();

            foreach (var group in _results.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                var changed = group.Count(r => r.Success);
                var failed = group.Count(r => !r.Success);
                sb.AppendLine($"[{group.Key}] Changed: {changed}, Failed: {failed}");

                foreach (var r in group.Where(x => !x.Success))
                    sb.AppendLine($"  FAIL: {r.EntityId} | {r.FieldName} | {r.Error}");
            }
        }

        // --- Details section (log content) ---
        if (!string.IsNullOrEmpty(LogContent))
        {
            sb.AppendLine();
            sb.AppendLine("========== DETAILS ==========");
            sb.AppendLine(LogContent);
        }

        var message = sb.ToString().TrimEnd();
        return TotalFailures > 0
            ? Result.Text.PartiallySucceeded(message)
            : Result.Text.Succeeded(message);
    }
}
