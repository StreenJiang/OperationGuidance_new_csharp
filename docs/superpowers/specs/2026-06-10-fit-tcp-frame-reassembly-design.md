# FIT AA55 帧 TCP 拆包残留缓冲设计

## 背景

FIT 协议使用 AA55 帧格式：

```
AA55 + cmd(1) + len(2,LE) + content(len bytes) + 55AA
```

`ToolTask` 的接收循环每次 `socket.Receive()` 拿到一批字节，直接交给 `ToolFIT.AnalyzeData` → `UnpackData` 处理。当前实现没有跨 `Receive` 的字节缓冲，当 TCP 将一个 AA55 帧切分到两次 `Receive` 时，不完整的帧会被 `UnpackData` 的 fallback 路径当作文本数据丢弃，导致漏帧。

**影响范围**：心跳（HEART_BEAT_RSP）、锁枪（LOCK）、解枪（LOCK signal=1）、程序号下发（PESET）、拧紧数据（FINAL_DATA）——所有经 AA55 帧传输的响应。CURVE_DATA 不受影响（已有 `PacketReassembler` 做跨帧累积）。

## 设计目标

- AA55 帧被 TCP 边界切分时，残留字节自动拼接到下一次 `Receive`，帧不丢失
- 改动集中在 `ToolFIT` 内部，不影响 `ToolTask` 或其他调用方
- 对 `UnpackData` 已有的粘包处理（多帧一次 Receive）零影响

## 方案

### 核心思路

在 `ToolFIT` 增加一个 `_frameResidual` 字节缓冲区，保存每次 `Receive` 末尾不完整的 AA55 帧片段。下一次 `AnalyzeData` 调用时，将残留数据拼接到新数据前面，交给 `UnpackData` 统一处理。

```
Receive #1: [完整帧A] [帧B前半段]      → 帧A正常处理，帧B前半段→_frameResidual
Receive #2: [_frameResidual] [帧B后半段] → 拼接后帧B完整，正常处理
```

### 改动点

**文件**：`OperationGuidance_new/Constants/DeviceType_Tool.cs`

#### 1. 新增字段

```csharp
// ToolFIT 类
private byte[] _frameResidual = Array.Empty<byte>();
private int _residualAttempts = 0;
```

#### 2. UnpackData 增加 residual 输出

当前签名：
```csharp
private List<byte[]> UnpackData(byte[] data)
```

改为：
```csharp
private List<byte[]> UnpackData(byte[] data, out byte[] residual)
```

改动逻辑——在末尾不完整 AA55 帧的 fallback 路径（当前第 767-774 行）：

```csharp
// 旧逻辑：当作文本丢弃
} else {
    if (i < data.Length) {
        byte[] stringData = new byte[data.Length - i];
        Array.Copy(data, i, stringData, 0, stringData.Length);
        result.Add(stringData);  // ← 丢给上层当文本乱码
    }
    break;
}

// 新逻辑：作为残留保留
} else {
    if (i < data.Length) {
        residual = new byte[data.Length - i];
        Array.Copy(data, i, residual, 0, residual.Length);
    }
    break;
}
```

**注意**：只有"末尾无后续 AA55"的路径需要改。"中间有下一个 AA55"的路径保持不变——不完整的中间帧仍然丢弃，因为后续字节已经属于下一帧。

#### 3. AnalyzeData 拼接残留 + 诊断日志

在 `AnalyzeData` 开头拼接残留，末尾保存残留。关键埋点：

| 埋点 | 级别 | 场景 |
|------|------|------|
| `Prepending residual (N bytes, attempt X/3)` | Info | 有残留待拼接 |
| `Frame reassembled successfully` | Info | 拼接后形成完整帧 |
| `New partial frame detected` | Debug | 本次 Receive 末尾出现新的不完整帧 |
| `Still incomplete after attempt X/3` | Debug | 拼接后仍不完整 |
| `Residual not consumed after 3 attempts` | Warn | 重试超限丢弃 |
| `Malformed residual (header not AA55)` | Warn | 残留数据异常丢弃 |
| `Residual exceeded 4096 bytes` | Warn | 残留过大丢弃 |

