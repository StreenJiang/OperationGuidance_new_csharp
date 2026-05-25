# Admin Overlay Minimize & Lifecycle Fix

2026-05-25

## Problem

Three related bugs in the reimport overlay:

1. **Popup doesn't minimize with the app** — clicking the taskbar icon minimizes the main form and backdrop, but the popup stays visible. The app can't be restored because Windows thinks it's not fully minimized. Root cause: `FormBorderStyle.None` strips `WS_MINIMIZEBOX` from the Win32 window style, so Windows doesn't cascade minimize to borderless owned forms.

2. **Backdrop lingers after completion popup** — closing the completion dialog leaves the backdrop visible. Root cause: `Hide()` only sets `Visible = false` without destroying the form; a hidden borderless form in a broken owner state may reappear.

3. **Null reference on second reimport click** — `Close()` + `Dispose()` from the previous execution destroys the forms, but `ShowLoadingOverlay(true)` assumes they still exist. Root cause: no recreate-on-demand mechanism.

## Solution

### Fix 1: Add WS_MINIMIZEBOX to borderless forms

Rename `OverlayBackdropForm` → `OverlayForm`. Add `CreateParams` override to re-add the `WS_MINIMIZEBOX` (0x20000) style:

```csharp
internal sealed class OverlayForm : Form {
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;
    private const int WS_MINIMIZEBOX = 0x20000;

    protected override CreateParams CreateParams {
        get {
            CreateParams cp = base.CreateParams;
            cp.Style |= WS_MINIMIZEBOX;
            return cp;
        }
    }

    protected override void WndProc(ref Message m) {
        if (m.Msg == WM_MOUSEACTIVATE) {
            m.Result = (IntPtr)MA_NOACTIVATE;
            return;
        }
        base.WndProc(ref m);
    }
}
```

Both `_overlayBackdrop` and `_overlayPopup` use this type. The `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` behavior is harmless on the popup (it already sits above the backdrop and can't be clicked-through).

### Fix 2: Close() lifecycle + on-demand creation

**Constructor** — no longer creates overlay forms. Instead calls `CreateOverlayForms()` which creates both forms with all child controls.

**`CreateOverlayForms()`** — new private method:
- If `_overlayBackdrop` exists and is not disposed, return (no-op)
- Otherwise: create `_overlayBackdrop` (OverlayForm), `_overlayPopup` (OverlayForm), all child controls (titleLabel, _reimportLogBox, _reimportProgressBar, _elapsedLabel, _percentLabel, _etaLabel), Resize handler
- Set `_overlayPopup.Owner = _overlayBackdrop` in the constructor (belt-and-suspenders)

**`ShowLoadingOverlay(true)`**:
1. Call `CreateOverlayForms()` — recreates if previously closed
2. Set `_overlayBackdrop.Owner = mainForm` (after backdrop.Show if handle wasn't ready)
3. Show backdrop, set popup.Owner = backdrop, set location, show popup

**`ShowLoadingOverlay(false)`**:
```csharp
_overlayPopup.Close();
_overlayBackdrop.Close();
```

`Close()` destroys the forms (disposed = true). Next `ShowLoadingOverlay(true)` → `CreateOverlayForms()` recreates them.

### Fix 3: Set Owner after Show (defensive)

In `ShowLoadingOverlay(true)`:
```csharp
_overlayBackdrop.Show();
_overlayPopup.Owner = _overlayBackdrop;  // set after backdrop has handle
_overlayPopup.Show();
```

The constructor also sets `_overlayPopup.Owner = _overlayBackdrop` (managed-level, get/set deferred), but the runtime assignment after Show ensures the native HWND relationship is definitely in place.

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Views/AdminManagementView.cs` | Modify | Rename class, CreateParams, CreateOverlayForms, Close lifecycle |

## Data Flow

```
Constructor
  └── CreateOverlayForms() → creates both OverlayForm instances + all child controls

OnReimport click
  └── ShowLoadingOverlay(true)
        ├── CreateOverlayForms() → no-op if not disposed, recreate if was Closed
        ├── _overlayBackdrop.Owner = mainForm
        ├── _overlayBackdrop.Show()
        ├── _overlayPopup.Owner = _overlayBackdrop  ← handle exists now
        ├── _overlayPopup.Show()
        └── ... import runs ...

Completion / Error
  └── ShowLoadingOverlay(false)
        ├── _overlayPopup.Close()    → destroys popup, disposes child controls
        └── _overlayBackdrop.Close() → destroys backdrop
```
