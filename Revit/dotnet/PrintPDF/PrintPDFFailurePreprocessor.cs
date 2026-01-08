namespace PrintPDF
{
    internal class PrintPDFFailurePreprocessor : IFailuresPreprocessor
    {
        public string Message => string.Join(Environment.NewLine, Failures);
        public List<string> Failures { get; } = new List<string>();

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var messages = failuresAccessor.GetFailureMessages();
            
            foreach (var message in messages)
            {
                var description = message.GetDescriptionText();
                if (!string.IsNullOrEmpty(description) && !Failures.Contains(description))
                    Failures.Add(description);
                var severity = message.GetSeverity();
                var hasResolutions = message.HasResolutions();

                if (severity != FailureSeverity.Error && severity != FailureSeverity.DocumentCorruption && hasResolutions)
                {
                    failuresAccessor.ResolveFailure(message);
                    continue;
                }

                if (severity != FailureSeverity.Error && severity != FailureSeverity.DocumentCorruption && !hasResolutions)
                {
                    failuresAccessor.DeleteWarning(message);
                    continue;
                }
            }

            return FailureProcessingResult.Continue;
        }

        public void AppendToResult(ref string message)
        {
            if (string.IsNullOrEmpty(Message))
                return;

            if (!string.IsNullOrEmpty(message))
                message += Environment.NewLine;

            message += Message;                
        }

        internal static PrintPDFFailurePreprocessor Attach(Transaction transaction)
        {
            var failurePreprocessor = new PrintPDFFailurePreprocessor();
            var options = transaction.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(failurePreprocessor);
            transaction.SetFailureHandlingOptions(options);

            return failurePreprocessor;
        }
    }
}