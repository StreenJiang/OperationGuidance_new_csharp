# PSet 重试竞态修复设计

**日期：** 2026-08-13
**状态：** 已批准

## 概述

阿特拉斯 PF 系列工具《程序号下发》失败后，客户端自动"断连+重连+重发"的重试机制永远无效（每轮重试 100% 超时），用户只能重新激活任务才能恢复。日志分析定位到两个环节：

1. **触发环节**：Lock/Unlock 命令密集切换后，PF 控制器会话失活（停止响应客户端命令，但 TCP 连接仍存活、仍推送拧紧数据），旧连接上的 PSet 等不到 ACK 而超时。
2. **重试锁死环节**：`ReconnectAndResendPset` 用 TCP 层 `Connected` 判定"重连完成"，早于 PF 应用层握手（connect/data/curve 三连，实测 300–470ms）。PSet 每次都抢在握手完成前发出——此时 RunTask 接收循环尚未启动（无人解析 ACK），且 PSet 的 ACK（MID 0005 + tail 0018，约 25 字节）会被握手线程的 `ReceiveAsync` 当作 data enable 握手响应误读吞掉。结果每轮重试必然超时，7 轮全败。

本设计修复重试锁死竞态，并加固握手过程，使每轮重试具备独立成功能力。日志证明重连后的新会话本身健康（握手成功、Lock 反馈恢复），竞态修复后重试机制即可真正生效。

## 日志证据（2026-08-07 现场，M011 / M040）

三个失败集群，模式完全一致（每轮 ~1.3s，7 轮全败）：

```
20:14:30,532 PSet sending timeout          ← 旧连接首次失败（环节1）
20:14:30,566 ReconnectAndResendPset start
20:14:30,662 Socket connected              ← TCP 建立
20:14:30,857 reconnected after 200ms      ← 轮询 Connected 误判完成
20:14:30,858 Sending PSet 1               ← ★ PSet 抢在握手前发出
20:14:31,133 Handshake response len=222   ← 握手第一响应此刻才到
20:14:31,225 Connection successful → Task thread started  ← RunTask 此刻才启动
20:14:31,876 PSet sending timeout         ← 必然超时，进入下一轮，重复同一竞态
```

## 方案

涉及 3 个文件，共 5 个修复点。

### 修复点 1（核心）：重连判定改为会话就绪

**文件：** `OperationGuidance_new/Tasks/ToolTask.cs`
**方法：** `ReconnectAndResendPset`（第581行）

轮询条件从 `!Connected` 改为 `Status != ATaskBase.CONNECTED`：

```csharp
// 改前
while (!Connected && pollCount < pollMax && !token.IsCancellationRequested) { ... }

// 改后
while (Status != ATaskBase.CONNECTED && pollCount < pollMax && !token.IsCancellationRequested) { ... }
```

`Status = CONNECTED` 只在握手三连完成、`RunTask()` 接收循环启动之后才置位（`Connect()` 第222–223行，`RunTask()` 先于置位调用）。轮询上限保持 200ms×50=10s（日志实测握手 300–470ms，最慢一轮重连 3.8s，余量充足）。

日志中"reconnected after 200ms"将变为"reconnected after ~400–600ms"，且 `Sending PSet` 必然出现在 `Connection successful` / `Task thread started` 之后。

### 修复点 2（核心）：SendPSetAsync 发送前置条件收紧

**文件：** 同上
**方法：** `SendPSetAsync`（第478行）

```csharp
// 改前
if (!Connected) { ... return false; }

// 改后
if (Status != ATaskBase.CONNECTED || !Connected) { ... return false; }
```

两个条件缺一不可：
- `Status == CONNECTED` 保证握手完成（排除重连窗口内抢跑）；
- `Connected` 保证 RunTask 死亡清理（finally 置 `socketClient = null`）后不会误发。

