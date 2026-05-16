# OperationGuidance_new

WinForms-based operation guidance system with multi-site support (WHYC, SCII, GLB, YF, TZYX).

## Build

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

## Project Structure

```
OperationGuidance_new/
├── Program.cs, MainForm.cs
├── Views/
│   ├── AbstractViews/        # AWorkplaceMissionView<T,V>, AWorkplaceContentPanel
│   ├── ReusableWidgets/      # MissionListPanel, ProductMissionBlock, TitlePanel
│   └── SubViews/             # DeviceBlock, ProductImageDisplayPanel
├── Utils/                    # MainUtils, ProductImageCache
├── CustomLibrary/            # Shared WinForms controls (buttons, panels, utils)
│   ├── Buttons/AbstractClasses/  # AbstractCustomButton → AbstractCustomImageTextButton
│   ├── Panels/BaseClasses/       # CustomContentPanel, CustomVScrollingContentPanel
│   └── Utils/                    # WidgetUtils (ResizeImage, RotateImage)
└── OperationGuidance_service/ # DTOs, Controllers, API client
    └── Models/DTOs/           # ProductMissionDTO, ProductSideDTO
```

## Key Patterns

### Image Loading (v1.5.7+)

Images load via `ProductImageCache` (thread-safe `ConcurrentDictionary<string, Image>` in `Utils/`).

`MainUtils.LoadProductImageFromDisk` uses Base64 PNG round-trip through `CommonUtils.ImageToBase64` / `ImageBase64ToImage`. The round-trip normalizes GDI+ image data; skipping it (e.g. `MemoryStream` + `Graphics.DrawImage` copy) causes incorrect image display. Fallback path handles OOM.

### Mission List Lazy Loading

`MissionListPanel.RefreshMissionBlocks` uses two-phase loading:
1. Skeleton UI — all blocks created instantly with placeholder images
2. Background cover loading — `SemaphoreSlim(4)` + `Task.Run` + `BeginInvoke` back to UI

`StartLoadingCoverImages` fires one `LoadOneCoverAsync` per block. Cancellation via `CancellationTokenSource _loadCts`. Dispose guard: `!block.IsDisposed && block.Parent != null`.

When mission IDs are unchanged (e.g., after image edit), blocks are NOT rebuilt. Instead, existing blocks get their `Entity` references synced to the new DTOs and cover loading re-triggered — this picks up new images from disk after `ProductImageCache.Invalidate`.

### ProductMissionBlock Image Update

`CoverImage` setter clones incoming `Image` via `new Bitmap(value)` before storing in both `_coverImage` and `_innerButton.Icon`. On `new Bitmap` failure (corrupted handle), falls back to `NormalizeImageHandle`. Old `_coverImage` is disposed before replacement (blocks may be reused when mission IDs are unchanged after image edit).

`RefreshImage()` calls `ResizeIconImage()`, updates `_imageBorderRect`, disposes old `ImageShowing`, and `Invalidate()`.

`CalcImageSize` wraps `CalcImageDimensions` (tuple-returning width/height helper) in try-catch for `ArgumentException`. On corrupted handle, calls `RecoverIconHandle` — three-tier recovery: NormalizeImageHandle PNG repair → `LoadProductImageFromDisk` bypassing cache → fall through to `_defaultImage`. Recovery updates both `this.Icon` and `_missionBlock._coverImage`.

`ImageFileName` property bridges filename from `LoadOneCoverAsync` to `RecoverIconHandle` for Tier 2 disk reload.

### Async Data Fetch

`AWorkplaceMissionView.VisibleToTrue` → `async void`, awaits `CheckAndDisplayAsync` → `FetchDataAsync` (`Task.Run` wrapping sync API). Cancellation via `_checkCts`. Guards: `IsDisposed || !Visible` after await. Same pattern in `MissionManagementView` and `MissionManagementView_SCII`.

### Cache Invalidation

After `SaveProductImage`, call `ProductImageCache.Invalidate(fileName)` in `MissionEditionView`, `MissionEditionView_SCII`, and `MissionEditionView_SCII_XT` (all three edition views).

### Startup Lazy View Loading

`AfterLogin` menu loop creates buttons and panels eagerly; views deferred to first click via `WireLazyLoader`. Each lazily-loaded menu item must:
1. Create a lightweight placeholder `CustomContentPanel`, assign to `button.CorrespondingContentPanel`
2. Call `WireLazyLoader(parentPanel, button, viewType, viewName)` — subscribes one-shot Click that replaces placeholder with real view
3. Call `WidgetUtils.RegisterLazyView(viewType, viewName, button, parentPanel)` for cross-view reference fallback

