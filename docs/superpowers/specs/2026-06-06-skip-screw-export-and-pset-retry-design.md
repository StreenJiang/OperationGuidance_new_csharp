# Design: 跳过螺丝点位导出修复 & PSet/Lock 优化

**Date**: 2026-06-06
**Status**: Draft

## 背景

四个问题，涉及 SCII 工作台的数据导出、mission_record 存储、PSet 下发、Lock/Unlock 冷却机制。

---

## 问题 #5 — 跳过螺丝点位导出物料码

### 现状

`WorkplaceMissionView_SCII.ActivateMission()` 在 `SkipScrewPoints = true` 时：
1. 创建 `_missionRecord`（含 `product_bar_code`, `parts_bar_code` 等）
2. 保存到 DB → 立即 `TerminateMission(FINISHED_OK)` → `OnMissionCompleted` → `ExportAsync`
3. 导出时 `_tighteningDataVOs` 为空 → 文件只有表头行（row 1），row 2 空白

### 目标

若 `parts_bar_code`（物料码）有值，导出文件的第二行 "物料码" 对应列填入该值。

### 方案

修改 `ExportRequest` + `DataExportService`，传递物料码并在空数据时生成一行。

**改动点**：

1. **`ExportRequest`** — 新增 `PartsBarCode` 属性
```csharp
public string? PartsBarCode { get; init; }
```

2. **`AWorkplaceContentPanel.OnMissionCompleted`** — 传递物料码
```csharp
var request = new ExportRequest {
    // ... existing fields ...
    PartsBarCode = _missionRecord?.parts_bar_code,  // 新增
};
```

3. **`AVariableSettingsView.cs`** — 同上，传递 `PartsBarCode`（可传 null）

4. **`DataExportService.ExportAsync`** — 空数据时构造物料码行
```csharp
if (data.Count == 0 && !string.IsNullOrEmpty(request.PartsBarCode)) {
    // 找到 parts_bar_code 列索引
    int colIndex = propertyNames.IndexOf("parts_bar_code");
    if (colIndex >= 0) {
        var row = new List<object?>(new object?[propertyNames.Count]);
        row[colIndex] = request.PartsBarCode;
        rows.Add(row);
    }
}
```

**影响范围**：4 个文件，纯增量改动。

---

## 问题 #6 — 更新后首次 mission_record 未保存

### 现状

客户反馈：软件更新后第一次跳过螺丝点位，`mission_record` 表没有记录；
之后的都正常。代码中 `ActivateMission` (line 1198) 已调用 `AddOrUpdateMissionRecord`。

### 目标

定位并修复首次不保存的问题。

### 方案：诊断 + 加固

由于无法在本地复现（仅在客户现场"更新后首次"出现），采用**先诊断、再修复**策略：

**Step 1 — 加日志**：
在 `ActivateMission` 的 save 调用前后加详细日志，记录：
- 调用前：`_missionRecord` 各字段值
- 调用后：返回值中的 `MissionRecordDTO.id`
- 异常：完整 stack trace

```csharp
logger.Info($"[SCII:SkipScrew] Saving mission_record: mission_id={_missionRecord.mission_id}, ...");
try {
    var rsp = _apis.AddOrUpdateMissionRecord(new(_missionRecord));
    logger.Info($"[SCII:SkipScrew] Saved OK, returned id={rsp.MissionRecordDTO.id}");
} catch (Exception ex) {
    logger.Error($"[SCII:SkipScrew] Save failed", ex);
}
```

**Step 2 — 发布后让客户验证**，根据日志定位根因。

**可能根因假设**（待日志验证）：
- A) DB 连接池未初始化 → 首次 Insert 超时/失败
- B) IoC 懒加载某服务未就绪 → `InsertOrUpdate` 静默失败
- C) `AddOrUpdateMissionRecordReq` 拷贝构造函数问题 → DTO 字段丢失

## 问题 #3 — PSet 重试失败 & Lock/Unlock 冷却失效

### 日志分析（2026-06-06 08:19 客户现场日志）

#### 失败序列（boltNum=6, pset=13）

| 时间 | 事件 |
|---|---|
| 08:18:56,953 | boltNum=5 pset=12 成功 |
| 08:19:00,884 | Force locking |
| 08:19:00,917 | **第1次尝试**: `Sending PSet 13`（快速路径） |
| 08:19:00,943 | Unlocking ← **PSet 发出后仅 26ms 就有 Unlock 命令！** |
| 08:19:00,990~01,935 | 工具连续回复 `Lock ok`（tail=0042），**从未回复 `Pset sending ok`（tail=0018）** |
| 08:19:01,955 | **超时** — waited=5, psetSentOk=false |
| 08:19:01,956 | **重试1**: ReconnectAndResendPset → 重连 → `Sending PSet 13` |
| 08:19:02~03 | PSet 等待期间发送了 **13 条 Locking 命令** |
| 08:19:03,279 | **超时** — 工具不回复 Pset sending ok |
| 08:19:03,280 | **重试2**: 同流程 → **超时** |
| 08:19:04,517 | **重试3**: 同流程 → 继续失败... |

#### 对比：同任务 pset=13 成功案例（boltNum=3）

| 时间 | 事件 |
|---|---|
| 08:18:44,602 | `Sending PSet 13` |
| 08:18:44,623~45,517 | 期间 14 条 Unlocking 命令（与失败案例一样密集） |
| 08:18:45,518 | **`Pset sending ok`** ✅ — 工具在 916ms 后确认 |

#### 结论

