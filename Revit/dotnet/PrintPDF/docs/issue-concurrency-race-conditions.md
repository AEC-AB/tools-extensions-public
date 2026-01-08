# Issue: Concurrency & File Rename Race Conditions

Summary

Users observed errors likely caused by multiple separate processes running exports at the same time and renaming/resaving files mid-export. There are also general instability concerns when running multiple exports concurrently across processes (note: there is no in-process parallelism; the concurrency is between separate processes).

Reproduction

- Try running multiple exports concurrently in separate processes (the user tried 5 concurrently). Observe errors or rename failures.
- Use the provided action file and repeat runs while collecting I/O errors and stack traces.

Investigation plan

 - Inspect file handling code around temp filenames, renaming, and final atomic moves.
 - Check whether unique per-export work folders or unique temp names are used by each process.
 - Look for shared global state, well-known temp filenames, or common directories that could be written by different processes and cause collisions.

Hypotheses

- Temp filenames may be predictable or reused across processes causing different processes to step on each other's files.
- File moves/rename operations may not handle IOException and do not retry, leading to observed errors when simultaneous processes attempt to finalize files.
- Shared resources like a single temp directory or static file-naming strategy used by multiple processes are causing conflicts.

Proposed fixes

- Use unique per-export identifiers (GUID + timestamp) and per-export temporary subdirectories so different processes do not share filenames.
- Prefix per-sheet temp filenames with a zero-padded sequence index to preserve order and avoid collisions when files are later enumerated.
- Perform atomic moves for finalization and implement retry with exponential backoff for transient I/O errors that may occur when multiple processes race to move/rename files.
- Add per-export correlation IDs in logs and include the process id (PID) so issues can be traced across steps and between processes.

Tests

- Concurrency test: run multiple exports (2-20) simultaneously and assert no filename collisions or rename errors.
- Fault injection test: simulate transient IOExceptions on move/rename and verify retry behavior.

Next steps

- Implement temp-file isolation and retry logic.
- Add correlation IDs in logs and rerun parallel reproductions to validate fix.