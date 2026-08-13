# PSet 重试竞态修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 Atlas PF 工具程序号下发失败后重试机制每轮必败的竞态（PSet 抢在应用层握手完成前发出），使每轮重试具备独立成功能力。

**Architecture:** 用 `Status == CONNECTED`（会话就绪：握手完成 + 接收循环已启动）替代 TCP 层 `Connected` 作为 PSet 发送与重连完成的判定；握手接收增加预期 MID 过滤与 5s 超时；移除 `CloseToTriggerReconnectionAsync` 中无条件清零 `_connectInProgress`，由防重入收敛为单一连接任务。自动化测试（单元 + TcpListener 模拟 PF6000 集成测试）是唯一交付前验证防线。

**Tech Stack:** C# net6.0-windows、WinForms、xunit、TcpListener（Open Protocol 模拟）

**Spec:** `docs/superpowers/specs/2026-08-13-pset-retry-race-fix-design.md`

## Global Constraints

- **禁止执行任何 git 提交命令或调用 git-commit 技能**（用户全局规则，提交由用户手动执行；任务间无需停顿等待提交）。
- 手术式改动：不重构无关代码、不清理既有死代码；匹配现有风格（中文注释、`logger.Info/Warn/Error` 日志格式）。
- 测试项目 `OperationGuidance_new.Tests`（xunit，net6.0-windows），测试类命名空间 `OperationGuidance_new.Tests.Tasks`。
- 构建命令：`dotnet build OperationGuidance_new/OperationGuidance_new.csproj`；测试命令：`dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`。
- 所有日志断言基于英文 marker（生产日志为 GBK 编码，勿用中文匹配）。

---

### Task 1: Status 改 volatile + SendPSetAsync 前置条件收紧

