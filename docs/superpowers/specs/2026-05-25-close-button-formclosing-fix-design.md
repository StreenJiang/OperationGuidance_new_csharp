# Close Button FormClosing Fix & Wider Overlay

2026-05-25

## Overview

Two fixes:

1. Close button still doesn't work — `FormClosing` handler cancels `UserClosing`, and the button's programmatic `Close()` call is flagged as `UserClosing`
2. Overlay width: 600 → 700

## Fix 1: Remove FormClosing handlers before enabling close button

**Root cause:** When the user clicks the close button, WinForms treats the subsequent `_overlayPopup.Close()` as `CloseReason.UserClosing`. The `FormClosing` handler (`if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true`) cancels the close.

**Fix:** Before enabling the close button, unsubscribe both `FormClosing` handlers. Extract the lambdas into named methods for unsubscription:

- `OnOverlayFormClosing` (for backdrop)
- `OnPopupFormClosing` (for popup)

In `OnReimport` finally block, before `_closeBtn.Enabled = true`:
```csharp
_overlayBackdrop.FormClosing -= OnOverlayFormClosing;
_overlayPopup.FormClosing -= OnPopupFormClosing;
```

## Fix 2: Width 700

- Popup: `Width = 700`
- LogBox, ProgressBar: `Width = 636` (700 - 32×2)

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `AdminManagementView.cs` | Modify | Named FormClosing handlers, remove in finally, wider |
