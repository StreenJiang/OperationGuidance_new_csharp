# 速动 X7 程序号下发失败专项修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除速动 X7 程序号下发失败：修掉 TCP 粘包丢 ACK，并在 PSet 前等待设备进入可接收指令的静默窗口，使锁/解锁序列与 PSet 不再互相穿插。

**Architecture:** 三项 X7 专属改动 —— (1) `SudongFrameCodec` 用长度前缀把 TCP 字节流重组成完整帧；(2) `ToolTask` 记录最近一次拧紧结果与最近一次指令发送时间，PSet 前等待两个时间都超过 `PSetQuietWindowMs`（默认 1200ms，含 100ms 接收循环延迟余量）；(3) `SemaphoreSlim` 命令闸门只保护「最后一次静默复核 + PSet 发送」，避免与锁/解锁序列交叠。全部行为按 `_toolType is ToolSudongX7` 隔离，PF/FIT 不变；同时增加连接世代号、原始收发帧、PSet 上下文等诊断日志，供现场验证设备侧规则。

**Tech Stack:** C# net6.0-windows、WinForms、xunit 2.6.6、TcpListener（速动 55AA 帧模拟）

**Spec:** `docs/superpowers/specs/2026-10-26-sudong-x7-pset-timing-fix-design.md`

---

## 本次变更记录（Change Record）

| 项目 | 内容 |
|---|---|
| 日期 | 2026-10-26 |
| 触发问题 | 现场偶发「程序号下发失败」，重连重试最长 20 秒才恢复 |
| 现场 | 西艾爱；日志 `D:\安维能\work_files\西艾爱\2026-10-26\logs`（7 天） |
| 控制器 | **速动 SudongX7**（`TOOL[扭矩枪 - 192.168.1.23: 1200 - SudongX7]`） |
| 协议约束 | **协议完全不可修改**；仅改上位机 |
| 历史澄清 | 以往 PSet 重试修复均为 Atlas PF 系列；**X7 从未做过下发专项修复**（git log：Sudong 相关提交只有 CRC/错误处理/锁冷却） |
| 关键证据 | 首次下发距拧紧结果 <0.75s：1,179/1,179 失败；≥0.93s：48/48 成功。重试 2,788 次全部正确新建连接，但 PSet 固定在 `ForceUnlock` 后 206ms 发出（正压在第二次解锁上）。2 次 `IS_TIGHTENING+PSET_OK` 粘包被整包精确比较丢弃，设备实际已切换程序 |
| 配置项 | ini `pset_quiet_window_ms`（默认 1200，范围 0–10000，0 = 关闭），不做 UI，现场工程师直接改 ini |
| 预期效果 | 首次下发成功率由 9% 提升到接近 100%；重试不再自造失败条件；粘包 ACK 可被正确识别 |
| 回滚方式 | 仅改 `ToolTask.cs`、`DeviceType_Tool.cs`、新增一个 codec 文件与测试；`git revert` 单个提交即可回滚 |
| 实施结果 | 2026-10-26 完成 Task 1–6；新增单元/集成用例 25 个（含 Theory 展开）；全量构建通过，回归 69/69 通过 |
| 终审修订 | 2026-10-26 | 新增 `PSet go` 实际下发日志；命令闸门按 D2 收敛为 X7 且受 `0` 控制；ACK 发送前不计入；8KB 缓冲告警；静默等待期间重连则放弃本次下发 |

---

## Global Constraints

- **禁止执行任何 git 提交命令或调用 git-commit 技能**（用户全局规则）。每个任务末尾只做本地构建/测试检查点，不提交。
- **速动 X7 协议不可修改**：不改命令格式、不加 ACK 字段、不要求固件回显程序号、不加读程序号命令。
- 所有新行为必须用 `_toolType is ToolSudongX7` 隔离；PF/FIT 的命令、握手、PSet 流程保持原样。
- 手术式改动：不重构无关代码、不清理既有死代码；匹配现有风格（中文注释、`logger.Info/Warn/Error`）。
- **改动尽量小**：只做解决问题所必需的改动；不重构不相关代码、不做投机性抽象、不加未被要求的配置/UI/功能。不设行数上限，但每一行改动都要能追溯到本计划的目标。明确不做：重写 `ReconnectAndResendPSet`、改 `Connect()` 置位顺序、短写重试循环（只告警）。
- **生产安全（客户无法现场测试，直接上生产）**：所有新增路径必须 **fail-open** —— 解析/等待/闸门异常或超时一律回落到现有重连重试，不引入新的失败模式；闸门等待与静默等待必须设上限，不得无限阻塞；解析层改动必须有**真实帧回放测试**（含粘连帧）。
- 测试项目：`OperationGuidance_new.Tests`（xunit，net6.0-windows），命名空间 `OperationGuidance_new.Tests.Tasks`。
- 构建：`dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
- 测试：`dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
- 测试断言基于英文日志 marker（生产日志为 GBK，勿用中文匹配）。
- 现有测试中 `TestableToolTask` 固定使用 PF6000；本计划新增 X7 构造重载，不改变旧构造行为。

---

## 已知限制（本版不解决）

- **15–22s 长尾**：少数失败跨多次重连仍持续 15–22s，期间设备完全静默。本版不做窗口自适应加重；靠现有重连重试 + 诊断日志分类原因（见 Spec D9）。长尾不会比现状更差。
- **PSet ACK 不携带程序号**：协议不可改，无法从 ACK 直接证明设备切换成功；只能通过后续拧紧结果帧的扭矩上下限间接观察。
- **Mock 的 `BusyAfterCommandMs` 是推断值**：仅用于锁住修复逻辑，不是设备实测规格；真实值由现场日志校准。

---

## 文件结构

| 文件 | 职责 |
|---|---|
| `OperationGuidance_new/Constants/SudongFrameCodec.cs`（新建） | 速动 X7 帧重组：长度前缀、粘包拆分、半包缓存、非法数据重同步 |
| `OperationGuidance_new/Constants/DeviceType_Tool.cs`（修改 X7 段） | `AnalyzeData` 拆为 `AnalyzeSingleFrame`；未知帧告警；结果帧结构化摘要 |
| `OperationGuidance_new/Tasks/ToolTask.cs`（修改） | codec 接线、静默窗口、命令闸门、诊断日志、短写告警 |
| `OperationGuidance_new/Configs/IniFileKeys.cs`（修改） | 新增 `pset_quiet_window_ms` 配置键 |
| `OperationGuidance_new/Utils/MainUtils.cs`（修改） | `GetPSetQuietWindowMs()` 读取 ini；`NewToolTask*` 赋值给 ToolTask |
| `OperationGuidance_new.Tests/Tasks/SudongFrameCodecTests.cs`（新建） | 帧重组单元测试 + 真实帧回放 + fuzz |
| `OperationGuidance_new.Tests/Tasks/SudongX7AnalyzeTests.cs`（新建） | 单帧 ACK 解析测试 |
| `OperationGuidance_new.Tests/Tasks/MockSudongX7Server.cs`（新建） | 模拟 X7 控制器（忙碌窗口、粘包、静默） |
| `OperationGuidance_new.Tests/Tasks/ToolTaskTestHarness.cs`（修改） | 新增 X7 建连辅助 |
| `OperationGuidance_new.Tests/Tasks/TestableToolTask.cs`（修改） | X7 构造重载；记录指令发送时间 |
| `OperationGuidance_new.Tests/Tasks/SudongX7PSetIntegrationTests.cs`（新建） | 下发时机、粘包、重试端到端测试 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskPSetQuietWindowTests.cs`（新建） | 静默窗口单元测试 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskCommandGateTests.cs`（新建） | 命令串行化与锁后静默测试 |
| `OperationGuidance_new.Tests/Tasks/SudongX7DiagnosticsTests.cs`（新建） | 连接世代号与迟到 ACK 测试 |
| `CONTEXT.md`（修改） | 补充 X7「TCP 连接 ≠ 指令就绪」的领域说明 |
| `docs/adr/0002-sudong-x7-pset-quiet-window.md`（新建） | 记录「等待静默窗口而非继续加强重连」的决策 |

