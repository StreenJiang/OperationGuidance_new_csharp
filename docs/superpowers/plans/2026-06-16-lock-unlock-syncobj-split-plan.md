# ToolTask SyncObject 拆分 + 锁状态机重构 — 实施计划

## 关联

- Spec: `docs/superpowers/specs/2026-06-16-lock-unlock-syncobj-split-design.md`
- 目标文件: `OperationGuidance_new/Tasks/ToolTask.cs`

## 改动清单

### Step 1: 新增 `_sendLock` + `HeartBeatCounter` 加 `volatile`

```csharp
// Fields 区域，SyncObject 下方：
private readonly object SyncObject = new();
private readonly object _sendLock = new();          // ← 新增

// HeartBeatCounter 加 volatile：
private volatile int HeartBeatCounter;              // ← int → volatile int
```

### Step 2: 删除 Cooldown 相关字段

删除以下字段和常量（`_pendingLockCommand` 本身承担去重职责）：

```csharp
// 删除：
private const int LockCooldownMs = 5000;
private static readonly int StaleResponseThresholdMs = ...;
private long _lastLockTimestamp = 0;
private long _lastUnlockTimestamp = 0;
```

### Step 3: `SendCommand()` — 换锁 + 重置心跳

```csharp
// 改动前：
lock (SyncObject) {
    num = socketClient?.Send(data);
}

// 改动后：
lock (_sendLock) {
    num = socketClient?.Send(data);
}
if (num.HasValue && num.Value > 0) {
    HeartBeatCounter = 0;   // 任意命令发送成功 → 推迟心跳
    return true;
}
```

同时在 `ConnectToServer()` socket 创建处增加：

```csharp
socketClient.SendTimeout = 500;  // ← 新增
```

### Step 4: 主循环 — 移除 HeartBeatCounter 重置

PF Series（约第 81-83 行）：

```csharp
// 改动前：
SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
HeartBeatCounter = 0;

// 改动后：
SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
```

FITFTC6 同理。

### Step 5: `SendLock()` — pending 去重 + 放宽 + 锁外 I/O

```csharp
public void SendLock() {
    lock (LockSyncObject) {
        if (!Connected) return;
        if (_pendingLockCommand == PendingLockCommand.Lock) return;     // 相同命令已发出 → 去重
        if (_locked && _pendingLockCommand != PendingLockCommand.Unlock) return;  // 已锁且无冲突 pending → 跳过

        logger.Info("Locking");
        _pendingLockCommand = PendingLockCommand.Lock;                 // 覆盖 None 或 Unlock
    }
    PerformLock();
}
```

**逻辑**：
- `pending == Lock` → 去重
- `_locked && pending != Unlock` → 已是目标状态，跳过
- `_locked && pending == Unlock` → unlock 在路上，arm 切回 lock → 覆盖，发 Lock
- `!_locked` → 正常发出
- `pending == None` → 正常发出

### Step 6: `SendUnlock()` — 同理

```csharp
public void SendUnlock() {
    lock (LockSyncObject) {
        if (!Connected) return;
        if (_pendingLockCommand == PendingLockCommand.Unlock) return;  // 去重
        if (!_locked && _pendingLockCommand != PendingLockCommand.Lock) return;  // 已解锁且无冲突 pending → 跳过

        logger.Info("Unlocking");
        _pendingLockCommand = PendingLockCommand.Unlock;               // 覆盖 None 或 Lock
    }
    PerformUnlock();
}
```

### Step 7: `ForceSendLock()` / `ForceSendUnlock()` — 状态在锁内，I/O 在锁外

```csharp
public void ForceSendLock() {
    lock (LockSyncObject) {
        if (!Connected) return;
        logger.Info("Force locking");
        _pendingLockCommand = PendingLockCommand.None;
        UpdateInternalLockState(true);                        // ← 留在锁内，无竞态窗口
    }
    PerformLock();
}

public void ForceSendUnlock() {
    lock (LockSyncObject) {
        if (!Connected) return;
        logger.Info("Force unlocking");
        _pendingLockCommand = PendingLockCommand.None;
        UpdateInternalLockState(false);                       // ← 留在锁内
    }
    PerformUnlock();
}
```

