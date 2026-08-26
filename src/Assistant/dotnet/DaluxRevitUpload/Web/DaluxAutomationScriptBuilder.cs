namespace DaluxRevitUpload;

internal static class DaluxAutomationScriptBuilder
{
    private static readonly string[] ScriptResourceSuffixes =
    [
        ".Web.Scripts.10-WaitForPopup.js",
        ".Web.Scripts.20-ResetSelections.js",
        ".Web.Scripts.30-FindTargetFile.js",
        ".Web.Scripts.40-UpdateMetadata.js",
        ".Web.Scripts.50-ActionButton.js",
        ".Web.Scripts.90-BuildSummary.js",
        ".Web.Scripts.99-Main.js",
    ];

    private static readonly Lazy<string> ScriptTemplate = new(LoadScriptTemplate);

    public static string Generate(DaluxAutomationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return ScriptTemplate.Value
            .Replace("__TARGET_JSON__", JsonSerializer.Serialize(config.TargetFilename))
            .Replace("__REVISION_INCREMENT__", config.RevisionIncrement.ToString())
            .Replace("__COLUMN_CONFIG_JSON__", JsonSerializer.Serialize(config.ColumnFields))
            .Replace("__ACTION_BUTTON_JSON__", JsonSerializer.Serialize(config.ActionButtonText));
    }

    private static string LoadScriptTemplate()
    {
        var sb = new StringBuilder();
        var assembly = typeof(DaluxAutomationScriptBuilder).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var suffix in ScriptResourceSuffixes)
        {
            var resourceName = resourceNames.SingleOrDefault(name => name.EndsWith(suffix, StringComparison.Ordinal));
            if (resourceName == null)
                throw new InvalidOperationException($"Embedded resource not found: *{suffix}");

            sb.AppendLine(ReadResource(assembly, resourceName));
        }

        return sb.ToString();
    }

    private static string ReadResource(System.Reflection.Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource could not be opened: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