**Files:**
- Modify: `OperationGuidance_new/Tasks/AbstractClasses/ATaskBase.cs:19`
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:478-481`
- Test: `OperationGuidance_new.Tests/Tasks/SendPSetPreconditionTests.cs`（新建）

**Interfaces:**
- Consumes: `TestableToolTask`（已有：`SetConnected`、`SetSendHandler`、`SetSendResults`）；`ATaskBase.CONNECTED/CONNECTING/DISCONNECTED` 常量。
- Produces: `ATaskBase.Status`（volatile 字段语义不变）；`SendPSetAsync(int)` 行为：会话未就绪时立即返回 false 且不发送。

- [ ] **Step 1: 写失败的单元测试**

新建 `OperationGuidance_new.Tests/Tasks/SendPSetPreconditionTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SendPSetPreconditionTests
{
    [Fact]
    public async Task SendPSetAsync_RejectsWhenSessionNotReady()
    {
        var task = new TestableToolTask();
        task.SetConnected(true);          // TCP 已连
        task.Status = ATaskBase.CONNECTING; // 但会话未就绪（握手未完成）

        bool sendCalled = false;
        task.SetSendHandler(_ => { sendCalled = true; return true; });

        bool result = await task.SendPSetAsync(5);

        Assert.False(result);
        Assert.False(sendCalled); // 关键断言：不得发送任何命令
    }

    [Fact]
    public async Task SendPSetAsync_RejectsWhenNotConnected()
    {
        var task = new TestableToolTask();
        task.SetConnected(false);
        task.Status = ATaskBase.CONNECTED;

        task.SetSendHandler(_ => true);

        bool result = await task.SendPSetAsync(5);

        Assert.False(result);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SendPSetPreconditionTests"`
Expected: `SendPSetAsync_RejectsWhenSessionNotReady` FAIL（修复前 `!Connected` 为 false，进入发送，`sendCalled == true`）；`SendPSetAsync_RejectsWhenNotConnected` PASS（既有行为）。

- [ ] **Step 3: 改 ATaskBase.Status 为 volatile 字段**

`OperationGuidance_new/Tasks/AbstractClasses/ATaskBase.cs` 第19行：

```csharp
// 改前
public int Status { get; set; }
// 改后
private volatile int _status;
public int Status { get => _status; set => _status = value; }
```

- [ ] **Step 4: 收紧 SendPSetAsync 前置条件**

`OperationGuidance_new/Tasks/ToolTask.cs` 第478-481行：

```csharp
// 改前
if (!Connected) {
    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet failed - not connected");
    return false;
}
// 改后
if (Status != ATaskBase.CONNECTED || !Connected) {
    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet failed - session not ready (Status={Status}, Connected={Connected})");
    return false;
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SendPSetPreconditionTests"`
Expected: 2 PASS。

- [ ] **Step 6: 全量回归**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS（含既有 ToolTaskLockUnlockTests）。

---

### Task 2: MockPF6000Server 集成测试基础设施 + 连接 smoke 测试

**Files:**
- Create: `OperationGuidance_new.Tests/Tasks/MockPF6000Server.cs`
- Test: `OperationGuidance_new.Tests/Tasks/ToolTaskConnectionSmokeTests.cs`（新建）

**Interfaces:**
- Consumes: `DeviceType_Tool.PF6000_OP`；`ToolTask` 公开成员（`Connect()`、`Status`、`Connected`、`CloseConnection()`）。
- Produces: `MockPF6000Server` 类，后续 Task 3/4/5/6 依赖以下成员：
  - `MockPF6000Server()` / `Start()` / `int Port`
  - `bool SilentHandshake`（不回握手响应）、`bool PushTighteningOnHandshake`（握手期间先推 MID 0061 报文）、`bool SilentMode`（会话建立后不响应任何命令）
  - `int AcceptedConnections`、`int MaxConcurrentConnections`（活跃连接历史峰值）
  - `DateTime? LastHandshakeResponseSentUtc`（最后一轮握手 curve 响应发出时刻）、`DateTime? PSetCommandArrivalUtc`（PSet 命令到达时刻）
  - `void ResetTimeline()`（清空两个时间戳）
  - `static Task WaitUntilAsync(Func<bool> condition, int timeoutMs)`（轮询辅助，超时抛 `TimeoutException`）
  - `IDisposable`（停止监听、取消所有连接处理）

- [ ] **Step 1: 创建 MockPF6000Server**

新建 `OperationGuidance_new.Tests/Tasks/MockPF6000Server.cs`：

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// 模拟 Atlas PF6000 Open Protocol 控制器（TCP 服务端）。
/// 命令/响应格式：长度头(4位十进制, =总字符数-1, 不含尾部 \x00) + MID(4位) + 数据 + \x00。
/// 客户端无分帧逻辑，每条响应必须独立写入并间隔 ≥20ms，避免被客户端一次 Receive 合并。
/// </summary>
public sealed class MockPF6000Server : IDisposable
{
    private const int InterMessageDelayMs = 20;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private Task? _acceptLoop;
    private int _activeConnections;

    public int Port { get; private set; }

    /// <summary>不回任何握手响应（用于握手超时测试）。</summary>
    public bool SilentHandshake { get; set; }

    /// <summary>收到 connect 命令后、回握手响应前，先推送一条 MID 0061 拧紧数据报文。</summary>
    public bool PushTighteningOnHandshake { get; set; }

    /// <summary>会话建立后不响应任何命令，但保持 TCP 连接（模拟控制器会话失活）。</summary>
    public bool SilentMode { get; set; }

    public int AcceptedConnections { get; private set; }
    public int MaxConcurrentConnections { get; private set; }
    public DateTime? LastHandshakeResponseSentUtc { get; private set; }
    public DateTime? PSetCommandArrivalUtc { get; private set; }

    public MockPF6000Server()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0); // 随机可用端口
    }

    public void Start()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public void ResetTimeline()
    {
        lock (_lock)
        {
            LastHandshakeResponseSentUtc = null;
            PSetCommandArrivalUtc = null;
        }
    }

    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException($"condition not met within {timeoutMs}ms");
            await Task.Delay(50);
        }
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            lock (_lock)
            {
                _activeConnections++;
                AcceptedConnections++;
                MaxConcurrentConnections = Math.Max(MaxConcurrentConnections, _activeConnections);
            }
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var pending = new List<byte>();
                while (!_cts.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buffer, _cts.Token);
                    if (n <= 0) break; // 客户端断开
                    pending.AddRange(buffer.Take(n));
                    int idx;
                    while ((idx = pending.IndexOf(0)) >= 0)
                    {
                        string cmd = Encoding.ASCII.GetString(pending.Take(idx).ToArray());
                        pending.RemoveRange(0, idx + 1);
                        await HandleCommandAsync(stream, cmd);
                    }
                }
            }
        }
        catch
        {
            // 客户端断开或 Dispose 取消，正常退出
        }
        finally
        {
            lock (_lock) { _activeConnections--; }
        }
    }

    private async Task HandleCommandAsync(NetworkStream stream, string cmd)
    {
        string mid = cmd.Length >= 8 ? cmd.Substring(4, 4) : "";
        if (SilentMode) return;

        switch (mid)
        {
            case "0001": // connect 命令
                if (PushTighteningOnHandshake)
                {
                    await WriteAsync(stream, BuildResponse("0061", 50)); // 拧紧数据（握手线程应过滤掉）
                }
                if (SilentHandshake) return;
                await WriteAsync(stream, BuildResponse("0002", 221)); // 222 字节，与现场日志一致
                break;
            case "0060": // data enable 命令（勘误：生产代码 COMMAND_DATA_ASCII="002000600031..." 的 MID 实为 0060，非 0006）
                if (SilentHandshake) return;
                await WriteAsync(stream, BuildResponse("0005", 25));
                break;
            case "0008": // curve enable 命令（客户端不校验 MID）
                if (SilentHandshake) return;
                await WriteAsync(stream, BuildResponse("0005", 27));
                lock (_lock) { LastHandshakeResponseSentUtc = DateTime.UtcNow; }
                break;
            case "0018": // PSet 命令
                lock (_lock) { PSetCommandArrivalUtc = DateTime.UtcNow; }
                await WriteAsync(stream, "0025" + "0005" + "001" + new string('0', 10) + "0018" + "\x00");
                break;
            case "0042": // lock 命令 → lock ACK（tail 0042）
                await WriteAsync(stream, "0025" + "0005" + "001" + new string('0', 10) + "0042" + "\x00");
                break;
            case "0043": // unlock 命令 → unlock ACK（tail 0043）
                await WriteAsync(stream, "0025" + "0005" + "001" + new string('0', 10) + "0043" + "\x00");
                break;
            default:
                break; // 心跳(MID 9999)等不响应
        }
    }

    private async Task WriteAsync(NetworkStream stream, string payload)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(payload), _cts.Token);
        await Task.Delay(InterMessageDelayMs, _cts.Token); // 保证分段
    }

    /// <summary>构造 Open Protocol 响应：{长度:D4} + MID + "001" + 填充 + "\x00"。</summary>
    private static string BuildResponse(string mid, int totalChars)
    {
        string head = $"{totalChars:D4}";
        string pad = new string('0', totalChars - head.Length - mid.Length - 3);
        return head + mid + "001" + pad + "\x00";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
```

- [ ] **Step 2: 创建连接 smoke 测试**

新建 `OperationGuidance_new.Tests/Tasks/ToolTaskConnectionSmokeTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskConnectionSmokeTests
{
    [Fact]
    public async Task ToolTask_Connects_AgainstMockServer()
    {
        using var server = new MockPF6000Server();
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        task.Connect(); // fire-and-forget；ConnectAsync 只是同步包装，必须轮询等待

        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, 10000);

        Assert.True(task.Connected);
        Assert.True(server.AcceptedConnections >= 1);
        task.CloseConnection();
    }
}
```

- [ ] **Step 3: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskConnectionSmokeTests"`
Expected: PASS（约 1-2 秒；连接建立后 RunTask 启动、握手完成）。

