# DaluxRevitUpload — Codebase Guide

## What this project does

Dalux's REST API has no Revit file upload endpoint. The Dalux Revit plugin ships a ribbon button that opens a WebView2 popup where the user manually fills in metadata and clicks Upload. This extension automates that entire flow end-to-end:

1. Clicks the **Dalux ribbon tab → Upload button** in Revit using Windows UI Automation.
2. Discovers the WebView2 popup's **Chrome DevTools Protocol (CDP) port**.
3. Connects over **WebSocket** and injects a JavaScript automation script via `Runtime.evaluate`.
4. The script fills in **revision number, revision date, dropdowns, text fields**, then optionally clicks Upload and waits for the Done button.

This is a pure UI-automation workaround — no Dalux API calls are made.

---

## Project type

`.NET 8.0`, Windows-only (`net8.0-windows`), x64, WPF enabled (for `System.Windows.Automation`).  
Implements `IAssistantExtension<DaluxRevitUploadArgs>` — the host platform calls `RunAsync` and renders the result as text.

---

## File map

| File | Role |
|---|---|
| `DaluxRevitUploadArgs.cs` | UI input model — each property becomes a form field in the runner |
| `DaluxRevitUploadCommand.cs` | Entry point — validates inputs, builds config, runs service, returns result |
| `DaluxAutomationConfig.cs` | Plain config record passed into the service |
| `DaluxAutomationService.cs` | 1800-line orchestrator — env var, UI automation, CDP, JS generation |
| `CdpClient.cs` | CDP WebSocket client — connect, send commands, evaluate JS, probe ports |

---

## End-to-end flow

### Step 1 — `EnsureRemoteDebuggingEnvVar()`

Writes `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=0` to `HKCU\Environment`.

- Port `0` tells each WebView2 process to pick its own ephemeral port and write it to `{UDF}/DevToolsActivePort`. This avoids bind collisions with Revit's internal WebView2, Teams, etc.
- If the env var is **missing entirely**: writes it, then **aborts and tells the user to restart Revit**. Revit must be launched after the flag is in its environment.
- If it exists with a **non-zero port** (legacy `=9222` left by older Dalux addon): silently rewrites to `=0` — no restart needed because WebView2 reads the registry at popup spawn time, not at Revit start.
- If `=0` is already present: proceeds immediately.

### Step 2 — `RunPreflightDiagnosticsAsync()`

Cheap checks logged before any UI interaction:
- WebView2 Evergreen Runtime version (from registry `SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-...}`)
- Revit process start time vs. env var last-write time — warns if Revit predates the env var
- Port conflict detection: if a fixed port is configured, classifies the owning process as Expected/SystemCritical/Foreign and optionally kills foreign processes (`TryAutoFreeCdpPortAsync`)

### Step 3 — `ClickRevitDaluxUploadAsync()`

Uses `System.Windows.Automation`:
- `AutomationElement.FromHandle(revitProc.MainWindowHandle)` — works even on a locked screen
- `FindFirst(TreeScope.Descendants, ControlType.TabItem, Name="Dalux")` → click
- Waits 1.5 s, then finds `ControlType.Button, Name="Upload"` (5 attempts × 500 ms)
- Clicks via `InvokePattern.Invoke()` when available; falls back to `PostMessage(WM_LBUTTONDOWN/UP)` with screen-to-client coords — the PostMessage path bypasses the active desktop and works on locked screens

### Step 4 — `FindDaluxEndpointAnywhereAsync()` (up to 2 minutes)

Polls every 2 s with two parallel strategies:

**Fast path** — reads `%LocalAppData%\DaluxWebView2\Default\EBWebView\DevToolsActivePort`. Chromium writes this file when it starts with `--remote-debugging-port`. Line 1 is the bound port. If present and CDP responds, returns the first tab with a WebSocket URL — no title/URL matching needed since everything at that UDF belongs to Dalux.

**Fallback path** — `GetExtendedTcpTable` P/Invoke enumerates all IPv4 LISTEN sockets, filters to `msedgewebview2.exe` PIDs, probes each port's `/json`, checks for a tab whose URL or title contains "dalux". Logs new/changed/gone ports and tabs on every tick for diagnostics.