- PSet 失败是**偶发**的（pset=13 第一次成、第二次败），不是代码逻辑必然错误
- 成功案例和失败案例都有 Lock/Unlock 命令在 PSet 等待期间并发发送
- **根因是 PSet 等待期间 Lock/Unlock 命令不受控制地并发**，工具偶尔因此忽略 PSet 确认

---

## 问题 #4 — Lock/Unlock 冷却机制失效

### 现状

`SendLock()` / `SendUnlock()` 有 5 秒冷却（`LockingCooldownPeriod = 5000`），`ForceSendLock()` / `ForceSendUnlock()` 无冷却。

### 根因分析

**冷却时间戳在收到工具 RESPONSE 时更新，而不是发送命令时更新。**

ToolTask.cs `UpdateInternalLockState` (line 748):
```csharp
if (newLockedState) {
    _lastLockTimestamp = now;
    _lastUnlockTimestamp = 0;   // <- 重置为 0，解除 unlock 冷却！
}
```

日志证据 — 所有 Unlock 命令后工具回复 `Lock ok`（不是 `Unlock ok`）：
```
8:19:00,943 Unlocking
8:19:00,991 Lock ok -> Lock state: True -> True
```
`_locked` 仍为 `true` -> 下一轮 `SendUnlock()` 通过 `!_locked` 检查 -> 再次发送

冷却对 **Lock** 方向失效同理：工具回复 `Lock ok` -> `_lastUnlockTimestamp = 0` -> unlock 冷却被清除 -> 下一帧又能发 Unlock。

**死循环路径**：
1. `SendUnlock()` -> `IsInCooldown(0)` -> false（通过） -> `!_locked` -> true（通过） -> 发送
2. 工具回复 `Lock ok` -> `UpdateInternalLockState(true)` -> `_lastUnlockTimestamp = 0`
3. 回到步骤 1，无限循环

### 附带问题

- `ForceSendLock()`/`ForceSendUnlock()` 完全绕过冷却和 `_locked` 检查

### 方案

全部改动在 `ToolTask.cs` 一个文件。核心原则：

1. 冷却时间戳在 **send 时** 设置
2. Lock/Unlock 冷却**各自独立**，互不影响
3. `ForceSendLock`/`ForceSendUnlock` 不检查冷却、不检查 `_locked`，但发送后设置同方向冷却
4. 收到工具回复后，若 `_locked` 状态与预期不符，**重置对应方向冷却**（允许立即重试）

#### 新增字段

```csharp
// 0=none, 1=waiting lock confirm, -1=waiting unlock confirm
private volatile int _pendingLockState = 0;
```

#### 规则表

| 方法 | Lock冷却检查 | Unlock冷却检查 | `_locked`检查 | 发送后设置 | pendingLockState |
|---|---|---|---|---|---|
| `SendLock()` | Yes | - | Yes | `_lastLockTimestamp` | 1 |
| `SendUnlock()` | - | Yes | Yes | `_lastUnlockTimestamp` | -1 |
| `ForceSendLock()` | - | - | - | `_lastLockTimestamp` | 0 |
| `ForceSendUnlock()` | - | - | - | `_lastUnlockTimestamp` | 0 |

#### UpdateInternalLockState — 响应确认

```csharp
private void UpdateInternalLockState(bool newLockedState) {
    bool oldLocked = _locked;
    _locked = newLockedState;
    int expected = Volatile.Read(ref _pendingLockState);

    if (expected == 1 && !newLockedState) {
        // 期望 Lock，但工具报告 unlocked → Lock 失败，重置冷却
        Volatile.Write(ref _lastLockTimestamp, 0);
        logger.Warn($"[TOOL:...] Lock failed (tool reports unlocked), cooldown reset");
    } else if (expected == -1 && newLockedState) {
        // 期望 Unlock，但工具报告 locked → Unlock 失败，重置冷却
        Volatile.Write(ref _lastUnlockTimestamp, 0);
        logger.Warn($"[TOOL:...] Unlock failed (tool reports locked), cooldown reset");
    }
    // 匹配或 Force(expected==0)：保持冷却，不做额外处理
    Volatile.Write(ref _pendingLockState, 0);

    logger.Info($"[TOOL:...] Lock state: {oldLocked} -> {_locked}");
}
```

#### 场景示例

```
SendLock()       → _lastLockTimestamp=now, _pendingLockState=1
... 工具回复 Lock ok → _locked=true, expected匹配 → 冷却保持
SendLock()       → 冷却检查(skip, 5s内) → return

SendLock()       → _lastLockTimestamp=now, _pendingLockState=1
... 工具回复 Unlock ok → _locked=false, expected不匹配 → 冷却重置！
SendLock()       → 冷却检查(ok, 刚重置) → 可以重试
````IsInCooldown` 保持原有签名不变。

---

## 测试

### 问题 #5
- [ ] 跳过螺丝点位 + 有物料码 -> 导出文件 row 2 物料码列有值
- [ ] 跳过螺丝点位 + 无物料码 -> 导出文件只有表头（与现状一致）
- [ ] 正常拧紧任务导出不受影响

### 问题 #6
- [ ] 首次启动后跳过螺丝点位 -> 日志有完整 save 记录
- [ ] `mission_record` 表有对应记录且字段完整

### 问题 #4 — Lock/Unlock 冷却
- [ ] Lock 命令发送后 5s 内 `SendLock()` 被跳过
- [ ] Unlock 命令发送后 5s 内 `SendUnlock()` 被跳过
- [ ] `ForceSendLock()` 发送后，`SendLock()` 在 5s 内被跳过
- [ ] `ForceSendUnlock()` 发送后，`SendUnlock()` 在 5s 内被跳过
- [ ] Force 连续调用不受冷却限制
