# MissionListPanel 懒加载改造 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 MissionListPanel 的封面图加载从 UI 线程同步 IO 改为后台异步渐进加载 + 内存缓存，消除进入任务列表界面时的 UI 卡死。

**Architecture:** 新增 ProductImageCache 静态缓存层；MainUtils.GetProductImage 改为走缓存并去掉 Base64 往返；ProductMissionBlock 修正 CoverImage setter 增加 RefreshImage；MissionListPanel 拆分骨架秒开 + SemaphoreSlim(4) 异步加载；AWorkplaceMissionView / MissionManagementView 的 FetchData 改为 async Task。

**Tech Stack:** WinForms, System.Drawing, System.Threading.Tasks (SemaphoreSlim, CancellationTokenSource), ConcurrentDictionary

---

## Task 1: 新增 ProductImageCache

**Files:**
- Create: `OperationGuidance_new/Utils/ProductImageCache.cs`

- [ ] **Step 1: 创建 ProductImageCache**

```csharp
using OperationGuidance_service.Models.DTOs;
using System.Collections.Concurrent;

namespace OperationGuidance_new.Utils {
    public static class ProductImageCache {
        private static readonly ConcurrentDictionary<string, Image> _cache = new();

        public static Image? Get(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return null;
            _cache.TryGetValue(fileName, out var image);
            return image;
        }

        public static Image? GetOrLoad(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return null;
            return _cache.GetOrAdd(fileName, key => MainUtils.LoadProductImageFromDisk(key));
        }

        public static void Invalidate(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return;
            if (_cache.TryRemove(fileName, out var image)) {
                image?.Dispose();
            }
        }

        public static void InvalidateByMission(ProductMissionDTO mission) {
            if (mission?.ProductSides == null) return;
            foreach (var side in mission.ProductSides) {
                if (!string.IsNullOrEmpty(side.image))
                    Invalidate(side.image);
            }
        }

        public static void Clear() {
            foreach (var kv in _cache) {
                kv.Value?.Dispose();
            }
            _cache.Clear();
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/ProductImageCache.cs
git commit -m "feat: add ProductImageCache for thread-safe image caching"
```

---

## Task 2: 改造 GetProductImage — 去掉 Base64 往返 + 走缓存

**Files:**
- Modify: `OperationGuidance_new/Utils/MainUtils.cs:224-252`

- [ ] **Step 1: 将 GetProductImage 改为走缓存，新增 LoadProductImageFromDisk**

替换 `GetProductImage` 方法 (第 224-252 行)：

```csharp
public static Image? GetProductImage(string? fileName) {
    return ProductImageCache.GetOrLoad(fileName);
}

/// <summary>
/// 从磁盘加载图片。通过 MemoryStream + Graphics.DrawImage 复制图片，防止锁定图片文件。
/// </summary>
internal static Image? LoadProductImageFromDisk(string? fileName) {
    if (string.IsNullOrEmpty(fileName)) return null;
    string imageFilePath = GetProductImagesPath() + "\\" + fileName;
    if (!File.Exists(imageFilePath)) return null;

    using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(imageFilePath)))
    using (Bitmap bitmap = new Bitmap(ms)) {
        Bitmap newBitmap = new(bitmap.Width, bitmap.Height, bitmap.PixelFormat);
        using (Graphics g = Graphics.FromImage(newBitmap)) {
            g.DrawImage(bitmap, Point.Empty);
            g.Flush();
        }
        return newBitmap;
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/MainUtils.cs
git commit -m "perf: route GetProductImage through cache, remove Base64 round-trip"
```

---

## Task 3: 修正 ProductMissionBlock — CoverImage setter + RefreshImage

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs:23-29` (CoverImage setter)
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs:131` (InnerButton, add method)

- [ ] **Step 1: 修正 CoverImage setter（第 23-29 行）**

旧代码：
```csharp
public Image? CoverImage {
    get => _coverImage;
    set {
        _coverImage = value;
        _innerButton.Image = value;
    }
}
```

改为：
```csharp
public Image? CoverImage {
    get => _coverImage;
    set {
        _coverImage = value;
        _innerButton.Icon = value;
        _innerButton.RefreshImage();
    }
}
```

- [ ] **Step 2: 在 InnerButton 类新增 RefreshImage 方法（在 InnerButton 类末尾，第 203 行之前）**