On timeout, `LogCdpTimeoutDiagnosticsAsync` dumps all WebView2 endpoints and suggests PowerShell commands to verify the popup's command line.

### Step 5 — WebSocket connect + retry

`ConnectAsync(wsUrl)` with a 44 s timeout (linked `CancellationTokenSource`). Retries up to 10 times on HTTP 500 (WebView2 not ready yet), 500 ms each. Re-fetches the WS URL between retries in case it changed.

After connecting, handles `-32000` errors (CDP context destroyed = popup navigated after connect) by re-fetching the URL and reconnecting. Retry count is controlled by `DaluxAutomationConfig.RetryCount` (default 3, exposed to the user as "Retry count" in Advanced options).

### Step 6 — JavaScript automation

`GenerateMainAutomationScript()` builds a ~650-line JavaScript IIFE as a `List<string>`, then `EvaluateAsync` runs it via `Runtime.evaluate` with `awaitPromise: true`.

**Popup load wait** (up to 10 min, 500 ms poll): counts `tr`/`[role="row"]` and checkboxes. Every 2 s fires a `resize` event and forces `cdk-virtual-scroll-viewport` heights to 600 px to trigger Angular CDK lazy rendering.

**Step 0 — Reset selections**: Finds `input[data-cy="checkbox-input-field"]` master checkbox. Uses a counter element matching `/^\d+ \/ \d+$/` to detect selection count. Toggles master checkbox until count reaches 0 (up to 4 clicks).

**Step 1 — Find target row**: Iterates `tr`/`[role="row"]`/`[role="listitem"]`, matches `row.textContent.toLowerCase().includes(target)`. Clicks the row's checkbox.

**Step 2 — Metadata scraping + updates** (`scrapeCurrentView`): Iterates the row's visible cells, matches each to a column header by `aria-colindex` attribute or horizontal position proximity (≤15 px left-edge tolerance). Per cell:
- **Revision column** (header contains "revision", not "date"): strips non-numeric chars, adds `incrementValue`, formats with `.toFixed(2)`, writes via `typeText()`
- **Revision Date column** (header contains "revision d" or "date"): opens calendar widget via pointer events, navigates month-by-month with `[data-cy="date-next/prev-month-btn"]`, clicks the target day
- **Dropdown columns**: click/dblclick cell, dispatch Alt+Down, find `[role="option"]` matching value; fallback — type into search input if no option is visible
- **Text columns**: `typeText()` — character-by-character with `keydown/input/keyup` events to trigger Angular/React reactivity

Handles wide grids by scrolling left→right in `clientWidth/2` steps and calling `scrapeCurrentView` at each position.

**Step 3 — Action button**: Finds button by text match, clicks it. If "upload" is in the text, polls every 5 s for a "Done"/"OK" button (up to 12 hours), then clicks it.

---

## CDP client (`CdpClient.cs`)

| Method | Purpose |
|---|---|
| `ConnectAsync(wsUrl)` | Opens WebSocket with 44 s handshake timeout |
| `SendCommandAsync(method, params)` | Sends JSON-RPC, loops `ReceiveFullMessageAsync` until matching `id` arrives |
| `ReceiveFullMessageAsync` | Accumulates frames until `EndOfMessage` — fixes silent 1 MB truncation |
| `EvaluateAsync(script)` | Wraps `Runtime.evaluate` with `awaitPromise: true` |
| `GetWebSocketUrlAsync(port)` | GET `/json`, find tab with "dalux" in URL/title |
| `GetAllTabsAsync(port)` | GET `/json` — full dump for diagnostics |
| `ProbeVersionAsync(port)` | GET `/json/version` — liveness check (2 s timeout) |
| `ResetConnection()` | Dispose + null socket (`ClientWebSocket` cannot be reused after failure) |

---

## Configuration (`DaluxAutomationConfig.cs`)

