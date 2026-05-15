# GDI+ Image Handle Concurrency Fix

## Problem

`System.ArgumentException: Parameter is not valid. at Image.get_Height()`

Call chain: `LoadOneCoverAsync` → `set_CoverImage` → `RefreshImage` → `ResizeIconImage` → `CalcImageSize`

## Root Cause

`LoadOneCoverAsync` returns a shared `Image` reference from `ProductImageCache` when no rotation is applied (no `RotateImage` clone). Multiple blocks share the same GDI+ `Image` object. UI thread's `CalcImageSize` accesses `.Height`/`.Width` without any lock, while background `RotateImage` holds `_rotateLocker`. The two locks (`_resizeLocker` vs `_rotateLocker`) don't cover each other, so concurrent access corrupts the GDI+ handle.

## Fix

### File 1: `CustomLibrary/Utils/WidgetUtils.cs`

- Change `NormalizeImageHandle` from `private` to `public`

### File 2: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`

**A. `ProductMissionBlock<T>` — add `ImageFileName` property**

```csharp
public string? ImageFileName { get; set; }
```

**B. `CoverImage` setter — clone before assign**

Change `_innerButton.Icon = value` to normalize via `WidgetUtils.NormalizeImageHandle(value, null)`, with `?? value` fallback.

**C. `InnerButton<T>.CalcImageSize` — try-catch + recovery**

Extract width calculation into `CalcWidthFromImage(Image, int)`. Catch `ArgumentException` on `.Height`/`.Width` access, call `RecoverIconHandle()`.

**D. `InnerButton<T>.RecoverIconHandle` — three-tier recovery**

1. `NormalizeImageHandle(this.Icon, null)` — try to repair corrupted handle via PNG round-trip
2. Dispose old Icon; `MainUtils.LoadProductImageFromDisk(_missionBlock.ImageFileName)` — bypass cache, fresh load from disk
3. Null out `this.Icon` and `_missionBlock._coverImage` — fall through to `_defaultImage`

Each success tier updates both `this.Icon` and `_missionBlock._coverImage` for proper disposal tracking.

### File 3: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

`LoadOneCoverAsync` — capture the last valid `side.image` filename, set `block.ImageFileName` before `block.CoverImage` in `BeginInvoke`.

## Disposal Ownership

| Object | Owner | Disposed When |
|---|---|---|
| `_coverImage` (Icon) | `ProductMissionBlock` | `Dispose()` / `OnHandleDestroyed()` / replaced during recovery |
| `ImageShowing` | `InnerButton` | `RefreshImage()` oldShowing |
| Recovery-created Image | `ProductMissionBlock` | Same as `_coverImage` (written back to field) |
