namespace OpenWorksets;

/// <summary>
/// Resolves or dismisses the warnings Revit raises while the temporary workset, view and
/// elements are created, so the run does not stop on a modal dialog. Errors are left alone;
/// Revit then rolls the transaction back and the caller reports the workset as still closed.
/// Everything this suppresses is discarded with the surrounding transaction group anyway.
/// </summary>
internal class OpenWorksetsFailurePreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        foreach (var message in failuresAccessor.GetFailureMessages())
        {
            if (message.GetSeverity() is FailureSeverity.Error or FailureSeverity.DocumentCorruption)
                continue;

            if (message.HasResolutions())
                failuresAccessor.ResolveFailure(message);
            else
                failuresAccessor.DeleteWarning(message);
        }

        return FailureProcessingResult.Continue;
    }

    internal static void Attach(Transaction transaction)
    {
        var options = transaction.GetFailureHandlingOptions();
        options.SetFailuresPreprocessor(new OpenWorksetsFailurePreprocessor());
        transaction.SetFailureHandlingOptions(options);
    }
}