### Step 8: `UpdateInternalLockState()` — 删除过期响应 + cooldown 分支

`_lastLockTimestamp` / `_lastUnlockTimestamp` / `StaleResponseThresholdMs` 已删除，过期响应过滤和 cooldown 相关分支全部移除。方法简化为：

```csharp
private void UpdateInternalLockState(bool newLockedState) {
    bool oldLocked = _locked;
    _locked = newLockedState;
    _pendingLockCommand = PendingLockCommand.None;

    if (oldLocked != _locked) {
        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
    }
}
```

### Step 9: 清理 `MainUtils.GetStaleResponseDelayMs()` 死代码

`StaleResponseThresholdMs` 删除后，`MainUtils.GetStaleResponseDelayMs()` 无任何调用方。
删除该方法（及配套 `IniFileKeys.StaleResponseDelayMs` 字符串常量）。

**MainUtils.cs** 约第 722 行：
```csharp
// 删除 ↓
public static int GetStaleResponseDelayMs() {
    string value = Settings.Read(IniFileKeys.StaleResponseDelayMs);
    if (string.IsNullOrEmpty(value)) {
        Settings.Write(IniFileKeys.StaleResponseDelayMs, "100");
        return 100;
    }
    return int.TryParse(value, out int result) ? result : 100;
}
```

**IniFileKeys.cs** 第 31 行：
```csharp
// 删除 ↓
public static string StaleResponseDelayMs => "stale_response_delay_ms";
```

> **注**：SudongX7 `PerformLock()`/`PerformUnlock()` 内含 `Thread.Sleep(200)`，不受影响。

## 删改汇总

| 类型 | 项 | 行数 |
|------|----|------|
| 新增 | `_sendLock` 字段 | +1 |
| 修改 | `HeartBeatCounter` 加 `volatile` | ±1 |
| 删除 | `LockCooldownMs`, `StaleResponseThresholdMs`, `_lastLockTimestamp`, `_lastUnlockTimestamp` | -4 |
| 修改 | `SendCommand()` 换锁 + 重置计数器 | ~5 |
| 新增 | `socketClient.SendTimeout = 500` | +1 |
| 删除 | 主循环 `HeartBeatCounter = 0` ×2 | -2 |
| 重写 | `SendLock()` | ~8 |
| 重写 | `SendUnlock()` | ~8 |
| 重写 | `ForceSendLock()` | ~5 |
| 重写 | `ForceSendUnlock()` | ~5 |
| 简化 | `UpdateInternalLockState()` | -30 |
| 删除 | `MainUtils.GetStaleResponseDelayMs()` + `IniFileKeys.StaleResponseDelayMs` | -9 |
| **净变化** | | **~-15 行** |
| **涉及文件** | `ToolTask.cs`, `MainUtils.cs`, `IniFileKeys.cs` | 3 files |

## 不改动

- `CloseConnection()` / `CloseToTriggerReconnectionAsync()` — 仍用 `SyncObject`
- 主循环 Receive — 仍用 `SyncObject`
- `SendAndReceiveOnlyForPreparingAsync()` — 握手阶段
- `AnalyzeData()` — 业务逻辑不变
- `PerformLock()` / `PerformUnlock()` — 方法体不变
- PF/FITFTC6/速动 X7 的工具类型分支不变

## 验证

| # | 验证项 | 方法 |
|---|--------|------|
| 1 | 编译通过 | `dotnet build OperationGuidance_new` |
| 2 | Arm 50ms 高频 dup 不被重复发送 | grep "Unlocking" 确认 200ms 内只有 1 条 |
| 3 | Lock↔Unlock 即时切换（< 50ms） | arm 调 Unlock 后下个周期即发送 |
| 4 | 心跳在空闲后才发 | 连续发送命令期间无 `Sending heartbeat` |
| 5 | 手动按钮 lock/unlock 仍然即时 | 点击后 `Lock state` 立即变化 |
