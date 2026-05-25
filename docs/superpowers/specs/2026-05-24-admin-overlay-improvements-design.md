# Admin Management Overlay Improvements

2026-05-24

## Overview

Fix three issues with the parts-barcode reimport overlay in `AdminManagementView`:
1. Remove `TopMost` so the overlay doesn't block other applications
2. Ensure the overlay stays on top of the main application and the backdrop cannot be activated by clicks
3. Add a real-time log text area with progress info and elapsed time

## Z-Order Fix

### Problem

`_overlayBackdrop` and `_overlayPopup` both set `TopMost = true`, which makes them stay on top of ALL windows including unrelated applications. Clicking the backdrop form activates it, bringing it above the popup in z-order and breaking the visual layering.

### Solution

**Owner chain approach:**

- Remove `TopMost = true` from both forms
- In `ShowLoadingOverlay(true)`, set:
  - `_overlayBackdrop.Owner = (Form)TopLevelControl` — keeps backdrop above main form
  - `_overlayPopup.Owner = _overlayBackdrop` — keeps popup above backdrop (owned forms are always Z-ordered above their owner)
- Create a minimal subclass `OverlayBackdropForm : Form` that overrides `WndProc` to intercept `WM_MOUSEACTIVATE` (0x0021) and return `MA_NOACTIVATE` (3). This prevents mouse clicks from activating the backdrop, so z-order never changes.

**Behavior:**
- Overlay stays on top of the main application but not other apps
- When the main form is minimized, overlay follows (natural Owner behavior)
- Clicking the backdrop has no effect — the popup stays on top
- The main form's controls are blocked because the backdrop (a Form with no transparency to clicks) covers them

## Progress Reporting Bridge

### Problem

`ReimportPartsBarcode` is a synchronous API call that returns once complete. The UI shows a static label + marquee progress bar with no real-time feedback during the potentially long operation.

### Solution

**Shared state + Timer polling (5-second interval):**

- `ReimportPartsBarcodeReq` gains an `Action<ReimportProgressInfo>? OnProgress` callback
- New DTO `ReimportProgressInfo` with `BatchCount`, `TotalInserted`, `LastId`, `TotalBatches`, `TotalBatches`
- `OperationGuidanceApis.ReimportPartsBarcode`: before the batch loop, runs `SELECT COUNT(*) FROM mission_record WHERE <same conditions>` to compute `totalBatches = (int)Math.Ceiling(totalRows / (double)batchSize)`. Calls `req.OnProgress?.Invoke(info)` after each batch.
- `AdminManagementView` holds `ReimportProgressInfo? _latestProgress` (updated via callback, `lock` protected)
- `System.Windows.Forms.Timer` with 5000ms interval reads `_latestProgress` and updates the log text area, elapsed time, percentage, and ETA labels
- Callback fires from the background thread; the `lock` ensures safe reads from the UI timer

**Progress calculations (UI side, in timer tick):**
- Percentage: `(int)((double)progress.BatchCount / progress.TotalBatches * 100)`, clamped to 0-99 until complete
- Estimated remaining: `elapsed / batchCount * (totalBatches - batchCount)` → formatted as `hh:mm:ss`
- Estimated end time: `DateTime.Now + estimatedRemaining` → formatted as `HH:mm:ss`

## Popup UI Layout

### Problem

The current popup (400×120) only shows static text and a marquee progress bar — no real-time feedback.

### Solution

Resize from 400×120 to 460×370, with six elements:

1. **Title label**: "正在重新导入物料码..." (kept from current)
2. **Log text area**: Read-only multiline `TextBox`, `ScrollBars.Vertical`, `Font: Consolas 12px`, height ~160px. Each 5-second tick appends a line like:
   ```
   [14:32:06] 已处理 100 批, 插入 15,230 行, 耗时 5.0s
   ```
   Auto-scroll via `ScrollToCaret()` after append.
3. **Marquee progress bar**: Kept below the log area for visual reassurance
4. **Elapsed time label**: "已运行 00:00:15", updated by the same 5-second timer
5. **Percentage label**: "进度: 45%" — `batchCount / totalBatches * 100`, clamped 0-99 until complete
6. **ETA label**: "预计剩余: 00:05:30，预计结束: 14:38:00" — computed from elapsed, batchCount, totalBatches

On completion: append result to log area ("导入完成！删除 X 条，插入 Y 条，耗时 Zs"), stop the marquee (set `ProgressBarStyle.Blocks` + `Value = 100`), `await Task.Delay(2000)`, then `ShowLoadingOverlay(false)`. Replaces `MessageBox.Show` — user reads result directly in the log area.

On error: append error line to log, stop marquee, `await Task.Delay(2000)`, `ShowLoadingOverlay(false)`.

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Views/AdminManagementView.cs` | Modify | Z-order fix, popup UI rebuild, timer logic, `_latestProgress` field |
| `Models/Requests/ReimportPartsBarcodeReq.cs` | Modify | Add `OnProgress` callback |
| `Models/Responses/ReimportProgressInfo.cs` | **New** | Progress info DTO |
| `Controllers/OperationGuidanceApis.cs` | Modify | Invoke `OnProgress` callback each batch |

## Data Flow

```
UI Thread                              Background Thread (Task.Run)
─────────                              ──────────────────────────
req.OnProgress = info => { lock... } ─→ stored in request
Start Timer (5s interval)
Show overlay
await Task.Run(apis.Reimport) ───────→ SELECT COUNT(*) → calc TotalBatches
                                       loop batches:
                                          insert batch
                                          req.OnProgress?.Invoke(info)
                                              └── _latestProgress = info (lock)
                                       return ReimportPartsBarcodeRsp
←── await completes
Append result line to log
Set percentage → 100%, stop marquee
await Task.Delay(2000)
ShowLoadingOverlay(false)
Stop Timer
```