```csharp
public override void AnalyzeData(byte[] msgBytes, ...) {
    bool hadResidual = _frameResidual.Length > 0;
    byte[] combined;

    if (hadResidual) {
        _residualAttempts++;
        if (_residualAttempts > 3) {
            logger.Warn($"[FIT] Residual not consumed after 3 attempts, discarding");
            _frameResidual = Array.Empty<byte>(); _residualAttempts = 0;
            hadResidual = false; combined = msgBytes;
        } else if (_frameResidual[0] != 0xAA || _frameResidual[1] != 0x55) {
            logger.Warn($"[FIT] Malformed residual, discarding");
            _frameResidual = Array.Empty<byte>(); _residualAttempts = 0;
            hadResidual = false; combined = msgBytes;
        } else {
            logger.Info($"[FIT] Prepending residual ({_frameResidual.Length} bytes, attempt {_residualAttempts}/3)");
            combined = new byte[_frameResidual.Length + msgBytes.Length];
            Array.Copy(_frameResidual, 0, combined, 0, _frameResidual.Length);
            Array.Copy(msgBytes, 0, combined, _frameResidual.Length, msgBytes.Length);
        }
    } else {
        _residualAttempts = 0; combined = msgBytes;
    }

    List<byte[]> dataList = UnpackData(combined, out byte[] residual);

    // 诊断跟踪
    if (hadResidual && residual.Length == 0)
        logger.Info("[FIT] Frame reassembled successfully from residual");
    else if (hadResidual && residual.Length > 0)
        logger.Debug($"[FIT] Frame still incomplete after attempt {_residualAttempts}/3");
    else if (!hadResidual && residual.Length > 0)
        logger.Debug($"[FIT] New partial frame detected, residual={residual.Length} bytes");

    // 保存残留（带上限保护）
    if (residual.Length > 4096) {
        logger.Warn($"[FIT] Residual exceeded 4096 bytes, discarding");
        _frameResidual = Array.Empty<byte>(); _residualAttempts = 0;
    } else {
        _frameResidual = residual;
    }

    string originalMsg = MainUtils.ToHexString(combined);
    logger.Info($"OriginalMsg = {originalMsg}");
    // 后续处理不变...
}
```

### 边界情况

| 场景 | 行为 |
|------|------|
| 残留 + 新数据仍不完整 | 新的不完整帧再次保存到 `_frameResidual`，`_residualAttempts++` |
| 残留 + 新数据形成完整帧 | 正常处理，`_frameResidual` 清空，`_residualAttempts` 归零 |
| 残留 + 新数据形成多帧 | while 循环逐帧消费，最后的残留（如有）继续保存，计数递增 |
| 连续 3 次拼接未消费残留 | 丢弃残留并记录 Warning，重置计数 |
| 残留数据积累过大（异常） | 上限保护：`_frameResidual.Length > 4096` 时丢弃并记录 Warning |
| 残留数据不以 AA55 开头 | 拼接后 `combined[0] != 0xAA`，丢弃残留并记录 Warning |

### 线程模型

`AnalyzeData` 调用链：`ToolTask` 接收循环 → `AnalyzeData` → `UnpackData`，所有调用在同一个后台线程上。无需加锁。

### 连接重置

重连时 `ToolFIT` 实例复用。旧连接的残留数据属于已关闭的 TCP 流，必须在新连接建立后清除，防止帧污染。通过 `virtual ClearResidual()` 实现：

- `DeviceTypeTool` 基类：空实现
- `ToolFIT`：清空 `_frameResidual` 并重置 `_residualAttempts`
- `ToolTask.Connect()`：`ConnectToServer()` 成功后、`RunTask()` 前调用
- `ToolTask.CloseConnection()`：socket 关闭后调用

### 不做的事

- **不兼容旧版 FIT 文本协议**（`Signal N, ret = M` / `[PSET N] Succeeded`）——这是工具厂商固件退化的问题，应由厂商修复
- **不改动 `ToolTask` 的接收循环**——改动完全封装在 `ToolFIT` 内部（`ClearResidual()` 调用除外，仅一行）
- **不改动 `PacketReassembler`**——曲线数据的累积机制与此独立

### 顺手修复

- **`_checkHeadOk` 为 false 时仍进入 switch**：在 `AnalyzeData` 的 foreach 循环中，`headOk == false` 时加 `continue` 跳过 `switch(cmd)`。文本数据不应被当作二进制帧解析，之前碰巧无害（`data[2]` 的 ASCII 字节不匹配任何枚举值），但属于结构性缺陷。
