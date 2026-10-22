namespace ClashDetectiveRunTest;

using Autodesk.Navisworks.Api.Clash;

internal sealed class ClashTestNameCollector : INavisworksAutoFillCollector<ClashDetectiveRunTestArgs>
{
    public Dictionary<string, string> Get(ClashDetectiveRunTestArgs args)
    {
        try
        {
            var document = Application.ActiveDocument;
            if (document is null)
                return [];

            var testsData = document.GetClash()?.TestsData;
            if (testsData is null)
                return [];

            var tests = testsData.Tests;
            if (tests is null)
                return [];

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["(None - use manual clash test name)"] = string.Empty,
            };

            foreach (var name in tests
                .OfType<ClashTest>()
                .Select(test => test.DisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                options[name] = name;
            }

            return options;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Failed collecting clash test names: {ex}");
            return [];
        }
    }
}
