# Data Storage 消息队列设计

**日期**: 2026-06-20  
**状态**: 已实现（多轮审查通过） — 见 plans/2026-06-20-message-queue-data-storage.md
**最后更新**: 2026-06-20  
**术语表**: 参见仓库根目录 `CONTEXT.md`  
**目标**: 引入 `System.Threading.Channels` 消息队列机制，保证拧紧数据存储的严格顺序和零丢失，并为后续模块扩展提供通用消息队列基础设施

## 1. 背景

### 当前架构问题

```
拧紧数据 → StoreTighteningData (SemaphoreSlim 锁)
  → StoreTighteningDataInternal
    → StoreDataToDatabaseAsync (DB 写入)
    → ConcurrentBag.Add
    → BeginInvoke UI 刷新
曲线数据 → while 轮询 currentOperationData (最多 10 秒超时)
  → AddOrUpdateCurveData
导出     → OnMissionCompleted: 排空锁 (5s 超时) → 快照 → ExportAsync → Clear
```

**三个痛点：**

1. **曲线数据脆弱**：`while` 轮询 + 10 秒超时，超时抛 `InvalidDataException` 丢失数据
2. **导出与写入耦合**：`OnMissionCompleted` 需要等待锁排空，5 秒超时可能截断数据
3. **SCII_XT 绕过锁**：完全覆盖 `StoreTighteningData`，不参与基类锁机制

### 需求

- **正确性优先**：工业生产数据零丢失，严格顺序
- **通用基础设施**：先落地 dataStorage，后续其他模块可接入
- **所有版本统一**：WHYC, SCII, SCII_XT, GLB, YF, TZYX + 基类

## 2. 方案

`System.Threading.Channels`：单 Channel + 单线程顺序消费。

### 为什么选 Channel

| 标准 | 评价 |
|------|------|
| 正确性 | .NET 官方内置，线程安全，FIFO 保证 |
| 性能 | 异步原生，有界/无界可选，背压控制 |
| 维护性 | API 极简（WriteAsync / ReadAllAsync），零第三方依赖 |
| 与 ConcurrentQueue 对比 | 原生 async 支持，无需手动轮询/信号量 |
| 与 BlockingCollection 对比 | 支持 async/await，WinForms 场景更适配 |
| 与 MediatR 对比 | 不需要 DI 容器，轻量，不引入新范式 |

## 3. 组件架构

```
Utils/
├── IMessageQueue.cs              # 通用消息队列接口
├── ChannelMessageQueue.cs        # Channel<T> 实现
└── DataStorage/
    ├── DataStorageMessage.cs     # 消息基类 (record)
    └── DataStorageConsumer.cs    # 消费者（顺序处理，含虚方法钩子供子类覆盖）
```

### 接口

```csharp
interface IMessageQueue<T> {
    ValueTask EnqueueAsync(T message, CancellationToken ct = default); // Channel 关闭时抛 ChannelClosedException
    bool TryComplete();                                                // 幂等
    Task WaitForDrainAsync(CancellationToken ct);
}
```

### 消息类型

```csharp
abstract record DataStorageMessage;
record TighteningDataMessage(OperationDataDTO Data) : DataStorageMessage;
record CurveDataMessage(CurveDataTemp Data, int DeviceId) : DataStorageMessage;
record ExportDataMessage(string Result) : DataStorageMessage;
```

### 消费者

`DataStorageConsumer` 通过构造函数接收对 `AWorkplaceContentPanel` 的引用以访问 `_apis`、`_tighteningDataVOs`、`BeginInvoke` 等成员。钩子定义在 `AWorkplaceContentPanel` 上，方便子类覆盖。

