# Plan: Refactor INFRAIFCIDSValidation into smaller files

## TL;DR
Split two large files (750 + ~600 lines) into 6–8 focused files following collaborator feedback. Also apply targeted code improvements to the Args class and Command class. The R339 architectural note (CLI/IPC instead of reflection) is out of scope — requires INFRA team collaboration.

---

## Phase 1: Extract to separate files (no behavior changes)

**Step 1** — Create `InfraCommand.cs`
- Move `InfraCommand` enum (lines 5–10 of Args.cs) verbatim.
- No dependencies; parallel with steps 2–5.

**Step 2** — Create `AvailableProjectsCollector.cs`
- Move `AvailableProjectsCollector` class (lines 45–122 of Args.cs) verbatim.
- Keep `[SupportedOSPlatform("windows")]`.

**Step 3** — Create `AvailableIdsFilesCollector.cs`
- Move `AvailableIdsFilesCollector` class (lines 124–263 of Args.cs) verbatim.
- Keep `[SupportedOSPlatform("windows")]`.

**Step 4** — Create `InfraApiCollectorHelpers.cs`
- Move `InfraApiCollectorHelpers` class (lines 281–750 of Args.cs) verbatim.
- Covers R286 and R709 (EnumerateFilesSafe is inside this class).

**Step 5** — Create `IfcFileResolver.cs`
- Extract all IFC file resolution static helpers out of `INFRAIFCIDSValidationCommand.cs` (lines 269–548): `ResolveIfcFiles`, `ExpandIfcEntry`, `NormalizeIfcEntry`, `TryResolveVariableEntries`, `TryResolveInterpolatedEntry`, `ResolveVariableValue`, `ExtractVariableName`, `SplitMultiValue`, `ContainsWildcard`, `ExpandWildcardEntry`, `TryParseRegexEntry`, `ExpandRegexEntry`.
- Wrap in a new `internal static class IfcFileResolver`.
- Update call site in `RunAsync`: `ResolveIfcFiles(context, args)` → `IfcFileResolver.ResolveIfcFiles(context, args)`.

Steps 1–5 are independent and can be done in parallel (all pure moves, no logic changes).

---

## Phase 2: Args class cleanup (behavior changes)

**Step 6** — Delete `AvailableValidationCommandsCollector` (lines 265–278, Args.cs)
- Remove the class entirely (R268).
- *Depends on Step 7.*

**Step 7** — Change `Commands` to `List<InfraCommand>` (R28, R29)
- In `INFRAIFCIDSValidationArgs`: change property type from `List<string>` to `List<InfraCommand>`.
- Remove `CollectorType = typeof(AvailableValidationCommandsCollector)` from `[OptionsField]` attribute.
- Update default value: `[nameof(InfraCommand.IFC_CHECK)]` → `[InfraCommand.IFC_CHECK]`.

After Phase 2, `INFRAIFCIDSValidationArgs.cs` contains only `INFRAIFCIDSValidationArgs` (~30 lines).

---

## Phase 3: Create InfraApiWrapper (R124, R37, R42)

**Step 8** — Create `InfraApiWrapper.cs`
- New `internal sealed class InfraApiWrapper` wrapping the reflection `object api`.
- Constructor: `private InfraApiWrapper(object api)`.
- Static factory: `public static InfraApiWrapper Create(...)` using existing `TryCreateApiInstance` logic, returning `InfraApiWrapper?`.
- Expose strongly-typed methods (replacing raw `Invoke<T>` call sites in `RunAsync`):
  - `string? GetCommonProjectsLocation()`
  - `Dictionary<string, string> ScanAllProjects()`
  - `List<string> ScanIdsFiles(string idsPath)`
  - `void CreateMetadataFile(string projectName, string[] ifcFiles)`
  - `void WriteProjectPathToRegistry(string projectName, string projectPath)`
  - `void SaveSelectedIdsFilesToRegistry(string projectName, List<string> idsFiles)`
  - `void WriteOutputDirectoryToRegistry(string projectName, string outputDir)`
  - `void LaunchInfraAutomation(string arguments, string projectName)`
- Each method uses the private `Invoke<T>` helper already in `InfraApiCollectorHelpers` (or a copy).

