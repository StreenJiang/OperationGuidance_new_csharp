# DELETE Progress Enhancement & Close Button Fix

2026-05-25

## Overview

Four fixes:

1. Close button doesn't work — `_overlayPopup` is `OverlayForm` with `WM_MOUSEACTIVATE → MA_NOACTIVATE` which interferes with child control interaction
2. DELETE phase shows no ratio (should show "5000/10000 行")
3. ETA should cover entire operation (DELETE + import), not just import phase
4. No transition log between DELETE and import phases

## Fix 1: Close Button

**Problem:** `_overlayPopup` uses `OverlayForm` type, inheriting the `WM_MOUSEACTIVATE → MA_NOACTIVATE` override designed for the backdrop. This prevents proper mouse interaction with the close Button child control.

**Fix:** Create `_overlayPopup` as a plain `Form` instead of `OverlayForm`. The `OverlayForm` type is only needed for the backdrop (to block activation on click). The popup only needs standard Form behavior for button interaction.

Close button click handler directly disposes both forms:

```csharp
_closeBtn.Click += (s, e) => {
    if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
        _overlayPopup.Owner = null;
        _overlayPopup.Close();
    }
    if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
        _overlayBackdrop.Close();
    }
};
```

## Fix 2: TotalToDelete + Unified ETA

### DTO change

Add to `ReimportProgressInfo`:
```csharp
public int TotalToDelete { get; set; }
```

### API change

Before DELETE loop, COUNT the rows, then include `TotalToDelete` in each deleting-phase progress call.

### UI timer change

Use `TotalToDelete` to calculate percentage and ETA during deleting phase, same formula as importing phase.

ETA formula is unified across both phases:
- **deleting:** `completed = DeletedCount`, `total = TotalToDelete`
- **importing:** `completed = BatchCount`, `total = TotalBatches`
- Both: `elapsed / completed * (total - completed)` → remaining time
- ETA label shows: `预计剩余: hh:mm:ss，预计结束: HH:mm:ss`

Deleting log shows ratio: `正在清空旧数据... 已删除 5000/10000 行 (50%), 已耗时 00:00:15`

## Fix 3: Wider Overlay

- Popup width: `460 → 600`
- LogBox width: `396 → 536`
- ProgressBar width: `396 → 536`

## Fix 4: Transition Log

Detect phase transition from "deleting" to "importing" in `OnProgressTimerTick` using a `_lastPhase` field. On transition, append:

```
[HH:mm:ss] 删除完成，开始导入物料码...
```

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `ReimportProgressInfo.cs` | Modify | Add `TotalToDelete` |
| `OperationGuidanceApis.cs` | Modify | COUNT before DELETE, include `TotalToDelete` in progress |
| `AdminManagementView.cs` | Modify | Form popup, close button, wider, unified ETA, transition log |
