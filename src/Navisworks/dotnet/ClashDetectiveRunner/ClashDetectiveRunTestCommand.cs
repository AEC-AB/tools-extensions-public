namespace ClashDetectiveRunTest;

using Autodesk.Navisworks.Api.Clash;

public class ClashDetectiveRunTestCommand : INavisworksExtension<ClashDetectiveRunTestArgs>
{
    public IExtensionResult Run(INavisworksExtensionContext context, ClashDetectiveRunTestArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var document = Application.ActiveDocument;

            if (document is null)
                return Result.Text.Failed("Navisworks has no active model open");

            var testsData = document.GetClash()?.TestsData;
            if (testsData is null)
                return Result.Text.Failed("Clash data is not available for the active model");

            var clashTests = testsData.Tests;
            if (clashTests is null)
                return Result.Text.Failed("Clash tests are not available for the active model");

            var tests = clashTests.OfType<ClashTest>().ToList();
            if (tests.Count == 0)
                return Result.Text.Failed("No clash tests were found");

            if (args.RunAllTests)
            {
                try
                {
                    testsData.TestsRunAllTests();
                    return Result.Text.Succeeded($"Ran all clash tests ({tests.Count})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed running all clash tests: {ex}");
                    return Result.Text.Failed("Failed to run all clash tests in Navisworks. See logs for details.");
                }
            }

            // Prioritize explicit manual input so users can override a selected dropdown value.
            var manualTestName = args.ManualTestName?.Trim();
            var selectedTestName = args.TestName?.Trim();
            var requestedTestName = !string.IsNullOrWhiteSpace(manualTestName)
                ? manualTestName
                : selectedTestName;

            if (string.IsNullOrWhiteSpace(requestedTestName))
            {
                return Result.Text.Failed(
                    $"Provide a clash test name (dropdown or manual) or enable 'Run all tests'. Available tests: {FormatTestNames(tests)}");
            }

            var selectedTest = tests.FirstOrDefault(test =>
                string.Equals(test.DisplayName, requestedTestName, StringComparison.OrdinalIgnoreCase));

            if (selectedTest is null)
            {
                return Result.Text.Failed(
                    $"Clash test '{requestedTestName}' was not found. Available tests: {FormatTestNames(tests)}");
            }

            // Cache managed values before executing the native-backed test object,
            // because Navisworks can invalidate handles after run completion.
            var selectedTestDisplayName = selectedTest.DisplayName;

            try
            {
                testsData.TestsRunTest(selectedTest);
                return Result.Text.Succeeded($"Ran clash test: {selectedTestDisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed running clash test '{requestedTestName}': {ex}");
                return Result.Text.PartiallySucceeded(
                    $"Clash test '{selectedTestDisplayName}' appears to have executed, but Navisworks reported an error afterward: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Unexpected error while preparing clash test execution: {ex}");
            return Result.Text.Failed("An unexpected error occurred while preparing clash test execution. See logs for details.");
        }
    }

    private static string FormatTestNames(IEnumerable<ClashTest> tests)
    {
        return string.Join(", ", tests
            .Select(test => test.DisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name)));
    }
}