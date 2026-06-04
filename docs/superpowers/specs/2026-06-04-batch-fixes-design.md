# Batch Fixes — 2026-06-04

## Overview

修复 6 个线上 bug：GDI 泄漏 OOM/图片异常、跳过螺丝点位空文件生成、批次累计数量统计错误、条码匹配规则编辑 OOM、GridView 查询遮罩残留、PF Series PSet 重试必败。

Bug #3（追溯码/物料码重码校验失效）暂不处理，待客户确认。

---

## Bug #1 & #5: GDI 对象泄漏 → OutOfMemoryException / 产品图片变占位图

### 问题

任务编辑保存后偶发：
1. 列表刷新后产品图片变成默认占位图
2. 点击无反应或报 `System.OutOfMemoryException`

### 根因

**四个 GDI 对象泄漏/所有权问题**，积累到句柄耗尽后任何资源加载（包括 `input_error` 位图）均失败：

#### 泄漏点 A：`CustomTextBox.ResetErrorIcon()`（CustomLibrary/TextBoxes/CustomTextBox.cs）

每次 `ResizeChildren`（即每次 SizeChanged）调用 `ResetErrorIcon`：
- 旧 `_iconShowing`（Image）未 dispose 即被新图替换
- 旧 `_errorProvider.Icon` 未 dispose 即被新 Icon 替换
- `new Bitmap(_iconShowing).GetHicon()` 创建的中间 `Bitmap` 也未释放

#### 泄漏点 B：`AbstractCustomImageTextButton.InvokeResizing()`（CustomLibrary/Buttons/AbstractClasses/AbstractCustomImageTextButton.cs）

`OnSizeChanged` → `InvokeResizing` → `ResizeIconImage` 每次 resize 调用 `WidgetUtils.ResizeImage` 创建新 `ImageShowing`，但旧 `ImageShowing` 未 dispose。影响所有子类：`InnerButton`（ProductMissionBlock）、`AvatarButton`、`FoldButton`。

#### 所有权问题 C：`MissionListPanel.LoadOneCoverAsync` 共享引用被写入 Icon（OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs）

`ProductImageCache.GetOrLoad` 返回**共享**缓存引用，直接赋给 `CoverImage` → `InnerButton.Icon`。当 block 重建或刷新时，若尝试 dispose 旧 Icon，会损坏其他 component 正在使用的同一张缓存图片。当前因 Icon 从未被 dispose，这个问题被隐藏了，但一旦加 dispose 就会 crash。

`Icon` 基类 setter **不能**加 dispose：所有赋值点中 `Properties.Resources.sign_plus`、`mainMenuConfig.Icon`、`FoldButton.FoldedIcon` 等都是共享资源，dispose 会导致后续使用时崩溃。

#### 触发链路

任务编辑保存 → `ProductImageCache.InvalidateByMission()` → `RefreshAllBlocksById` / `RefreshMissionBlocks` → 重建 block → 异步加载封面 → 大量 resize 事件（触发泄漏点 A、B）→ GDI 句柄持续泄漏 → 最终 OOM。

Bug #5（条码匹配规则编辑 OOM）同根因：编辑弹窗打开时创建多个 `CustomTextBox` 控件 → 每个触发 `ResetErrorIcon` 泄漏 → 加上已绑定任务被删除时弹出警告窗口 → 更多控件创建/销毁 → 加速泄漏。

### 修复

#### 1. `CustomTextBox.ResetErrorIcon()` — 释放旧 GDI 资源

文件：`CustomLibrary/TextBoxes/CustomTextBox.cs`

