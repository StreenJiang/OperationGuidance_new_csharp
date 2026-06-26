# ToolTask 握手阶段连接失败修复

**日期：** 2026-06-05
**版本：** v1.6.x
**关联提交：** 669b0dc（引入 bug）

## 问题

进入工作台后 ToolTask（扭矩枪 PF6000-OP）无限重连失败，日志表现为：

```
Socket connected → Send/receive error (SocketException 10060) → 递归重发命令
→ Connect response: 0002 ✓ → Data enable response: 0004 ✗ → Connection failed → 重试
```

`0004` 是工具的异常响应码，期望值为 `0002` 或 `0005`。

## 根因

提交 `669b0dc` 对 `SendAndReceiveOnlyForPreparingAsync` 做了两个改动：

1. **`ReceiveAsync` → 同步 `Receive`**：同步 `Receive` 受 `ReceiveTimeout=200ms` 限制，而 PF6000-OP 握手响应需要 300-800ms，每次 Receive 必然超时
2. **catch 块递归重发命令**：超时后 `return await SendAndReceiveOnlyForPreparingAsync(command)` 向工具重复发送同一条握手命令，工具收到两条重复命令后协议状态机混乱，后续 DATA ENABLE 返回 `0004`

旧代码的 `ReceiveAsync`（Task-based）不受 `ReceiveTimeout` 限制，可以无限等待响应，因此旧代码正常工作。

## 修复方案

### 改动文件

`OperationGuidance_new/Tasks/ToolTask.cs` — 仅 `SendAndReceiveOnlyForPreparingAsync` 方法

### 改动 1：Send 锁内 + ReceiveAsync 锁外

```csharp
// 之前：lock 内同步 Receive，200ms 超时必死
lock (SyncObject) {
    if (!Connected) {
        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive aborted - disconnected");
        return null;
    }
    socketClient.Send(data);
    msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
}

// 之后：lock 只保护 Send，ReceiveAsync 在锁外无超时等待
lock (SyncObject) {
    socketClient.Send(data);
}
int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
```

- `Send` 保留在锁内：与 `RunTask` 主循环和 `SendCommand` 保持一致的线程安全模式
- 去掉 `Connected` 检查：握手时 socket 刚建连，`ConnectToServer` 已做 ping + connect 验证，不需要重复打断
- `ReceiveAsync` 在锁外：握手阶段 `RunTask` 尚未启动，没有其他线程在收数据

### 改动 2：移除递归重发

```csharp
// 之前：超时后递归重发同一条命令
} catch (Exception e) {
    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Send/receive error", e);
    return await SendAndReceiveOnlyForPreparingAsync(command);
}

// 之后：异常直接返回 null
} catch (Exception e) {
    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive error", e);
    return null;
}
```

### 改动 3：`ConnectToServer` 失败时无条件关闭 socket

```csharp
// 之前：依赖 Connected 属性（异常后不可靠，可能为 false 导致 socket 不释放）
} else {
    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed");
    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
        socketClient.Close();
        socketClient = null;
    }
}

// 之后：失败即关，与 CloseToTriggerReconnection/CloseConnection 模式一致
} else {
    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed");
    socketClient?.Close();
    socketClient = null;
}
```

失败链路：`SendAndReceiveOnlyForPreparingAsync` 返回 null → `ConnectToServer` 返回 false → `Connect()` 的 `while (!Connected)` 循环关闭 socket、`await Task.Delay(AutoReconnectingTrialDelay)` 后从 socket 新建连开始重试，给工具一个干净的协议起点。

## 不改的部分

| 项 | 理由 |
|----|------|
| `SyncObject` 实例级 | 语义正确，不同设备不应共享锁 |
| `_connectInProgress` | 防并发 Connect，正确 |
| `RunTask` 主循环的同步 `Receive` + 200ms 超时 | 用于驱动心跳计数器，需要保持 |
| `CloseToTriggerReconnectionAsync` | PSet 重连需要，正确 |
| `SendCommand` 的 `lock (SyncObject) { Send }` | 正确，保持不变 |

## 风险评估

- **低风险**：改动范围局限在一个方法内，只影响握手阶段，不影响已连接后的正常收发
- `ReceiveAsync` 无超时：如果工具永远不响应，Task 会一直挂着。但外层 `Connect()` 本身在 `Task.Run` 里跑，不会阻塞主线程；且 socket 连接失败或工具断电时 ReceiveAsync 会抛异常自然退出
