# Batch Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 GDI 泄漏 OOM、跳过螺丝点位空文件生成、批次累计数量统计、条码规则编辑 OOM、GridView 查询遮罩残留五个线上 bug

**Architecture:** 11 个独立任务，每个任务修改 1-2 个文件，互不依赖。Bug #1 GDI 泄漏修复涉及 5 个任务（Task 1-5），同时解决 Bug #5 的 OOM。Bug #7 PSet 重试修复涉及 1 个任务（Task 11）

**Tech Stack:** C# WinForms, GDI+, System.Drawing

---

## File Structure

| 文件 | 修改内容 |
|------|----------|
| `CustomLibrary/TextBoxes/CustomTextBox.cs:263-274` | `ResetErrorIcon` — dispose 旧 GDI 资源 |
| `CustomLibrary/Buttons/AbstractClasses/AbstractCustomImageTextButton.cs:45-52` | `InvokeResizing` — dispose 旧 ImageShowing |
| `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs:242-250` | `LoadOneCoverAsync` — 共享缓存图 clone 为 owned |
| `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs:23-29` | `CoverImage` setter — 安全 dispose 旧值 |
| `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs` | `Dispose` override — block 销毁时释放 `_coverImage` |
| `OperationGuidance_new/Utils/DataExportService.cs:24-28` | `ExportAsync` — 移除空数据 early return |
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2735,2754` | `OnMissionCompleted` — 允许空数据导出 + WorkstationName 回退 |
| `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:512-563` | `GetRecoreds` + `SetTodayData` — PageSize + 统计修正 |
| `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs:176-291` | `QueryAndRefresh` — 界面不可见时隐藏遮罩 |
| `OperationGuidance_new/Tasks/ToolTask.cs` | RunTask 生命周期追踪 + Connect 门禁 + 实例锁 + 握手加锁 |

---

### Task 1: Fix GDI leak in `CustomTextBox.ResetErrorIcon()`

**Files:**
- Modify: `CustomLibrary/TextBoxes/CustomTextBox.cs:263-274`

- [ ] **Step 1: 替换 `ResetErrorIcon` 方法体**

```csharp
private void ResetErrorIcon() {
    Size newIconSize = new((int) (Height / 2), (int) (Height / 2));
    if (_iconShowing == null || _iconShowing.Size != newIconSize) {
        _iconShowing?.Dispose();
        _iconShowing = WidgetUtils.ResizeImage(CustomResources.input_error, newIconSize);
        _errorProvider.Icon?.Dispose();
        using (Bitmap bmp = new Bitmap(_iconShowing)) {
            _errorProvider.Icon = Icon.FromHandle(bmp.GetHicon());
        }
        _errorProvider.SetIconPadding(_box, (int) (_box.Padding.Right * .5));
    }
    int boxErrorNewWidth = _boxOriginalWidth - newIconSize.Width;
    if (_boxErrorWidth != boxErrorNewWidth) {
        _boxErrorWidth = boxErrorNewWidth;
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build CustomLibrary/CustomLibrary.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add CustomLibrary/TextBoxes/CustomTextBox.cs
git commit -m "fix: dispose old GDI resources in CustomTextBox.ResetErrorIcon to prevent OOM"
```

---

### Task 2: Fix GDI leak in `AbstractCustomImageTextButton.InvokeResizing()`

**Files:**
- Modify: `CustomLibrary/Buttons/AbstractClasses/AbstractCustomImageTextButton.cs:45-52`

- [ ] **Step 1: 替换 `InvokeResizing` 方法体**

当前代码（lines 45-52）：

```csharp
private void InvokeResizing() {
    Form? form = TopLevelControl as Form;
    if (form is not null && form.WindowState == FormWindowState.Minimized) {
        return;
    }
    // Rescale image
    ResizeIconImage();
}
```

替换为：

```csharp
private void InvokeResizing() {
    Form? form = TopLevelControl as Form;
    if (form is not null && form.WindowState == FormWindowState.Minimized) {
        return;
    }
    // Rescale image — save and dispose old ImageShowing (ResizeImage always creates new Bitmap)
    var oldShowing = ImageShowing;
    ResizeIconImage();
    oldShowing?.Dispose();
}
```

> **说明：** `ResizeIconImage` 内部调用 `WidgetUtils.ResizeImage` 始终创建新 `Bitmap`，故 `ImageShowing` 始终为 owned。此修复影响所有子类：`InnerButton`（ProductMissionBlock）、`AvatarButton`、`FoldButton`。

- [ ] **Step 2: 编译验证**

```bash
dotnet build CustomLibrary/CustomLibrary.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add CustomLibrary/Buttons/AbstractClasses/AbstractCustomImageTextButton.cs
git commit -m "fix: dispose old ImageShowing in InvokeResizing to prevent GDI leak across all subclasses"
```

---

### Task 3: Clone shared cache images in `MissionListPanel.LoadOneCoverAsync`

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs:242-250`

- [ ] **Step 1: 在 `LoadOneCoverAsync` 中 clone 共享缓存引用**

当前代码（lines 241-251）：

```csharp
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
```

替换为：

```csharp
if (block.Entity.ProductSides != null) {
    foreach (var side in block.Entity.ProductSides) {
        if (!string.IsNullOrEmpty(side.image)) {
            loaded = ProductImageCache.GetOrLoad(side.image);
            if (loaded != null) {
                if (side.rotate_angle != null) {
                    loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value);
                    // 旋转后已是新对象（owned）
                } else {
                    // 共享缓存引用，clone 为 owned copy
                    loaded = MainUtils.DeepCopyImage(loaded);
                }
                break;
            }
        }
    }
}
```

> **关键：** `ProductImageCache.GetOrLoad` 返回共享引用，不能 dispose。`DeepCopyImage` 通过 Base64 往返创建独立副本（与现有 Image Loading 模式一致）。`RotateImage` 已经创建新 `Bitmap`，无需额外 clone。

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs
git commit -m "fix: clone shared cache images before assigning to CoverImage to establish clear ownership"
```

---

### Task 4: Safe dispose in `ProductMissionBlock.CoverImage` setter

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs:23-29`

- [ ] **Step 1: 替换 `CoverImage` setter**

当前代码（lines 23-29）：

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

替换为：

```csharp
public Image? CoverImage {
    get => _coverImage;
    set {
        _coverImage?.Dispose();
        _coverImage = value;
        _innerButton.Icon?.Dispose();
        _innerButton.Icon = value;
        _innerButton.RefreshImage();
    }
}
```

> **前提：** Task 3 保证传入的 `value` 始终为 owned copy，故此处 dispose 旧值是安全的。`_innerButton.Icon?.Dispose()` 只影响 `InnerButton` 场景（不触及 `FoldButton`、`AvatarButton` 等共享资源使用者）。

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs
git commit -m "fix: safely dispose old CoverImage and Icon in ProductMissionBlock setter"
```

---

### Task 5: Add `Dispose` override in `ProductMissionBlock`

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs` — 新增方法

- [ ] **Step 1: 在 `ProductMissionBlock` 类中添加 `Dispose` 覆写**

在类的末尾（`}` 之前，约 line 112 附近）添加：

```csharp
protected override void Dispose(bool disposing) {
    if (disposing) {
        _coverImage?.Dispose();
        _coverImage = null;
    }
    base.Dispose(disposing);
}
```

> **说明：** `_coverImage` 和 `_innerButton.Icon` 指向同一 Image 对象，dispose 一次即可。`_coverImage = null` 防止 InnerButton 后续访问已释放的图片。

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs
git commit -m "fix: dispose _coverImage in ProductMissionBlock.Dispose to prevent leak on block destroy"
```

---

### Task 6: Allow empty data export in `DataExportService.ExportAsync`

**Files:**
- Modify: `OperationGuidance_new/Utils/DataExportService.cs:24-28`

- [ ] **Step 1: 删除 `ExportAsync` 中的空数据 early return**

找到并删除以下 4 行（lines 25-28）：

```csharp
if (request.Data == null || request.Data.Count == 0) {
    _logger.Warn("[DataExport] ExportAsync skipped: no data");
    return;
}
```

修改后的 `ExportAsync` 方法开头：

```csharp
public async Task ExportAsync(ExportRequest request) {
    var data = request.Data ?? new List<OperationDataVO>();

    string workstation = string.IsNullOrEmpty(request.WorkstationName) ? "null" : request.WorkstationName;
    string mission = string.IsNullOrEmpty(request.MissionName) ? "null" : request.MissionName;
    string date = request.CompletedAt.ToString("yyyy-MM-dd");
    string batch = string.IsNullOrEmpty(request.ProductBatch) ? "null" : request.ProductBatch;
    string batchFolder = Path.Combine(request.BasePath, workstation, mission, date, batch);
    string barCode = string.IsNullOrEmpty(request.ProductBarCode) ? "null" : request.ProductBarCode;
    string timestamp = request.CompletedAt.ToString("yyyyMMdd_HHmmss");
    string fileNameBody = $"{barCode}_{timestamp}_{request.Result}";

    try {
        Directory.CreateDirectory(batchFolder);
    } catch (Exception ex) {
        _logger.Error($"[DataExport] Failed to create directory: {batchFolder}", ex);
        throw new IOException($"无法创建导出目录: {batchFolder}", ex);
    }

    var propertyNames = request.Fields.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
    var headers = request.Fields.Where(f => f.Visible).Select(f => f.FieldName).ToList();
    if (propertyNames.Count == 0) {
        _logger.Warn("[DataExport] No visible fields configured — export may produce empty columns");
    }

    var rows = BuildRows(data, propertyNames);
    _logger.Info($"[DataExport] Exporting {rows.Count} rows x {propertyNames.Count} cols to {batchFolder}");
    // ... 后续不变（WriteExcelAsync / WriteTxtAsync 调用）
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Utils/DataExportService.cs
git commit -m "fix: allow empty data export to generate header-only file for skip-screw missions"
```

---

### Task 7: Allow empty snapshot export in `OnMissionCompleted`

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2735,2754`

- [ ] **Step 1: 删除 `snapshot.Count == 0` early return，修复 `WorkstationName` 取值**

找到 `OnMissionCompleted` 方法中的这段代码（约 line 2734-2755）：

```csharp
var snapshot = GetTighteningDataSnapshot();
if (snapshot.Count == 0) return;  // ← 删除这行

string result = status == WorkplaceProcessStatus.FINISHED_OK ? "OK" : "NG";
var fields = MainUtils.GetOperationDataFields(ExportSortConfig);

// 回填 parts_bar_code 到每个 VO（同一批次所有行共享同一个物料码）
if (_missionRecord?.parts_bar_code != null) {
    foreach (var vo in snapshot) {
        vo.parts_bar_code = _missionRecord.parts_bar_code;
    }
}

var request = new ExportRequest {
    Data = snapshot, Fields = fields, BasePath = ExportBasePath,
    ProductBatch = _missionRecord?.product_batch,
    ProductBarCode = _missionRecord?.product_bar_code,
    CompletedAt = DateTime.Now, Result = result,
    EnableExcel = IsExcelExportEnabled, EnableTxt = IsTxtExportEnabled,
    MissionName = _mission?.name,
    WorkstationName = snapshot[0].workstation_name,  // ← 需要修改
};
```

修改为：

```csharp
var snapshot = GetTighteningDataSnapshot();

string result = status == WorkplaceProcessStatus.FINISHED_OK ? "OK" : "NG";
var fields = MainUtils.GetOperationDataFields(ExportSortConfig);

// 回填 parts_bar_code 到每个 VO（同一批次所有行共享同一个物料码）
if (_missionRecord?.parts_bar_code != null) {
    foreach (var vo in snapshot) {
        vo.parts_bar_code = _missionRecord.parts_bar_code;
    }
}

// WorkstationName：优先从 snapshot 取，空数据时回退到 _missionRecord
string workstationName = snapshot.Count > 0
    ? snapshot[0].workstation_name
    : _missionRecord?.workstation_name ?? _mission?.name ?? "Unknown";

var request = new ExportRequest {
    Data = snapshot, Fields = fields, BasePath = ExportBasePath,
    ProductBatch = _missionRecord?.product_batch,
    ProductBarCode = _missionRecord?.product_bar_code,
    CompletedAt = DateTime.Now, Result = result,
    EnableExcel = IsExcelExportEnabled, EnableTxt = IsTxtExportEnabled,
    MissionName = _mission?.name,
    WorkstationName = workstationName,
};
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "fix: allow empty snapshot export for skip-screw missions with fallback WorkstationName"
```

---

### Task 8: Fix `GetRecoreds()` PageSize to fetch all records

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:538-540`

- [ ] **Step 1: 在 `GetRecoreds()` 请求中加入 `PageSize = int.MaxValue`**

找到（line 538-540）：

```csharp
QueryMissionRecordListReq req = new() {
    MissionId = _mission.id,
};
```

替换为：

```csharp
QueryMissionRecordListReq req = new() {
    MissionId = _mission.id,
    PageSize = int.MaxValue,
};
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "fix: set large PageSize in GetRecoreds to fetch all batch records beyond 20"
```

---

### Task 9: Fix `SetTodayData` statistics logic

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:516-521`

- [ ] **Step 1: 修正统计计算逻辑**

当前代码（lines 516-521）：

```csharp
IEnumerable<MissionRecordDTO> distinctData = missionRecordDTOs
            .DistinctBy(dto => dto.product_bar_code);
sum = distinctData.Count();
okSum = distinctData
            .Where(dto => dto.mission_result == (int) TighteningStatus.OK)
            .Count();
```

替换为：

```csharp
// sum = 不同产品数（按 product_bar_code 去重）
sum = missionRecordDTOs
            .DistinctBy(dto => dto.product_bar_code)
            .Count();

// okSum = 有 OK 结果的不同产品数（全量筛选 OK → 去重）
okSum = missionRecordDTOs
            .Where(dto => dto.mission_result == (int) TighteningStatus.OK)
            .DistinctBy(dto => dto.product_bar_code)
            .Count();
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "fix: correct SetTodayData statistics — filter OK before distinct for oksum"
```

---

### Task 10: Hide loading overlay when `DataGridViewGroup` becomes invisible

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs:176-291`

- [ ] **Step 1: 抽取 `HideLoadingOverlay()` 方法**

在 `InitializeLoadingOverlay` 方法后面添加：

```csharp
private void HideLoadingOverlay() {
    if (_loadingOverlay.Visible) {
        _loadingOverlay.Visible = false;
        _loadingOverlay.Region?.Dispose();
        _loadingOverlay.Region = null;
        _loadingOverlay.BackgroundImage?.Dispose();
        _loadingOverlay.BackgroundImage = null;
        _loadingLabel.Visible = true;
    }
}
```

- [ ] **Step 2: 添加 `OnVisibleChanged` 覆写**

在 `Override methods` region（line 421 附近）添加：

```csharp
protected override void OnVisibleChanged(EventArgs e) {
    base.OnVisibleChanged(e);
    if (!Visible) {
        HideLoadingOverlay();
    }
}
```

- [ ] **Step 3: 替换 `QueryAndRefresh` 中 `finally` 块的遮罩清理逻辑**

将 `finally` 块中的：

```csharp
} finally {
    if (!IsDisposed) {
        _searchButton.Enabled = true;
        _resetButton.Enabled = true;
        _loadingOverlay.Visible = false;
        _loadingOverlay.Region?.Dispose();
        _loadingOverlay.Region = null;
        _loadingOverlay.BackgroundImage?.Dispose();
        _loadingOverlay.BackgroundImage = null;
        _loadingLabel.Visible = true;
    }
    _isQuerying = 0;
}
```

替换为：

```csharp
} finally {
    if (!IsDisposed) {
        _searchButton.Enabled = true;
        _resetButton.Enabled = true;
        HideLoadingOverlay();
    }
    _isQuerying = 0;
}
```

- [ ] **Step 4: 查询完成后检查可见性**

将（line 273-274）：

```csharp
var result = await Task.Run(() => _queryData(_filterParametersVO));
if (IsDisposed) return;
_voGridView.DataSource = result;
```

修改为：

```csharp
var result = await Task.Run(() => _queryData(_filterParametersVO));
if (IsDisposed || !Visible) return;
_voGridView.DataSource = result;
```

- [ ] **Step 5: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs
git commit -m "fix: hide loading overlay when DataGridViewGroup becomes invisible"
```

---

### Task 11: Fix PF Series PSet retry — ToolTask 可靠性修复

修复四个问题：旧 RunTask finally 销毁新 socket、Connect 并发门禁、静态锁改实例锁、握手加锁。全部在同一文件内，原子修改。

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`

- [ ] **Step 1: 新增字段**

在 `#region Fields` 中添加（约 line 30 `private int SendMessageRecevingCount` 下方）：

```csharp
private Task? _runTaskTask;
private volatile int _connectInProgress;
```

- [ ] **Step 2: `SyncObject` / `LockSyncObject` 去掉 `static`**

当前（lines 15-16）：
```csharp
private static readonly object SyncObject = new();
private static readonly object LockSyncObject = new();
```

修改为：
```csharp
private readonly object SyncObject = new();
private readonly object LockSyncObject = new();
```

- [ ] **Step 3: `RunTask()` 保存 task 引用**

当前（line 64-65）：
```csharp
protected override void RunTask() {
    Task.Run(async () => {
```

修改为：
```csharp
protected override void RunTask() {
    _runTaskTask = Task.Run(async () => {
```

- [ ] **Step 4: 新增 `CloseToTriggerReconnectionAsync()` 方法**

在 `CloseToTriggerReconnection()` 方法后面添加：

```csharp
public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection and waiting for RunTask to exit...");
    _currentPSet = -1;
    socketClient?.Close();
    socketClient = null;
    _connectInProgress = 0;

    if (_runTaskTask != null) {
        var runTask = _runTaskTask;
        _runTaskTask = null;
        try {
            await runTask.WaitAsync(TimeSpan.FromSeconds(3), token);
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Old RunTask exited cleanly");
        } catch (TimeoutException) {
            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Old RunTask did not exit within 3s timeout");
        } catch (OperationCanceledException) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync cancelled");
        }
    }
}
```

- [ ] **Step 5: 修改 `ReconnectAndResendPset` 使用新方法**

当前（lines 522-530）：
```csharp
// 2. 关闭旧连接
CloseToTriggerReconnection();

// 3. 等待 RunTask 主循环感知断连并退出
try {
    await Task.Delay(100, token);
} catch (OperationCanceledException) {
    Status = DISCONNECTED;
    return false;
}
```

替换为：
```csharp
// 2. 关闭旧连接并等待 RunTask 彻底退出（保证 finally 已执行）
try {
    await CloseToTriggerReconnectionAsync(token);
} catch (OperationCanceledException) {
    Status = DISCONNECTED;
    return false;
}
```

- [ ] **Step 6: 修改 `Connect()` — 入口加门禁 + finally 重置**

当前（lines 203-232）：
```csharp
public override void Connect() {
    lock (SyncObject) {
        Task.Run(async () => {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Initiating connection");
            HeartBeatCounter = 0;
            CloseConnectionManually = false;

            int retryCount = 0;
            while (!Connected) {
                retryCount++;
                Status = CONNECTING;

                if (await ConnectToServer()) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
                    RunTask();
                    Status = CONNECTED;
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Status: CONNECTED");

                    ForceSendUnlock();
                    break;
                }
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed, retrying ({retryCount})");
                await Task.Delay(AutoReconnectingTrialDelay);
            }
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection completed after {retryCount} attempt(s)");
            } else {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed after {retryCount} attempt(s)");
            }
        });
    }
}
```

替换为：
```csharp
public override void Connect() {
    if (Interlocked.Exchange(ref _connectInProgress, 1) == 1) {
        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect already in progress, skipping");
        return;
    }
    Task.Run(async () => {
        try {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Initiating connection");
            HeartBeatCounter = 0;
            CloseConnectionManually = false;

            int retryCount = 0;
            while (!Connected) {
                retryCount++;
                Status = CONNECTING;

                if (await ConnectToServer()) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
                    RunTask();
                    Status = CONNECTED;
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Status: CONNECTED");

                    ForceSendUnlock();
                    break;
                }
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed, retrying ({retryCount})");
                await Task.Delay(AutoReconnectingTrialDelay);
            }
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection completed after {retryCount} attempt(s)");
            } else {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed after {retryCount} attempt(s)");
            }
        } finally {
            _connectInProgress = 0;
        }
    });
}
```

> 去掉原 `lock (SyncObject)`，`_connectInProgress` 门禁替代其作用。

- [ ] **Step 7: `SendAndReceiveOnlyForPreparingAsync` 加 `lock (SyncObject)`**

当前（lines 392-414）：
```csharp
byte[] data;
// ... data encoding ...

// Send command to controller
socketClient.Send(data);

// Receive data
byte[] msgBytes = new byte[1024 * 1024];
int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
// ... encoding switch ...
return result;
```

修改为：
```csharp
byte[] data;
// ... data encoding ...

// Send command and receive response under lock for socket safety
byte[] msgBytes = new byte[1024 * 1024];
int msgLen;
lock (SyncObject) {
    if (!Connected) return null;
    socketClient.Send(data);
    msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
}
string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
// ... encoding switch ...
return result;
```

> 同步 `Receive` 替换异步 `ReceiveAsync`，与 `RunTask` 的用法一致，且都在 `lock` 内保证线程安全。

- [ ] **Step 8: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj --no-restore
```

Expected: Build succeeded.

- [ ] **Step 9: Commit**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "fix: ToolTask reliability — await RunTask exit, Connect guard, instance locks, handshake locking"
```

---

## Verification

所有修改完成后，完整编译：

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期: Build succeeded, 0 errors, 0 warnings.