---

### Task 1: SudongFrameCodec + X7 单帧解析

**Files:**
- Create: `OperationGuidance_new/Constants/SudongFrameCodec.cs`
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs:377-510`
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:129-201`
- Test: `OperationGuidance_new.Tests/Tasks/SudongFrameCodecTests.cs`（新建）
- Test: `OperationGuidance_new.Tests/Tasks/SudongX7AnalyzeTests.cs`（新建）

**Interfaces:**
- Produces: `SudongFrameCodec.Feed(byte[]) : IReadOnlyList<byte[]>`、`Reset()`
- Produces: `ToolSudongX7.AnalyzeSingleFrame(byte[] frameBytes, Action<bool?,bool?,bool?,bool?,bool?> toolAction, Action<TighteningData,int>? actionAfterAnalysis = null, Func<CurveDataTemp,int,Task>? actionAfterCurveDataReceived = null, int? deviceId = null) : void`
- Consumes: `MainUtils.ToHexString(byte[])`、`MainUtils.ToBytes(string)`

- [x] **Step 1: 写失败的帧重组单元测试**

新建 `OperationGuidance_new.Tests/Tasks/SudongFrameCodecTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongFrameCodecTests
{
    private const string StatusFrame = "55AA0585007B450D0A";
    private const string AckFrame = "55AA058205B9760D0A";
    private const string ResultFrame =
        "55AA2781001F01FE0300000C080D000C08000000020100224C048403E70300008813000000000158BC0D0A";

    [Fact]
    public void Feed_CoalescedStatusAndAck_ReturnsTwoFrames()
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(StatusFrame + AckFrame));

        Assert.Equal(2, frames.Count);
        Assert.Equal(StatusFrame, MainUtils.ToHexString(frames[0]));
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[1]));
    }

    [Fact]
    public void Feed_SplitAck_ReturnsFrameOnceComplete()
    {
        var codec = new SudongFrameCodec();
        byte[] all = MainUtils.ToBytes(AckFrame);

        Assert.Empty(codec.Feed(all.Take(4).ToArray()));
        var frames = codec.Feed(all.Skip(4).ToArray());

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_ThreeFramesInOneChunk_ReturnsAllInOrder()
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(ResultFrame + StatusFrame + AckFrame));

        Assert.Equal(3, frames.Count);
        Assert.Equal(ResultFrame, MainUtils.ToHexString(frames[0]));
        Assert.Equal(StatusFrame, MainUtils.ToHexString(frames[1]));
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[2]));
    }

    [Fact]
    public void Feed_GarbageBeforeHeader_Resyncs()
    {
        var codec = new SudongFrameCodec();
        byte[] input = new byte[] { 0x00, 0x11, 0x22 }
            .Concat(MainUtils.ToBytes(AckFrame)).ToArray();

        var frames = codec.Feed(input);

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_InvalidLengthByte_ResyncsWithoutThrowing()
    {
        var codec = new SudongFrameCodec();
        byte[] input = new byte[] { 0x55, 0xAA, 0xFF, 0xFF, 0xFF }
            .Concat(MainUtils.ToBytes(AckFrame)).ToArray();

        var frames = codec.Feed(input);

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Reset_DropsPartialBuffer()
    {
        var codec = new SudongFrameCodec();
        byte[] all = MainUtils.ToBytes(AckFrame);
        codec.Feed(all.Take(4).ToArray());

        codec.Reset();
        var frames = codec.Feed(all);

        Assert.Single(frames);
    }

    // ---- 生产安全：真实帧回放（取自 2026-10-14 ~ 10-23 现场日志）----

    private const string RealResultFrame1 =
        "55AA2781001F01FE0300000C080D000C08000000020100224C048403E70300008813000000000158BC0D0A";
    private const string RealResultFrame2 =
        "55AA2781001F0100040000450000004500000000020100234C048403E70300008813000000000175010D0A";

    [Theory]
    [InlineData(StatusFrame)]
    [InlineData(AckFrame)]
    [InlineData(RealResultFrame1)]
    [InlineData(RealResultFrame2)]
    public void Feed_RealSingleFrame_RoundTripsExactly(string captured)
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(captured));

        Assert.Single(frames);
        Assert.Equal(captured, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_RealCoalescedPair_ReturnsTwoExactFrames()
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(StatusFrame + AckFrame));

        Assert.Equal(2, frames.Count);
        Assert.Equal(StatusFrame, MainUtils.ToHexString(frames[0]));
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[1]));
    }

    [Fact]
    public void Feed_RandomBytes_NeverThrows()
    {
        var random = new Random(12345);
        for (int i = 0; i < 1000; i++) {
            byte[] junk = new byte[random.Next(0, 256)];
            random.NextBytes(junk);
            var codec = new SudongFrameCodec();
            var frames = codec.Feed(junk); // 断言：不抛异常
            Assert.NotNull(frames);
        }
    }
}
```

- [x] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongFrameCodecTests"`
Expected: 编译失败，`SudongFrameCodec` 不存在（CS0246）。

- [x] **Step 3: 实现 SudongFrameCodec**

新建 `OperationGuidance_new/Constants/SudongFrameCodec.cs`：

```csharp
namespace OperationGuidance_new.Constants {
    /// <summary>
    /// 速动 X7 帧格式：55 AA &lt;len&gt; &lt;payload...&gt; &lt;crc2&gt; 0D 0A。
    /// 现场所有帧满足：总长度 = len + 4（len 为第 3 个字节）。
    /// TCP 是字节流，一次 Receive 可能包含多帧，也可能只有半帧；本类负责重组。
    /// </summary>
    public sealed class SudongFrameCodec {
        private const int MaxFrameBytes = 256;
        private const int MaxBufferBytes = 8 * 1024;
        private readonly List<byte> _buffer = new();

        /// <summary>喂入一次 Receive 的原始字节，返回本次能拼出的完整帧（可能 0 个或多个）。</summary>
        public IReadOnlyList<byte[]> Feed(byte[] chunk) {
            var frames = new List<byte[]>();
            if (chunk != null && chunk.Length > 0) {
                _buffer.AddRange(chunk);
            }

            while (true) {
                // 1. 对齐帧头 55 AA
                int start = -1;
                for (int i = 0; i + 1 < _buffer.Count; i++) {
                    if (_buffer[i] == 0x55 && _buffer[i + 1] == 0xAA) {
                        start = i;
                        break;
                    }
                }
                if (start < 0) {
                    // 保留最后一个字节，它可能是下一帧 0x55 的后半截
                    if (_buffer.Count > 1) {
                        _buffer.RemoveRange(0, _buffer.Count - 1);
                    }
                    break;
                }
                if (start > 0) {
                    _buffer.RemoveRange(0, start);
                }
                if (_buffer.Count < 3) {
                    break;
                }

                // 2. 长度前缀：总长 = len + 4
                int len = _buffer[2];
                int total = len + 4;
                if (total < 5 || total > MaxFrameBytes) {
                    // 非法长度：丢掉当前 0x55 重新找帧头
                    _buffer.RemoveAt(0);
                    continue;
                }
                if (_buffer.Count < total) {
                    break; // 半包，等下一次 Feed
                }

                // 3. 帧尾校验 0D 0A
                if (_buffer[total - 2] != 0x0D || _buffer[total - 1] != 0x0A) {
                    _buffer.RemoveAt(0);
                    continue;
                }

                frames.Add(_buffer.GetRange(0, total).ToArray());
                _buffer.RemoveRange(0, total);
            }

            if (_buffer.Count > MaxBufferBytes) {
                _buffer.Clear();
            }
            return frames;
        }

        public void Reset() => _buffer.Clear();
    }
}
```

