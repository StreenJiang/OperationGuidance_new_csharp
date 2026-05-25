# Admin Overlay UI Experience Improvements

2026-05-25

## Overview

Four UX fixes for the reimport overlay in `AdminManagementView`:

1. Popup doesn't minimize with owner app (Owner not applied in constructor)
2. Status labels feel frozen at 5s refresh interval
3. Overlay auto-closes after completion — user may miss the result
4. Table-lock errors silently return rows=0 instead of showing a friendly popup

## Fix 1: Owner in Constructor

**Problem:** `_overlayPopup.Owner = _overlayBackdrop` is set in `ShowLoadingOverlay(true)` each show, but `Show()` may ignore late Owner assignment. When the user clicks the taskbar icon, the main form and backdrop minimize but the popup stays visible, breaking the window state.

**Fix:** Move `_overlayPopup.Owner = _overlayBackdrop` from `ShowLoadingOverlay` into the constructor, immediately after `_overlayPopup` is created. Remove the line from `ShowLoadingOverlay`.

## Fix 2: Dual Timer — Data 5s / Display 1s

**Problem:** A single `_progressTimer` at 5s drives both log appends (data I/O) and label refresh (elapsed, percent, ETA). The labels appear frozen between ticks.

**Fix:** Split into two timers:

| Timer | Interval | Handler | What it does |
|-------|----------|---------|--------------|
| `_progressTimer` | 5000ms | `OnProgressTimerTick` (unchanged) | Read `_latestProgress` under lock, append log line |
| `_statusTimer` (new) | 1000ms | `OnStatusTimerTick` | Refresh `_elapsedLabel`, `_percentLabel`, `_etaLabel` — reads `_latestProgress` without lock (reference assignment is atomic, stale-for-1s is harmless) |

Both timers start/stop together in `OnReimport` (start after overlay shown, stop in finally).

`OnStatusTimerTick` does NOT touch `_progressLock`, NOT append to log, NOT access `_reimportStopwatch` beyond reading `.Elapsed`.

## Fix 3: Completion Popup Instead of Auto-Close

**Problem:** After completion, the overlay auto-closes after 2 seconds. If the user was away during a long import, they miss the result entirely.

**Fix:** Instead of `await Task.Delay(2000)` + auto-hide:

**Success path:**
1. Append result line to log
2. Stop marquee (`Blocks` + `Value=100`)
3. Set labels to completion state (Elapsed, 100%, ETA done)
4. `ShowLoadingOverlay(false)` — hide overlay forms
5. `WidgetUtils.ShowNoticePopUp($"导入完成！\n删除 {rsp.DeletedRows} 条旧记录\n插入 {rsp.InsertedRows} 条新记录\n耗时 {elapsed}")` — blocks until user clicks OK

**Error path:**
1. Determine error type: `rsp.ErrorMessage` or `ex.Message`
2. Append error line to log
3. `ShowLoadingOverlay(false)`
4. Check error type:
   - Table-lock → `WidgetUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）")`
   - Other API error → `WidgetUtils.ShowErrorPopUp($"重新导入失败：{rsp.ErrorMessage}")`
   - Client exception → `WidgetUtils.ShowErrorPopUp($"重新导入异常：{ex.Message}")`

## Fix 4: Check rsp.ErrorMessage in UI Layer (Table-Lock Popup)

**Problem:** The API `ReimportPartsBarcode` catches all exceptions internally and sets `rsp.ErrorMessage` instead of throwing. The UI's `try` block receives a successful return and treats it as success — showing "导入完成！删除 X，插入 0 条" when a table-lock aborted the operation before any inserts.

Additionally, the `IsTableLockError` check in the API's catch block runs on a background thread via `Task.Run`. Calling `ShowWarningPopUp` from a non-UI thread is unreliable in WinForms.

**Fix:**

API side (`OperationGuidanceApis.cs`): Remove `IsTableLockError` and `ShowWarningPopUp` from the catch block. Keep only the guarded rollback. The catch block becomes:
```csharp
} catch (Exception ex) {
    if (conn.State == ConnectionState.Open) {
        transaction.Rollback();
    }
    rsp.ErrorMessage = ex.Message;
}
```
Also remove the `IsTableLockError` helper method (it moves to the UI layer).

UI side (`AdminManagementView.cs`): After `await Task.Run(() => apis.ReimportPartsBarcode(req))` returns, check `rsp.ErrorMessage != null` before treating it as success:

```csharp
try {
    var rsp = await Task.Run(() => apis.ReimportPartsBarcode(req));
    _reimportStopwatch.Stop();

    if (rsp.ErrorMessage != null) {
        // API-level error (table lock, permission, connection, etc.)
        HandleReimportError(rsp.ErrorMessage);
    } else {
        // Success
        HandleReimportSuccess(rsp.DeletedRows, rsp.InsertedRows);
    }
} catch (Exception ex) {
    // Client-side exception (Task.Run failure, etc.)
    HandleReimportError(ex.Message);
}
```

Add `IsTableLockError` as a private method in `AdminManagementView`.

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Views/AdminManagementView.cs` | Modify | Owner fix, dual timer, completion popup, rsp.ErrorMessage check, IsTableLockError |
| `Controllers/OperationGuidanceApis.cs` | Modify | Remove IsTableLockError + ShowWarningPopUp from catch block |

## Data Flow

```
UI: OnReimport
  ├── Show overlay + start both timers (5s + 1s)
  ├── await Task.Run(apis.ReimportPartsBarcode)
  │     └── API catches exception → sets rsp.ErrorMessage, returns
  ├── Stop timers, stop stopwatch
  ├── rsp.ErrorMessage != null?
  │     ├── Yes → ShowLoadingOverlay(false) → IsTableLockError? → ShowWarningPopUp : ShowErrorPopUp
  │     └── No  → ShowLoadingOverlay(false) → ShowNoticePopUp("导入完成！...")
  └── finally: re-enable button
```