效果：重连窗口内任何外部 PSet 触发（工作台快速路径 `AWorkplaceContentPanel.cs:2117`、`ToolOperationPopUpForm.cs:152`）都被拒绝并进入既有重试流程，从所有入口杜绝抢跑。Sudong X7 / FIT FTC6 的 Connect 同样在连接完成后才置 CONNECTED，语义一致，无副作用。

### 修复点 3：Status 改 volatile

**文件：** `OperationGuidance_new/Tasks/AbstractClasses/ATaskBase.cs`（第19行）

```csharp
// 改前
public int Status { get; set; }

// 改后
private volatile int _status;
public int Status { get => _status; set => _status = value; }
```

Status 从本次修复起承担跨线程同步职责（重试轮询线程 ← Connect 后台线程；`TaskCheckingLoop`（`TaskInitializer.cs:92`）← 重试线程），普通 int 无内存可见性保证。所有继承类（ToolTask、CommunicationTask、IoBoxTask、ArmTask、SerialPortTask）读写语义不变。

### 修复点 4（防御）：握手接收的 MID 过滤 + 超时

**文件：** `OperationGuidance_new/Tasks/ToolTask.cs`
**方法：** `SendAndReceiveOnlyForPreparingAsync`（第427–472行）

现状两个隐患：
1. 读到的任何报文都直接返回——握手期间控制器推送的拧紧数据（MID 0061，日志中 420 字节报文随时可能来）或 PSet ACK（MID 0005）会导致握手误判/误吞；
2. `ReceiveAsync` 无超时（`ReceiveTimeout = 200` 只约束同步 `Receive`）——控制器不响应握手命令时握手线程永久挂起。

改为增加响应校验和总超时：

```csharp
// command: 要发送的命令
// responsePredicate: 预期响应判定，null 表示任意响应都接受（保持现状）
// timeoutMs: 总超时，默认 5000
private async Task<string?> SendAndReceiveOnlyForPreparingAsync(
    string command, Func<string, bool>? responsePredicate = null, int timeoutMs = 5000)
```

逻辑：发送 → 带超时的 `ReceiveAsync` → 响应不匹配 predicate 则丢弃并继续等待（拧紧数据/杂讯被安全跳过）→ 总超时（默认 5s）耗尽返回 null。

调用处（`ConnectToServer`）传入与现有校验一致的条件：
- connect 握手（第321行）：`mid == "0002" || mid == "0005"`；
- data enable 握手（第338行）：同；
- curve enable 握手：保持宽松（不校验 MID）。

超时返回 null → `ConnectToServer` 判失败 → `Connect()` 内部循环 500ms 后重试，不再永久挂起。

实现注意：当前调用使用 `ReceiveAsync` 的 ArraySegment 重载（第449行），需切换为带 `CancellationToken` 的 Memory 重载（`CancellationTokenSource.CancelAfter(5000)`）。取消挂起的 `ReceiveAsync` 后该 socket 不可复用（运行时语义），因此取消/超时后一律走"返回 null → 放弃 socket 重建"路径，与 `ConnectToServer` 的失败清理（第384-387行）自然衔接。

**握手期间非预期报文的去向**：被过滤丢弃（如拧紧数据 MID 0061）。这与现状等价——现状下握手收到 MID 0061 会导致 MID 校验失败、整个连接重建，该报文同样丢失；修复不引入新的数据损失。

### 修复点 5：消除失败路径下的双并发 Connect 任务

**文件：** `OperationGuidance_new/Tasks/ToolTask.cs`
**方法：** `CloseToTriggerReconnectionAsync`（第264行）

现状：该方法无条件执行 `_connectInProgress = 0`。而 `Connect()` 是 fire-and-forget（内部 `while (!Connected)` 无限重试、无取消机制），重试轮之间又是零延迟（`RetryStrategy.FixedDelay(..., 0)`）。当某一轮重连 10s 轮询超时、下一轮立即开始时，上一轮遗留的 Connect 后台任务仍在运行——无条件清零使其与新任务**双并发**：两个任务同时写共享字段 `socketClient`、同时握手、同时 `RunTask()`（`_runTaskTask` 被覆盖），两个接收循环共享同一 socket，任一 finally 关闭 socket 会杀死另一个循环。这与"每轮重试具备独立成功能力"直接冲突。