- [x] **Step 4: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongFrameCodecTests"`
Expected: 全部 PASS（含单帧/粘连/半包/垃圾数据/随机字节 fuzz/真实帧回放）。

- [x] **Step 5: 写 X7 单帧解析的失败测试**

新建 `OperationGuidance_new.Tests/Tasks/SudongX7AnalyzeTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7AnalyzeTests
{
    private const string AckFrame = "55AA058205B9760D0A";

    [Fact]
    public void AnalyzeSingleFrame_PsetAck_InvokesPsetOkCallback()
    {
        var tool = new ToolSudongX7();
        bool? psetOk = null;
        Action<bool?, bool?, bool?, bool?, bool?> callback =
            (heart, pset, locked, data, curve) => psetOk = pset;

        tool.AnalyzeSingleFrame(MainUtils.ToBytes(AckFrame), callback);

        Assert.True(psetOk);
    }
}
```

- [x] **Step 6: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongX7AnalyzeTests"`
Expected: 编译失败，`AnalyzeSingleFrame` 不存在（CS1061）。

- [x] **Step 7: 拆分 ToolSudongX7.AnalyzeData**

在 `OperationGuidance_new/Constants/DeviceType_Tool.cs` 中把现有 `ToolSudongX7.AnalyzeData` 的方法体原样移动到新方法，原方法只做委托：

```csharp
        public override void AnalyzeData(byte[] msgBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction,
            Action<TighteningData, int>? actionAfterAnalysis = null,
            Func<CurveDataTemp, int, Task>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
            AnalyzeSingleFrame(msgBytes, toolAction, actionAfterAnalysis, _actionAfterCurveDataReceived, deviceId);
        }

        /// <summary>分析一个完整帧（由 SudongFrameCodec 重组后传入）。</summary>
        public void AnalyzeSingleFrame(byte[] frameBytes, Action<bool?, bool?, bool?, bool?, bool?> toolAction,
            Action<TighteningData, int>? actionAfterAnalysis = null,
            Func<CurveDataTemp, int, Task>? _actionAfterCurveDataReceived = null, int? deviceId = null) {
            string dataMessage = MainUtils.ToHexString(frameBytes);
            string head = GetHead(dataMessage);
            logger.Info($"dataMessage = {dataMessage}");
            // ...以下保持原 AnalyzeData 方法体完全不变...
        }
```

在原方法体最后的 `else if (head == "55AA2781") { ... }` 之后补上「已知但无需处理」分支与未识别帧告警：

```csharp
            } else if (head == "55AA2781") {
                // ...原有结果帧处理逻辑保持不变...
            } else if (head == "55AA0585") {
                // IS_TIGHTENING（55AA0585007B450D0A）：拧紧进行中，保持原有静默行为
            } else {
                logger.Warn($"frame=UNKNOWN hex={dataMessage}");
            }
```

> 注意：`55AA0585`（IS_TIGHTENING）是每次拧紧都会出现的已知帧，**不能**走到 UNKNOWN 告警，否则现场观察清单里的 `frame=UNKNOWN` 会天天报假警。已知帧头：`55AA2781`（结果）、`55AA0585`（拧紧中）、`55AA0582`（PSet ACK，整帧精确匹配）、`55AA05CF`（ERROR_RSP）。

- [x] **Step 8: ToolTask 接入 codec**

在 `OperationGuidance_new/Tasks/ToolTask.cs` 字段区新增：

```csharp
        private readonly SudongFrameCodec _sudongCodec = new();
```

把 `AnalyzeData` 局部函数中的 X7 分支（约 `ToolTask.cs:157-171`）改为逐帧分析：

```csharp
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        IReadOnlyList<byte[]> frames = _sudongCodec.Feed(msgBytes);
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] RX {msgBytes.Length}B, frames={frames.Count}");
                        foreach (byte[] frame in frames) {
                            toolX7.AnalyzeSingleFrame(frame, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                                if (pSetSendingOk != null && _sendingPSet != -1) {
                                    if (pSetSendingOk.HasValue) {
                                        _psetSentOk = pSetSendingOk.Value;
                                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending to {_sendingPSet} result: {(_psetSentOk ? "Accepted" : "Rejected")}");
                                    }
                                }
                                if (dataReceived != null && dataReceived.Value) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                                }
                                if (curveReceived != null && curveReceived.Value) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received");
                                }
                            }), _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                        }
                    }
```

在 `Connect()` 成功后、`RunTask()` 之前重置 codec：

```csharp
                            _toolType.ClearResidual();  // 清除旧连接残留
                            _sudongCodec.Reset();
                            RunTask();
```

- [x] **Step 9: 运行全部测试**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 新增 7 个用例 PASS，现有用例无回归。

---

### Task 2: MockSudongX7Server + 端到端复现测试

**Files:**
- Create: `OperationGuidance_new.Tests/Tasks/MockSudongX7Server.cs`
- Modify: `OperationGuidance_new.Tests/Tasks/ToolTaskTestHarness.cs`
- Create: `OperationGuidance_new.Tests/Tasks/SudongX7PSetIntegrationTests.cs`

**Interfaces:**
- Produces: `MockSudongX7Server.Start()`、`Port`、`BusyAfterRundownMs`、`Silent`、`CoalesceAckWithStatus`、`PushRundownResultAsync()`、`PushFrameAsync(string hex)`、`AcceptedConnections`、`IgnoredPSetCount`、`AcceptedPSetTimes`
- Produces: `ToolTaskTestHarness.CreateX7Task(MockSudongX7Server)`、`ConnectX7Async(MockSudongX7Server, int timeoutMs = 10000)`

- [x] **Step 0: 先声明 PSetQuietWindowMs 属性（仅声明，行为在 Task 3 实现）**

在 `OperationGuidance_new/Tasks/ToolTask.cs` 字段区新增：

```csharp
        /// <summary>PSet 前要求的静默窗口（毫秒）。距最近一次拧紧结果与最近一次指令发送均需超过该值。0 = 关闭。</summary>
        public int PSetQuietWindowMs { get; set; } = 1200;
```

这样本任务的测试可以先编译；Task 3 再实现等待逻辑。

- [x] **Step 1: 实现 MockSudongX7Server**

新建 `OperationGuidance_new.Tests/Tasks/MockSudongX7Server.cs`：

