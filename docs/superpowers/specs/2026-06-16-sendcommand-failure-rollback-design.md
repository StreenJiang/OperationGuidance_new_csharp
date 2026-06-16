# ToolTask SendCommand 失败回滚 + 诊断日志

## 背景

用户反馈：螺丝点位切换后（switchbolt），软件没有及时下发解锁命令，频率很高。

经过代码审查和日志分析，确认以下问题链：
1. `SendLock/SendUnlock` 通过 `_pendingLockCommand` 去重防止重复发送
2. `_pendingLockCommand` 只在控制器 Lock/Unlock 确认响应中清零
3. `PerformLock/PerformUnlock` 中 `SendCommand` 的返回值被丢弃
4. 如果 `SendCommand` 失败（socket 瞬间断开、send 返回 0），命令未发出但 `_pendingLockCommand` 已设 → **永久死锁**

日志证据：在 2026-06-16.log 中，部分 Force locking → Unlocking 间隔达 8-37 秒（arm 移动导致），但未发现 `_pendingLockCommand` 去重阻塞的直接日志证据（因为之前缺少诊断日志）。

## 方案

### 1. SendCommand 失败回滚（核心修复）

**文件**: `OperationGuidance_new/Tasks/ToolTask.cs`

`PerformLock/PerformUnlock` 检查 `SendCommand` 返回值，失败则在 `LockSyncObject` 内清除 `_pendingLockCommand`。

使用 compare-and-swap 保护竞态条件：只在 `_pendingLockCommand == expected` 时清零，防止清除另一个线程刚设置的 pending。方法体末尾无条件调用，`sent=true` 时直接跳过——保证所有退出路径（包括 Unknown tool type）都能回滚。

抽出公共 helper `ClearPendingOnSendFailure(bool sent, PendingLockCommand expected)`。

### 2. 诊断日志

**文件**: `OperationGuidance_new/Tasks/ToolTask.cs`

`SendLock/SendUnlock` 每个 silent return 出口加 DEBUG 日志，包含当前 `_locked` 和 `_pendingLockCommand` 值：

```
SendUnlock skipped — pending Unlock already in flight
SendUnlock skipped — already unlocked (locked=False, pending=None)
SendLock skipped — pending Lock already in flight
SendLock skipped — already locked (locked=True, pending=None)
```

**文件**: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

Lock checking task 加状态变化检测：只在上次迭代状态变化时打 DEBUG 日志：

```
[LockCheck:任务名] lockMsgs=【ARM位置不正确】 → calling SendLock, tool locked=True
[LockCheck:任务名] lockMsgs=empty → calling SendUnlock, tool locked=True
```

### 3. 测试

新增测试项目 `OperationGuidance_new.Tests`，xUnit 框架。

`TestableToolTask` 继承 `ToolTask`，通过 override `SendCommand` 和反射访问内部状态实现可控测试。

覆盖场景：
- SendLock/SendUnlock/ForceSendLock/ForceSendUnlock 正常流程
- 去重（pending 已存在时跳过）
- 已锁定/已解锁时跳过（状态检查）
- SendCommand 失败回滚 + compare-and-swap 竞态保护
- 高频 lock/unlock 切换（50 次循环无死锁）
- 高频去重防刷（50 次调用仅 1 次实际发送）
- 响应延迟/无响应场景
- PSet 发送成功/失败/超时/被拒/重入
- 断连场景

### 4. 测试可见性改动

`SendCommand` 从 `private` 改为 `protected virtual`，允许测试子类 override。

## 不改动

- `_pendingLockCommand` 的去重语义不变
- `SendLock/SendUnlock/ForceSendLock/ForceSendUnlock` 的对外接口不变
- Lock checking task 的轮询周期不变（50ms）
- Arm position 检查和 lockMsgs 管理逻辑不变

## 验证

1. 构建通过：`dotnet build`
2. 36 个单元测试全部通过：`dotnet test`
3. 运行时 DEBUG 日志可定位 `SendUnlock` 被跳过的具体出口

## 相关文档

- `docs/superpowers/specs/2026-06-16-lock-unlock-syncobj-split-design.md` — 上次 SyncObject 拆分
- `docs/superpowers/specs/2026-06-06-stale-lock-response-filter-design.md` — 过期响应过滤
- `docs/bugs/2026-06-16-lock-unlock-delay/2026-06-16.log` — 问题日志
