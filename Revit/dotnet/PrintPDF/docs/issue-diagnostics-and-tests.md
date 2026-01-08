# Issue: Diagnostics & Tests

Summary

We need better diagnostics and automated tests to reproduce, detect, and prevent regressions for the sheet ordering, empty outputs, and concurrency issues.

Goals

- Add a diagnostic mode or verbose logging option.
- Record per-sheet metadata (sheet id, name, number, sequence index, start time, end time, filename, file size, page count, errors).
- Add unit and integration tests for ordering and file I/O behaviors.

Diagnostic features to add

- `--diagnostic` flag or config setting to enable verbose logs and to write a JSON manifest for each export run recording per-sheet metadata.
- Correlation ID for each export run and per-sheet operation.
- Log enough context to correlate Revit API responses with files produced.

Tests to add

- Unit tests for ordering function: preservation of sheet set index, numeric-aware sorting, tie-breakers.
- Integration test using the user's action file to validate merged PDF order.
- Concurrency tests that run multiple exports concurrently and verify no collisions and correct final outputs.
- Post-export validation tests to assert that PDFs are non-empty and contain expected page counts.

Next steps

- Add a diagnostic manifest writer and the `--diagnostic` flag.
- Implement initial unit tests for ordering logic.
- Add CI job to run unit tests and select integration tests when resources are available.