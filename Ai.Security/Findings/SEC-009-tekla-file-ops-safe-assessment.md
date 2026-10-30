# SEC-009: Tekla five file operation extensions - safe assessment

| | |
|---|---|
| Severity | Info - Documented security assessment showing no injection or path traversal risk in these extensions |
| Status | Open |
| Category | Injection / Path Traversal |
| Location | `src/Tekla/dotnet/ReadIn/`, `RefreshReferenceModels/`, `SaveModel/`, `WriteOut/`, `ZoomToSelected/` |
| First seen | run 20260926-215844 at commit 4658b6b |
| Last verified | run 20260926-215844 |
| Introduced | N/A |

## What

Security review of five Tekla extension file operation modules (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected) confirms they pose no injection, path traversal, or command execution risk. None accept user-typed file paths or command strings as input. All operate purely through the Tekla SDK API on in-model objects or hardcoded macro calls.

## Evidence

All five extensions follow the same safe pattern:

**ReadIn** (`TeklaReadInCommand.cs`, `TeklaReadInArgs.cs`):
- Args: only two boolean fields (`Save`, `FailTask`) - no file paths or strings.
- Uses `ModelHistory.GetLocalChanges()` and `ModelHistory.TakeModifications()` - Tekla SDK APIs operating on in-memory model state.
- Macro callback uses hardcoded literal: `Callback("acmdRunPluginMethod", "SharingToolsFeature;Tool.SharingAutomation;r0.00:00:00", "main_frame")` (line 27).
- `ModelHandler.Save()` on line 58 is the Tekla SDK save, not a user-supplied path.

**RefreshReferenceModels** (`RefreshReferenceModelsCommand.cs`, `RefreshReferenceModelsArgs.cs`):
- Args: empty class (line 3).
- Iterates `REFERENCE_MODEL` objects from `Model.GetModelObjectSelector().GetAllObjectsWithType()` - model objects already in the Tekla file.
- Calls `ReferenceModel.RefreshFile()` (line 24) on existing model references - SDK API, not user input.

**SaveModel** (`TeklaSaveModelCommand.cs`, `TeklaSaveModelArgs.cs`):
- Args: empty class (line 3).
- Single call to `ModelHandler.Save()` (line 7) - standard SDK save, no path parameter.

**WriteOut** (`TeklaWriteOutCommand.cs`, `TeklaWriteOutArgs.cs`):
- Args: empty class (line 3).
- Macro callback uses hardcoded literal: `Callback("acmdRunPluginMethod", "SharingToolsFeature;Tool.SharingAutomation;w", "main_frame")` (line 10).

**ZoomToSelected** (`ZoomToSelectedCommand.cs`, `ZoomToSelectedArgs.cs`):
- Args: empty class (line 3).
- Uses `DrawingHandler`, `DrawingObjectSelector.GetSelected()`, `ModelObject.Hideable.ShowInDrawing()/HideFromDrawing()` - all SDK UI APIs.
- `ModelInternal.Operation.dotStartAction("ZoomToSelected", string.Empty)` (line 14) - hardcoded string.

## Impact

No exploitable vector identified. These extensions cannot be used for injection, path traversal, or arbitrary command execution via user input.

## What a fix involves

No fix needed. This finding documents that the review of these five extensions is complete and they are safe.

## References

- SEC-008: Tekla IFCExport path traversal (different extension with user input)
- Coverage.md: Backlog item "Tekla file operations (ReadIn, RefreshReferenceModels, SaveModel, WriteOut, ZoomToSelected)"
