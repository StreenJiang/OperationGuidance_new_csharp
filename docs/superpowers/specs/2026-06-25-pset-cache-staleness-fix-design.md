# PSet 缓存过期修复设计

## 问题

客户反馈 sendPset 出现最终结果错位：软件感知下发 PSet X 成功，但设备实际被设置为 PSet Y。

### 根因

`_currentPSet` 缓存用于跳过重复 PSet 下发（`SendPSetAsync` line 487-490）：

```csharp
if (_currentPSet == pSetNumber) {
    return true;  // 跳过，不发送
}
```

缓存只在以下路径重置为 -1：

- `CloseToTriggerReconnection()` (line 256)
- `CloseToTriggerReconnectionAsync()` (line 262)

但当 RunTask 因网络异常退出后，`TaskCheckingLoop` 通过 `Reconnect()` → `ConnectAsync()` → `Connect()` 自动重连时，**`_currentPSet` 未重置**。导致 `SendPSetAsync` 基于过期缓存直接返回 true，工具实际可能已回退到默认 PSet。

**错位场景**：

1. 工具连接正常，`_currentPSet = 5`
2. 网络抖动 → RunTask 抛 SocketException 退出
3. TaskCheckingLoop 检测 `!Connected` → `Reconnect()` → `Connect()` → 重连成功
4. `_currentPSet` 保持 5
5. 下个螺栓需 PSet 5 → `SendPSetAsync(5)` → `_currentPSet == 5` → 返回 true，不发送
6. 工具已重置为 PSet 0 → 拧紧用错参数

### 日志佐证

`2026-06-24.log` 中 PSet 超时后 `ReconnectAndResendPset` 路径正确重置 `_currentPSet = -1`，但 `TaskCheckingLoop` → `Reconnect()` 路径无任何重置日志。

## 方案

在 `ToolTask.Connect()` 入口重置 `_currentPSet = -1`。`Connect()` 是所有重连路径的汇聚点：

- `ReconnectAndResendPset` → `Connect()`
- `TaskCheckingLoop` → `Reconnect()` → `ConnectAsync()` → `Connect()`
- 任何其他重连入口

一行覆盖全部场景。

## 变更清单

| 文件 | 变更 |
|------|------|
| `Tasks/ToolTask.cs` | `Connect()` 方法第一行加 `_currentPSet = -1;` |

### 同时清理冗余重置

`CloseToTriggerReconnection()` 和 `CloseToTriggerReconnectionAsync()` 中的 `_currentPSet = -1` 删除。`Connect()` 是唯一重置点，这些调用在 `Connect()` 之前，不再需要。

## 变更清单

| 文件 | 变更 |
|------|------|
| `Tasks/ToolTask.cs` | `Connect()` 第一行加 `_currentPSet = -1;` |
| `Tasks/ToolTask.cs` | `CloseToTriggerReconnection()` 删除 `_currentPSet = -1;` |
| `Tasks/ToolTask.cs` | `CloseToTriggerReconnectionAsync()` 删除 `_currentPSet = -1;` |

## 不改动

- `SendPSetAsync` 其余逻辑不变
- `SendPSet` (AWorkplaceContentPanel) 不变
