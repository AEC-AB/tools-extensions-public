# Issue: Empty drawings are sometimes printed

Summary

Users report that the extension sometimes produces empty PDFs or prints "empty drawings". In some reports this happens while multiple exports are running concurrently across separate processes (not in-process parallelism), which may exacerbate timing or I/O-related race conditions.

Reproduction

- Use the user's model and the `COWI_Assistant_PrintTaskIssues` action file and observe if some resulting PDFs are empty or contain no views.
- Collect logs and per-sheet output file sizes and page counts.

Investigation plan

- Find the export/print code paths to see how views/sheets are selected and exported.
- Confirm the extension is selecting the right view/sheet and that Revit actually has content to print for that sheet.
- Check for timing issues: Is the sheet being exported while another process (a separate export process) modifies it or when it's temporarily invalid? Consider how multiple processes running concurrently could affect temporary Revit state or file I/O.
- Validate that the PDF generator returns a non-zero page count and size; if zero, consider retry logic or marking the sheet as failed.

Hypotheses

- Sheets could be empty because the selected view has no visible content or the view is hidden/filtered.
- The PDF generation may be invoked before the sheet's temporary plot state is fully ready (race/timing issue).
- A bug in page merging or detection treats a valid PDF as empty due to an incorrect check.

Proposed fixes

- Validate per-sheet output after export: check file size and page count; if invalid, retry export once or twice with a short backoff.
- Add a check to confirm the view contains printable geometry before invoking export (if possible through Revit API).
- Improve logging to capture when an empty PDF is produced, including the sheet id, view name, and any Revit warnings.

Tests

- Integration test that attempts to export known empty and known-non-empty sheets and validates the outcome.
- Add a flakiness test that exports the same set multiple times to detect intermittent empty outputs.

Next steps

- Add diagnostics to capture per-sheet post-export validation data.
- Implement retry logic and improved logging for empty outputs.
- Re-run user reproduction and measure improvements.