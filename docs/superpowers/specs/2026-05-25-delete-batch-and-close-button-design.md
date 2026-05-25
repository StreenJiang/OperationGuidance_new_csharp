# DELETE Batch Progress & Overlay Close Button

2026-05-25

## Overview

Two fixes:

1. DELETE timeout — single `DELETE FROM parts_bar_code` exceeds the 10-second command timeout for large tables, causing "Fatal error encountered during command execution."
2. Overlay dismissal — programmatic close via owner cascade is unreliable for borderless forms. Replace with a user-controlled close button.

## Fix 1: Batch DELETE with Progress

**Problem:** `AWrapperBase.commandTimeout = 10` seconds. A single `DELETE FROM parts_bar_code WHERE deleted = 0` on a large table hits this limit and throws a MySQL timeout error.

**Fix:** Instead of one big DELETE, loop with `LIMIT 1000` (MySQL) or `DELETE TOP(1000)` (SQL Server). Each batch stays under 10 seconds. Invoke `OnProgress` after each batch to show running count.

### ReimportProgressInfo changes

Add property:
```csharp
public int DeletedCount { get; set; }
```

### API changes (OperationGuidanceApis.cs)

Replace the single DELETE with a batch loop:

```
Phase = "deleting"
while true:
    DELETE 1000 rows WHERE deleted = 0
    if rows affected == 0: break
    DeletedCount += rows affected
    OnProgress({ Phase = "deleting", DeletedCount })
Phase = "importing"
// ... existing batch insert loop (unchanged) ...
```

### UI timer changes (AdminManagementView.cs)

In `OnProgressTimerTick`, update the "deleting" phase to show running count:
```
[HH:mm:ss] 正在清空旧数据... 已删除 5000 行, 已耗时 00:00:15
```

## Fix 2: Close Button on Overlay

**Problem:** `ShowLoadingOverlay(false)` only closes `_overlayBackdrop`, relying on Win32 owner cascade to close `_overlayPopup`. For borderless forms this cascade is unreliable, leaving the popup stuck after error dismissal.

**Fix:** Add a "关闭" (Close) button to the overlay popup. User dismisses overlay manually after reading the log.

- **During import:** Button shows "正在导入...", disabled
- **After completion or error:** Button shows "关闭", enabled
- **On click:** Calls `ShowLoadingOverlay(false)` to dismiss both forms

### Remove separate popups

- Remove `ShowNoticePopUp` (success popup), `ShowErrorPopUp` (error popup), `ShowWarningPopUp` (lock warning)
- Remove `IsTableLockError` method — no longer needed
- All result info is already written to `_reimportLogBox` in OnReimport; no additional popup needed

### ShowLoadingOverlay(false) — close both forms

Replace the current close-only-backdrop approach. Break owner chain before closing to avoid cascade ObjectDisposedException:

```csharp
} else {
    _overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
    if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
        _overlayPopup.Owner = null;
        _overlayPopup.Close();
    }
    if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
        _overlayBackdrop.Close();
    }
}
```

## Data Flow

```
API.Thread
├── OnProgress({ Phase = "deleting", DeletedCount = 0 })
├── DELETE 1000 rows → OnProgress({ Phase = "deleting", DeletedCount = 1000 })
├── DELETE 1000 rows → OnProgress({ Phase = "deleting", DeletedCount = 2000 })
├── ... repeat until no more rows ...
├── OnProgress({ Phase = "importing", BatchCount = 0, TotalBatches = N })
├── loop batches (unchanged)
└── return

UI.5sTimer
├── Phase == "deleting" → append "正在清空旧数据... 已删除 N 行, 已耗时 ..."
├── Phase == "importing" → append batch log + update percent/ETA
└── _latestProgress == null → nothing

OnReimport completion (success or error):
├── Stop timers
├── Append final log line
├── Update progress bar + labels
├── _closeBtn.Text = "关闭"
├── _closeBtn.Enabled = true
└── (no popup — user reads log and clicks close button)

Close button click → ShowLoadingOverlay(false):
├── Popup.Owner = null
├── Popup.Close()
├── Backdrop.Close()
```

## OnReimport (simplified flow)

```
try:
    rsp = await Task.Run(() => apis.ReimportPartsBarcode(req))
    stop timers
    if rsp.ErrorMessage != null:
        log error to _reimportLogBox
        update progress bar + labels to error state
    else:
        log success to _reimportLogBox
        update progress bar + labels to 100%
    _closeBtn.Text = "关闭"
    _closeBtn.Enabled = true
catch Exception ex:
    stop timers
    log exception to _reimportLogBox
    update progress bar + labels to error state
    _closeBtn.Text = "关闭"
    _closeBtn.Enabled = true
finally:
    _reimportBtn.Enabled = true
```

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Models/Responses/ReimportProgressInfo.cs` | Modify | Add `DeletedCount` property |
| `Controllers/OperationGuidanceApis.cs` | Modify | Batch DELETE loop with progress |
| `Views/AdminManagementView.cs` | Modify | Close button, deleted count in timer, remove popups, close both forms |