```csharp
public void RefreshImage() => ResizeIconImage();
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs
git commit -m "fix: add RefreshImage() and fix CoverImage setter to use Icon property"
```

---

## Task 4: MissionListPanel 骨架秒开 + 异步渐进加载

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

**新增字段** (第 15 行附近，`_titleHeight` 之后)：

- [ ] **Step 1: 新增字段和 Dispose 模式**

```csharp
private CancellationTokenSource? _loadCts;
private readonly SemaphoreSlim _loadSemaphore = new(4);
```

在类尾部（第 211 行之前）新增 Dispose 重写：

```csharp
protected override void Dispose(bool disposing) {
    if (disposing) {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        // _loadSemaphore intentionally NOT disposed — pending async tasks may Release() after Cancel
    }
    base.Dispose(disposing);
}
```

- [ ] **Step 2: 修改 RefreshMissionBlocks — 阶段1（骨架秒开）**

替换第 79-141 行的 `RefreshMissionBlocks` 方法：

```csharp
public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
    // Cancel previous round of image loading
    _loadCts?.Cancel();
    _loadCts?.Dispose();
    _loadCts = new CancellationTokenSource();
    var ct = _loadCts.Token;

    if (missionDTOs.Count > 0) {
        _contentPanel.BigButtonPanel.Hide();
        _contentPanel.MissionsTable.Show();

        // Dispose old controls to prevent memory leaks (ToList avoids collection-modified-during-enumeration)
        _currentToggledMission = null;
        var oldControls = _contentPanel.MissionsTable.Controls.Cast<Control>().ToList();
        foreach (Control ctrl in oldControls) {
            ctrl.Dispose();
        }
        _contentPanel.MissionsTable.Controls.Clear();

        for (int i = 0; i < missionDTOs.Count; i++) {
            ProductMissionDTO mission = missionDTOs[i];
            if (mission.ProductSides != null && mission.ProductSides.Count > 0) {
                // 骨架：使用占位图，不加载真实封面
                ProductMissionBlock<ProductMissionDTO> block = new(
                    mission,
                    null,                         // 不加载封面图
                    Properties.Resources.image_choose,
                    mission.name,
                    ColorConfigs.COLOR_MISSION_BLOCK_BORDER,
                    ColorConfigs.COLOR_MISSION_BLOCK_BACKGROUND,
                    ColorConfigs.COLOR_MISSION_BLOCK_IMAGE_BORDER
                ) {
                    Parent = _contentPanel.MissionsTable,
                };
                block.InnerButton.ToggledButton = toggleBlock;
                block.InnerButton.ToggledColor = WidgetUtils.DarkenColor(block.BackColor, .2);
                block.InnerButton.MouseUp += (sender, eventArgs) => {
                    if (block.InnerButton.ToggledButton) {
                        if (_currentToggledMission == null) {
                            _currentToggledMission = block;
                        } else {
                            _currentToggledMission.InnerButton.SetToggle(false);
                            if (_currentToggledMission == block) {
                                _currentToggledMission = null;
                            } else {
                                _currentToggledMission = block;
                                _currentToggledMission.InnerButton.SetToggle(true);
                            }
                        }
                    }
                    if (blockClickAction != null) {
                        blockClickAction(block.Entity.id);
                    }
                };
            }
        }
        if (!_missionDTOs.Select(m => m.id).SequenceEqual(missionDTOs.Select(m => m.id))) {
            _missionDTOs = missionDTOs;
            _contentOuterPanel.ResizeChildren();
        }

        // 阶段2：启动后台异步加载封面图
        StartLoadingCoverImages(ct);
    } else {
        _contentPanel.MissionsTable.Hide();
        _contentPanel.BigButtonPanel.Show();
    }
    _contentPanel.ResizeCells();
}
```

- [ ] **Step 3: 新增 StartLoadingCoverImages 和 LoadOneCover 方法**

在 `RefreshMissionBlocks` 之后新增：

