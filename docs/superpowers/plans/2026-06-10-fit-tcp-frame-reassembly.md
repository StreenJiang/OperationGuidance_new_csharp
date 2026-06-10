# FIT AA55 帧 TCP 拆包残留缓冲实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `ToolFIT` 中增加跨 `Receive` 的字节残留缓冲，防止 TCP 拆包导致 AA55 帧丢失

**Architecture:** 在 `ToolFIT` 增加 `_frameResidual` 字段。`UnpackData` 通过 `out byte[] residual` 返回末尾不完整的 AA55 帧片段。`AnalyzeData` 在下一次调用时拼接残留与新数据

**Tech Stack:** C#, .NET, 无新依赖

**改动文件:** `OperationGuidance_new/Constants/DeviceType_Tool.cs`, `OperationGuidance_new/Tasks/ToolTask.cs`

---

### Task 1: 添加 `_frameResidual` 字段

**Files:**
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs`

- [ ] **Step 1: 在 ToolFIT 类中添加 `_frameResidual` 字段**

在 `ToolFIT` 类中，紧接已有的 `_reassembler` 字段（第 520 行）之后插入：

```csharp
private readonly PacketReassembler _reassembler = new PacketReassembler();
private byte[] _frameResidual = Array.Empty<byte>();  // ← 新增
private int _residualAttempts = 0;                    // ← 新增
```

- [ ] **Step 2: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Constants/DeviceType_Tool.cs
git commit -m "feat(fit): add _frameResidual field for cross-Receive frame reassembly"
```

---

### Task 2: 修改 UnpackData 签名、残留逻辑和日志

**Files:**
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs:721-806`

- [ ] **Step 1: 修改 UnpackData 签名**

将第 721 行：
```csharp
private List<byte[]> UnpackData(byte[] data) {
```

改为：
```csharp
private List<byte[]> UnpackData(byte[] data, out byte[] residual) {
```

- [ ] **Step 2: 在方法开头初始化 residual**

在第 722 行 `var result = new List<byte[]>();` 之后插入：

```csharp
residual = Array.Empty<byte>();
```

- [ ] **Step 3: 将末尾不完整 AA55 帧保存为 residual，加诊断日志**

将第 767-774 行：
```csharp
                    } else {
                        // 剩余全是字符串数据
                        if (i < data.Length) {
                            byte[] stringData = new byte[data.Length - i];
                            Array.Copy(data, i, stringData, 0, stringData.Length);
                            result.Add(stringData);
                        }
                        break;
                    }
```

改为：
```csharp
                    } else {
                        // 末尾不完整的 AA55 帧 → 作为残留保留，等待下一次 Receive 拼接
                        if (i < data.Length) {
                            residual = new byte[data.Length - i];
                            Array.Copy(data, i, residual, 0, residual.Length);
                            logger.Debug($"Partial AA55 frame at stream end, saved {residual.Length} bytes as residual: {MainUtils.ToHexString(residual)}");
                        }
                        break;
                    }
```

注意：仅改此一处（AA55 头 + 不完整帧 + 无后续 AA55 的 fallback 路径）。非 AA55 开头的末尾数据（第 794-800 行）保持不变，那确实是文本数据，不应保存为残留。

- [ ] **Step 4: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：编译失败，因为 `AnalyzeData` 中调用 `UnpackData(msgBytes)` 还未更新参数。这是预期的，下一步修复。

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Constants/DeviceType_Tool.cs
git commit -m "feat(fit): UnpackData returns partial frame as residual with diagnostic logging"
```

---

### Task 3: 修改 AnalyzeData — 拼接残留、日志埋点、安全保护

**Files:**
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs:541-560`

- [ ] **Step 1: 替换 AnalyzeData 方法体开头**

将 `AnalyzeData` 方法体开头（第 541-544 行）：
```csharp
public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, Func<CurveDataTemp, int, Task>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
    List<byte[]> dataList = UnpackData(msgBytes);
    string originalMsg = MainUtils.ToHexString(msgBytes);
    logger.Info($"OriginalMsg = {originalMsg}");
```

改为：
```csharp
public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction, Action<TighteningData, int>? actionAfterAnalysis = null, Func<CurveDataTemp, int, Task>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
    // ── 跨 Receive 帧残留拼接 ──
    bool hadResidual = _frameResidual.Length > 0;
    byte[] combined;

    if (hadResidual) {
        _residualAttempts++;

        // 安全保护1：连续 3 次拼接仍未消费 → 丢弃残留
        if (_residualAttempts > 3) {
            logger.Warn($"[FIT] Residual not consumed after 3 attempts ({_frameResidual.Length} bytes), discarding: {MainUtils.ToHexString(_frameResidual)}");
            _frameResidual = Array.Empty<byte>();
            _residualAttempts = 0;
            hadResidual = false;
            combined = msgBytes;
        }
        // 安全保护2：残留不以 AA55 开头 → 数据异常，丢弃
        else if (_frameResidual[0] != 0xAA || _frameResidual[1] != 0x55) {
            logger.Warn($"[FIT] Malformed residual (header not AA55, {_frameResidual.Length} bytes), discarding: {MainUtils.ToHexString(_frameResidual)}");
            _frameResidual = Array.Empty<byte>();
            _residualAttempts = 0;
            hadResidual = false;
            combined = msgBytes;
        }
        // 正常拼接
        else {
            logger.Info($"[FIT] Prepending residual ({_frameResidual.Length} bytes, attempt {_residualAttempts}/3)");
            combined = new byte[_frameResidual.Length + msgBytes.Length];
            Array.Copy(_frameResidual, 0, combined, 0, _frameResidual.Length);
            Array.Copy(msgBytes, 0, combined, _frameResidual.Length, msgBytes.Length);
        }
    } else {
        _residualAttempts = 0;
        combined = msgBytes;
    }

    List<byte[]> dataList = UnpackData(combined, out byte[] residual);

    // ── 诊断日志：跟踪残留消费结果 ──
    if (hadResidual) {
        if (residual.Length == 0) {
            logger.Info("[FIT] Frame reassembled successfully from residual");
        } else {
            logger.Debug($"[FIT] Frame still incomplete after attempt {_residualAttempts}/3, residual={residual.Length} bytes");
        }
    } else if (residual.Length > 0) {
        logger.Debug($"[FIT] New partial frame detected, residual={residual.Length} bytes");
    }

    // ── 保存本次残留（带安全上限）──
    if (residual.Length > 4096) {
        logger.Warn($"[FIT] Residual exceeded 4096 bytes ({residual.Length}), discarding to prevent memory growth");
        _frameResidual = Array.Empty<byte>();
        _residualAttempts = 0;
    } else {
        _frameResidual = residual;
    }

    // ── 原有日志（改为记录 combined，确保拼接后的完整数据可见）──
    string originalMsg = MainUtils.ToHexString(combined);
    logger.Info($"OriginalMsg = {originalMsg}");