```csharp
using System.Net;
using System.Net.Sockets;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// 模拟速动 X7（55AA 长度前缀帧）。
/// 默认行为：
/// - 锁/解锁（cmd 0x01）只记录时间，不回包（与真机一致）；
/// - PSet（cmd 0x02）：距最近一次结果帧不足 BusyAfterRundownMs 时忽略（模拟控制器忙碌窗口），否则回 PSET_OK。
/// </summary>
public sealed class MockSudongX7Server : IDisposable
{
    public const string StatusFrameHex = "55AA0585007B450D0A";
    public const string PSetOkFrameHex = "55AA058205B9760D0A";
    public const string ResultFrameHex =
        "55AA2781001F01FE0300000C080D000C08000000020100224C048403E70300008813000000000158BC0D0A";

    private const int MaxFrameBytes = 256;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private int _activeConnections;
    private NetworkStream? _stream;

    public int Port { get; private set; }
    public int AcceptedConnections { get; private set; }
    public int IgnoredPSetCount { get; private set; }
    public DateTime? LastRundownUtc { get; private set; }
    public DateTime? LastLockUnlockUtc { get; private set; }
    public bool Silent { get; set; }
    public bool CoalesceAckWithStatus { get; set; }
    public int BusyAfterRundownMs { get; set; } = 1000;
    /// <summary>距上一次锁/解锁命令不足该值时忽略 PSet。0 = 不模拟（默认）。该值是推断，不是设备实测规格。</summary>
    public int BusyAfterCommandMs { get; set; } = 0;
    public int InterMessageDelayMs { get; set; } = 20;
    public List<DateTime> AcceptedPSetTimes { get; } = new();

    public MockSudongX7Server() {
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    public void Start() {
        _listener.Start();
        Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
        _ = Task.Run(AcceptLoopAsync);
    }

    public async Task PushRundownResultAsync() {
        lock (_lock) {
            LastRundownUtc = DateTime.UtcNow;
        }
        NetworkStream? stream;
        lock (_lock) {
            stream = _stream;
        }
        if (stream != null) {
            await WriteAsync(stream, ResultFrameHex);
        }
    }

    public async Task PushFrameAsync(string hex) {
        NetworkStream? stream;
        lock (_lock) {
            stream = _stream;
        }
        if (stream != null) {
            await WriteAsync(stream, hex);
        }
    }

    private async Task AcceptLoopAsync() {
        while (!_cts.IsCancellationRequested) {
            TcpClient client;
            try {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            } catch {
                break;
            }
            lock (_lock) {
                AcceptedConnections++;
                _activeConnections++;
            }
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client) {
        NetworkStream? stream = null;
        try {
            using (client) {
                stream = client.GetStream();
                lock (_lock) {
                    _stream = stream;
                }
                var buffer = new List<byte>();
                var chunk = new byte[4096];
                while (!_cts.IsCancellationRequested) {
                    int n = await stream.ReadAsync(chunk, _cts.Token);
                    if (n <= 0) break;
                    buffer.AddRange(chunk.Take(n));
                    while (true) {
                        int start = -1;
                        for (int i = 0; i + 1 < buffer.Count; i++) {
                            if (buffer[i] == 0x55 && buffer[i + 1] == 0xAA) {
                                start = i;
                                break;
                            }
                        }
                        if (start < 0) {
                            if (buffer.Count > 1) buffer.RemoveRange(0, buffer.Count - 1);
                            break;
                        }
                        if (start > 0) buffer.RemoveRange(0, start);
                        if (buffer.Count < 3) break;
                        int total = buffer[2] + 4;
                        if (total < 5 || total > MaxFrameBytes) {
                            buffer.RemoveAt(0);
                            continue;
                        }
                        if (buffer.Count < total) break;
                        byte[] frame = buffer.GetRange(0, total).ToArray();
                        buffer.RemoveRange(0, total);
                        await HandleFrameAsync(stream, frame);
                    }
                }
            }
        } catch {
            // 客户端断开或 Dispose 取消，正常退出
        } finally {
            lock (_lock) {
                _activeConnections--;
                if (_stream == stream) _stream = null;
            }
        }
    }

    private async Task HandleFrameAsync(NetworkStream stream, byte[] frame) {
        if (frame.Length < 4) return;
        byte cmd = frame[3];
        if (cmd == 0x01) { // 锁 / 解锁
            lock (_lock) {
                LastLockUnlockUtc = DateTime.UtcNow;
            }
            return;
        }
        if (cmd != 0x02) return; // 只处理 PSet

        if (Silent) return;

        DateTime? lastResult;
        DateTime? lastCommand;
        lock (_lock) {
            lastResult = LastRundownUtc;
            lastCommand = LastLockUnlockUtc;
        }
        bool busyByRundown = lastResult.HasValue
            && (DateTime.UtcNow - lastResult.Value).TotalMilliseconds < BusyAfterRundownMs;
        bool busyByCommand = BusyAfterCommandMs > 0
            && lastCommand.HasValue
            && (DateTime.UtcNow - lastCommand.Value).TotalMilliseconds < BusyAfterCommandMs;
        if (busyByRundown || busyByCommand) {
            lock (_lock) {
                IgnoredPSetCount++;
            }
            return;
        }

        lock (_lock) {
            AcceptedPSetTimes.Add(DateTime.UtcNow);
        }
        if (CoalesceAckWithStatus) {
            await WriteRawAsync(stream, StatusFrameHex + PSetOkFrameHex);
        } else {
            await WriteAsync(stream, PSetOkFrameHex);
        }
    }

    private async Task WriteAsync(NetworkStream stream, string hex) {
        await WriteRawAsync(stream, hex);
        await Task.Delay(InterMessageDelayMs, _cts.Token);
    }

    private static async Task WriteRawAsync(NetworkStream stream, string hex) {
        await stream.WriteAsync(MainUtils.ToBytes(hex), CancellationToken.None);
    }

    public void Dispose() {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
```

- [x] **Step 2: 扩展测试脚手架**

在 `OperationGuidance_new.Tests/Tasks/ToolTaskTestHarness.cs` 末尾追加：

```csharp
    /// <summary>创建指向 mock X7 控制器的 ToolTask（不发起连接）。</summary>
    public static ToolTask CreateX7Task(MockSudongX7Server server)
    {
        return new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.SudongX7, 1);
    }

    /// <summary>建 Task 并 Connect，轮询等待会话就绪后返回。</summary>
    public static async Task<ToolTask> ConnectX7Async(MockSudongX7Server server, int timeoutMs = 10000)
    {
        var task = CreateX7Task(server);
        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, timeoutMs);
        return task;
    }
```

- [x] **Step 3: 写复现测试（当前应通过，锁定现场行为）**

新建 `OperationGuidance_new.Tests/Tasks/SudongX7PSetIntegrationTests.cs`：

```csharp
using System.Diagnostics;
using OperationGuidance_new.Constants;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7PSetIntegrationTests
{
    [Fact]
    public async Task SendPSetAsync_ImmediatelyAfterRundown_ReproducesTimeout()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 1000 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0; // 关闭静默窗口，复现现场「收到结果后立即下发」

        await server.PushRundownResultAsync();
        bool ok = await task.SendPSetAsync(2);

        Assert.False(ok);
        Assert.Equal(1, server.IgnoredPSetCount);
        task.CloseConnection();
    }

    [Fact]
    public async Task SendPSetAsync_CoalescedStatusAndAck_ReturnsTrue()
    {
        using var server = new MockSudongX7Server { CoalesceAckWithStatus = true };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0;

        bool ok = await task.SendPSetAsync(2);

        Assert.True(ok);
        task.CloseConnection();
    }
}
```

> 说明：第一个用例是「特征化测试」，在 Task 3 之前就应通过；第二个用例在 Task 1 完成后应通过（若在 Task 1 之前运行会红，正好守住了粘包修复）。

- [x] **Step 4: 运行测试**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongX7PSetIntegrationTests"`
Expected: 2 个用例 PASS（`PSetQuietWindowMs` 若尚未定义则会出现编译错误——若如此，先在 Task 3 的 Step 1 之后再跑本任务）。

---