```csharp
private void StartLoadingCoverImages(CancellationToken ct) {
    var blocks = MissionBlocks;
    foreach (var block in blocks) {
        _ = LoadOneCoverAsync(block, ct);
    }
}

private async Task LoadOneCoverAsync(ProductMissionBlock<ProductMissionDTO> block, CancellationToken ct) {
    await _loadSemaphore.WaitAsync(ct);
    try {
        // 背景线程加载图片（磁盘 IO + 旋转）
        Image? image = await Task.Run(() => {
            ct.ThrowIfCancellationRequested();
            Image? loaded = null;
            if (block.Entity.ProductSides != null) {
                foreach (var side in block.Entity.ProductSides) {
                    if (!string.IsNullOrEmpty(side.image)) {
                        loaded = ProductImageCache.GetOrLoad(side.image);
                        if (loaded != null) {
                            if (side.rotate_angle != null) {
                                loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value);
                            }
                            break;
                        }
                    }
                }
            }
            return loaded;
        }, ct);

        // 回到 UI 线程更新封面
        if (image != null && !ct.IsCancellationRequested && !IsDisposed) {
            BeginInvoke(() => {
                if (!block.IsDisposed && block.Parent != null) {
                    block.CoverImage = image;
                }
            });
        }
    } catch (OperationCanceledException) {
        // 正常取消，忽略
    } finally {
        _loadSemaphore.Release();
    }
}
```

需要新增的 `using` (文件顶部第 4 行后新增)：
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 编译通过，无警告。

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs
git commit -m "perf: split RefreshMissionBlocks into skeleton UI + async cover loading

- Skeleton shows immediately with placeholder images
- Background tasks load real covers via SemaphoreSlim(4)
- CancellationTokenSource cancels previous round on refresh
- Dispose old controls before Clear() to prevent leaks"
```

---

## Task 5: AWorkplaceMissionView 异步化 FetchData + CheckAndDisplay

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceMissionView.cs`

- [ ] **Step 1: 新增 _checkCts 字段和 using**

在文件顶部新增 using：
```csharp
using System.Threading;
using System.Threading.Tasks;
```

在字段区域（第 16 行后）新增：
```csharp
private CancellationTokenSource? _checkCts;
```

- [ ] **Step 2: 修改 VisibleToTrue 为 async void**

替换第 55-64 行：

```csharp
public override async void VisibleToTrue() {
    if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
        System.Console.WriteLine($"_workplacePanel.Activated: {_workplacePanel.Activated}");
    }
    await CheckAndDisplayAsync();
    base.VisibleToTrue();
}
```

- [ ] **Step 3: 替换 CheckAndDisplay 为 CheckAndDisplayAsync**

```csharp
private async Task CheckAndDisplayAsync() {
    if (_missionListPanel != null) {
        // 取消上一轮
        _checkCts?.Cancel();
        _checkCts?.Dispose();
        _checkCts = new CancellationTokenSource();
        var ct = _checkCts.Token;

        try {
            // 后台获取数据
            await FetchDataAsync();
            ct.ThrowIfCancellationRequested();
        } catch (OperationCanceledException) {
            return;
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"CheckAndDisplayAsync error: {ex.Message}");
            return;
        }

        // 回到 UI 线程，检查面板是否仍然有效
        if (IsDisposed || !Visible) return;
        ct.ThrowIfCancellationRequested();

        if (_productMissionDTOs != null) {
            _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenWorkplaceView);
        }
    }
}
```

- [ ] **Step 4: 替换 FetchData 为 FetchDataAsync**

```csharp
private async Task FetchDataAsync() {
    if (apis != null) {
        _productMissionDTOs = await Task.Run(() =>
            apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs
        );
    }
}
```

- [ ] **Step 5: 新增 Dispose 重写清理 _checkCts**

在类尾部（`GetWrokplacePanel` 方法之后）新增：

```csharp
protected override void Dispose(bool disposing) {
    if (disposing) {
        _checkCts?.Cancel();
        _checkCts?.Dispose();
    }
    base.Dispose(disposing);
}
```

- [ ] **Step 6: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 编译通过。

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceMissionView.cs
git commit -m "perf: async FetchData and CheckAndDisplay in AWorkplaceMissionView

