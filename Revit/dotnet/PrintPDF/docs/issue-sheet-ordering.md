# Issue: Sheet Ordering when printing sheet sets

Summary

When printing sheet sets with the Print PDF V1.0.2 assistant Revit Extension, the order of sheets appears random: it does not follow the order sheets were added to the set nor their sheet numbers.

Reproduction

1. Use the model and action file the user provided.
2. Set the export path in the action file or the UI to a writable folder.
3. Run the action `COWI_Assistant_PrintTaskIssues` and inspect the resulting PDF(s).
4. Compare the sheet order in the PDF(s) against the sheet set order inside Revit.

Note: the user observed ordering problems while multiple exports were running concurrently across separate processes. The extension does not use in-process parallelization; concurrency issues are caused when different processes run exports at the same time and interact via the filesystem or shared resources.

Investigation plan

- Locate code paths that enumerate/export sheets (`PrintPDFCommand.cs`, `PrintPDFArgs.cs`, and any helpers). Search for Revit types like `ViewSheet`, `PrintManager`, `SheetSet`, `ViewSet`.
- Check whether unordered collections or parallel processing are used.
- Add instrumentation/logging to print the order of sheets as returned by the Revit API and right before merge.

Hypotheses

- The code enumerates sheets with a non-order-preserving collection or LINQ that loses order.
- Parallel processing causes non-deterministic output ordering.
- The merge step uses directory enumeration (OS order) rather than explicit ordered list.

Proposed fixes

1. Preserve sheet set order when available.
2. Implement a deterministic fallback sort: sheet set index -> numeric-aware sheet number -> sheet name.
3. Centralize ordering logic in `GetOrderedSheets(IEnumerable<ViewSheet>)` and add unit tests.
4. Ensure merge respects captured ordering (either by using an explicit sequence or using zero-padded prefixed filenames).

Tests

- Unit tests for ordering behavior (preserve index, numeric-aware sort, tie-breakers).
- Integration test to verify final merged PDF order using the provided action file.

Next steps

- Add instrumentation and reproduce. Collect logs showing the order as returned by Revit vs. order in final PDF.
- Implement ordering function and tests.
- Verify with user's provided model/action file.

Notes

The user-provided action file: `COWI_Assistant_PrintTaskIssues` (set export path before running).