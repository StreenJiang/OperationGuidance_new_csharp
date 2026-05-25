# Admin Overlay Minimize & DELETE Phase Progress

2026-05-25

## Overview

Three fixes:

1. Popup doesn't minimize with the app (taskbar icon issue) — backdrop minimizes correctly but popup doesn't follow
2. Popup doesn't close after completion — `Close()` order causes cascade exception
3. DELETE phase has no progress feedback — long-running DELETE appears stuck

## Fix 1: VisibleChanged Sync + Close Order

**Problem A:** `_overlayPopup.Owner = _overlayBackdrop` is set, but the native HWND owner relationship may not be fully established before `_overlayPopup.Show()`. The `WS_MINIMIZEBOX` style is present, but the minimize cascade from backdrop to popup doesn't fire.

**Fix:** In `ShowLoadingOverlay(true)`, after `_overlayBackdrop.Show()`, subscribe to `_overlayBackdrop.VisibleChanged`:

```csharp
private void OnBackdropVisibleChanged(object? sender, EventArgs e) {
    if (!_overlayBackdrop.Visible) {
        _overlayPopup.Hide();
    } else {
        _overlayPopup.Show();
    }
}
```

When Windows hides the backdrop (minimize), the handler hides the popup. When the backdrop becomes visible again (restore), the handler shows the popup. Belt-and-suspenders on top of the Owner chain.

In `ShowLoadingOverlay(false)`, remove the handler before closing:
```csharp
_overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
```

**Problem B:** Current close order is `_overlayPopup.Close()` then `_overlayBackdrop.Close()`. When backdrop closes, it cascades `Close()` to all owned forms — but popup is already disposed, throwing `ObjectDisposedException` and preventing backdrop from closing.

**Fix:** Close only `_overlayBackdrop`. The owner cascade handles the popup:
```csharp
} else {
    _overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
    _overlayBackdrop.Close();
}
```

## Fix 2: DELETE Phase Progress

**Problem:** `ReimportPartsBarcode` deletes all existing rows before the batch loop. The DELETE can take minutes for large tables. During this time, `OnProgress` is never called, `_latestProgress` is null, and the log shows nothing — the user sees only elapsed time ticking up and thinks it's stuck/locked.

**Fix:** Add a `Phase` field to `ReimportProgressInfo`, and send progress during the DELETE phase.

### ReimportProgressInfo changes

Add property:
```csharp
public string? Phase { get; set; }
```

Phase values: `"deleting"` | `"importing"` | `null` (pre-first-call)

### API changes

Before DELETE, send:
```csharp
req.OnProgress?.Invoke(new ReimportProgressInfo { Phase = "deleting" });
```

After DELETE and COUNT (before batch loop), update `TotalBatches` in the same progress object.

Each batch continues to send `Phase = "importing"` alongside batch counts.

### UI timer changes

In `OnProgressTimerTick`, handle the "deleting" phase:
```csharp
if (progress != null && progress.Phase == "deleting") {
    _reimportLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据...\r\n");
    // no batch/ETA update during delete
} else if (progress != null && progress.TotalBatches > 0) {
    // existing batch progress log (unchanged)
}
```

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Views/AdminManagementView.cs` | Modify | VisibleChanged handler, Close order, Phase handling in timer |
| `Models/Responses/ReimportProgressInfo.cs` | Modify | Add `Phase` property |
| `Controllers/OperationGuidanceApis.cs` | Modify | Send deleting-phase progress |

## Data Flow

```
API.Thread
├── OnProgress({ Phase = "deleting" })
├── DELETE FROM parts_bar_code
├── SELECT COUNT(*)
├── OnProgress({ Phase = "importing", TotalBatches = N, BatchCount = 0 })
├── loop batches:
│   └── OnProgress({ Phase = "importing", BatchCount, TotalInserted, ... })
└── return

UI.5sTimer
├── Phase == "deleting" → append "正在清空旧数据..."
├── Phase == "importing" → append batch log + update percent/ETA
└── _latestProgress == null → nothing (timer fires before API starts)
```