改为**移除无条件清零**：

```csharp
// 改前
socketClient?.Close();
socketClient = null;
_connectInProgress = 0;   // ← 删除此行

// 改后
socketClient?.Close();
socketClient = null;
```

删除后，`Connect()` 的防重入（`Interlocked.Exchange`，第203行）自然保证**全系统只有一个 Connect 后台任务**：新 `Connect()` 调用在旧任务存活时被跳过（打 WARN），轮询等待旧任务成功即可——旧任务的 `while (!Connected)` 无限重试，控制器恢复后必然置 `Status = CONNECTED`，行为等价且无双并发。旧任务最终退出时 `finally`（第238行）自会清零 `_connectInProgress`，无需外力干预。

## 修复后的时序推演

**重试轮**（对照日志失败轮）：

```
Socket connected                ← TCP 建立，Status 仍 CONNECTING
轮询等待（~400–600ms）          ← 不发任何东西；外部 PSet 被修复点2拒绝
握手完成 → RunTask 启动 → Status=CONNECTED
Sending PSet                    ← 接收循环已就绪
ACK 被 RunTask 解析（解析 ≤100ms；SendPSetAsync 的 200ms 轮询粒度使其观察到成功 ≤500ms，1000ms 等待窗口余量 ≥2 倍）→ PSet success
```

**控制器失活再发**（环节1）：旧连接上 PSet 超时（1000ms 是固有检测手段）→ 触发重试轮 → 新会话健康（日志已证明）→ 重试成功。失活被重试机制治愈，无需单独处理。

**握手期间控制器推送拧紧数据**：被 MID 过滤丢弃，握手继续。最多损失一条拧紧数据（重连场景本就可能丢），换取会话可靠建立。

**控制器完全不响应握手**：5s 超时 → 重试循环，不再永久挂起。

## 错误处理

- 重连 10s 超时、握手 5s 超时均返回失败 → `RetryStrategy.FixedDelay(_resendPsetMaxTimes, 0)` 的下一轮重试自动接管；`Status = DISCONNECTED` 恢复（第593行既有逻辑），`TaskCheckingLoop` 继续兜底。
- `OperationCanceledException`（任务切换/重新激活）路径保持现状：`Status = DISCONNECTED` 后返回 false，与 `_activeMissionCts` 取消语义兼容。

## 测试计划

### 单元测试（`OperationGuidance_new.Tests`，扩展 `TestableToolTask`）

1. `SendPSetAsync` 在 `Status != CONNECTED` 时返回 false 且不调用 `SendCommand`（`SetConnected(true)` + Status 默认 DISCONNECTED）。
2. `SendPSetAsync` 在 `Connected == false` 时返回 false（现有行为回归）。
3. 现有 `ToolTaskLockUnlockTests` 等全部回归通过。

### 集成测试（本地 TcpListener 模拟 PF 控制器）

用 `TcpListener`（127.0.0.1）按 Open Protocol 模拟 PF6000：
- 收到 connect 命令 → 回 MID 0002 响应（222 字节，格式参照 `ToolPF6000OP.COMMAND_CONNECT_ASCII`）；
- 收到 data enable 命令 → 回 MID 0005 响应；
- 收到 curve enable 命令 → 回响应；
- 收到 PSet 命令（MID 0018）→ 回 MID 0005 + tail 0018 的 ACK；
- 服务端记录每条命令到达时刻与响应发出时刻。