> **修改前：**
> ```csharp
> private void ResetErrorIcon() {
>     Size newIconSize = new((int)(Height / 2), (int)(Height / 2));
>     if (_iconShowing == null || _iconShowing.Size != newIconSize) {
>         _iconShowing = WidgetUtils.ResizeImage(CustomResources.input_error, newIconSize);
>         _errorProvider.Icon = Icon.FromHandle(new Bitmap(_iconShowing).GetHicon());
>         _errorProvider.SetIconPadding(_box, (int)(_box.Padding.Right * .5));
>     }
>     int boxErrorNewWidth = _boxOriginalWidth - newIconSize.Width;
>     if (_boxErrorWidth != boxErrorNewWidth) {
>         _boxErrorWidth = boxErrorNewWidth;
>     }
> }
> ```
> **修改后：**
> ```csharp
> private void ResetErrorIcon() {
>     Size newIconSize = new((int)(Height / 2), (int)(Height / 2));
>     if (_iconShowing == null || _iconShowing.Size != newIconSize) {
>         _iconShowing?.Dispose();
>         _iconShowing = WidgetUtils.ResizeImage(CustomResources.input_error, newIconSize);
>         _errorProvider.Icon?.Dispose();
>         using (Bitmap bmp = new Bitmap(_iconShowing)) {
>             _errorProvider.Icon = Icon.FromHandle(bmp.GetHicon());
>         }
>         _errorProvider.SetIconPadding(_box, (int)(_box.Padding.Right * .5));
>     }
>     int boxErrorNewWidth = _boxOriginalWidth - newIconSize.Width;
>     if (_boxErrorWidth != boxErrorNewWidth) {
>         _boxErrorWidth = boxErrorNewWidth;
>     }
> }
> ```

#### 2. `AbstractCustomImageTextButton.InvokeResizing()` — 释放旧 ImageShowing

文件：`CustomLibrary/Buttons/AbstractClasses/AbstractCustomImageTextButton.cs`

在 resize 前保存旧 `ImageShowing`，resize 后 dispose。`WidgetUtils.ResizeImage` 始终创建新 Bitmap，所以 `ImageShowing` 始终是 owned 的，安全 dispose。

```csharp
private void InvokeResizing() {
    Form? form = TopLevelControl as Form;
    if (form is not null && form.WindowState == FormWindowState.Minimized) {
        return;
    }
    var oldShowing = ImageShowing;
    ResizeIconImage();
    oldShowing?.Dispose();
}
```

影响范围：`InnerButton`（ProductMissionBlock）、`AvatarButton`、`FoldButton` 全部受益。

> **修改前：**
> ```csharp
> private void InvokeResizing() {
>     Form? form = TopLevelControl as Form;
>     if (form is not null && form.WindowState == FormWindowState.Minimized) {
>         return;
>     }
>     // Rescale image
>     ResizeIconImage();
> }
> ```
> **修改后：**
> ```csharp
> private void InvokeResizing() {
>     Form? form = TopLevelControl as Form;
>     if (form is not null && form.WindowState == FormWindowState.Minimized) {
>         return;
>     }
>     // Rescale image — save and dispose old ImageShowing
>     var oldShowing = ImageShowing;
>     ResizeIconImage();
>     oldShowing?.Dispose();
> }
> ```

#### 3. `MissionListPanel.LoadOneCoverAsync` — 共享缓存图 clone 后再赋值

文件：`OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

从缓存取出的图如果不需要旋转（共享引用），用 `DeepCopyImage` 创建 owned copy。已旋转的图（`RotateImage` 返回新对象）不需要额外 clone。

> **修改前：**
> ```csharp
> loaded = ProductImageCache.GetOrLoad(side.image);
> if (loaded != null) {
>     if (side.rotate_angle != null) {
>         loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value);
>     }
>     break;
> }
> ```
> **修改后：**
> ```csharp
> loaded = ProductImageCache.GetOrLoad(side.image);
> if (loaded != null) {
>     if (side.rotate_angle != null) {
>         loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value);
>         // 旋转后已是新对象，owned
>     } else {
>         // 共享缓存引用，clone 为 owned copy
>         loaded = MainUtils.DeepCopyImage(loaded);
>     }
>     break;
> }
> ```

#### 4. `ProductMissionBlock.CoverImage` setter — 安全 dispose 旧值

文件：`OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`

自 LoadOneCoverAsync 保证传入的是 owned copy 后，setter 可以安全 dispose 旧值：

> **修改前：**
> ```csharp
> public Image? CoverImage {
>     get => _coverImage;
>     set {
>         _coverImage = value;
>         _innerButton.Icon = value;
>         _innerButton.RefreshImage();
>     }
> }
> ```
> **修改后：**
> ```csharp
> public Image? CoverImage {
>     get => _coverImage;
>     set {
>         _coverImage?.Dispose();
>         _coverImage = value;
>         _innerButton.Icon?.Dispose();
>         _innerButton.Icon = value;
>         _innerButton.RefreshImage();
>     }
> }
> ```

#### 5. `ProductMissionBlock.Dispose` — 释放 block 销毁时残留的 `_coverImage`

文件：`OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`

当 block 被销毁且未设置新 CoverImage 时，最后持有的 `_coverImage`/`InnerButton.Icon` 需要释放。`_coverImage` 和 `_innerButton.Icon` 指向同一个 Image 对象，dispose 一次即可。

> **新增：**
> ```csharp
> protected override void Dispose(bool disposing) {
>     if (disposing) {
>         _coverImage?.Dispose();
>         _coverImage = null;
>     }
>     base.Dispose(disposing);
> }
> ```

---

## Bug #2: 跳过螺丝点位无文件生成

### 问题

跳过螺丝点位完成任务时，没有任何 `operation_data`，因此不生成导出文件。但用户需要生成一个空文件（仅表头无数据行），文件命名与正常任务一致。

### 根因

两处「无数据就跳过」：

1. `AWorkplaceContentPanel.OnMissionCompleted`（line 2735）：`if (snapshot.Count == 0) return;`
2. `DataExportService.ExportAsync`（line 25）：`if (request.Data == null || request.Data.Count == 0) return;`

### 修复

#### 1. 移除 OnMissionCompleted 的空数据 early return

文件：`OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