### Task 3: PSet 静默窗口

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`（字段、`NowMs`、`PSetQuietWindowMs`、`WaitForQuietWindowAsync`、`SendCommand` 时间戳、`SendPSetAsync`）
- Modify: `OperationGuidance_new/Configs/IniFileKeys.cs`（新增 `pset_quiet_window_ms`）
- Modify: `OperationGuidance_new/Utils/MainUtils.cs`（`GetPSetQuietWindowMs()` + `NewToolTask` 接线）
- Modify: `OperationGuidance_new.Tests/Tasks/TestableToolTask.cs`
- Create: `OperationGuidance_new.Tests/Tasks/ToolTaskPSetQuietWindowTests.cs`

**Interfaces:**
- Produces: `ToolTask.PSetQuietWindowMs`（`public int`，默认 1200）
- Produces: `MainUtils.GetPSetQuietWindowMs() : int`（ini 值，非法时返回 1200，范围 [0, 10000]）
- Produces: ini 键 `pset_quiet_window_ms`（0 = 关闭）
- Produces: `protected virtual long NowMs`
- Produces: `protected void NoteDeviceCommandSent()`
- Consumes: 现有 `SendPSetAsync(int)` 签名不变

- [x] **Step 1: 写失败的静默窗口测试**

新建 `OperationGuidance_new.Tests/Tasks/ToolTaskPSetQuietWindowTests.cs`：

```csharp
using System.Diagnostics;
using System.Reflection;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskPSetQuietWindowTests
{
    private static TestableToolTask CreateX7Task() =>
        new TestableToolTask(DeviceType_Tool.SudongX7);

    private static void SetLastRundown(TestableToolTask t, long ms)
    {
        typeof(ToolTask).GetField("_lastRundownAtMs", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(t, ms);
    }

    [Fact]
    public async Task SendPSetAsync_QuietWindow_DefersSend()
    {
        var t = CreateX7Task();
        t.PSetQuietWindowMs = 400;
        long sendAtMs = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(_ => {
            sendAtMs = stopwatch.ElapsedMilliseconds;
            return true;
        });
        SetLastRundown(t, t.NowForTest);

        var task = t.SendPSetAsync(2);
        await Task.Delay(150);
        Assert.True(t.SendingPSet == 2, "静默窗口内不应完成发送");
        Assert.Equal(-1, sendAtMs); // 150ms 时仍未真正 SendCommand
        t.SimulatePSetResponse(true);
        bool ok = await task;

        Assert.True(ok);
        Assert.True(sendAtMs >= 350, $"sendAtMs={sendAtMs}, expected >= 350");
    }

    [Fact]
    public async Task SendPSetAsync_WhenDeviceQuiet_SendsImmediately()
    {
        var t = CreateX7Task();
        t.PSetQuietWindowMs = 500;
        long sendAtMs = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(_ => {
            sendAtMs = stopwatch.ElapsedMilliseconds;
            return true;
        });
        SetLastRundown(t, t.NowForTest - 5000);

        var task = t.SendPSetAsync(2);
        await Task.Delay(30);
        t.SimulatePSetResponse(true);
        await task;

        Assert.True(sendAtMs >= 0 && sendAtMs < 150,
            $"sendAtMs={sendAtMs}, expected < 150");
    }
}
```

- [x] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskPSetQuietWindowTests"`
Expected: 编译失败，`TestableToolTask(DeviceTypeTool)` 构造与 `PSetQuietWindowMs`/`NowForTest` 不存在。

- [x] **Step 3: 扩展 TestableToolTask**

在 `OperationGuidance_new.Tests/Tasks/TestableToolTask.cs` 中：

1. 增加 X7 构造重载（保留原构造行为）：
```csharp
    public TestableToolTask(int deviceId = 1, string? name = "TEST", string ip = "127.0.0.1", int port = 4545)
        : this(DeviceType_Tool.PF6000_OP, deviceId, name, ip, port) { }

    public TestableToolTask(DeviceTypeTool tool, int deviceId = 1, string? name = "TEST",
        string ip = "127.0.0.1", int port = 4545)
        : base(deviceId, name, ip, port, tool, workstationId: 1)
    {
        Status = ATaskBase.CONNECTED;
    }
```
2. `SendCommand` 成功时上报时间戳：
```csharp
    protected override bool SendCommand(string command)
    {
        bool ok = _sendHandler?.Invoke(command) ?? true;
        if (ok) NoteDeviceCommandSent();
        return ok;
    }
```
3. 暴露测试时钟与时间戳设置：
```csharp
    public long NowForTest => NowMs;
    public void SetLastRundownForTest(long ms) => SetField("_lastRundownAtMs", ms);
    public void SetLastCommandForTest(long ms) => SetField("_lastDeviceCommandAtMs", ms);
```

- [x] **Step 4: 在 ToolTask 中实现静默窗口**

在字段区新增：

```csharp
        private const long NeverMs = long.MinValue / 2;
        private long _lastRundownAtMs = NeverMs;
        private long _lastDeviceCommandAtMs = NeverMs;
```

> **注意**：Task 2 Step 0 已声明 `PSetQuietWindowMs`，这里不要重复声明。两个 `long` 字段会跨 RunTask / SendCommand / PSet 线程读写，**统一用 `Interlocked.Exchange` 写、`Interlocked.Read` 读**（见下面的辅助方法），不要直接赋值。`Environment.TickCount64` 在 .NET 6 可用且单调递增。

在属性区新增：

```csharp
        protected virtual long NowMs => Environment.TickCount64;
        protected void NoteDeviceCommandSent() => Interlocked.Exchange(ref _lastDeviceCommandAtMs, NowMs);
        private void NoteRundown() => Interlocked.Exchange(ref _lastRundownAtMs, NowMs);
        private long ReadLastRundown() => Interlocked.Read(ref _lastRundownAtMs);
        private long ReadLastCommand() => Interlocked.Read(ref _lastDeviceCommandAtMs);
        private long SinceMs(long then) => then == NeverMs ? -1 : NowMs - then;
```

在 X7 回调中记录结果帧时间（`dataReceived != null && dataReceived.Value` 分支内）：

```csharp
                                if (dataReceived != null && dataReceived.Value) {
                                    NoteRundown();
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                                }
```

在 `SendCommand` 成功返回前调用 `NoteDeviceCommandSent()`（见 Task 5 的完整 SendCommand 改造；本任务先加一行即可）：

```csharp
                if (num.HasValue && num.Value > 0) {
                    NoteDeviceCommandSent();
                    HeartBeatCounter = 0;
                    return true;
                }
```

新增等待方法：

```csharp
        /// <summary>X7 专属：两个静默条件是否同时满足。</summary>
        private bool IsQuietWindowSatisfied() {
            if (_toolType is not ToolSudongX7 || PSetQuietWindowMs <= 0) {
                return true;
            }
            long now = NowMs;
            long lastRundown = ReadLastRundown();
            long lastCommand = ReadLastCommand();
            // 「从未发生」视为已静默无限久（注意不能用 SinceMs 的 -1 语义）
            long sinceRundown = lastRundown == NeverMs ? long.MaxValue : now - lastRundown;
            long sinceCommand = lastCommand == NeverMs ? long.MaxValue : now - lastCommand;
            return Math.Min(sinceRundown, sinceCommand) >= PSetQuietWindowMs;
        }

        /// <summary>X7 专属：等待设备进入可接收指令的静默窗口（不持有命令闸门）。返回 false = 超时，调用方按失败处理。</summary>
        private async Task<bool> WaitForQuietWindowAsync() {
            long start = NowMs;
            long deadline = start + Math.Max(PSetQuietWindowMs * 3, 3000);
            while (!IsQuietWindowSatisfied()) {
                if (NowMs > deadline) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet quiet window exceeded {deadline - start}ms, giving up");
                    return false;
                }
                // 50ms 轮询：等待期间若有新指令发出，_lastDeviceCommandAtMs 会被刷新
                await Task.Delay(50);
            }
            return true;
        }
```

在 `SendPSetAsync` 发送前调用：

```csharp
                    bool sendResult = false;
                    try {
                        if (_toolType is ToolSudongX7) {
                            if (!await WaitForQuietWindowAsync()) {
                                return false; // fail-open：交给上层重连重试
                            }
                        }
                        sendResult = SendCommand(command);
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send error", e);
                    }
```

- [x] **Step 5: 接线 ini 配置（现场可调）**

在 `OperationGuidance_new/Configs/IniFileKeys.cs` 末尾新增：

```csharp
        public static string PSetQuietWindowMs => "pset_quiet_window_ms";
```

在 `OperationGuidance_new/Utils/MainUtils.cs` 中新增（放在 `IsAutoLockToolEnabled` 附近）：

```csharp
        /// <summary>X7 程序号下发前的静默窗口（毫秒）。0 = 关闭。默认 1200ms。</summary>
        public static int GetPSetQuietWindowMs() {
            try {
                string value = Settings.Read(IniFileKeys.PSetQuietWindowMs);
                if (!int.TryParse(value, out int ms)) {
                    return 1200;
                }
                return Math.Clamp(ms, 0, 10000);
            } catch {
                // ini 缺失/损坏不得影响工具创建
                return 1200;
            }
        }
```

在 `NewToolTask` 与 `NewToolTaskAsync` 创建 ToolTask 之后立即赋值：

```csharp
            ToolTask task = new(toolId, toolName, ip, port, tool);
            task.PSetQuietWindowMs = GetPSetQuietWindowMs();
```

（两个方法各加一行。）

在 `ToolTask.Connect()` 的连接成功日志处补一行，便于现场确认生效值：

```csharp
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSetQuietWindowMs={PSetQuietWindowMs}ms");
```

- [x] **Step 6: 运行测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskPSetQuietWindowTests"`
Expected: 2 个用例 PASS。

- [x] **Step 7: 运行端到端静默窗口测试**

在 `SudongX7PSetIntegrationTests` 追加：

```csharp
    [Fact]
    public async Task SendPSetAsync_AfterRundown_WithQuietWindow_Succeeds()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 600 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 800;

        await server.PushRundownResultAsync();
        var stopwatch = Stopwatch.StartNew();
        bool ok = await task.SendPSetAsync(2);
        stopwatch.Stop();

        Assert.True(ok);
        Assert.True(stopwatch.ElapsedMilliseconds >= 700,
            $"elapsed={stopwatch.ElapsedMilliseconds}, expected >= 700");
        Assert.Single(server.AcceptedPSetTimes);
        task.CloseConnection();
    }
```

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongX7PSetIntegrationTests"`
Expected: 3 个用例 PASS。

---

### Task 4: 命令闸门

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`（`_commandGate`；`PerformLock` 在 649-671、`PerformUnlock` 在 708-730；`SendPSetAsync`）
- Create: `OperationGuidance_new.Tests/Tasks/ToolTaskCommandGateTests.cs`

**Interfaces:**
- Produces: `private readonly SemaphoreSlim _commandGate = new(1, 1);`
- Consumes: `TestableToolTask`（Task 3 的 X7 构造）

- [x] **Step 1: 写失败的闸门测试**

新建 `OperationGuidance_new.Tests/Tasks/ToolTaskCommandGateTests.cs`：

```csharp
using OperationGuidance_new.Constants;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskCommandGateTests
{
    [Fact]
    public async Task ForceSendUnlock_And_PSet_AreSerialized_PSetLast()
    {
        var t = new TestableToolTask(DeviceType_Tool.SudongX7);
        t.PSetQuietWindowMs = 0;
        var order = new List<string>();

        t.SetSendHandler(cmd => {
            lock (order) {
                order.Add(cmd);
            }
            return true;
        });

        var unlock = Task.Run(() => t.ForceSendUnlock());
        // 等解锁序列真正开始（第一条 unlock 已发出），确保 PSet 一定在序列中间到来
        await MockPF6000Server.WaitUntilAsync(() => {
            lock (order) { return order.Count >= 1; }
        }, 2000);
        var pset = Task.Run(async () => {
            var send = t.SendPSetAsync(2);
            await Task.Delay(20);
            t.SimulatePSetResponse(true);
            return await send;
        });

        await Task.WhenAll(unlock, pset);

        Assert.Equal(3, order.Count);
        Assert.StartsWith("55AA0701", order[0]); // unlock #1
        Assert.StartsWith("55AA0701", order[1]); // unlock #2
        Assert.StartsWith("55AA0702", order[2]); // PSet 必须在最后
        Assert.True(pset.Result);
    }

    [Fact]
    public async Task SendPSetAsync_AfterLock_StillWaitsForQuietWindow()
    {
        var t = new TestableToolTask(DeviceType_Tool.SudongX7);
        t.PSetQuietWindowMs = 300;
        long lockAt = -1, psetAt = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(cmd => {
            if (cmd.StartsWith("55AA0701")) lockAt = stopwatch.ElapsedMilliseconds;
            if (cmd.StartsWith("55AA0702")) psetAt = stopwatch.ElapsedMilliseconds;
            return true;
        });

        t.ForceSendLock(); // 两条 lock，最后一条约在 200ms
        var pset = t.SendPSetAsync(2);
        await Task.Delay(30);
        t.SimulatePSetResponse(true);
        await pset;

        Assert.True(psetAt - lockAt >= 280,
            $"lockAt={lockAt}, psetAt={psetAt}, expected gap >= 280");
    }
}
```

- [x] **Step 2: 运行测试确认失败**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskCommandGateTests"`
Expected: FAIL —— PSet 可能插在两次 unlock 之间（`order[1]` 不是 unlock），或 `order[2]` 不是 PSet。

- [x] **Step 3: 增加命令闸门（只保护最后一次发送）**

在字段区新增：

```csharp
        private readonly SemaphoreSlim _commandGate = new(1, 1);
```

**关键设计**：静默等待在闸门之外；闸门只保护「最后一次静默确认 + `SendCommand`」，PSet 侧持有时长约一次 `Send`。`PerformLock`/`PerformUnlock` 持有时长是原有「双发 + `Thread.Sleep(200)`」的约 200ms（不是静默等待的 1.2s），因此不会把 UI 卡住 1 秒以上。

**不跳过 `SendLock`/`SendUnlock`**：LockCheck 只在状态切换时真正发送（`_locked`/`_pendingLockCommand` 会拦住后续 50ms 调用），因此不会持续刷新静默窗口；让加锁先发生、PSet 以最后一次锁命令为基准重新计时，可保证等待期间工具处于锁定状态（安全），也避免「跳过加锁导致 2 秒无锁」的空档。

`PerformLock()` / `PerformUnlock()` 的方法体包进闸门：

```csharp
        private void PerformLock() {
            if (!_commandGate.Wait(2000)) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock skipped — command gate timeout");
                return;
            }
            try {
                // ...原有方法体保持不变...
            } finally {
                _commandGate.Release();
            }
        }

        private void PerformUnlock() {
            if (!_commandGate.Wait(2000)) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock skipped — command gate timeout");
                return;
            }
            try {
                // ...原有方法体保持不变...
            } finally {
                _commandGate.Release();
            }
        }
```

> 闸门超时是防御性兜底：正常持有时间只有一次 `Send`。超时后锁/解锁跳过，`LockCheck` 50ms 后会再次调用。

`SendPSetAsync` 发送段改为「闸门外等待 → 闸门内复核 + 发送」的循环：

```csharp
                    bool sendResult = false;
                    try {
                        if (_toolType is ToolSudongX7) {
                            bool sent = false;
                            while (!sent) {
                                if (!await WaitForQuietWindowAsync()) {   // 闸门外等待，超时 fail-open
                                    return false;
                                }
                                if (!await _commandGate.WaitAsync(2000)) {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet aborted — command gate timeout");
                                    return false;
                                }
                                try {
                                    // 拿到闸门后复核：等待期间可能有 ForceSendLock/ForceSendUnlock
                                    if (!IsQuietWindowSatisfied()) {
                                        continue;
                                    }
                                    if (!Connected) {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet aborted - disconnected during quiet wait");
                                        return false;
                                    }
                                    sendResult = SendCommand(command);
                                    sent = true;
                                } finally {
                                    _commandGate.Release();
                                }
                            }
                        } else {
                            sendResult = SendCommand(command);
                        }
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send error", e);
                    }
```

> 注意：`continue` 位于 `try` 内，`finally` 会先释放闸门再进入下一轮，不会死锁。

- [x] **Step 4: 运行闸门测试确认通过**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~ToolTaskCommandGateTests"`
Expected: PASS。

- [x] **Step 5: 评估结论 —— 不修改 `Connect()`（本步无代码改动）**

原计划把 `ForceSendUnlock()` 移到 `Status = CONNECTED` 之前。复核后**取消**：

- `SendPSetAsync` 的静默窗口以「最近一次指令发送」为准，强制解锁第二发（`Thread.Sleep(200)` 之后）会刷新 `_lastDeviceCommandAtMs`，PSet 会重新等满 `PSetQuietWindowMs`；
- 命令闸门再保证发送瞬间不与锁/解锁交叠；
- 保持 `Connect()` 原样，PF/FIT 零影响；`pset_quiet_window_ms = 0` 时行为与现状完全一致，便于 A/B 对比。

本步骤只保留决策记录。若现场新日志显示「PSet 确实在强制解锁完成前发出」，再单独立项处理。

**验证方式**：部署后检查日志中同一 `epoch` 下 `Force unlocking` 与 `PSet go` 的时间差是否 ≥ `PSetQuietWindowMs`（`PSet attempt` 是等待前的值，不能用于此判定）。

- [x] **Step 6: 运行全部测试确认无回归**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS（含既有 PF 握手/重连用例）。

- [x] **Step 7: 追加重试端到端测试**

在 `SudongX7PSetIntegrationTests` 追加：

```csharp
    [Fact]
    public async Task ReconnectAndResendPset_AfterBusyWindow_Succeeds()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 600 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 800;

        await server.PushRundownResultAsync(); // 设备刚发完结果，处于忙碌窗口

        using var cts = new CancellationTokenSource(15000);
        bool ok = await task.ReconnectAndResendPset(2, cts.Token);

        Assert.True(ok);
        Assert.True(server.AcceptedConnections >= 2, "expected a reconnect");
        task.CloseConnection();
    }

    // 注意：BusyAfterCommandMs 是「锁/解锁后忙碌窗口」的推断值，仅用于锁住修复逻辑，
    // 不是设备实测规格。真实值由下一版现场日志校准。

    [Fact]
    public async Task SendPSetAsync_RightAfterLock_WithoutQuietWindow_IsIgnored()
    {
        using var server = new MockSudongX7Server { BusyAfterCommandMs = 300 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0; // 关闭静默窗口，复现「紧贴锁命令下发」

        task.ForceSendLock();       // 两条 lock，约 200ms
        bool ok = await task.SendPSetAsync(2);

        Assert.False(ok);
        Assert.True(server.IgnoredPSetCount >= 1);
        task.CloseConnection();
    }

    [Fact]
    public async Task SendPSetAsync_RightAfterLock_WithQuietWindow_IsAccepted()
    {
        using var server = new MockSudongX7Server { BusyAfterCommandMs = 300 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 400;

        task.ForceSendLock();
        bool ok = await task.SendPSetAsync(2);

        Assert.True(ok);
        Assert.Single(server.AcceptedPSetTimes);
        task.CloseConnection();
    }
```

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongX7PSetIntegrationTests"`
Expected: 6 个用例 PASS。

---

### Task 5: 诊断日志与短写处理

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs`
- Modify: `OperationGuidance_new/Constants/DeviceType_Tool.cs`
- Create: `OperationGuidance_new.Tests/Tasks/SudongX7DiagnosticsTests.cs`

**Interfaces:**
- Produces: `private string Tag()` → `[TOOL:name-ip:port][epoch=N,lport=xxxx]`
- Produces: `private int _connectionEpoch`、`private int _localPort`

- [x] **Step 1: 增加连接世代号与本地端口**

字段区新增：

```csharp
        private int _connectionEpoch;
        private int _localPort;
        private string Tag() => $"[TOOL:{_device_name}-{_ip}:{_port}][epoch={_connectionEpoch},lport={_localPort}]";
```

在 `ConnectToServer()` 的 `socketClient.Connect(...)` 成功之后：

```csharp
                        _connectionEpoch++;
                        _localPort = ((IPEndPoint) socketClient.LocalEndPoint!).Port;
                        logger.Info($"{Tag()} Socket connected");
```

（替换原来的 `logger.Info($"[TOOL:...] Socket connected");` 行。）

- [x] **Step 2: SendCommand 记录原始发送 + 短写告警**

**保持原发送逻辑不变**，只加一行 TX 日志和一行短写告警（不做重试循环，控制代码规模）：

```csharp
                logger.Debug($"{Tag()} TX {command} ({data.Length}B)");
                int? num;
                lock (_sendLock) {
                    num = socketClient?.Send(data);
                }
                if (num.HasValue && num.Value > 0) {
                    if (num.Value < data.Length) {
                        logger.Warn($"{Tag()} Partial send {num.Value}/{data.Length}B");
                    }
                    NoteDeviceCommandSent();
                    HeartBeatCounter = 0;
                    return true;
                }
```

- [x] **Step 3: PSet 上下文日志 + 迟到 ACK 告警**

在 `SendPSetAsync` 设置 `_sendingPSet` 之后、生成命令之前：

```csharp
                    logger.Info($"{Tag()} PSet attempt pset={pSetNumber}, status={Status}, connected={Connected}, "
                        + $"sinceRundownMs={SinceMs(_lastRundownAtMs)}, sinceCmdMs={SinceMs(_lastDeviceCommandAtMs)}, "
                        + $"cachedPSet={_currentPSet}, quietMs={PSetQuietWindowMs}");
```

在 X7 的 `pSetSendingOk` 分支中：

```csharp
                                if (pSetSendingOk != null) {
                                    if (_sendingPSet == -1) {
                                        logger.Warn($"{Tag()} PSet ACK received with no PSet in flight (late/stale ACK), ignored");
                                    } else if (pSetSendingOk.HasValue) {
                                        _psetSentOk = pSetSendingOk.Value;
                                        logger.Info($"{Tag()} PSet sending to {_sendingPSet} result: {(_psetSentOk ? "Accepted" : "Rejected")}");
                                    }
                                }
```

- [x] **Step 4: 结果帧结构化摘要**

在 `ToolSudongX7.AnalyzeSingleFrame` 的 `head == "55AA2781"` 分支中，计算完 `tighteningStatus` 后追加一行：

```csharp
                    logger.Info($"frame=RESULT torque={torque} torqueLimits=[{torqueMin},{torqueMax}] "
                        + $"angle={angle} angleLimits=[{angleMin},{angleMax}] "
                        + $"status={tighteningStatus} error={tighteningErrorStatus}");
```

- [x] **Step 5: 诊断测试**

新建 `OperationGuidance_new.Tests/Tasks/SudongX7DiagnosticsTests.cs`：

```csharp
using System.Reflection;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7DiagnosticsTests
{
    [Fact]
    public async Task ConnectionEpoch_IncrementsAfterReconnect()
    {
        using var server = new MockSudongX7Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        int before = (int) typeof(ToolTask)
            .GetField("_connectionEpoch", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task)!;

        using var cts = new CancellationTokenSource(15000);
        await task.ReconnectAndResendPset(2, cts.Token);

        int after = (int) typeof(ToolTask)
            .GetField("_connectionEpoch", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task)!;
        Assert.True(after > before, $"epoch before={before}, after={after}");
        task.CloseConnection();
    }

    [Fact]
    public async Task StaleAck_WhileIdle_DoesNotMarkPsetSent()
    {
        using var server = new MockSudongX7Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);

        await server.PushFrameAsync(MockSudongX7Server.PSetOkFrameHex);
        await Task.Delay(150);

        bool psetSentOk = (bool) typeof(ToolTask)
            .GetField("_psetSentOk", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task)!;
        Assert.False(psetSentOk);
        task.CloseConnection();
    }
}
```

- [x] **Step 6: 运行诊断测试**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj --filter "FullyQualifiedName~SudongX7DiagnosticsTests"`
Expected: 2 个用例 PASS。

