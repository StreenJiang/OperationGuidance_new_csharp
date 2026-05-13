# MissionListPanel Lazy Loading Design

**Date**: 2026-05-13
**Status**: Approved
**Author**: StreenJiang

## Problem

进入工作台/任务管理的任务列表界面时，UI 线程同步加载 30-100 个任务的封面图（磁盘 IO + Base64 编解码 + 旋转），导致整个窗口卡死数秒。

### Root Causes (identified from code analysis)

1. `RefreshMissionBlocks` 在 UI 线程同步循环加载所有封面图 — `GetProductImage`（磁盘 IO）+ `RotateImage`（像素级操作）
2. `GetProductImage` 无缓存，且做不必要的 Base64 往返（`Image → Base64 string → Image`）
3. `FetchData` 同步调用 API，阻塞 UI 线程等待网络响应

## Solution: Progressive Loading + Image Cache (方案 A)

### Architecture Overview

```
用户进入界面
  │
  ├─1. await FetchDataAsync()          ← 后台线程做 API 调用
  │     └─ CancellationTokenSource 取消上一轮
  │
  ├─2. RefreshMissionBlocks(skeleton)  ← UI 线程快速创建骨架（占位图）
  │     ├─ Dispose 旧控件 + Clear
  │     └─ 界面立即可操作
  │
  └─3. Task.WhenAll 渐进加载封面图    ← SemaphoreSlim(4) → BeginInvoke 回 UI 更新
        ├─ CancellationToken 支持取消
        ├─ IsDisposed / Parent 检查防竞态
        └─ ProductImageCache 缓存避免重复 IO
```

### Component 1: ProductImageCache

**File**: `Utils/ProductImageCache.cs` (新增)

```
static class ProductImageCache
├── ConcurrentDictionary<string, Image> _cache
├── Image? Get(string fileName)
├── Image GetOrLoad(string fileName)         // ConcurrentDictionary.GetOrAdd, disk IO on miss
├── void Invalidate(string fileName)         // 精准失效单张
├── void InvalidateByMission(ProductMissionDTO) // 遍历 ProductSides 逐个失效
└── void Clear()
```

- 线程安全：`ConcurrentDictionary.GetOrAdd` + key 为文件名
- 生命周期：应用级，进程存活期间有效

### Component 2: GetProductImage 改造

**File**: `Utils/MainUtils.cs`

- 去掉 Base64 往返路径，统一用 `MemoryStream + Graphics.DrawImage`
- 改为调用 `ProductImageCache.GetOrLoad`，不再每次读磁盘

### Component 3: RefreshMissionBlocks 渐进加载

**File**: `Views/ReusableWidgets/MissionListPanel.cs`

**新增字段**：
- `CancellationTokenSource? _loadCts`
- `SemaphoreSlim _loadSemaphore = new(4)`

**阶段1 — 骨架秒开**（方法：`RefreshMissionBlocks`）：
1. 取消上一轮 `_loadCts`，new 新的
2. Dispose 旧控件（`foreach ctrl.Dispose()`），再 `Clear()`
3. 遍历 `missionDTOs`，创建所有 `ProductMissionBlock`，封面图用 `Resources.image_choose`
4. 事件绑定照常，布局计算照常

**阶段2 — 渐进加载封面**（方法：`StartLoadingCoverImages`）：
1. `Task.WhenAll` 为每个 block 启动 `LoadOneCover(block, ct)`
2. `LoadOneCover` 内部：`await _loadSemaphore.WaitAsync(ct)` → `Task.Run` 加载图片 → `BeginInvoke` 更新
3. 回调检查：`!block.IsDisposed && block.Parent != null`
4. 捕获 `OperationCanceledException`，finally 释放 Semaphore

### Component 4: ProductMissionBlock 改造

**File**: `Views/ReusableWidgets/ProductMissionBlock.cs`

**InnerButton 新增**：
```csharp
public void RefreshImage() => ResizeIconImage();
```

**CoverImage setter 修正**：
```csharp
set {
    _coverImage = value;
    _innerButton.Icon = value;    // 修正原 _innerButton.Image（不存在的属性）
    _innerButton.RefreshImage();  // 触发 ResizeIconImage 以正确缩放
}
```

### Component 5: FetchData 异步化

**`AWorkplaceMissionView`**（主要改动）：
- `VisibleToTrue()` → `async void`，await `CheckAndDisplayAsync()`
- `CheckAndDisplay()` → `async Task CheckAndDisplayAsync()`，await `FetchDataAsync()`
- `FetchData()` → `async Task FetchDataAsync()`，`Task.Run` 包裹同步 API
- 新增 `CancellationTokenSource? _checkCts` 防并发调用
- await 后检查 `IsDisposed || !Visible` 防竞态

**`MissionManagementView*.cs`**：
- 同上模式，改 `FetchData` 为 async

### Component 6: 缓存失效

任务编辑/修改后：
```csharp
ProductImageCache.InvalidateByMission(editedMission);
```

## Files Changed

| File | Change |
|------|--------|
| `Utils/ProductImageCache.cs` | **New** — static thread-safe image cache |
| `Utils/MainUtils.cs` | Remove Base64 round-trip; go through cache |
| `Views/ReusableWidgets/MissionListPanel.cs` | Split `RefreshMissionBlocks` into skeleton + async loading; `_loadCts`, `_loadSemaphore`; dispose old controls before Clear |
| `Views/ReusableWidgets/ProductMissionBlock.cs` | `InnerButton.RefreshImage()`; fix `CoverImage` setter |
| `Views/AbstractViews/AWorkplaceMissionView.cs` | `VisibleToTrue` async void; `CheckAndDisplayAsync`; `FetchDataAsync`; `_checkCts` |
| `Views/*/MissionManagementView*.cs` | `FetchData` → `FetchDataAsync` |
| Business edit/modify callers | Call `ProductImageCache.InvalidateByMission()` after edits |

## Key Design Decisions

1. **SemaphoreSlim(4) at instance level** — limits concurrent disk IO, lifecycle tied to panel
2. **`Task.WhenAll` per block, not `Parallel.ForEach`** — better CancellationToken integration
3. **`BeginInvoke` callback guards** — `!block.IsDisposed && block.Parent != null`
4. **Dispose old controls before `Clear()`** — prevents orphaned controls staying alive via closures
5. **`async void` for `VisibleToTrue`** — WinForms lifecycle method standard pattern, with internal try-catch
6. **`_checkCts` at `AWorkplaceMissionView`** — caller owns lifecycle, cancels previous round on re-entry
7. **`GetProductImage` Base64 round-trip removed** — `MemoryStream + Graphics.DrawImage` is equivalent and faster

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| 缓存图片内存增长 (30-100 × ~100KB ≈ 10MB) | 可接受；长期可加 LRU 上限 |
| 任务编辑后图片未刷新 | 业务调用方负责 `InvalidateByMission` |
| 快速切换界面控件已 Dispose | `IsDisposed` + `Parent != null` 双重检查 |
| `async void` 异常被吞 | try-catch + 日志记录 |
| 旧轮 Task 完成时更新旧 block | `_loadCts.Cancel()` + `Clear()` 前 Dispose |
