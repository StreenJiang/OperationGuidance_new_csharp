# Fix: 工具 Lock/Unlock 响应乱序导致解锁迟滞

## 问题

点位切换时频繁出现 `Unlock failed (tool reports locked), cooldown reset`。虽
然 cooldown 重置后能恢复，但多次重试造成操作员可感知的迟滞。

日志统计：全天 91 次 Unlock failed，0 次 Lock failed，高频簇遍布全天。

## 根因

PF6000 协议无序列号/消息ID，无法将响应匹配到具体指令。当 Lock/Unlock 切换
速度快于工具响应速度时，前一条指令的响应在后一条指令的等待期间到达，被
误判为后一条指令失败。

### 精确时序（15:53:45 簇）

```
T=44.706  SendLock()         → _pendingLockCommand = Lock
                              ↓ 发送锁枪指令

T=44.972  拧紧数据到达        → ForceSendLock()
                              → UpdateInternalLockState(true)
                              → _pendingLockCommand = None   ← 丢失了"Lock 响应在路上"的信息

T=45.002  切换下一点位

T=45.049  SendUnlock()       → _pendingLockCommand = Unlock
                              ↓ 发送解锁指令

T=45.081  工具响应 "Lock ok"  ← 44.706 那条 Lock 的延迟响应(375ms)
          ↓
          UpdateInternalLockState(true):
            expected=Unlock, got=Locked → 矛盾！
            → ⚠️ "Unlock failed, cooldown reset"
            → _lastUnlockTimestamp = 0 (允许立即重试)

T=45.111  lock checking task → SendUnlock() retry
T=45.192  又一条 "Lock ok"    ← 仍是延迟响应
          → ⚠️ 再次 Unlock failed, cooldown reset

T=45.331  "Unlock ok" 到达   → _locked = False ✓ (282ms 后终于成功)
```

**核心矛盾**：`ForceSendLock` 调用 `UpdateInternalLockState(true)` 将
`_pendingLockCommand` 清为 `None`，丢失了"还有一条 Lock 响应在传输中"的
状态。后续 `SendUnlock` 将 `_pendingLockCommand` 设为 `Unlock` 后，延迟到达
的 "Lock ok" 就被当作 Unlock 失败。

### 已验证：正确响应不会被误丢弃

阈值的担心需要澄清。过滤条件有两个必要条件同时满足：

1. **响应与期望矛盾**（expected=Unlock 但收到 Locked）
2. **时间窗口短**（< 阈值）

正确响应（expected=Unlock + 收到 Unlocked）**不会**触发过滤，无论多快。
从日志统计的合法响应延迟范围：

| 场景 | 延迟范围 |
|---|---|
| 正确 Unlock 响应 | 90ms ~ 470ms |
| 延迟 Lock 响应（触发误判） | 30ms ~ 80ms |
| 正确 Lock 响应 | 100ms ~ 400ms |

合法最快响应 90ms，延迟误判响应集中在 30-80ms。选择 100ms 作为阈值：

- 延迟响应（30-80ms）< 100ms → 被过滤 ✓
- 合法响应（90ms+）> 100ms → 不被过滤 ✓
- 合法但矛盾的响应（理论场景：工具真的拒绝解锁且响应 < 100ms）→ 会被过滤，
  但此场景极端罕见

## 方案

收到与 `_pendingLockCommand` 矛盾的响应时，检查当前指令的发出时间：

| 延迟 | 判定 | 行为 |
|---|---|---|
| < 100ms | 延迟响应 | 丢弃，保留 `_pendingLockCommand` |
| 100ms ~ 5s | 真正失败 | cooldown 重置 + WARN |
| ≥ 5s | 响应超时 | 清 `_pendingLockCommand` + WARN（兜底） |

```csharp
// UpdateInternalLockState 中
if (expected == PendingLockCommand.Unlock && newLockedState) {
    long unlockAge = now - Volatile.Read(ref _lastUnlockTimestamp);
    if (unlockAge < StaleResponseThresholdMs) {  // < 100ms
        _pendingLockCommand = PendingLockCommand.Unlock;
        return;  // 丢弃，保持等待
    }
    if (unlockAge >= LockCooldownMs) {  // ≥ 5s
        logger.Warn("超时，清理 pending"); // 兜底
    } else {
        _lastUnlockTimestamp = 0;        // 真正失败
        logger.Warn("Unlock failed...");
    }
}
```

**改动范围**：`ToolTask.cs` + `MainUtils.cs` + `IniFileKeys.cs`，3 个文件  
**风险**：低。只在矛盾场景生效，正常路径不受影响  
**效果**：消除 30-80ms 窗口内的误判重试，预计减少 80%+ 的 Unlock failed  
**配置**：`stale_response_delay_ms`（ini 键，默认 100ms，无 UI，开发者可手动修改）

## 阈值选择依据

基于日志中提取的响应延迟数据：

| 响应类型 | 样本数 | 延迟范围 |
|---|---|---|
| 正确 Unlock ok | 全程正常点位切换 | 90ms ~ 470ms |
| 正确 Lock ok | 全程正常点位切换 | 100ms ~ 400ms |
| 延迟 Lock ok（导致误判） | 91 次 Unlock failed 对应的延迟响应 | 30ms ~ 80ms |

`StaleResponseThresholdMs = 100` 落在两组之间，不会误杀合法响应。

## 影响范围

- `ToolTask.cs` — `UpdateInternalLockState` 三路判断 + `ForceSendLock/Unlock` 前置清理
- `MainUtils.cs` — `GetStaleResponseDelayMs()` 配置读取
- `IniFileKeys.cs` — `stale_response_delay_ms` 键
- 不改变正常 lock/unlock 流程
- 不影响 PSet、拧紧数据等其他消息的解析
- 日志变化：部分 `WARN Unlock failed` 变为 `DEBUG Stale ... discarded`

## 验证方法

1. 构建通过：`dotnet build`
2. 部署后观察日志：`grep "Unlock failed\|Stale.*discarded"` 确认误判减少
3. 操作员反馈：点位切换迟滞是否改善