`WireLazyLoader`: on click, checks `button.CorrespondingContentPanel is CustomVScrollingContentPanel` (already created) → return. Otherwise dispose placeholder, call `CreateViewPanel` (delegates to `WidgetUtils.CreateContentView`), add wrapper to parent, set `Visible` if toggled.

### Lazy View Registry

`WidgetUtils.RegisterLazyView` stores creation params in `ConcurrentDictionary<Type, LazyViewInfo>`. `GetView<V>()` searches `_views` list first, then falls back to on-demand creation via `CreateViewInstance` (calls `CreateContentView`, adds wrapper to parent panel, removes from lazy registry). `ClearViews()` clears both collections for re-login.

### CustomVScrollingContentPanel ResizeChildren Timing

Must call `_contentPanel.ResizeChildren()` BEFORE `_contentPanel.CheckNeedsScrollBar()`. Set preliminary width on content panel first so sub-panel heights are computed correctly. After final sizing, if content height was determined by `NewHeight`, call `_contentPanel.ResizeChildren()` again. Skipping this causes clipped content in lazy-loaded views.

### RotateImage dispose Default

`WidgetUtils.RotateImage` has `dispose = true` default. When passing a cached `Image` reference (from `ProductImageCache`), always pass `dispose: false`. Otherwise `RotateImage` destroys the cached Image, causing `ArgumentException: "parameter is not valid"` on all subsequent Width/Height accesses to that same cached object.

`RotateImage` holds `lock (_rotateLocker)` for the entire operation. `ResizeImage` holds `lock (_resizeLocker)` — two separate locks so resize and rotate don't block each other. The lock is required because GDI+ `Image` is not thread-safe and `ProductImageCache` may return the same object to concurrent callers (e.g., `MissionListPanel` background cover loading).

### GDI+ Handle Recovery

`WidgetUtils.NormalizeImageHandle` wraps PNG stream encode/decode as a shared GDI+ handle repair utility. Both `ResizeImage` and `RotateImage` recovery paths use it when `DrawImage` or `.Width`/`.Height` throws `ArgumentException`.

### CustomVScrollingContentPanel VisibleToTrue Propagation

`CustomVScrollingContentPanel.VisibleToTrue()` calls `_contentPanel.VisibleToTrue()` before sizing itself. This ensures wrapped views can react every time the panel becomes visible (menu toggle), not just on first creation. The call runs before `Size` is set so disposed child pages are recreated before resize touches them.

Views that override `VisibleToTrue` (e.g. `MissionEditionView` recreates disposed page, `MissionManagementView` refreshes mission list) now work correctly on every menu switch. Async views already have `_checkCts` cancellation guards; the propagation makes those guards actually exercised.

### MissionEditionView_SCII_XT Field Hiding

XT inherits from SCII and uses `new` on `_editionPage` / `EditionPage`. Base class methods resolve `_editionPage` to the base field (always null in XT). Any base method touching `_editionPage` must be overridden in XT with identical body — the duplicate is intentional because field access resolves at compile time. Currently overridden: `OpenEditionPage`, `CreateANewOne`, `VisibleToTrue`, `ResizeChildren`.

### ProductImageCache Null Policy

`GetOrLoad` uses `TryGetValue` + `TryAdd` — null is never cached. Corrupted files retry disk load on next access instead of being permanently broken in cache. This differs from `GetOrAdd` which would cache null forever.

### CustomPopUpForm.Show() Is Blocking

`CustomPopUpForm.Show()` (not `ShowDialog()`) internally calls `base.ShowDialog()` when `ShowInFront == true` (the default). The call blocks until `Hide()`/`Dispose()` is called on the form. This means any `form.Show()` on a `CustomPopUpForm` subclass (including `WaitDialog`) is a modal blocking call — code after it won't execute until the form closes.

### Image Cache Ownership (anti-poisoning)

Never dispose a shared `ProductImageCache` reference. Three defenses:

1. **`ProductMissionBlock.CoverImage` setter/constructor** — clones incoming Image via `new Bitmap()`. `_coverImage` is always a private copy, safely disposed on block Dispose or re-set.
2. **`ProductImageFile._ownsImage`** — defaults `true`. Set `false` only in constructor (holds cache ref). `Dispose()` checks flag. `Image` setter snapshots old value before reassign, disposes old only if it was owned. `ReloadImage()` clones cache result via `new Bitmap()` so it always returns an owned image.
3. **`AWorkplaceContentPanel` side switch** — calls `RefreshImage()` directly (which creates its own display clone via `GetDisplayImage` → `TryCreateDisplayImage`). Never passes raw cache refs to `AProductImageDisplayPanel.SetImage` (which unconditionally disposes its old `_productImage`).

## Docs

- `docs/superpowers/specs/` — Design specs
- `docs/superpowers/plans/` — Implementation plans
