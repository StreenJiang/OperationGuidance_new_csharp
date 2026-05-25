# Delete Count Log, Timer Order, Alt+F4 Backdrop Sync

2026-05-25

## Overview

Three small fixes:

1. Transition log doesn't show how many rows were deleted
2. Progress labels appear before first log line (timer ordering)
3. Alt+F4 / keyboard close only closes popup, not backdrop

## Fix 1: Delete Count in Transition Log

Add `_lastDeletedCount` field, update it on each deleting-phase tick. Transition log includes the count:

```
[14:30:15] 删除完成，共删除 5000 条，开始导入物料码...
```

## Fix 2: Timer First-Fire Order

`_progressTimer` starts with `Interval = 1000` (1s), first tick writes log immediately. At end of first tick, change to `Interval = 5000` (5s) for subsequent ticks. Ensures first log line appears at ~1s, roughly synchronized with first StatusTimer tick.

No new flag fields needed.

## Fix 3: Alt+F4 Closes Backdrop Too

In `finally`, after removing old FormClosing handlers, add a new one on popup that closes backdrop on any close reason:

```csharp
_overlayPopup.FormClosing += (s, e) => {
    if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
        _overlayBackdrop.Close();
    }
};
```

This handles both button click and Alt+F4.

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `AdminManagementView.cs` | Modify | `_lastDeletedCount`, timer Interval, popup FormClosing handler |