删除 `if (snapshot.Count == 0) return;`（line 2735），允许空数据进入导出流程。`snapshot[0].workstation_name` 在空数据时不可用，需用 `_workstationName` 或从 mission 信息中获取。

#### 2. 移除 ExportAsync 的空数据 early return

文件：`OperationGuidance_new/Utils/DataExportService.cs`

删除 `if (request.Data == null || request.Data.Count == 0) { ... return; }`（lines 25-28）。

`BuildRows` 在空数据时自然返回空 rows 列表。`WriteExcelAsync` / `WriteTxtAsync` 在 rows 为空时正常写入仅表头的文件。`WorkstationName` 改为从 request 中取值（已存在字段）。

---

## Bug #4: 批次累计数量超过 20 不增加

### 问题

任务完成后批次累计数量计算不对，一旦超过 20 就不再继续增加。

### 根因

`QueryMissionRecordList` API 默认 `PageSize = 20`。`GetRecoreds()` 调用时不传 `PageSize`，永远只返回前 20 条记录，导致计数封顶在 20。

### 修复

#### 1. `GetRecoreds()` 传入足够大的 `PageSize`

文件：`OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

```csharp
private List<MissionRecordDTO> GetRecoreds() {
    QueryMissionRecordListReq req = new() {
        MissionId = _mission.id,
        PageSize = int.MaxValue,  // 不限制分页，拉取全量
    };
    // ... 其余逻辑不变
}
```

#### 2. 修正 `SetTodayData` 统计逻辑

文件：`OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

当前逻辑错误：先用 `DistinctBy(product_bar_code)` 去重再算 OK 数，导致一个产品多条 NG 记录 + 一条 OK 记录时统计不准。

修正为：
- **okSum**：全量 → 筛选 `mission_result == OK` → 按 `product_bar_code` 去重 → 计数
- **sum**：全量 → 按 `product_bar_code` 去重 → 计数

```csharp
// OK数 = 有OK结果的不同产品数
okSum = missionRecordDTOs
    .Where(dto => dto.mission_result == (int)TighteningStatus.OK)
    .DistinctBy(dto => dto.product_bar_code)
    .Count();

// 总数 = 不同产品数
sum = missionRecordDTOs
    .DistinctBy(dto => dto.product_bar_code)
    .Count();
```

---

## Bug #6: GridView 查询遮罩切换界面后残留

### 问题

GridView 数据查询未结束时切换到其他界面，遮罩层会被带过去挡住新界面的内容。

### 根因

`DataGridViewGroup.QueryAndRefresh`（`DataGridViewGroup.cs:219-292`）中的 `_loadingOverlay` 挂载在顶层 Form 上（而非 `DataGridViewGroup` 自身），因为 `FlowLayoutPanel` 会强制覆盖子控件的 `Location`，只有挂在 Form 上才能自由定位遮罩位置。

但 QueryAndRefresh 在 `finally` 块中仅用 `if (!IsDisposed)` 判断是否隐藏遮罩。当用户切换界面时，`DataGridViewGroup` 只是被隐藏（Parent 变更或 Visible=false），不会被 Dispose，所以遮罩继续显示在 Form 上直到查询完成。

### 修复

#### 1. 覆写 `OnVisibleChanged` — 不可见时立即隐藏遮罩