```csharp
// AWorkplaceContentPanel 上的钩子（子类可覆盖）
protected virtual Task OnTighteningDataStored(OperationDataDTO dto) => Task.CompletedTask;

class DataStorageConsumer {
    readonly AWorkplaceContentPanel _panel;
    readonly Channel<DataStorageMessage> _channel;
    Task _consumeTask;
    volatile bool _consumerCrashed;

    async Task ConsumeLoopAsync(CancellationToken ct) {
        try {
            await foreach (var msg in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                switch (msg) {
                    case TighteningDataMessage t:
                        await ProcessWithRetry(async () => {
                            var dto = await _panel.StoreDataToDatabaseAsync(t.Data).ConfigureAwait(false);
                            _panel.currentOperationData = dto;
                            var vo = ConvertToVO(t.Data);
                            _panel._tighteningDataVOs.Add(vo);
                            var snapshot = _panel._tighteningDataVOs.ToList();
                            SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(snapshot));
                            await _panel.OnTighteningDataStored(t.Data).ConfigureAwait(false);
                        }, ct);
                        break;
                    case CurveDataMessage c:
                        await ProcessWithRetry(async () => {
                            var opId = _panel.currentOperationData?.id;
                            if (opId != null) {
                                await StoreCurveData(c.Data, opId.Value).ConfigureAwait(false);
                            }
                            // opId == null 在单工具+FIFO下不应发生，若发生则 log.Error + 跳过
                        }, ct);
                        break;
                    case ExportDataMessage e:
                        await ProcessWithRetry(async () => {
                            var snapshot = _panel._tighteningDataVOs.ToList();
                            var request = new ExportRequest {
                                Data = snapshot,
                                Fields = _panel.ExportFields,
                                BasePath = _panel.ExportBasePath,
                                ProductBatch = _panel._missionRecord?.product_batch,
                                ProductBarCode = _panel._missionRecord?.product_bar_code,
                                CompletedAt = DateTime.Now,
                                Result = e.Result,
                                EnableExcel = _panel.IsExcelExportEnabled,
                                EnableTxt = _panel.IsTxtExportEnabled,
                                MissionName = _panel._mission?.name,
                                WorkstationName = snapshot.Count > 0 ? snapshot[0].workstation_name : "",
                            };
                            await new DataExportService().ExportAsync(request).ConfigureAwait(false);
                            _panel._tighteningDataVOs.Clear();
                            SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(new List<OperationDataVO>()));
                        }, ct);
                        break;
                }
            }
        } catch (OperationCanceledException) {
            // 强制停止（窗关闭超时兜底）
        } catch (Exception ex) {
            _consumerCrashed = true;
            SafeBeginInvoke(() => ShowCrashError(ex.Message));
        }
    }

    void SafeBeginInvoke(Action action) {
        if (!_panel.IsDisposed && _panel.IsHandleCreated) {
            try { _panel.BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
    }
}
```

## 4. 数据流

```
拧紧回调 → EnqueueAsync(TighteningDataMessage)
曲线回调 → EnqueueAsync(CurveDataMessage)
导出触发 → EnqueueAsync(ExportDataMessage)   ← 最后入队，最后处理

消费者 (FIFO, 单线程顺序):

  TighteningMsg
    → StoreDataToDatabaseAsync
    → 设置 currentOperationData
    → List.Add → SafeBeginInvoke UI 刷新
    → OnTighteningDataStored 钩子

  CurveMsg
    → currentOperationData.id (时序保证)
    → AddOrUpdateCurveData

  ExportMsg
    → 快照 → DataExportService.ExportAsync
    → Clear → BeginInvoke UI 刷新
```

### 曲线数据关联机制

曲线数据与拧紧数据的关联基于**时序**，不依赖显式 ID 匹配。

**为什么时序是安全的：**

| 工具系列 | 消息来源 | 时序保证 |
|----------|---------|---------|
| PF 系列 | 拧紧(MID=0061)和曲线(MID=0900)在同一 ASCII 消息 `\0` 分割 | 回调中先入队 Tightening，后入队 Curve |
| FIT 系列 | FINAL_DATA 和 CURVE_DATA 在同一 `AnalyzeData` 循环中顺序处理 | 先调 `actionAfterAnalysis` 入队 Tightening，后调 `_actionAfterCurveDataReceived` 入队 Curve |

**关键保证**：如果一次拧紧有曲线数据，曲线一定紧跟在该拧紧之后入队。如果一次拧紧没有曲线，则根本不会产生 CurveDataMessage。因此不存在"T#1→T#2→C#1"这样的跨消息错配。

消费者直接使用 `currentOperationData.id`（上一条 TighteningData 写入 DB 后的 ID）。
单工具假设 + FIFO Channel 下，曲线始终紧跟其对应的拧紧数据，`currentOperationData` 在 CurveMsg 处理时一定已就绪。

### 曲线数据不存在的场景

曲线数据是可选的。没有曲线数据时，下一个消息自然就是新的拧紧数据或导出消息，消费者无特殊处理。

## 5. 错误处理 & 重试

**三种消息类型使用统一的重试策略：3 次重试 → 弹窗阻断。**

工业场景下，任何数据丢失都不可接受——曲线数据和导出文件与拧紧数据同等重要。

| 消息类型 | 重试策略 | 最终失败 |
|---------|---------|---------|
| TighteningData | 3 次（1s/2s/4s）→ 弹窗 | 操作员选择 [重试] 或 [终止任务] |
| CurveData | 同上 | 同上 |
| ExportData | 同上 | 同上 |