```

- [ ] **Step 2: 修复 _checkHeadOk 为 false 时仍进入 switch 的缺陷**

在 `AnalyzeData` 的 `foreach` 循环中，`logger.Info($"Handling dataMessage = {dataMessage}");`（第 555 行）之后，`FitCommandType cmd = ...`（第 557 行）之前插入：

```csharp
                    if (!headOk) continue; // 非 AA55 帧头的数据不按二进制协议解析
```

完整上下文：
```csharp
                    logger.Info($"Handling dataMessage = {dataMessage}");

                    if (!headOk) continue; // ← 新增

                    FitCommandType cmd = (FitCommandType) data[2];
```

- [ ] **Step 3: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：编译成功，无警告。

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Constants/DeviceType_Tool.cs
git commit -m "feat(fit): reassemble partial AA55 frames across Receive calls with diagnostic logging"
```

---

### Task 4: 添加 ClearResidual() 并在 ToolTask 重连时调用

**Files:**
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs`
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`

- [ ] **Step 1: 在 DeviceTypeTool 基类添加虚方法**

在 `DeviceTypeTool` 类中，`GetPSetCommand` 声明之后（第 61 行）插入：

```csharp
public virtual void ClearResidual() { }
```

- [ ] **Step 2: 在 ToolFIT 中 override**

在 `ToolFIT` 类中，`_frameResidual` 字段之后（第 522 行附近）添加：

```csharp
public override void ClearResidual() {
    if (_frameResidual.Length > 0) {
        logger.Debug($"[FIT] Clearing stale frame residual ({_frameResidual.Length} bytes) on connection reset");
        _frameResidual = Array.Empty<byte>();
        _residualAttempts = 0;
    }
}
```

- [ ] **Step 3: 在 ToolTask.Connect() 连接成功后调用**

在 `ToolTask.cs` 第 226-227 行之间插入 `ClearResidual()` 调用：

```csharp
// 当前代码（第 225-228 行）：
if (await ConnectToServer()) {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
    RunTask();                                               // ← 插入前
    Status = CONNECTED;

// 改为：
if (await ConnectToServer()) {
    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
    _toolType.ClearResidual();  // ← 清除旧连接残留
    RunTask();
    Status = CONNECTED;
```

- [ ] **Step 4: 在 ToolTask.CloseConnection() 断开时调用**

在 `ToolTask.cs` 的 `CloseConnection()` 方法中，`socketClient.Close()` 之后插入：

```csharp
public override void CloseConnection() {
    // ...现有逻辑...
    if (Connected) {
        socketClient.Close();
        socketClient = null;
        _toolType.ClearResidual();  // ← 清除残留
    }
    // ...
}
```

- [ ] **Step 5: 构建验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：编译成功，无警告。

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Constants/DeviceType_Tool.cs OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "feat(fit): clear frame residual on connection reset to prevent stale data pollution"
```

---

### Task 5: 最终验证

**Files:**
- 无新文件

- [ ] **Step 1: 完整构建**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

预期：Build succeeded，0 Error(s)，0 Warning(s)。

- [ ] **Step 2: 检查调用链无遗漏**

确认 `UnpackData` 的唯一调用点在 `AnalyzeData`（第 542 行），无其他调用方需要更新：

```bash
grep -n "UnpackData" OperationGuidance_new/Constants/DeviceType_Tool.cs
```

预期输出两行：方法定义 + AnalyzeData 中的调用，全部已更新。

- [ ] **Step 3: Commit**

```bash
# 如有任何遗漏修复则提交，否则此步可跳过
```

---

## 改动小结

| 位置 | 改动 | 行数 |
|------|------|------|
| `ToolFIT` 字段区 | 新增 `_frameResidual` + `_residualAttempts` | +2 |
| `DeviceTypeTool` | 新增 `virtual ClearResidual()` | +1 |
| `ToolFIT` override | `ClearResidual()` 实现 | +7 |
| `UnpackData` 签名 | 增加 `out byte[] residual` | 改1 |
| `UnpackData` 方法体 | 初始化 residual + 残留路径改 + Debug 日志 | +1, 改4 |
| `AnalyzeData` 开头 | 拼接 + 重试计数 + 3 级安全保护 + 5 处诊断日志 | +38, 改2 |
| `AnalyzeData` foreach | `if (!headOk) continue;` 防止文本数据误入二进制解析 | +1 |
| `ToolTask` | Connect + CloseConnection 各加 1 行 ClearResidual 调用 | +2 |
| **总计** | | **~55 行净增，2 文件** |