文件：`OperationGuidance_new/Views/ReusableWidgets/DataGridViewGroup.cs`

在 `DataGridViewGroup` 类中添加：

```csharp
protected override void OnVisibleChanged(EventArgs e) {
    base.OnVisibleChanged(e);
    if (!Visible) {
        HideLoadingOverlay();
    }
}
```

#### 2. 抽取遮罩隐藏逻辑为独立方法

将 `QueryAndRefresh` 的 `finally` 块中的遮罩隐藏逻辑提取为 `HideLoadingOverlay()`，避免代码重复：

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

`QueryAndRefresh` 的 `finally` 块改为调用 `HideLoadingOverlay()`。

#### 3. `QueryAndRefresh` 完成后检查可见性

```csharp
var result = await Task.Run(() => _queryData(_filterParametersVO));
if (IsDisposed || !Visible) return;  // 组件不可见时不再更新数据源
_voGridView.DataSource = result;
```

---

## Bug #7: PF Series 工具 PSet 下发失败后重发也必然失败

### 问题

PF Series 工具首次 PSet 下发约 5-15% 概率失败（工具无响应），触发断连重连重试机制后重发也必然失败。

### 根因

**旧 RunTask 的 `finally` 块可能销毁新连接的 socket。** 竞态时序如下：

1. `ReconnectAndResendPset` 调用 `CloseToTriggerReconnection()` 关闭 socket，然后 `Task.Delay(100)` 等待旧 RunTask 退出
2. 旧 RunTask 在 `catch` 中执行 `logger.Error` 写日志（I/O 延迟可能超 100ms），尚未进入 `finally`
3. `Connect()` → `ConnectToServer()` 创建新 socket 并完成 PF Series 握手
4. 旧 RunTask 终于进入 `finally`，此时 `socketClient` 已被替换为新 socket
5. `finally` 中的 `if (socketClient != null) { socketClient.Close(); socketClient = null; }` 销毁了新 socket
6. 新 `RunTask` 和 `SendPSetAsync` 操作已关闭的 socket → 必败

此外还有三个次要问题：

- **`Connect()` 可被并发重复调用**：`TaskCheckingLoop` 和 `ReconnectAndResendPset` 可能同时触发 `Connect()`，产生多个重连 task
- **`SyncObject` / `LockSyncObject` 是 `static`**：所有 ToolTask 实例共享锁，A 工具的 `Receive`（200ms timeout）阻塞 B 工具的 `SendCommand`
- **`SendAndReceiveOnlyForPreparingAsync` 不锁 `SyncObject`**：握手期间的 `Send`/`Receive` 与 `RunTask` 的 `Receive` 存在并发访问风险

### 修复

全部修改在 `OperationGuidance_new/Tasks/ToolTask.cs`。

#### 1. RunTask 生命周期追踪 → 可靠等待

新增 `_runTaskTask` 字段，`RunTask()` 中将 `Task.Run(...)` 的返回值赋给它。新增 `CloseToTriggerReconnectionAsync()` 在关闭 socket 后 `await _runTaskTask`（3s 超时兜底），替代原有的 `CloseToTriggerReconnection()` + `Task.Delay(100)`。

> **修改前（ReconnectAndResendPset）：**
> ```csharp
> CloseToTriggerReconnection();
> await Task.Delay(100, token);
> ```
> **修改后：**
> ```csharp
> await CloseToTriggerReconnectionAsync(token);
> ```

#### 2. `Connect()` 并发门禁

新增 `_connectInProgress`（`volatile int`），`Connect()` 入口 `Interlocked.Exchange` 判断，已在重连中则跳过。`Connect()` finally 中重置为 0。`CloseToTriggerReconnectionAsync()` 中也重置为 0 使连接关闭后立即可发起新连接。

#### 3. 静态锁改为实例锁

去掉 `SyncObject` 和 `LockSyncObject` 的 `static` 修饰符，每个 ToolTask 实例独立管理自己的 socket 和 lock state。

#### 4. 握手加锁

`SendAndReceiveOnlyForPreparingAsync` 中的 `socketClient.Send` 和 `ReceiveAsync` 统一走 `lock (SyncObject)`。

---

## Out of Scope

- Bug #3（追溯码/物料码重码校验失效）— 待客户确认
- 条码匹配规则中已删除任务的显示处理（当前已有 warning popup，仅 OOM 需通过 Bug #1 修复解决）