### ProcessWithRetry 语义

```csharp
async Task ProcessWithRetry(Func<Task> action, CancellationToken ct) {
    int[] delays = { 1000, 2000, 4000 };
    for (int attempt = 0; attempt <= delays.Length; attempt++) {
        try {
            await action().ConfigureAwait(false);
            return; // 成功
        } catch (OperationCanceledException) { throw; }
        catch (Exception ex) {
            if (attempt == delays.Length) {
                // 3 次全失败 → 弹窗
                bool shouldRetry = await ShowRetryPopupAsync();
                if (!shouldRetry) throw new OperationCanceledException("用户终止");
                // shouldRetry → 再试最后一次（第 4 次）
                await action().ConfigureAwait(false);
                return;
            }
            await Task.Delay(delays[attempt], ct).ConfigureAwait(false);
        }
    }
}
```

**重试粒度**：`action` 内部如果 DB 写入成功但后续钩子失败，只重试钩子部分，不重复 DB 写入。

```csharp
// TighteningData: 分两阶段
await ProcessWithRetry(async () => {
    dto = await _panel.StoreDataToDatabaseAsync(t.Data);  // 阶段1：DB 写入
}, ct);
// DB 成功后，钩子 + UI 等不重试 DB
// 钩子本身也有独立 try-catch
```

### 重试流程

```
处理消息失败
  → 第 1 次重试 (1s 后)
  → 第 2 次重试 (2s 后)
  → 第 3 次重试 (4s 后)
  → 全部失败 → 异步弹窗（不阻塞消费者）
  → [重试] → 最后尝试一次
  → [终止任务] → 跳过当前消息，排空剩余
```

### 弹窗内容

```
⚠️ 数据存储失败

拧紧数据写入数据库失败，已自动重试 3 次（等待共 7 秒）仍未能成功。

可能原因：网络中断、数据库服务未启动、磁盘空间不足。

[重试] — 再尝试一次。如网络刚刚恢复，点击后数据将继续正常存储，不影响当前任务。
[终止任务] — 停止当前任务。已完成的拧紧数据不会丢失，任务结束后可手动导出 Excel/TXT 文件。

若重试后问题仍存在，请联系系统管理员检查网络和数据库状态。
```

### 弹窗线程模型

消费者**不通过 `Invoke` 阻塞**自己等待弹窗，而是用异步模式：

```
消费者(后台线程):
  tcs = new TaskCompletionSource<bool>(RunContinuationsAsynchronously);
  _panel.BeginInvoke(() => {
      popup.Show();                        // 模态，阻塞 UI 线程
      if (popup.ShouldRetry) tcs.SetResult(true);
      else tcs.SetResult(false);
      popup.Dispose();
  });
  if (await tcs.Task) { continue; }        // 消费者不阻塞，等待 TCS
  else { /* 终止 */ }
```

- `BeginInvoke`：发布弹窗到 UI 线程，消费者继续（不被 Invoke 卡住）
- `RunContinuationsAsynchronously`：确保 TCS 续体不在 UI 线程执行
- `[终止任务]`：设 `ShouldRetry=false` + `Close()` → 弹窗回调内通过 `BeginInvoke` 调 `TerminateMission(FINISHED_NG)`
- 消费者收到 `false` 后，跳过当前失败消息，继续排空剩余消息
- `TerminateMission`（由弹窗回调发起）：`TryComplete()` → 消费者排空 → `Close()` → `FormClosing` 等排空

### [终止任务] 后剩余消息策略

```
消费者收到 tcs=false:
  1. 跳过当前失败的消息（已重试 3 次失败）
  2. 继续消费 Channel 中剩余消息以排空
  3. 每条消息正常处理（含重试，可能再次弹窗）
  4. ReadAllAsync 因 Complete() 被调用而自然结束
  5. _consumeTask 完成 → TerminateMission 继续
```

- 当前失败消息之前的拧紧数据已成功入库，ExportDataMessage 会将其导出
- 剩余消息正常尝试处理（含重试），不是直接丢弃
- 如果剩余消息也失败 → 再次弹窗（操作员可再次选择终止）

## 6. 生命周期

### 启动

```
PrepareBeforeActivatingMission()
  → _exportTriggered = 0
  → 创建 Channel (Unbounded, SingleReader = true)
  → 创建 DataStorageConsumer + CancellationTokenSource
  → 启动 _consumeTask = ConsumeLoopAsync(_cts.Token)
```

### 停止（三入口协调）