---

### Task 3: 握手接收 MID 过滤 + 5s 超时（修复点 4）

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:427-472`（`SendAndReceiveOnlyForPreparingAsync`）
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:318、335、352`（`ConnectToServer` 三个调用点传入 predicate）
- Test: `OperationGuidance_new.Tests/Tasks/HandshakeRobustnessTests.cs`（新建）

**Interfaces:**
- Consumes: `MockPF6000Server`（Task 2 产出）。
- Produces: `private Task<string?> SendAndReceiveOnlyForPreparingAsync(string command, Func<string, bool>? responsePredicate = null, int timeoutMs = 5000)`——无 predicate 时行为与旧实现等价；有 predicate 时丢弃不匹配响应、总超时后返回 null；取消/超时后 socket 不可复用，返回 null 由 `ConnectToServer` 既有失败清理处理。

- [ ] **Step 1: 写失败的集成测试（场景 2、3）**

新建 `OperationGuidance_new.Tests/Tasks/HandshakeRobustnessTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class HandshakeRobustnessTests
{
    [Fact]
    public async Task Connect_SucceedsWhenTighteningDataArrivesDuringHandshake()
    {
        using var server = new MockPF6000Server { PushTighteningOnHandshake = true };
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        task.Connect();

        // 修复前：握手收到 MID 0061 → MID 校验失败 → 无限重试 → 10s 内 CONNECTED 不出现 → TimeoutException（红）
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, 10000);

        Assert.True(task.Connected);
        task.CloseConnection();
    }

    [Fact]
    public async Task Connect_RetriesInsteadOfHanging_WhenNoHandshakeResponse()
    {
        using var server = new MockPF6000Server { SilentHandshake = true };
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        task.Connect();

        // 修复前：握手 ReceiveAsync 无超时 → 永久挂起 → 8s 后 AcceptedConnections 仍为 1（红）
        // 修复后：5s 超时 → Connect 循环 500ms 后重试 → 第二次 accept
        await MockPF6000Server.WaitUntilAsync(() => server.AcceptedConnections >= 2, 8000);

        Assert.True(server.AcceptedConnections >= 2);
        task.CloseConnection();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~HandshakeRobustnessTests"`
