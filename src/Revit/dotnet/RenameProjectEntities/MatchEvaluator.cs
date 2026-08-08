namespace RenameProjectEntities;

/// <summary>
/// Evaluates string matches and performs replacements based on
/// the configured MatchMode, case sensitivity, or regex.
/// </summary>
public static class MatchEvaluator
{
    public static string? Replace(string? input, RenameProjectEntitiesArgs args)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        if (args.UseRegex)
        {
            try
            {
                var regexOptions = args.MatchCase ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                var regex = new System.Text.RegularExpressions.Regex(args.Find, regexOptions);
                if (!regex.IsMatch(input))
                    return null;
                return regex.Replace(input, args.Replace);
            }
            catch
            {
                return null;
            }
        }

        bool matches = args.MatchMode switch
        {
            MatchMode.Exact => input!.Equals(args.Find, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase),
            MatchMode.Contains => input!.IndexOf(args.Find, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0,
            MatchMode.StartsWith => input!.StartsWith(args.Find, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase),
            MatchMode.EndsWith => input!.EndsWith(args.Find, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase),
            _ => input!.IndexOf(args.Find, args.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0
        };

        if (!matches)
            return null;

        if (args.MatchMode == MatchMode.Exact)
            return args.Replace;

        return ReplaceIgnoreCase(input!, args.Find, args.Replace, args.MatchCase);
    }

    private static string ReplaceIgnoreCase(string input, string find, string replace, bool matchCase)
    {
        if (matchCase)
            return input.Replace(find, replace);

        var sb = new System.Text.StringBuilder();
        int currentIndex = 0;
        int matchIndex;

        while ((matchIndex = input.IndexOf(find, currentIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            sb.Append(input, currentIndex, matchIndex - currentIndex);
            sb.Append(replace);
            currentIndex = matchIndex + find.Length;
        }

        sb.Append(input, currentIndex, input.Length - currentIndex);
        return sb.ToString();
    }
}