三个停止入口：`TerminateMission`（任务结束）、`FormClosing`（用户关窗）、`Dispose`（异常直接销毁）。
核心原则：**先 Complete Writer 让消费者自然排空，超时兜底才 Cancel Token。**

```
TerminateMission(status):
  1. ...取消后台任务、锁定工具...
  2. OnMissionCompleted(status)
       → if (OK/NG 且导出启用)
           → if (Interlocked.Exchange(ref _exportTriggered, 1) == 0)
               → await EnqueueAsync(ExportDataMessage)   // 在 Complete 之前
  3. _channel.Writer.TryComplete()                       // 仅停止写入，不 Cancel
  4. this.Close() / BeginInvoke(Close)                   // 触发 FormClosing

OnFormClosing(e):
  1. if (Interlocked.Exchange(ref _disposeSignaled, 1) != 0) return
  2. _channel.Writer.TryComplete()                       // 确保已 Complete
  3. await Task.WhenAny(_consumerTask, Task.Delay(3000))  // 等排空（3s 超时）
  4. if (超时) _cts.Cancel()                              // 兜底取消
  5. await Task.WhenAny(_consumerTask, Task.Delay(500))
  6. base.OnFormClosing(e)

Dispose(disposing):
  1. if (Interlocked.Exchange(ref _disposeSignaled, 1) != 0) return
  2. _channel.Writer.TryComplete()
  3. _cts.Cancel()                                        // 不能 await，Cancel 兜底
  4. base.Dispose(disposing)
  5. // 消费者 PostToUI 中的 IsDisposed/ObjectDisposedException 兜底
```

### 生产者异常处理

```csharp
try {
    await _channel.Writer.WriteAsync(msg, ct);
} catch (ChannelClosedException) {
    // 任务已结束/窗体已关闭，正常丢弃
} catch (OperationCanceledException) {
    // 强制关闭
}
```

### 关键决策

| 决策 | 选择 | 原因 |
|------|------|------|
| Channel 类型 | `Unbounded` + `SingleReader` | 零丢失优先；单工具消费远快于生产；弹窗积压 <1MB |
| 数据集合 | `List<OperationDataVO>` + `ToList()` 快照 | 消费者单线程修改，UI 线程读到独立快照，无需 ConcurrentBag |
| `_exportTriggered` | 保留 | 防重复入队 Export 消息 |
| `Complete()` | `TryComplete()` | 幂等，三入口都可能调用 |
| Token 策略 | Complete 优先 → 排空 → 超时 Cancel | 零丢失路径靠自然排空，Cancel 仅兜底 |
| `_disposeSignaled` | `Interlocked.Exchange` guard | 三入口竞争中只有一个执行排空逻辑 |

## 7. 线程安全

```
生产者 (多线程)                    消费者 (单线程)
══════════════                     ═══════════════
工具回调线程 → WriteAsync(T)    Channel.Reader.ReadAllAsync()
工具回调线程 → WriteAsync(C)    foreach 顺序处理:
UI 线程     → WriteAsync(E)       1. DB 写入 (单线程，无竞争)
                                  2. List<VO>.Add (消费者线程)
                                  3. SafeBeginInvoke UI 更新 (快照)
```

- Channel 自身线程安全，`WriteAsync` 可被多线程并发调用（Unbounded 下同步完成）
- 消费者单线程处理，无需额外锁
- `List<OperationDataVO>` — 消费者单线程修改，UI 通过 `ToList()` 快照读取
- UI 更新通过 `SafeBeginInvoke`（检查 `IsDisposed` + `IsHandleCreated`）

### 消费者 crash 检测

```csharp
// 消费者最外层
try {
    await foreach (var msg in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        Process(msg);
} catch (OperationCanceledException) {
    // 强制停止（超时兜底）
} catch (Exception ex) {
    _consumerCrashed = true;
    SafeBeginInvoke(() => ShowCrashError(ex.Message));
}
```

| 检测机制 | 方式 |
|---------|------|
| `_consumeTask.Status == Faulted` | UI 定时器轮询 |
| `_consumerCrashed` flag | 消费者自身设置 |
| `_channel.Reader.Count` 持续增长 | 消费者停止但未 crash 时的监控 |

检测到异常后：禁止新任务激活，显示错误提示，标记当前任务为异常。

### 移除/重构的旧代码