---

### Task 6: 文档、变更记录收尾与全量回归

**Files:**
- Modify: `CONTEXT.md`
- Modify: `docs/superpowers/specs/2026-10-26-sudong-x7-pset-timing-fix-design.md`（若实施中发现偏差）
- Modify: `docs/superpowers/plans/2026-10-26-sudong-x7-pset-timing-fix.md`（勾选完成项）

- [x] **Step 1: 创建 ADR-0002**

新建 `docs/adr/0002-sudong-x7-pset-quiet-window.md`，内容即本计划 Q7 确认的版本（标题 + 1 段决策 + Status + Considered Options + Consequences）。

- [x] **Step 2: 更新 CONTEXT.md**

在「TCP connection vs. session readiness」小节末尾追加（**只写领域事实，不写类名/配置键/实现机制**）：

```markdown
**Sudong X7 exception (速动 X7 例外):** Sudong X7 has **no application-layer handshake**.
`Status == CONNECTED` means only that the TCP socket is up; it does **not** mean the
controller accepts commands. After emitting a tightening result the controller ignores
host commands for roughly one second. Therefore program-number delivery must wait for a
quiet period, and a TCP reconnect does not clear this state.
```

- [x] **Step 3: 全量构建**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: 0 error。

- [x] **Step 4: 全量测试**