- FetchData runs on thread pool via Task.Run
- CancellationTokenSource cancels previous round on re-entry
- IsDisposed/Visible guards after await prevent disposal race
- Dispose override cleans up _checkCts"
```

---

## Task 6: MissionManagementView / MissionManagementView_SCII 异步化

**Files:**
- Modify: `OperationGuidance_new/Views/MissionManagementView.cs`
- Modify: `OperationGuidance_new/Views/MissionManagementView_SCII.cs`

两个文件改动几乎一致（区别仅在于类的泛型和 `OpenEditionPageView` 调用），以下展示 `MissionManagementView.cs`：

- [ ] **Step 1: 修改 MissionManagementView.cs**

在文件顶部新增 using：
```csharp
using System.Threading;
using System.Threading.Tasks;
```

新增字段（第 13 行后）：
```csharp
private CancellationTokenSource? _checkCts;
```

替换 `VisibleToTrue`（第 45-49 行）：
```csharp
public override async void VisibleToTrue() {
    await CheckAndDisplayAsync();
}
```

替换 `CheckAndDisplay`（第 38-43 行）：
```csharp
private async Task CheckAndDisplayAsync() {
    _checkCts?.Cancel();
    _checkCts?.Dispose();
    _checkCts = new CancellationTokenSource();
    var ct = _checkCts.Token;

    try {
        await FetchDataAsync();
        ct.ThrowIfCancellationRequested();
    } catch (OperationCanceledException) {
        return;
    } catch (Exception ex) {
        System.Diagnostics.Debug.WriteLine($"CheckAndDisplayAsync error: {ex.Message}");
        return;
    }

    if (IsDisposed || !Visible) return;
    _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenEditionPageView);
}
```

替换 `FetchData`（第 69-71 行）：
```csharp
private async Task FetchDataAsync() {
    _productMissionDTOs = await Task.Run(() =>
        apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id) { IsEditing = true }).ProductMissionDTOs
    );
}
```

- [ ] **Step 2: 在 MissionManagementView.cs 新增 Dispose 重写**

在类尾部（`FetchData` 方法之后）新增：

```csharp
protected override void Dispose(bool disposing) {
    if (disposing) {
        _checkCts?.Cancel();
        _checkCts?.Dispose();
    }
    base.Dispose(disposing);
}
```

- [ ] **Step 3: 对 MissionManagementView_SCII.cs 做同样改动**

将 Task 6 Step 1 的全部改动（using、字段、`VisibleToTrue`、`CheckAndDisplayAsync`、`FetchDataAsync`、`Dispose`）应用于 `MissionManagementView_SCII.cs`。

- [ ] **Step 4: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/MissionManagementView.cs OperationGuidance_new/Views/MissionManagementView_SCII.cs
git commit -m "perf: async FetchData in MissionManagementView / _SCII

- async void VisibleToTrue + CheckAndDisplayAsync + FetchDataAsync
- _checkCts guards against concurrent re-entry
- Dispose override cleans up _checkCts"
```

---

## Task 7: 缓存失效 — 图片保存后刷新缓存

**Files:**
- Modify: `OperationGuidance_new/Views/MissionEditionView.cs`
- Modify: `OperationGuidance_new/Views/MissionEditionView_SCII.cs`

- [ ] **Step 1: 在 MissionEditionView.cs 添加 using**

在文件顶部新增：
```csharp
using OperationGuidance_new.Utils;
```

- [ ] **Step 2: 在保存按钮回调中加缓存失效**

在 `MissionEditionView.cs` 第 296 行 `MainUtils.SaveProductImage(...)` 之后新增一行：
```csharp
ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
```

改动后代码：
```csharp
foreach (SideButton sideBtn in _sideButtons) {
    MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
    ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
}
```

- [ ] **Step 3: 复制按钮回调同样添加**

在 `MissionEditionView.cs` 第 436 行同样位置：
```csharp
foreach (SideButton sideBtn in _sideButtons) {
    MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
    ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
}
```

- [ ] **Step 4: 对 MissionEditionView_SCII.cs 做同样改动**

在 `MissionEditionView_SCII.cs` 第 488 行和第 629 行的保存循环中同样添加缓存失效。

- [ ] **Step 5: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/MissionEditionView.cs OperationGuidance_new/Views/MissionEditionView_SCII.cs
git commit -m "feat: invalidate image cache after product image save"
```

---

## 验证清单

完成所有 Task 后，进行以下验证：

- [ ] 项目编译通过：`dotnet build`
- [ ] 进入任务管理界面 → 骨架立即可见，封面图渐进出现
- [ ] 进入工作台界面 → 同上
- [ ] 快速切界面 → 无 ObjectDisposedException，无闪退
- [ ] 编辑保存任务 → 回到列表封面图已刷新
- [ ] 任务列表为空时 → 正常显示 BigButtonPanel

---

## 综合 commit 格式参考

每个 task 的 commit message 格式：
```
<type>: <简短描述>

- <细节1>
- <细节2>
```

Types: `feat` (新功能), `perf` (性能优化), `fix` (修bug), `chore` (杂项)