| 移除项 | 位置 | 原因 |
|--------|------|------|
| `SemaphoreSlim _storeTighteningDataLock` | `AWorkplaceContentPanel` | 消费者单线程替代 |
| `StoreTighteningData` 方法 | `AWorkplaceContentPanel` | 逻辑移入 `DataStorageConsumer.ConsumeLoopAsync` |
| `StoreTighteningDataInternal` 方法 | `AWorkplaceContentPanel` | 同上 |
| `while` 轮询 + 10 秒超时 | `DoAfterRecevingCurveDataAsync` | FIFO 时序保证，不再需要 |
| `OnMissionCompleted` 中的锁排空 | `AWorkplaceContentPanel` | Export 消息自然排在最后 |
| `StoreTighteningData` override | WHYC, TZYX, GLB | 不再需要，改用钩子 |
| `StoreTighteningData` override | SCII_XT | 完全移除，改用钩子 |
| `DoAfterRecevingTighteningDataAsync` 中的 `StoreTighteningData` 调用 | SCII, YF | 改为 `_messageQueue.EnqueueAsync(...)` |
| `ConcurrentBag<OperationDataVO>` | `AWorkplaceContentPanel` | 改为 `List<OperationDataVO>`（消费者单线程修改，UI 通过 `ToList()` 快照读取） |

## 8. 各版本影响

变更集中在 `AWorkplaceContentPanel` 基类。

| 版本 | 影响 | 改动 |
|------|------|------|
| WorkplaceContentPanel (默认) | 无覆盖 | 零改动，继承基类 |
| WHYC | 覆盖 `StoreTighteningData` → 调 `base` | 零改动 |
| YF | 直接继承基类 | 零改动 |
| TZYX | 覆盖 `StoreTighteningData` → 调 `base` | 零改动 |
| SCII | 直接继承基类 | 零改动 |
| GLB | 覆盖 `StoreTighteningData` → `base` + 外部DB | 覆盖 `OnTighteningDataStored` 钩子 |
| SCII_XT | 完全覆盖 `StoreTighteningData` | 删除 override，覆盖 `OnTighteningDataStored` 钩子 |

### GLB 外部 DB

GLB 外部数据库写入为批量操作。改为在 `OnTighteningDataStored` 钩子中**积累数据**，在 `TerminateMission` 中批量写入。**关键：批量写入必须在 `base.TerminateMission` 之后，因为排空在基类中完成：**

```csharp
// GLB OnTighteningDataStored：只积累
override Task OnTighteningDataStored(OperationDataDTO dto) {
    _outerDbBatch.Add(dto);
    return Task.CompletedTask;
}

// GLB TerminateMission：先排空（base），再批量写入（此时数据完整）
override async Task TerminateMission(WorkplaceProcessStatus status) {
    await base.TerminateMission(status);                  // 排空 → 钩子全部触发
    StoreTighteningDataToOuterDatabase(_outerDbBatch);    // 数据完整
    _outerDbBatch.Clear();
}
```

### SCII_XT MES 数据暂存

SCII_XT 的 `_operationDataDTOs.Add(dto)` 移至 `OnTighteningDataStored` 钩子中处理。MES 发送移至 `base.TerminateMission` **之后**（排空完成，数据完整）：

```csharp
// SCII_XT OnTighteningDataStored：只积累
override Task OnTighteningDataStored(OperationDataDTO dto) {
    _operationDataDTOs.Add(dto);
    return Task.CompletedTask;
}

// SCII_XT TerminateMission：先排空，再发 MES
override async Task TerminateMission(WorkplaceProcessStatus status) {
    await base.TerminateMission(status);       // 排空
    if (!isPointInspection) {
        await SendDataToMES(_operationDataDTOs);
        _operationDataDTOs = new();
    }
}

## 9. 测试验证

### 单元测试

- `ChannelMessageQueue` EnqueueAsync/TryComplete
- TryComplete 多次调用不抛异常
- TryComplete 后 WriteAsync 抛 `ChannelClosedException`
- 多线程并发 EnqueueAsync 不丢消息

### 集成测试

- 拧紧 + 曲线正常顺序 → `currentOperationData.id` 正确关联
- 只有拧紧（无曲线）→ 消费者不卡住，下一条消息正常处理
- 导出消息在所有数据之后处理
- 所有消息类型写入失败 → 3 次重试 → 弹窗 → [重试] [终止任务]
- [终止任务] → TryComplete → 消费者排空 → FormClosing 等排空
- 窗体关闭 → Complete → 3s 排空 → 超时 Cancel 兜底
- 消费者 crash → `SafeBeginInvoke` 显示错误 → 禁止新任务
- GLB 外部 DB 批量写入在排空之后执行
- SCII_XT MES 发送在排空之后执行

### 回归测试

- 所有 7 个版本编译通过
- 现有功能（数据面板刷新、导出 Excel/TXT）行为不变
- 现有任务切换、激活、终止流程不受影响