Run: `dotnet test OperationGuidance_new.Tests/OperationGuidance_new.Tests.csproj`
Expected: 全部 PASS（含既有 PF 握手、重连并发、PSet 前置条件用例）。

- [x] **Step 5: 检查日志 marker**

人工确认新日志包含以下可直接 grep 的英文 marker：

```text
TX 55AA0702...
PSet attempt pset=2, status=2, connected=True, sinceRundownMs=...
PSet ACK received with no PSet in flight
Partial send
frame=RESULT torque=...
frame=UNKNOWN hex=...
[epoch=3,lport=52344]
```

- [x] **Step 6: 更新计划的变更记录**

在本文件顶部「本次变更记录」表末尾追加一行：

```markdown
| 实施结果 | 2026-10-26 完成 Task 1–6；新增单元/集成用例 25 个（含 Theory 展开）；全量构建与回归通过 |
```

- [x] **Step 7: 本地检查点（不提交）**

Run: `git status --short`
Expected: 只包含本计划列出的文件；交由用户手动提交。

---

## 变更清单（实施完成后核对）

| 文件 | 变更类型 | 理由 |
|---|---|---|
| `OperationGuidance_new/Constants/SudongFrameCodec.cs` | 新增 | TCP 粘包/半包重组 |
| `OperationGuidance_new/Constants/DeviceType_Tool.cs` | 修改 | X7 单帧解析、未知帧告警、结果帧摘要 |
| `OperationGuidance_new/Tasks/ToolTask.cs` | 修改 | codec 接线、静默窗口、命令闸门、诊断、短写告警 |
| `OperationGuidance_new/Configs/IniFileKeys.cs` | 修改 | 新增 `pset_quiet_window_ms` |
| `OperationGuidance_new/Utils/MainUtils.cs` | 修改 | 读取 ini 并赋给 ToolTask |
| `OperationGuidance_new.Tests/Tasks/SudongFrameCodecTests.cs` | 新增 | 帧重组测试 |
| `OperationGuidance_new.Tests/Tasks/SudongX7AnalyzeTests.cs` | 新增 | 单帧 ACK 解析测试 |
| `OperationGuidance_new.Tests/Tasks/MockSudongX7Server.cs` | 新增 | X7 控制器模拟 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskTestHarness.cs` | 修改 | X7 建连辅助 |
| `OperationGuidance_new.Tests/Tasks/TestableToolTask.cs` | 修改 | X7 构造、指令时间戳 |
| `OperationGuidance_new.Tests/Tasks/SudongX7PSetIntegrationTests.cs` | 新增 | 下发时机/粘包/重试端到端 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskPSetQuietWindowTests.cs` | 新增 | 静默窗口单元测试 |
| `OperationGuidance_new.Tests/Tasks/ToolTaskCommandGateTests.cs` | 新增 | 命令串行化测试 |
| `OperationGuidance_new.Tests/Tasks/SudongX7DiagnosticsTests.cs` | 新增 | 世代号/迟到 ACK 测试 |
| `CONTEXT.md` | 修改 | 记录 X7「TCP 连接 ≠ 指令就绪」 |
| `docs/adr/0002-sudong-x7-pset-quiet-window.md` | 新增 | 记录静默窗口决策与放弃项 |
| `docs/superpowers/specs/2026-10-26-sudong-x7-pset-timing-fix-design.md` | 新增 | 设计说明与变更记录 |
| `docs/superpowers/plans/2026-10-26-sudong-x7-pset-timing-fix.md` | 新增 | 本实施计划 |

