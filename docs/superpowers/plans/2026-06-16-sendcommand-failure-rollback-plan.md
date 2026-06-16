# ToolTask SendCommand 失败回滚 — 实施计划

## 关联

- Spec: `docs/superpowers/specs/2026-06-16-sendcommand-failure-rollback-design.md`
- 目标文件: `OperationGuidance_new/Tasks/ToolTask.cs`, `AWorkplaceContentPanel.cs`
- 测试项目: `OperationGuidance_new.Tests/`

## 改动清单

### Step 1: `SendCommand` → `protected virtual`

```csharp
// 改动前: private bool SendCommand(string command)
// 改动后: protected virtual bool SendCommand(string command)
```

### Step 2: `PerformLock()` — 失败回滚

```csharp
private void PerformLock() {
    bool sent = false;
    // PF: sent = SendCommand(...)
    // X7: 第一次 SendCommand 不捕获，第二次 sent = SendCommand(...)
    // FITFTC6: sent = SendCommand(...)
    ClearPendingOnSendFailure(sent, PendingLockCommand.Lock);
}
```

X7 第一次 `SendCommand` 返回值不需捕获（死写，第二次覆盖）。

### Step 3: `PerformUnlock()` — 失败回滚

同 Step 2，`ClearPendingOnSendFailure(sent, PendingLockCommand.Unlock)`。

### Step 4: Helper `ClearPendingOnSendFailure()`

```csharp
private void ClearPendingOnSendFailure(bool sent, PendingLockCommand expected) {
    if (!sent) {
        lock (LockSyncObject) {
            if (_pendingLockCommand == expected) {
                _pendingLockCommand = PendingLockCommand.None;
            }
        }
        logger.Warn($"[TOOL:...] {expected} command send failed, pending cleared");
    }
}
```

### Step 5: `SendLock()` — 诊断日志

每个 silent return 出口加 `logger.Debug(...)`，包含 `_locked` 和 `_pendingLockCommand` 值。

### Step 6: `SendUnlock()` — 诊断日志

同 Step 5。

### Step 7: `StartLockCheckingTask()` — 状态变化日志

加 `lastIterationWasLock` / `lastLockMsgsHash` 状态变化检测，仅在切换时打 `logger.Debug(...)`。

### Step 8: 测试项目

创建 `OperationGuidance_new.Tests/`，xUnit + `TestableToolTask` subclass。

## 删改汇总

| 类型 | 项 | 行数 |
|------|----|------|
| 修改 | `SendCommand` private → protected virtual | ±1 |
| 新增 | `ClearPendingOnSendFailure` helper | +10 |
| 重构 | `PerformLock` — 返回值捕获 + 回滚调用 | ±5 |
| 重构 | `PerformUnlock` — 返回值捕获 + 回滚调用 | ±5 |
| 新增 | `SendLock` 两个 silent return 的 DEBUG 日志 | +2 |
| 新增 | `SendUnlock` 两个 silent return 的 DEBUG 日志 | +2 |
| 新增 | Lock checking task 状态变化日志 | +8 |
| 新增 | `OperationGuidance_new.Tests/` 测试项目 | +3 files |
| **净变化** | 生产代码 | **~20 行**（ToolTask.cs + AWorkplaceContentPanel.cs） |
| **测试** | 36 个单元测试 | 2 files, ~450 行 |

## 涉及文件

| 文件 | 改动类型 |
|------|---------|
| `OperationGuidance_new/Tasks/ToolTask.cs` | 修改 |
| `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs` | 修改 |
| `OperationGuidance_new.Tests/Tasks/TestableToolTask.cs` | 新增 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskLockUnlockTests.cs` | 新增 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskPSetTests.cs` | 新增 |
| `OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj` | 新增 |

## 验证

| # | 验证项 | 方法 |
|---|--------|------|
| 1 | 编译通过 | `dotnet build` |
| 2 | 36 单元测试全过 | `dotnet test` |
| 3 | 运行时 DEBUG 日志可见 SendUnlock 跳过原因 | 现场运行，DEBUG 级别 |