测试场景：
1. 建立连接并完成握手 → 服务端主动断开（模拟半开）→ 调 `ReconnectAndResendPset(5)` → 断言返回 true，且服务端记录的 **PSet 命令到达时刻晚于本轮握手响应发出时刻**（直接锁定"PSet 不抢跑"）。
2. 握手期间服务端先推送一条 MID 0061 拧紧数据再回握手响应 → 断言握手仍成功（MID 过滤生效）。
3. 服务端不回握手响应 → 断言 `Connect()` 5s 超时后进入重试而非挂起。
4. **端到端故障注入**（复现日志 20:14 失败集群的完整链路）：正常连接+握手后，服务端对客户端命令**停止响应但保持 TCP 连接**（模拟控制器会话失活）→ 客户端 `SendPSetAsync` 1000ms 超时（既有行为）→ 上层重试触发 `ReconnectAndResendPset` → 服务端接受新连接并正常握手、回 PSet ACK → 断言重试轮成功。该场景直接证明"下发失败后无需重新激活任务即可自动恢复"这一核心承诺。

集成测试使用真实 `ToolTask` 实例（非 `TestableToolTask`，后者重写 `Connected` 与 `SendCommand` 会掩盖真实 socket 状态与发送路径）。

**测试实现约束**（由协议与代码行为决定）：
- 客户端握手与接收均无分帧逻辑（一次只读一个 TCP 段），服务端每条响应必须**分开发送且间隔 ≥20ms**，否则多条响应合并进同一段会破坏握手/解析；
- `ConnectAsync()` 仅同步包装立即返回，测试必须**轮询 `Status`/`Connected`** 等待连接完成，不能 `await`；
- `ReconnectAndResendPset(pset, token)` 需显式传 `CancellationToken`；
- 心跳周期 5s（`HeartBeatDelay`），每个测试阶段控制在 5s 内，或让服务端对 MID 9999 心跳命令做无响应/响应处理；
- 集成测试服务端需按真实报文构造响应：长度头 `{len:D4}`（= 总字符数 − 1，不含尾部 `\x00`）+ MID + 数据 + `\x00`；PSet ACK 为 `"0025"+"0005"+"001"+"0    00  0018"+"\x00"`（末 4 字符须为 0018，`GetTail` 取末 4 位）。

## 影响范围

- 仅 `ToolTask` / `ATaskBase.Status` 的连接与 PSet 发送时序，不改协议报文、不改 UI、不改数据库。
- 受益路径：工作台自动重试、弹窗手动重试、`ToolOperationPopUpForm` 手动下发、`TaskCheckingLoop` 保活重连后的所有 PSet 发送。
- WHYC / SCII / GLB / YF / TZYX 全部站点共用 `ToolTask`，统一生效。

## 明确不做

- 不改 Lock/Unlock 命令风暴的 UI 调用链（独立问题，本次重试修复后其后果已被治愈）。
- 不加控制器会话失活主动探测（心跳响应校验等）——现有超时+重试已覆盖，属过度设计。
- 不重构 `Connect()` 为可等待/可取消（generation 令牌等）——修复点 5 的最小改动已消除双并发，完整重构超出本次范围。
- 不动 `_currentPSet` 缓存、`_sendLock`/`SyncObject` 锁结构。

## 验证步骤

客户直接用于生产、无手动测试窗口，因此**自动化测试是交付前唯一的验证防线**：

1. `dotnet build` 编译通过。
2. `dotnet test` 全绿（含新增单元测试 + 4 个集成测试，其中场景 4 端到端复现日志失败集群并断言自动恢复）。
3. 部署后**被动观察**（不要求客户配合）：若生产日志中再出现 `PSet sending timeout`，紧随其后的重试轮应呈现 `reconnected` → `Connection successful` → `Sending PSet` → `PSet success` 并自动恢复；连续多轮 `PSet sending timeout`（日志 20:14 模式）不应再出现。
4. 回滚安全：改动仅在重连窗口与发送前置条件处改变行为，正常连接路径（占绝大多数运行时间）行为不变；如需回滚，单文件（ToolTask.cs + ATaskBase.cs）即可还原。