---

## 附录 A：部署后观察与回退（客户现场无需开发到场）

### A.1 上线首日确认

1. 日志出现 `PSetQuietWindowMs=1200ms`（证明配置已生效）。
2. ini 中 `pset_quiet_window_ms` 缺省或为 `1200`；`0` 表示时序修复关闭（仅保留帧解析修复）。

### A.2 每日 grep 清单

```bash
# 下发总量 / 失败量（与历史 4,023 发送 / 2,788 超时对比）
grep -c "Sending PSet" app.log
grep -c "PSet sending timeout" app.log

# 静默窗口实际生效值（每次真实下发一行；看 waitedMs = 闸门外实际等待时长）
# 注意：pset_quiet_window_ms=0 时没有这一行（这正是「回退到基线」的判据）
grep "PSet go" app.log

# 每次尝试的上下文（等待前的间隔；与 PSet go 对照可看出实际等待了多久）
grep "PSet attempt" app.log

# 解析异常（应为 0）
grep -c "frame=UNKNOWN" app.log

# 接收缓冲超限（应为 0；出现说明收到大量无帧头的垃圾字节）
grep -c "frame buffer overflow" app.log

# 迟到 / 提前的 ACK（少量可接受；持续出现说明时序或超时窗口有问题）
grep -c "PSet ACK received with no PSet in flight" app.log
grep -c "PSet ACK before send" app.log

# 短写（应为 0）
grep -c "Partial send" app.log

# 跨连接追踪（epoch 每次重连 +1）
grep "\[epoch=" app.log

# 防御性超时（正常应为 0）
grep -c "command gate timeout" app.log
grep -c "quiet window exceeded" app.log
grep -c "PSet aborted" app.log
```

### A.3 判定与回退

| 现象 | 判定 | 动作 |
|---|---|---|
| `PSet go` 行中 `sinceRundownMs ≥ 1200` 且 `sinceCmdMs ≥ 1200`；配合 `PSet sending timeout` 占比下降 | 窗口生效，修复有效 | 保持 |
| 切换变慢且 `PSet sending timeout` 未下降 | 静默窗口不适配现场 | ini 改为 `pset_quiet_window_ms=0` 观察（静默窗口与命令闸门同时关闭，帧解析修复保留） |
| `frame=UNKNOWN` 持续出现，或拧紧数据缺失/错位 | 帧解析异常 | **立即回退上一版本**（不要只改 ini） |
| `command gate timeout` / `quiet window exceeded` / `PSet aborted` 持续出现 | 闸门/等待/连接切换异常 | 先改 ini 为 0；仍出现则回退版本 |
| `PSet ACK before send` 持续出现 | 上一轮 ACK 迟到，落在静默等待内（已忽略，不会误判成功） | 观察 `PSet sending timeout` 是否同步出现；若仅偶发可接受 |
| `frame buffer overflow` 出现 | 收到大量无帧头的垃圾字节 | 收集该时段的 `dataMessage` 行，评估是否设备侧异常输出 |
| `Partial send` 出现 | 发送层异常 | 收集日志，评估是否加强发送层 |

### A.4 回滚

- **只关时序修复**：ini `pset_quiet_window_ms=0`，重启应用——静默窗口与命令闸门同时关闭，帧解析修复继续生效。
- **完全回滚**：恢复上一版本可执行文件。本改动无数据库迁移、无 ini 迁移，回滚无残留。
- **A/B 对比**：保持同一版本，只切换 ini 的 `0` / `1200`，即可对比失败率与切换耗时（`0` = 改动前基线：静默窗口与命令闸门均关闭，帧解析修复仍生效）。
