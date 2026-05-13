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

### ProductMissionBlock Image Update

`CoverImage` setter sets `_innerButton.Icon` and calls `_innerButton.RefreshImage()`.
`RefreshImage()` calls `ResizeIconImage()`, updates `_imageBorderRect`, disposes old `ImageShowing`, and `Invalidate()`.

### Async Data Fetch

`AWorkplaceMissionView.VisibleToTrue` → `async void`, awaits `CheckAndDisplayAsync` → `FetchDataAsync` (`Task.Run` wrapping sync API). Cancellation via `_checkCts`. Guards: `IsDisposed || !Visible` after await. Same pattern in `MissionManagementView` and `MissionManagementView_SCII`.

### Cache Invalidation

After `SaveProductImage`, call `ProductImageCache.Invalidate(fileName)` in both `MissionEditionView` and `MissionEditionView_SCII`.

## Docs

- `docs/superpowers/specs/` — Design specs
- `docs/superpowers/plans/` — Implementation plans