| Property | Default | Notes |
|---|---|---|
| `TargetFilename` | — | Case-insensitive substring match against row text |
| `RevisionIncrement` | `0.01` | Added to current numeric revision; result formatted `.toFixed(2)` |
| `RevisionDate` | — | `"DD MMM YYYY"` e.g. `"10 Apr 2026"` |
| `ActionButtonText` | — | Button text to click; empty = skip |
| `RevitProcessId` | — | PID of target Revit instance |
| `DebuggingPort` | `0` | `0` = ephemeral per-UDF; >0 = fixed port (legacy) |
| `WebSocketTimeout` | `44000` | **Milliseconds** — handshake timeout per connect attempt |
| `AutoFreeCdpPort` | `true` | Kill foreign process holding the fixed port |
| `RetryCount` | `3` | CDP context retry count (−32000 / page-navigated errors) |
| `DropdownFields` | — | `Dictionary<string, string>` — column header → value |
| `TextFields` | — | `Dictionary<string, string>` — column header → value |

---

## Input args (`DaluxRevitUploadArgs.cs`)

- `RevitProcessId` — populated by an upstream Python script via `${{ revitprocessid }}` variable
- `UseCurrentDateForRevision` — checkbox that replaces `RevisionDate` with today's date (`dd MMM yyyy`)
- `UseUploadAction` — checkbox that sets `ActionButtonText = "Upload"` (hides the manual text field)
- `DropdownFieldsJson` / `TextFieldsJson` — multiline JSON text areas; validated for empty keys/values before deserialising
- `ShowAdvanced` — visibility gate for `RetryCount`, `DebuggingPort`, `AutoFreeCdpPort`

---

## Port discovery internals

- `EnumerateListeningPorts()` — `GetExtendedTcpTable` P/Invoke (iphlpapi), `TCP_TABLE_OWNER_PID_LISTENER`, IPv4 only. Note: port stored big-endian in `MIB_TCPROW_OWNER_PID.localPort` — byte-swap manually.
- `FindWebViewCdpPorts()` — filters results to `msedgewebview2.exe` processes; also records whether each PID descends from Revit via `IsDescendantOf` (Toolhelp32 snapshot walk, max 32 hops).
- `IsDescendantOf()` — important: WebView2 browser processes are often spawned under `RuntimeBroker`/`dllhost`, not as direct Revit children. The descendant flag is logged but **not used to filter candidates** for exactly this reason.
- `ClassifyPortOwner()` — `Expected` (Revit or its descendant), `SystemCritical` (pid 0/4 or svchost/lsass/etc.), `Foreign` (anything else).

---

## Known limitations / tech debt

| Issue | Location | Notes |
|---|---|---|
| `isRevisionDateCol` uses `includes('date')` | `GenerateMainAutomationScript` | Could over-match columns like "Created Date", "Upload Date" |
| Revision always `.toFixed(2)` | `GenerateMainAutomationScript` | Ignores increment magnitude; `1 + 0.1` → `"1.10"` |
| Month navigation fallback year hardcoded to 2026 | Date picker JS | If calendar header regex fails, wrong year assumed |
| JavaScript generated as `List<string>` | `GenerateMainAutomationScript` | No syntax highlighting or compile-time checking; difficult to maintain |
| `Math.max(currentMonth, ...)` in month parse | Date picker JS | `Math.max` is defensive but `currentMonth` resets to 1 per loop iteration anyway |
| IPv4 only in `EnumerateListeningPorts` | `DaluxAutomationService.cs` | Would miss a WebView2 that somehow binds on `::1` (unlikely in practice) |

---

## Audit log conventions

All log lines are accumulated in `_auditLog` and flushed to the user on completion.

| Prefix | Meaning |
|---|---|
| `[+]` | Success / confirmed state |
| `[*]` | Progress / informational |
| `[!]` | Warning or error |
| `[DEBUG]` | Diagnostic dump — only emitted on failure paths or post-run |
| `baseline` | Tab/port seen on first scan tick |
| `NEW` / `NEW-PORT` | Tab/port appeared after first tick |
| `changed` | Tab title or URL changed between ticks |
| `gone` / `gone-port` | Tab/port disappeared |

On success, `DaluxRevitUploadCommand` trims the output to the `=== AUTOMATION SUMMARY ===` section. On failure, the full log is returned.

---

## Environment prerequisites

1. **WebView2 Evergreen Runtime** installed (checked in pre-flight; version logged).
2. `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=0` in the **user** environment (`HKCU\Environment`). The extension writes this automatically but requires a **full Revit restart** on the very first run.
3. Revit must have been launched **after** the env var was written (pre-flight checks start time and warns if not).
4. The Dalux Revit plugin must be installed and its ribbon tab visible.