Expected: 场景 2 FAIL（TimeoutException：10s 内未 CONNECTED）；场景 3 FAIL（8s 内 `AcceptedConnections` 仍为 1）。注意：场景 3 修复前测试约 8s 结束（断言失败），不会永久挂起。

- [ ] **Step 3: 改造 SendAndReceiveOnlyForPreparingAsync**

`OperationGuidance_new/Tasks/ToolTask.cs` 第427-472行整体替换：

```csharp
        private async Task<string?> SendAndReceiveOnlyForPreparingAsync(string command, Func<string, bool>? responsePredicate = null, int timeoutMs = 5000) {
            SendMessageRecevingCount++;

            if (Connected && SendMessageRecevingCount < SendMessageRecevingTimes) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;

                    byte[] data;
                    if (_toolType is ToolPFSeries) {
                        data = Encoding.ASCII.GetBytes(command);
                    } else if (_toolType is ToolSudongX7 || _toolType is ToolFITFTC6) {
                        data = MainUtils.ToBytes(command);
                    } else {
                        data = new byte[0];
                    }

                    // Send under lock for socket safety, ReceiveAsync outside lock (no timeout)
                    byte[] msgBytes = new byte[1024 * 1024];
                    lock (_sendLock) {
                        socketClient.Send(data);
                    }

                    // 带总超时的接收；响应不匹配预期 MID 时丢弃并继续等待。
                    // 注意：取消挂起的 ReceiveAsync 后该 socket 不可复用，
                    // 因此取消/超时一律返回 null，由 ConnectToServer 失败清理关闭 socket 并重建。
                    using var cts = new CancellationTokenSource(timeoutMs);
                    while (true) {
                        int msgLen;
                        try {
                            msgLen = await socketClient.ReceiveAsync(msgBytes.AsMemory(), SocketFlags.None, cts.Token);
                        } catch (OperationCanceledException) {
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake receive timeout after {timeoutMs}ms");
                            return null;
                        }
                        if (msgLen <= 0) {
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake receive aborted - connection closed by remote");
                            return null;
                        }

                        // 保留原实现的解码分支（FIT 用 GBK）
                        string result;
                        if (_toolType is ToolFITFTC6) {
                            result = Encoding.GetEncoding("GBK").GetString(msgBytes.Take(msgLen).ToArray());
                        } else {
                            result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                        }
                        if (responsePredicate == null || responsePredicate(result)) {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake response received, len={msgLen}");
                            return result;
                        }
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake received unexpected response, discarding and waiting (len={msgLen})");
                    }
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive error", e);
                    return null;
                }
            } else {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - not connected");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - max retries reached");
                }
            }
            return null;
        }
```

