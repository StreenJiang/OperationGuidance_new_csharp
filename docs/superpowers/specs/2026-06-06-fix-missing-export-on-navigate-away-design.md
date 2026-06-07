# Fix: 导航离开时任务未终止导致文件丢失

## 业务场景

任务激活状态下（`_activated = true`），三个操作都以 NG 结束并需要导出：

| 操作 | 当前行为 | 应改为 |
|---|---|---|
| "中断"按钮 | `TerminateMission(FINISHED_NG)` → 导出 ✓ | 不变 |
| "返回"按钮 | Dispose → ❌ 无导出 | 补充导出 |
| "退出登录" | Dispose → ❌ 无导出 | 补充导出 |

正常任务完成（OK/NG）已有 `OnMissionCompleted` 导出，无需改动。

## 根因

"返回"和"退出登录"都经过 `CloseWorkplace()` → `Dispose()` → `OnHandleDestroyed()`，
后者只做设备清理，不调 `TerminateMission`，不触发导出。

调用链：
```
WorkplaceTopBar.CloseWorkplace()
  → _workplace.Dispose()
    → OnHandleDestroyed()      // AWorkplaceContentPanel.cs:2965
      → 取消 CTS、锁枪、清理委托 ✓
      → ❌ 不调 OnMissionCompleted
      → ❌ 不导出文件
```

## 修复

**两个改动，全部在 `AWorkplaceContentPanel.cs`。**

### 改动 1：`OnMissionCompleted` — BeginInvoke 加 Dispose 守卫

第 2764 行：
```csharp
// 改前
BeginInvoke(() => RefreshTighteningDataPanel(new List<OperationDataVO>()));
// 改后
if (!IsDisposed) {
    BeginInvoke(() => RefreshTighteningDataPanel(new List<OperationDataVO>()));
}
```

Dispose 场景下控件 handle 已销毁，`BeginInvoke` 会抛异常。

### 改动 2：`OnHandleDestroyed` — 开头补充导出

第 2965 行，在原有清理逻辑之前：

```csharp
protected override void OnHandleDestroyed(EventArgs e) {
    // 先取消后台任务，释放可能持有的 _storeTighteningDataLock
    _activeMissionCts.Cancel();
    _backgroundTaskCts.ForEach(cts => {
        cts.Cancel();
        cts.Dispose();
    });
    _backgroundTaskCts.Clear();
    _activeMissionCts.Dispose();

    // "返回"或"退出登录"时，如果导出从未触发，补充 NG 导出
    if (_missionRecord != null
            && (IsExcelExportEnabled || IsTxtExportEnabled)
            && Volatile.Read(ref _exportTriggered) == 0) {
        OnMissionCompleted(WorkplaceProcessStatus.FINISHED_NG).Wait();
    }

    base.OnHandleDestroyed(e);
    // ... 后续清理不变
}
```

### 为什么复用 `OnMissionCompleted`

- **不重复逻辑**：构建 ExportRequest、回填 parts_bar_code、调 ExportAsync 等已存在
- **`_exportTriggered` 防护**：正常完成的任务 Dispose 时不重复导出
- **CTS 先取消**：避免 `StoreTighteningData` 持 `_storeTighteningDataLock` 导致 `WaitAsync` 阻塞（最坏情况 5s 超时兜底）
- **`ExportAsync` 不依赖 UI 线程**：纯文件 I/O，`.Wait()` 不会死锁

## 业务场景覆盖

| 场景 | 触发路径 | 导出 |
|---|---|---|
| "中断"按钮 | `TerminateMission(FINISHED_NG)` → `OnMissionCompleted` | ✓ |
| "返回"按钮 | Dispose → `OnHandleDestroyed` → `OnMissionCompleted.Wait()` | ✓ |
| "退出登录" | 同上 | ✓ |
| 正常 OK 完成 | `TerminateMission(FINISHED_OK)` → `OnMissionCompleted` | ✓ |
| 正常 NG 完成 | `TerminateMission(FINISHED_NG)` → `OnMissionCompleted` | ✓ |
| 完成后 Dispose | `_exportTriggered == 1` → 跳过 | — |

## 影响范围

- `AWorkplaceContentPanel.cs` — 基类，影响所有工作站（SCII, YF, GLB, TZYX, WHYC）
- 不改 `TerminateMission`、不改正常流程
- 2 处改动，约 5 行代码

## 验证

1. 构建：`dotnet build`，0 errors
2. 日志：任务激活 → 点返回 → 确认有 `OnMissionCompleted - Start` 日志
3. 文件系统：导出目录有对应 NG 文件
