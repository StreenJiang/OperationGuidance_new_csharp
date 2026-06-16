# ToolTask lock/unlock SendCommand 延迟修复

## 背景

用户反馈：PF6000 工具在 lock 状态下调用 unlock，85% 概率延迟 1~2 秒才能完成。

日志分析（`docs/bugs/2026-06-16-lock-unlock-delay/2026-06-16.log`）确认：
- 工具 M040 (PF6000-OP, 192.168.1.98:4545)
- ForceLock 即时生效，Unlock 走 `SendUnlock()` → 等工具响应
- Unlock 状态变更延迟 80~400ms（平均值 ~200ms），但日志未捕获到 1~2 秒案例

延迟根因与 `2026-06-06-stale-lock-response-filter-design.md` 不同 —— 那次修复了过期响应竞态，
这次是 **Socket 层面的锁竞争**。

## 根因

`ToolTask.cs` 中 `SyncObject` 被**主循环 Receive** 和 **SendCommand** 共用：

```
主循环 (RunTask):
  while (Connected) {
      SendCommand(heartbeat);           ← 需要 SyncObject
      lock (SyncObject) {
          socketClient.Receive(200ms);  ← 持有 SyncObject 最多 200ms
      }
      await Task.Delay(100ms);
  }

外部调用 (SendUnlock):
  lock (LockSyncObject) {
      PerformUnlock()
        → SendCommand(unlock)
          → lock (SyncObject)           ← 阻塞等待，最多 200ms
            → socketClient.Send()       ← TCP 发送，可能阻塞更久
  }
```

**当主循环持有 `SyncObject` 做 `Receive(200ms)` 时，`SendUnlock` 的 `SendCommand` 在
`lock(SyncObject)` 处阻塞**。加上 `socketClient.Send()` 本身的阻塞（TCP 缓冲区满时），
延迟累加达到 1~2 秒。

TCP Socket 天然支持并发 Send/Receive（一个线程 Send，另一个 Receive 不会冲突），
`SyncObject` 的合并保护是不必要的过度串行化。

## 方案：拆分 SyncObject 为 Send/Receive 独立锁

### 改动

**文件**：`OperationGuidance_new/Tasks/ToolTask.cs`

1. **新增 `_sendLock`** 字段，专用于保护 Send 操作
2. **`SendCommand()`** 将 `lock(SyncObject)` 替换为 `lock(_sendLock)`
3. **主循环 Receive** 保持 `lock(SyncObject)` 不变
4. **`socketClient.SendTimeout`** 设为 500ms，防止极端网络异常时无限阻塞
5. **`LockSyncObject` 不包裹 I/O**：将 `PerformLock()`/`PerformUnlock()` 移到 `lock(LockSyncObject)` 外部，
   只保留状态检查和 pending command 设置受锁保护
6. **心跳计数器在任意命令发送时重置**：`SendCommand()` 发送成功后 `HeartBeatCounter = 0`。
   当前只在发送心跳命令时重置，改为只要有任何命令发出（lock/unlock/PSet/barcode/心跳）就重置。
   效果：工具活跃期间心跳自动推迟，只在真正空闲 5 秒后才发心跳，减少不必要的心跳与真实命令抢锁。

### 不改动

- `AnalyzeData` / `UpdateInternalLockState` 逻辑不变
- 心跳周期（5000ms）不变
- `SendAndReceiveOnlyForPreparingAsync`（握手阶段，不影响运行时）
- `CloseConnection` / `CloseToTriggerReconnectionAsync` 中的 `SyncObject` 使用保持

### 数据流变化

```
改动前：
  SendUnlock() → lock(LockSyncObject) → PerformUnlock() → SendCommand() → lock(SyncObject) → Send()

改动后：
  SendUnlock() → lock(LockSyncObject) { 状态检查 + 设 pending }  // 快速释放
              → PerformUnlock() → SendCommand() → lock(_sendLock) → Send()  // 不与 Receive 竞争
  
  主循环：lock(SyncObject) { Receive() }  // 不与 Send 竞争
```

### 预期效果

- `SendUnlock()` 不再等待主循环 `Receive` 释放锁
- Unlock 命令发出延迟从 0~200ms → ~0ms
- TCP 层 `SendTimeout = 500ms` 兜底极端网络异常
- 心跳只在空闲时发送，减少与真实命令的锁竞争
- 改动量 ~20 行，风险极低

## 验证

1. 构建通过：`dotnet build`
2. 运行时观察日志：Unlock 后 `Lock state: True -> False` 应在 500ms 内
3. Lock/Unlock 连续快速切换（1 秒内 lock→unlock→lock→unlock）不卡顿
4. 心跳行为：连续发送命令期间不出现 `Sending heartbeat`，命令停止 ~5 秒后心跳恢复

## 相关文档

- `docs/superpowers/specs/2026-06-06-stale-lock-response-filter-design.md` — 上次锁状态修复（过期响应过滤）
- `docs/bugs/2026-06-16-lock-unlock-delay/2026-06-16.log` — 问题日志