注意：原实现对 ToolFITFTC6 使用 GBK 解码（`Encoding.GetEncoding("GBK")`）；predicate 与解码仅用于 PF 握手路径（调用处只对 `ToolPFSeries` 传 predicate），FIT 的旧解码分支不再需要，因为 `SendAndReceiveOnlyForPreparingAsync` 对 FIT 的调用（第363行）不传 predicate、`mid` 检查也不做——但 FIT 的 result 被日志输出用（第364行 `logger.Info(... result: {result})`），统一 ASCII 解码对日志无影响。若 FIT 的 GBK 解码对业务有影响，保持现状即可，本任务不改。

- [ ] **Step 4: ConnectToServer 三个调用点传入 predicate**

`OperationGuidance_new/Tasks/ToolTask.cs`：

第317-318行（connect 握手）：
```csharp
// 改前
SendMessageRecevingCount = 0;
string? result1 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
// 改后
SendMessageRecevingCount = 0;
string? result1 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CONNECT_ASCII.GetMessage(),
    r => { string m = toolPF.GetMid(r); return m == "0002" || m == "0005"; });
```

第334-335行（data enable 握手）：
```csharp
// 改前
SendMessageRecevingCount = 0;
string? result2 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_DATA_ASCII.GetMessage());
// 改后
SendMessageRecevingCount = 0;
string? result2 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_DATA_ASCII.GetMessage(),
    r => { string m = toolPF.GetMid(r); return m == "0002" || m == "0005"; });
```

第351-352行（curve enable 握手，宽松不校验）：保持原样（不传 predicate）。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~HandshakeRobustnessTests"`
Expected: 2 PASS（场景 3 约 6s：5s 超时 + 500ms 重试间隔 + 第二次 accept）。

- [ ] **Step 6: 全量回归**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS（含 Task 1、2 的测试）。

---