**Step 9** — Add `[NotNullWhen(true)]` to `TryCreateApiInstance` (R37)
- Change signature in `InfraApiCollectorHelpers.cs`:
  `public static bool TryCreateApiInstance([NotNullWhen(true)] out object? api, out string error)`

---

## Phase 4: Replace AssemblyLoadContext (R341)

**Step 10** — Use `AssemblyLoadContext` in `InfraApiCollectorHelpers.TryCreateApiInstance`
- Add a private nested class `InfraAssemblyLoadContext : AssemblyLoadContext`.
- Override `Load(AssemblyName)` to resolve sibling DLLs from `apiFolder`.
- Replace the `AppDomain.CurrentDomain.AssemblyResolve` event add/remove pattern with `context.LoadFromAssemblyPath(dllPath)`.
- *Depends on Step 4 (InfraApiCollectorHelpers is in its own file).*

---

## Phase 5: Refactor RunAsync (R7, R22, R42)

**Step 11** — Simplify command parsing (R22)
- `selectedCommands` no longer needs `Enum.TryParse` — `args.Commands` is now `List<InfraCommand>`.
- Replace the 8-line LINQ chain with: `List<InfraCommand> selectedCommands = args.Commands.Distinct().ToList();`
- *Depends on Step 7.*

**Step 12** — Remove unnecessary `apiInstance` variable (R42)
- Replace `object apiInstance = api!;` and all `apiInstance` references with the `InfraApiWrapper` instance from Step 8.
- *Depends on Step 8.*

**Step 13** — Break `RunAsync` into sub-methods (R7)
- Extract private methods to keep `RunAsync` under ~60 lines:
  - `ResolveProject(InfraApiWrapper api, string projectName, List<string> diagnostics)` → returns `(string projectPath, IExtensionResult? error)`
  - `ResolveIdsFiles(InfraApiWrapper api, string projectPath, List<string> explicitSelection)` → returns `(List<string> idsFiles, IExtensionResult? error)`
  - `BuildSummary(string projectName, int ifcCount, int idsCount, List<InfraCommand> commands, string outputFolder, string? firstNewFile, List<string> diagnostics, string launchArgs)` → returns `string`
- `RunAsync` orchestrates these calls and early-returns on errors.
- *Depends on Steps 8, 11, 12.*

---

## Relevant files

- `src/Assistant/dotnet/INFRAIFCIDSValidation/INFRAIFCIDSValidationArgs.cs` — source + final trimmed form (~30 lines)
- `src/Assistant/dotnet/INFRAIFCIDSValidation/INFRAIFCIDSValidationCommand.cs` — source + final refactored form
- `src/Assistant/dotnet/INFRAIFCIDSValidation/InfraCommand.cs` — new
- `src/Assistant/dotnet/INFRAIFCIDSValidation/AvailableProjectsCollector.cs` — new
- `src/Assistant/dotnet/INFRAIFCIDSValidation/AvailableIdsFilesCollector.cs` — new
- `src/Assistant/dotnet/INFRAIFCIDSValidation/InfraApiCollectorHelpers.cs` — new
- `src/Assistant/dotnet/INFRAIFCIDSValidation/InfraApiWrapper.cs` — new
- `src/Assistant/dotnet/INFRAIFCIDSValidation/IfcFileResolver.cs` — new

---

## Verification

1. `dotnet build` passes with zero errors and zero warnings after each phase.
2. After Phase 2: `INFRAIFCIDSValidationArgs.cs` contains only the args class.
3. After Phase 3: All raw `Invoke<T>` call sites in `RunAsync` are gone.
4. After Phase 5: `RunAsync` body is ≤60 lines.

---

## Decisions

- R339 (CLI/IPC architectural redesign) is **out of scope** — requires INFRA team collaboration.
- `AvailableValidationCommandsCollector` is **deleted**, not moved (R268).
- `GetOutputFiles` and `WaitForFirstNewOutputFileAsync` stay in `INFRAIFCIDSValidationCommand.cs` — they're output-polling helpers specific to the command.
- `Invoke<T>` helper stays as a private method inside `InfraApiWrapper` (or delegates to `InfraApiCollectorHelpers`).
- The `Invoke<T>` in `InfraApiCollectorHelpers` (public) stays for use by collectors.
