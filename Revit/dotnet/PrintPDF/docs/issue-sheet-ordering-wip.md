# WIP: Sheet ordering investigation

Goal

- Find code paths that enumerate and export sheets. Identify where ordering might be lost and collect evidence.

Plan

- Search for Revit types and export/merge code: `ViewSheet`, `PrintManager`, `SheetSet`, `ViewSet`, `Export`, `MergePDF`, `PdfMerge`, `pdf`, `Order`, `Sort`.
- Read `PrintPDFCommand.cs`, `PrintPDFArgs.cs`, `PrintPDFWorker.cs`, `PrintPDFSelector.cs` and any helper classes.
- Record findings and propose a deterministic ordering function.

Progress

- Created this WIP notes file.
- Next: search the codebase for relevant symbols and list candidate files.

Findings (initial code review)

- Files inspected:
	- `PrintPDFSelector.cs` — collects sheets from the Revit document and view/sheet sets.
	- `PrintPDFWorker.cs` — performs the export calls to Revit and handles per-sheet file naming / moving.
	- `PrintPDFCommand.cs` and `PrintPDFArgs.cs` — wiring and configuration (how options are selected).

- Relevant behaviors observed:
	- `CollectByViewSet(string viewSetName)` reads the sheets in the ViewSheetSet and then applies `sheets.OrderBy(x => x.SheetNumber)` before returning. This explicitly sorts by sheet number.
	- `CollectByViewSetRegex(string pattern)` iterates matching `ViewSheetSet`s and appends their `viewSet.Views` in the order returned by Revit; it does NOT apply a deterministic sort before returning.
	- Several collectors (e.g. `CollectBySheetNameRegex`, `CollectBySheetNumberRegex`, `GetSheetsMatchingRule`) return lists from a `FilteredElementCollector(...).OfType<ViewSheet>().ToList()` without an explicit order. The Revit API/collector does not guarantee an ordering that matches sheet creation order or sheet number unless explicitly sorted.
	- `PrintPDFWorker.PrintPDF(...)` (Combine=true) calls:
			var exportViewIds = sheets.Select(x => x.Id).ToList();
			_document.Export(folderName, exportViewIds, _pdfOptions);
		This passes the sheet Ids in the sequence provided by the caller. Whether Revit preserves that sequence in the combined PDF is implementation-dependent, but it is the right place to ensure an explicit ordering.
	- `PrintPDFWorker.PrintPDF(...)` (Combine=false) exports each sheet separately by calling `_document.Export(folderName, exportViewIds, _pdfOptions)` for a single-sheet list, then moves a temp file named `Sheet-{sheet.Name}.pdf` to the final name. This has multiple issues that can break ordering or produce wrong files when multiple processes run concurrently:
		- The hard-coded temporary file name `Sheet-{sheet.Name}.pdf` is reused by all processes and doesn't include any unique token (process id / GUID / timestamp). Concurrent exports can overwrite each other's temp files.
		- `oldFileName` uses `sheet.Name` (raw) while the final filename uses the sanitized `CreatePdfName(sheet)`. That mismatch can cause FileNotFound or collisions when sheet names contain characters that are invalid on the file system or get normalized differently.

Potential root causes for "random" ordering reported by users

- Non-deterministic ordering from Revit collectors when no explicit sort is used.
- Different code paths: `CollectByViewSet` sorts by sheet number but `CollectByViewSetRegex` doesn't; the UI/action file may select regex-based collection leading to unsorted input.
- Concurrency between processes: shared temporary filenames (e.g. `Sheet-{sheet.Name}.pdf`) cause exports to get mixed or overwritten, producing PDFs with unexpected page orders.

Immediate, low-risk proposals

1. Centralize ordering logic: add a small helper `GetOrderedSheets(IEnumerable<ViewSheet> sheets, bool preferViewSetOrder = false)` that:
	 - If the caller is printing a specific `ViewSheetSet` (or we detect a preserved index), preserve the ViewSet order as returned by the set.
	 - Otherwise, apply a deterministic fallback: numeric-aware sort of `SheetNumber` (split numbers and text) -> then `SheetName` as tie-breaker.

2. Ensure callers supply ordered lists to `PrintPDFWorker.PrintPDF(...)`. The `PrintPDFSelector` should return ordered sequences for all collection methods (or PrintPDFWorker should order them right before export).

3. Remove or make temp filenames unique for non-combine exports. Use a GUID or process id prefix for `oldFileName` (e.g. `Sheet-{ProcessId}-{Guid}-{sheet.Name}.pdf`) so multiple processes won't collide.

4. Add lightweight instrumentation (log to a timestamped file in the destination folder) immediately before export:
	 - Log sheet Id, SheetNumber, SheetName and the target filename in the exact sequence sent to Revit.
	 - When Combine=true log the ordered sequence the export receives.

Longer-term / test changes

- Add unit tests for the ordering helper with these cases:
	- Sheets with numeric sheet numbers: "1", "2", "10" -> numeric-aware order: 1,2,10
	- Alphanumeric sheet numbers: "A1", "A2", "B1" -> alphabetical then numeric
	- Preserve explicit ViewSheetSet order when requested.

- Integration test: create a deterministic export run (single process) and assert the combined PDF pages follow the ordered sequence. For concurrency reproduction, run two exports concurrently to an isolated temp directory and assert both outputs are correct (this reproduces the user's environment where different processes were running exports concurrently).

Next steps (I'll proceed unless you want to change scope)

- Marked inspection steps complete and write these findings into this WIP file. (done)
- Implement the `GetOrderedSheets` helper and wire it into `PrintPDFSelector` and `PrintPDFWorker`.
- Replace temp file naming with a unique token to avoid cross-process collisions.
- Add logging instrumentation around export calls.

Assumptions

- We prefer not to change Revit Export API usage; we'll change inputs (ordering) and file handling around it.
- The reported problem often occurs when multiple external processes run exports concurrently; we'll make temp filenames per-process to mitigate.

Status

- Code review: initial pass complete.
- Next coding step: implement ordering helper and safer temp filenames in `PrintPDFWorker.cs` (will open a new todo for this).