### Task 4: 重连轮询条件改为会话就绪（修复点 1）+ 抢跑时序断言

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:581`
- Test: `OperationGuidance_new.Tests/Tasks/ToolTaskReconnectIntegrationTests.cs`（新建）

**Interfaces:**
- Consumes: `MockPF6000Server`（Task 2）、修复点 4（Task 3，重连握手有超时保障）。
- Produces: `ReconnectAndResendPset(int, CancellationToken)` 行为：仅在 `Status == CONNECTED`（握手完成 + RunTask 启动）后才发送 PSet。

- [ ] **Step 1: 写失败的集成测试（场景 1：时序断言）**

新建 `OperationGuidance_new.Tests/Tasks/ToolTaskReconnectIntegrationTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskReconnectIntegrationTests
{
    [Fact]
    public async Task ReconnectAndResendPset_SendsPSetOnlyAfterHandshake()
    {
        using var server = new MockPF6000Server();
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        // 首次正常连接
        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, 10000);
        server.ResetTimeline();

        // 触发断连+重连+重发
        using var cts = new CancellationTokenSource(20000);
        bool result = await task.ReconnectAndResendPset(5, cts.Token);

        Assert.True(result);
        // 核心时序断言：PSet 命令到达服务端的时刻，必须晚于本轮握手最后一条响应发出的时刻
        Assert.NotNull(server.PSetCommandArrivalUtc);
        Assert.NotNull(server.LastHandshakeResponseSentUtc);
        Assert.True(server.PSetCommandArrivalUtc >= server.LastHandshakeResponseSentUtc,
            $"PSet arrived at {server.PSetCommandArrivalUtc:O} before handshake completed at {server.LastHandshakeResponseSentUtc:O}");
        task.CloseConnection();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskReconnectIntegrationTests"`
Expected: FAIL——修复前轮询 `Connected` 在 TCP 建立后 200ms 即通过，PSet 抢在握手响应（~400ms 后）前到达服务端，时序断言失败（同时 `result` 为 false）。

- [ ] **Step 3: 修改轮询条件**

`OperationGuidance_new/Tasks/ToolTask.cs` 第581行：

```csharp
// 改前
while (!Connected && pollCount < pollMax && !token.IsCancellationRequested) {
// 改后
while (Status != ATaskBase.CONNECTED && pollCount < pollMax && !token.IsCancellationRequested) {
```

第591行的 `if (!Connected)` 保持不变：轮询退出时若 Status 已 CONNECTED 但 socket 又断（RunTask 秒死），按超时失败处理并返回 false，由下一轮重试接管。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskReconnectIntegrationTests"`
Expected: PASS（约 2-3s：重连 ~400-600ms + 发送 + ACK）。

- [ ] **Step 5: 全量回归**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS。

---

### Task 5: 移除 _connectInProgress 无条件清零（修复点 5）+ 并发连接数断言

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:264`
- Test: `OperationGuidance_new.Tests/Tasks/ReconnectConcurrencyTests.cs`（新建）

**Interfaces:**
- Consumes: `MockPF6000Server.MaxConcurrentConnections`（Task 2）、修复点 4（Task 3，握手 5s 超时是测试前提）。
- Produces: 全系统任意时刻至多一个 Connect 后台任务在运行。

- [ ] **Step 1: 写失败的集成测试**

新建 `OperationGuidance_new.Tests/Tasks/ReconnectConcurrencyTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ReconnectConcurrencyTests
{
    [Fact]
    public async Task ReconnectAndResendPset_NeverSpawnsSecondConcurrentConnect()
    {
        using var server = new MockPF6000Server { SilentHandshake = true };
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        // 后台 Connect 任务进入"5s 握手超时 → 重试"的循环节奏
        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => server.AcceptedConnections >= 1, 8000);

        // 在后台 Connect 任务存活期间触发重连重试（轮询 10s 必然超时返回 false）
        using var cts = new CancellationTokenSource(15000);
        bool result = await task.ReconnectAndResendPset(5, cts.Token);

        Assert.False(result); // 握手一直被静默，重试轮失败属预期
        // 核心断言：修复前 CloseToTriggerReconnectionAsync 无条件清零 _connectInProgress，
        // 新 Connect 任务与旧任务并发 → 服务端同一时刻存在 2 个活跃连接 → 红。
        // 修复后防重入跳过新任务，只有旧任务循环重试 → 并发恒为 1 → 绿。
        Assert.True(server.MaxConcurrentConnections <= 1,
            $"observed {server.MaxConcurrentConnections} concurrent connections, expected at most 1");
        task.CloseConnection();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ReconnectConcurrencyTests"`
Expected: FAIL（约 10-13s；`MaxConcurrentConnections == 2`，双并发 Connect 被观察到）。若测试偶发出现并发峰值未捕获（旧任务握手失败关闭与新任务连接的瞬间交错），属正常——服务端在 accept 即 +1，双任务并存窗口 ~5s，捕获概率极高。

- [ ] **Step 3: 删除无条件清零**

`OperationGuidance_new/Tasks/ToolTask.cs` 第260-264行：

```csharp
// 改前
        public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync start, _currentPSet={_currentPSet}, hasRunTask={_runTaskTask != null}");
            socketClient?.Close();
            socketClient = null;
            _connectInProgress = 0;
// 改后
        public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync start, _currentPSet={_currentPSet}, hasRunTask={_runTaskTask != null}");
            socketClient?.Close();
            socketClient = null;
```

`_connectInProgress` 仅由 `Connect()` 的防重入 `Interlocked.Exchange`（第203行）与 `finally`（第238行）管理；旧 Connect 任务存活时新 `Connect()` 调用被跳过并打 WARN，轮询等待旧任务成功——旧任务无限重试，控制器恢复后必然置 CONNECTED。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ReconnectConcurrencyTests"`
Expected: PASS（约 13s：5s 握手超时若干轮 + ReconnectAndResendPset 10s 轮询超时）。

- [ ] **Step 5: 全量回归**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS。

---

### Task 6: 端到端故障注入（场景 4）+ 最终回归 + 文档收尾

**Files:**
- Modify: `OperationGuidance_new.Tests/Tasks/ToolTaskReconnectIntegrationTests.cs`（追加场景 4）
- Modify: `docs/superpowers/specs/2026-08-13-pset-retry-race-fix-design.md`（状态 → 已批准）

**Interfaces:**
- Consumes: 修复点 1/2/3/4/5 全部完成后的 `ToolTask`。

- [ ] **Step 1: 追加端到端故障注入测试（场景 4）**

在 `OperationGuidance_new.Tests/Tasks/ToolTaskReconnectIntegrationTests.cs` 的类内追加：

```csharp
    /// <summary>
    /// 复现现场日志 20:14 失败集群的完整链路：会话失活（TCP 保持、命令无响应）
    /// → PSet 超时 → 重连重试 → 新会话恢复 → 下发成功。证明"无需重新激活任务即可自动恢复"。
    /// </summary>
    [Fact]
    public async Task PsetFailureRecoversViaReconnect_EndToEnd()
    {
        using var server = new MockPF6000Server();
        server.Start();
        var task = new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);

        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, 10000);

        // 模拟控制器会话失活：TCP 保持，但所有命令石沉大海
        server.SilentMode = true;
        bool first = await task.SendPSetAsync(5);
        Assert.False(first); // 1000ms 等待窗口内无 ACK，超时失败（环节 1 的固有检测手段）

        // 控制器恢复，触发断连+重连+重发
        server.SilentMode = false;
        using var cts = new CancellationTokenSource(20000);
        bool recovered = await task.ReconnectAndResendPset(5, cts.Token);

        // 修复前：重连轮 PSet 抢跑，recovered == false（红）
        // 修复后：会话就绪后才发送，ACK 被 RunTask 解析，recovered == true（绿）
        Assert.True(recovered);
        Assert.NotNull(server.PSetCommandArrivalUtc);
        Assert.NotNull(server.LastHandshakeResponseSentUtc);
        Assert.True(server.PSetCommandArrivalUtc >= server.LastHandshakeResponseSentUtc);
        task.CloseConnection();
    }
```

- [ ] **Step 2: 运行测试确认失败（可选，验证测试有效性）**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~PsetFailureRecoversViaReconnect_EndToEnd"`
Expected: PASS（修复点已在 Task 1-5 落地；若想观察红态可临时还原 Task 4 的轮询条件再运行，然后恢复）。

- [ ] **Step 3: 全量回归 + 构建**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj && dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 构建成功，全部测试 PASS（新增 2 单元 + 1 smoke + 5 集成 + 既有回归）。

- [ ] **Step 4: 设计文档状态更新**

`docs/superpowers/specs/2026-08-13-pset-retry-race-fix-design.md` 顶部：

```markdown
// 改前
**状态：** 待批准
// 改后
**状态：** 已批准
```

- [ ] **Step 5: 交付说明**

向用户汇报：改动文件清单（ATaskBase.cs、ToolTask.cs、Tests 下 6 个文件、spec/plan 文档）、测试结果摘要、部署后被动观察要点（生产日志中重试轮应呈现 `reconnected` → `Connection successful` → `Sending PSet` → `PSet success`；连续多轮 `PSet sending timeout` 不应再出现）。提示用户可自行 `/git-commit` 提交（agent 不执行提交）。
