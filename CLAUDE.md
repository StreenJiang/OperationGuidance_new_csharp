---
allowed-tools:
  - mcp__codegraph__codegraph_search
  - mcp__codegraph__codegraph_context
  - mcp__codegraph__codegraph_callers
  - mcp__codegraph__codegraph_callees
  - mcp__codegraph__codegraph_impact
  - mcp__codegraph__codegraph_node
  - mcp__codegraph__codegraph_explore
  - mcp__codegraph__codegraph_files
  - mcp__codegraph__codegraph_status
---

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

## Database

### MySQL minimum version: 5.7

All SQL migration scripts in `OperationGuidance_service/Database/sqls/modify_mysql_*.sql` MUST be
compatible with MySQL 5.7. Key implications:

- **`ALTER TABLE` is NOT preparable.** `PREPARE stmt FROM @sql` only supports `CREATE INDEX`,
  `DROP INDEX`, `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `CREATE TABLE`, `DROP TABLE`, `SET`.
  Any `ALTER TABLE` wrapped in `PREPARE`/`EXECUTE` will fail on MySQL 5.7.
- **No `CREATE INDEX IF NOT EXISTS`.** Use direct `CREATE INDEX`; idempotency is provided by
  the engine layer (`MySqlConnector.cs` catches error codes 1060/1061/1091 as benign).
- **No `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`.** Use direct `ADD COLUMN`; engine layer
  handles `Duplicate column name` (1060).
- **`datetime` type without fsp precision is safe.** `datetime` = `datetime(0)` on 5.7.
- **Migration scripts are split by `;`** in `MySqlConnector.cs:92`. Do NOT put semicolons
  inside SQL comments or string literals in migration files.
- **Write plain DDL — no `INFORMATION_SCHEMA` + `PREPARE`/`EXECUTE` wrappers.** The engine
  layer catches MySQL error codes 1060 (Duplicate column), 1061 (Duplicate key), and 1091
  (Can't DROP) as benign and logs them at INFO level. Migration scripts do not need their
  own idempotency logic.

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

## Docs

- `docs/superpowers/specs/` — Design specs
- `docs/superpowers/plans/` — Implementation plans

## Agent skills

### Issue tracker

GitHub Issues（`StreenJiang/OperationGuidance_new_csharp`），通过 `gh` CLI 操作。详见 `docs/agents/issue-tracker.md`。

### Triage labels

使用标准默认标签：`needs-triage`、`needs-info`、`ready-for-agent`、`ready-for-human`、`wontfix`。详见 `docs/agents/triage-labels.md`。

### Domain docs

单上下文仓库（single-context）——`CONTEXT.md` + `docs/adr/` 位于仓库根目录。详见 `docs/agents/domain.md`。
