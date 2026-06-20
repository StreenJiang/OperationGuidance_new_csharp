# Data Storage 消息队列实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **提交由用户手动执行** — 任务中不包含 git 操作。

**Goal:** 用 `System.Threading.Channels` 消息队列替代 `SemaphoreSlim` + `while` 轮询，实现拧紧/曲线/导出的严格顺序、零丢失处理。

**Architecture:** 单 `Channel<DataStorageMessage>`（Unbounded, SingleReader）+ 单线程消费者 `DataStorageConsumer`。三入口协调停止。

**Tech Stack:** .NET 8, System.Threading.Channels, WinForms

**Spec:** `docs/superpowers/specs/2026-06-20-message-queue-data-storage-design.md`
**术语表:** `CONTEXT.md`

## Global Constraints

- 数据零丢失（Complete → 排空；3s 超时 Cancel 兜底）
- 单工具假设（CONTEXT.md "Single-Tool Assumption"）
- 7 个版本全部编译通过
- 现有 UI 行为不变
- 零第三方依赖

## File Map

| 文件 | 操作 | 职责 |
|------|------|------|
| `Utils/IMessageQueue.cs` | Create | 通用接口 |
| `Utils/ChannelMessageQueue.cs` | Create | Channel 封装 |
| `Utils/DataStorage/DataStorageMessage.cs` | Create | 消息类型 |
| `Utils/DataStorage/DataStorageConsumer.cs` | Create | 消费者 |
| `Views/SubViews/RetryPopupForm.cs` | Create | 重试弹窗 |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | Modify | 基类重构 |
| `Views/WorkplaceMissionView_GLB.cs` | Modify | GLB 适配 |
| `Views/WorkplaceMissionView_SCII_XT.cs` | Modify | SCII_XT 适配 |
| `Views/WorkplaceMissionView_SCII.cs` | Modify | SCII 适配 |
| `Views/WorkplaceMissionView_YF.cs` | Modify | YF 适配 |
| `Views/WorkplaceMissionView_WHYC.cs` | Modify | WHYC 适配 |
| `Views/WorkplaceMissionView_TZYX.cs` | Modify | TZYX 适配 |

---

### Task 1: Create `IMessageQueue<T>` interface

**Files:** Create: `OperationGuidance_new/Utils/IMessageQueue.cs`

- [ ] Write the interface and build:

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Utils {
    public interface IMessageQueue<T> {
        ValueTask EnqueueAsync(T message, CancellationToken ct = default);
        bool TryComplete();
        Task WaitForDrainAsync(CancellationToken ct);
    }
}
```

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 2: Create `ChannelMessageQueue<T>` implementation

**Files:** Create: `OperationGuidance_new/Utils/ChannelMessageQueue.cs`

- [ ] Write the implementation and build:

```csharp
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OperationGuidance_new.Utils {
    public class ChannelMessageQueue<T> : IMessageQueue<T> {
        private readonly Channel<T> _channel;

        public ChannelMessageQueue() {
            _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions {
                SingleReader = true,
            });
        }

        public Channel<T> Channel => _channel;

        public async ValueTask EnqueueAsync(T message, CancellationToken ct = default) {
            await _channel.Writer.WriteAsync(message, ct).ConfigureAwait(false);
        }

        public bool TryComplete() => _channel.Writer.TryComplete();

        public async Task WaitForDrainAsync(CancellationToken ct) {
            await _channel.Reader.Completion.WaitAsync(ct).ConfigureAwait(false);
        }
    }
}
```

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 3: Create `DataStorageMessage` types

**Files:** Create: `OperationGuidance_new/Utils/DataStorage/DataStorageMessage.cs`

- [ ] Write and build:

```csharp
using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Utils.DataStorage {
    public abstract record DataStorageMessage;

    public record TighteningDataMessage(OperationDataDTO Data) : DataStorageMessage;

    public record CurveDataMessage(CurveDataTemp Data, int DeviceId) : DataStorageMessage;

    public record ExportDataMessage(string Result) : DataStorageMessage;
}
```

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 4: Create `RetryPopupForm`

**Files:** Create: `OperationGuidance_new/Views/SubViews/RetryPopupForm.cs`

- [ ] Write and build:

```csharp
using System.Drawing;
using System.Windows.Forms;
using CustomLibrary.Forms;

namespace OperationGuidance_new.Views.SubViews {
    public class RetryPopupForm : CustomPopUpForm {
        public bool ShouldRetry { get; private set; }

        public RetryPopupForm() {
            Text = "数据存储失败";
            Size = new Size(520, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            var msg = new Label {
                Text = "拧紧数据写入数据库失败，已自动重试 3 次（等待共 7 秒）仍未能成功。\n\n"
                     + "可能原因：网络中断、数据库服务未启动、磁盘空间不足。\n\n"
                     + "若重试后问题仍存在，请联系系统管理员检查网络和数据库状态。",
                Location = new Point(20, 20),
                Size = new Size(460, 120),
                AutoSize = false,
            };

            var retryBtn = new Button {
                Text = "重试 — 再尝试一次。如网络刚刚恢复，点击后数据将继续正常存储，不影响当前任务。",
                Location = new Point(20, 160),
                Size = new Size(460, 45),
            };
            retryBtn.Click += (s, e) => { ShouldRetry = true; Close(); };

            var terminateBtn = new Button {
                Text = "终止任务 — 停止当前任务。已完成的拧紧数据不会丢失，任务结束后可手动导出 Excel/TXT 文件。",
                Location = new Point(20, 215),
                Size = new Size(460, 45),
            };
            terminateBtn.Click += (s, e) => { ShouldRetry = false; Close(); };

            Controls.Add(msg);
            Controls.Add(retryBtn);
            Controls.Add(terminateBtn);
        }
    }
}
```

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 5: Create `DataStorageConsumer`

**Files:** Create: `OperationGuidance_new/Utils/DataStorage/DataStorageConsumer.cs`
Depends on: Tasks 1-4

- [ ] Write the consumer (expected compile fail — AWorkplaceContentPanel members not yet exposed; fixed in Tasks 6-10):

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Models.DTOs;
using CustomLibrary.Utils;

namespace OperationGuidance_new.Utils.DataStorage {
    public class DataStorageConsumer {
        private readonly ILog _logger;
        private readonly AWorkplaceContentPanel _panel;
        private readonly Channel<DataStorageMessage> _channel;
        private Task _consumeTask;
        private volatile bool _consumerCrashed;

        public DataStorageConsumer(ILog logger, AWorkplaceContentPanel panel, Channel<DataStorageMessage> channel) {
            _logger = logger;
            _panel = panel;
            _channel = channel;
        }

        public Task ConsumeTask => _consumeTask;
        public bool ConsumerCrashed => _consumerCrashed;

        public void Start(CancellationToken ct) {
            _consumeTask = Task.Run(() => ConsumeLoopAsync(ct), ct);
        }

        private async Task ConsumeLoopAsync(CancellationToken ct) {
            try {
                await foreach (var msg in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                    switch (msg) {
                        case TighteningDataMessage t:
                            if (!await ProcessWithRetry(async () => {
                                await _panel.StoreDataToDatabaseAsync(t.Data).ConfigureAwait(false);
                                var vo = ConvertToVO(t.Data);
                                _panel.TighteningDataVOs.Add(vo);
                                var snapshot = _panel.TighteningDataVOs.ToList();
                                SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(snapshot));
                                await _panel.OnTighteningDataStored(t.Data).ConfigureAwait(false);
                            }, ct)) continue;
                            break;
                        case CurveDataMessage c:
                            if (!await ProcessWithRetry(async () => {
                                var opId = _panel.currentOperationData?.id;
                                if (opId != null) {
                                    CurveDataDTO dto = new();
                                    CommonUtils.ObjectConverter<CurveDataTemp, CurveDataDTO>(c.Data, dto);
                                    dto.operation_data_id = opId.Value;
                                    _panel.Apis.AddOrUpdateCurveData(new(dto));
                                } else {
                                    _logger.Error("[Consumer] CurveData but currentOperationData is null — data may be lost");
                                }
                            }, ct)) continue;
                            break;
                        case ExportDataMessage e:
                            if (!await ProcessWithRetry(async () => {
                                var snapshot = _panel.TighteningDataVOs.ToList();
                                var request = new ExportRequest {
                                    Data = snapshot,
                                    Fields = _panel.ExportFields,
                                    BasePath = _panel.ExportBasePath,
                                    ProductBatch = _panel.MissionRecord?.product_batch,
                                    ProductBarCode = _panel.MissionRecord?.product_bar_code,
                                    CompletedAt = DateTime.Now,
                                    Result = e.Result,
                                    EnableExcel = _panel.IsExcelExportEnabled,
                                    EnableTxt = _panel.IsTxtExportEnabled,
                                    MissionName = _panel.Mission?.name,
                                    WorkstationName = snapshot.Count > 0 ? snapshot[0].workstation_name : "",
                                };
                                await new DataExportService().ExportAsync(request).ConfigureAwait(false);
                                _panel.TighteningDataVOs.Clear();
                                SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(new List<OperationDataVO>()));
                            }, ct)) continue;
                            break;
                    }
                }
            } catch (OperationCanceledException) {
                _logger.Info("[Consumer] Cancelled (timeout or forced stop)");
            } catch (Exception ex) {
                _consumerCrashed = true;
                _logger.Fatal("[Consumer] Crashed", ex);
                SafeBeginInvoke(() => WidgetUtils.ShowErrorPopUp("数据存储服务异常中断，请联系管理员"));
            }
        }

        /// <returns>true=成功; false=用户终止(跳过当前消息，继续排空剩余)</returns>
        private async Task<bool> ProcessWithRetry(Func<Task> action, CancellationToken ct) {
            int[] delays = { 1000, 2000, 4000 };
            for (int attempt = 0; attempt <= delays.Length; attempt++) {
                try {
                    await action().ConfigureAwait(false);
                    return true;
                } catch (OperationCanceledException) { throw; }
                catch (Exception ex) {
                    _logger.Warn($"[Consumer] Attempt {attempt + 1} failed: {ex.Message}");
                    if (attempt == delays.Length) {
                        bool shouldRetry = await ShowRetryPopupAsync();
                        if (!shouldRetry) return false;
                        // [重试]: DB 已成功则不重复 DB 写入，仅重试钩子
                        await action().ConfigureAwait(false);
                        return true;
                    }
                    await Task.Delay(delays[attempt], ct).ConfigureAwait(false);
                }
            }
            return false;
        }

        private Task<bool> ShowRetryPopupAsync() {
            // RunContinuationsAsynchronously = 防死锁 (Spec Section 5)
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _panel.BeginInvoke(new Action(() => {
                using var popup = new RetryPopupForm();
                popup.Show(); // ShowDialog, blocks UI thread only
                tcs.TrySetResult(popup.ShouldRetry);
                // [终止任务]: ShouldRetry=false → Close → BeginInvoke 异步调 TerminateMission
                if (!popup.ShouldRetry) {
                    _panel.BeginInvoke(new Action(() =>
                        _panel.TerminateMission(WorkplaceProcessStatus.FINISHED_NG)));
                }
            }));
            return tcs.Task;
        }

        private void SafeBeginInvoke(Action action) {
            if (!_panel.IsDisposed && _panel.IsHandleCreated) {
                try { _panel.BeginInvoke(action); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
        }

        private static OperationDataVO ConvertToVO(OperationDataDTO dto) {
            OperationDataVO vo = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(dto, vo);
            return vo;
        }
    }
}
```

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
# Expected: FAIL — AWorkplaceContentPanel members not yet exposed
```

---

### Task 6: Modify `AWorkplaceContentPanel` — expose members for Consumer

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Step 1: Replace `ConcurrentBag` with `List` + expose property (line ~103):
```csharp
// OLD: protected ConcurrentBag<OperationDataVO> _tighteningDataVOs = new();
internal List<OperationDataVO> _tighteningDataVOs = new();
internal List<OperationDataVO> TighteningDataVOs => _tighteningDataVOs;
```

- [ ] Step 2: Expose `_apis` (after line 33):
```csharp
internal OperationGuidanceApis Apis => _apis;
```

- [ ] Step 3: Expose `_missionRecord` and `_mission` (after line 56):
```csharp
internal MissionRecordDTO? MissionRecord => _missionRecord;
internal ProductMissionDTO? Mission => _mission;
```

- [ ] Step 4: Expose export config (change `protected virtual` to `internal virtual`):
```csharp
internal virtual bool IsExcelExportEnabled => false;
internal virtual bool IsTxtExportEnabled => false;
internal virtual string ExportBasePath => MainUtils.GetDefaultStoragePath();
internal virtual List<OperationDataField> ExportFields =>
    MainUtils.GetOperationDataFields(MainUtils.GetSortConfig());
```

- [ ] Step 5: Add virtual hook:
```csharp
protected internal virtual Task OnTighteningDataStored(OperationDataDTO dto) => Task.CompletedTask;
```

- [ ] Step 6: Change `StoreDataToDatabaseAsync` to return `Task<OperationDataDTO>`:
```csharp
internal virtual async Task<OperationDataDTO> StoreDataToDatabaseAsync(OperationDataDTO dto) {
    // ... existing body ...
    currentOperationData = _apis.AddOrUpdateOperationData(new(dto)).OperationDataDTO;
    return currentOperationData;
}
```

- [ ] Step 7: Change visibility of `currentOperationData` and `RefreshTighteningDataPanel`:
```csharp
// Line 166: protected → internal
internal OperationDataDTO? currentOperationData;
// Line 2709: protected → internal
internal void RefreshTighteningDataPanel(IEnumerable<OperationDataVO> vos) { ... }
```

- [ ] Step 8: Build should now pass:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 7: Modify `AWorkplaceContentPanel` — add queue infrastructure

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Step 1: Add fields (after `_exportTriggered`, line 65):
```csharp
private ChannelMessageQueue<DataStorageMessage> _messageQueue;
private DataStorageConsumer _consumer;
private CancellationTokenSource _consumerCts;
private int _disposeSignaled;
```
Add: `using OperationGuidance_new.Utils.DataStorage;` `using System.Threading.Channels;`

- [ ] Step 2: Initialize in `PrepareBeforeActivatingMission()`:
```csharp
_exportTriggered = 0;
_disposeSignaled = 0;
_consumerCts?.Cancel();
_consumerCts?.Dispose();
_consumerCts = new CancellationTokenSource();
_messageQueue = new ChannelMessageQueue<DataStorageMessage>();
_consumer = new DataStorageConsumer(MainUtils.GetLogger(GetType()), this, _messageQueue.Channel);
_consumer.Start(_consumerCts.Token);
```

- [ ] Step 3: Build → expect compile pass:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 8: Modify `AWorkplaceContentPanel` — rewrite `DoAfterRecevingTighteningDataAsync`

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Replace all `StoreTighteningData(dataDTO)` with `EnqueueAsync`:
```csharp
// OLD: StoreTighteningData(dataDTO);
// NEW:
try {
    await _messageQueue.EnqueueAsync(new TighteningDataMessage(dataDTO));
} catch (ChannelClosedException) {
    logger.Warn("Channel closed, tightening data discarded");
}
```

Note: `DoAfterRecevingTighteningDataAsync` is `void` — use `Task.Run` wrapper or make `async void`:
```csharp
protected virtual async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
    // ... existing logic ...
    try {
        await _messageQueue.EnqueueAsync(new TighteningDataMessage(dataDTO));
    } catch (ChannelClosedException) { /* channel closed */ }
}
```

- [ ] Build (may fail for subclass overrides — fixed in later tasks):
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 9: Modify `AWorkplaceContentPanel` — rewrite `DoAfterRecevingCurveDataAsync`

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2557-2590`

- [ ] Replace the entire method:
```csharp
protected virtual async Task DoAfterRecevingCurveDataAsync(CurveDataTemp data, int deviceId) {
    string taskName = _mission?.name ?? "Unknown";
    logger.Debug($"[{taskName}] Curve entry, deviceId={deviceId}, id={data.result_data_identifier}");
    try {
        await _messageQueue.EnqueueAsync(new CurveDataMessage(data, deviceId));
    } catch (ChannelClosedException) {
        logger.Warn($"[{taskName}] Channel closed, curve data discarded");
    }
}
```

Curve association assumption: single-tool + FIFO → `currentOperationData` is always set when CurveMsg is consumed (CONTEXT.md "Single-Tool Assumption").

- [ ] Build verify:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 10: Modify `AWorkplaceContentPanel` — rewrite `OnMissionCompleted` and `TerminateMission`

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Step 1: Add `StopMessageQueueAsync` helper:
```csharp
internal async Task StopMessageQueueAsync() {
    if (_messageQueue == null) return;
    _messageQueue.TryComplete();
    if (_consumer?.ConsumeTask != null) {
        var timeout = Task.Delay(3000);
        var completed = await Task.WhenAny(_consumer.ConsumeTask, timeout);
        if (ReferenceEquals(completed, timeout)) {
            logger.Warn("[StopMessageQueue] Drain timeout, cancelling consumer");
            _consumerCts?.Cancel();
            await Task.WhenAny(_consumer.ConsumeTask, Task.Delay(500));
        }
    }
}
```

- [ ] Step 2: Rewrite `OnMissionCompleted` — enqueue Export instead of direct export:
```csharp
protected virtual async Task OnMissionCompleted(WorkplaceProcessStatus status) {
    if (status != WorkplaceProcessStatus.FINISHED_OK && status != WorkplaceProcessStatus.FINISHED_NG) return;
    if (Interlocked.Exchange(ref _exportTriggered, 1) == 1) return;
    if (!IsExcelExportEnabled && !IsTxtExportEnabled) return;
    string result = status == WorkplaceProcessStatus.FINISHED_OK ? "OK" : "NG";
    try {
        await _messageQueue.EnqueueAsync(new ExportDataMessage(result));
    } catch (ChannelClosedException) {
        logger.Warn("Channel closed, export discarded");
    }
}
```

- [ ] Step 3: In `TerminateMission`, after `OnMissionCompleted(status)`, add:
```csharp
await StopMessageQueueAsync();
```

Full TerminateMission flow:
```
TerminateMission:
  → 取消后台任务、锁定工具、力臂、条码、arm coordinates
  → OnMissionCompleted(status)     ← Enqueue(ExportDataMessage)
  → await StopMessageQueueAsync()  ← TryComplete → WhenAny(3s) → 超时 Cancel
  → _isRedo 检查、auto-activation
```

- [ ] Build:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 11: Modify `AWorkplaceContentPanel` — crash detection + Dispose

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Step 1: Add crash detection timer (in constructor or `OnHandleCreated`):
```csharp
private System.Windows.Forms.Timer _consumerHealthTimer;

// In OnHandleCreated:
_consumerHealthTimer = new System.Windows.Forms.Timer { Interval = 2000 };
_consumerHealthTimer.Tick += (s, e) => {
    if (_consumer != null && (_consumer.ConsumerCrashed || _consumer.ConsumeTask?.Status == TaskStatus.Faulted)) {
        _consumerHealthTimer.Stop();
        WidgetUtils.ShowErrorPopUp("数据存储服务异常中断，请联系管理员");
    }
};
_consumerHealthTimer.Start();
```

- [ ] Step 2: Add Dispose cleanup (in `Dispose(bool disposing)`):
```csharp
if (disposing && Interlocked.Exchange(ref _disposeSignaled, 1) == 0) {
    _messageQueue?.TryComplete();
    _consumerCts?.Cancel();
    _consumerHealthTimer?.Stop();
    _consumerHealthTimer?.Dispose();
}
```

- [ ] Step 3: Parent form FormClosing integration (in MainForm or parent form):
```csharp
protected override async void OnFormClosing(FormClosingEventArgs e) {
    if (_activeWorkplacePanel != null) {
        await _activeWorkplacePanel.StopMessageQueueAsync();
    }
    base.OnFormClosing(e);
}
```

- [ ] Build:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 12: Remove old code from `AWorkplaceContentPanel`

**Files:** Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs`

- [ ] Step 1: Remove `_storeTighteningDataLock` (line 64):
```csharp
// REMOVE: private readonly SemaphoreSlim _storeTighteningDataLock = new SemaphoreSlim(1, 1);
```

- [ ] Step 2: Remove `StoreTighteningData` method (lines 2592-2607)

- [ ] Step 3: Remove `StoreTighteningDataInternal` method (lines 2609-2644)

- [ ] Step 4: Remove `ConcurrentBag` import if no longer used (keep `System.Threading` for `CancellationTokenSource`, `Interlocked`)

- [ ] Build — expected FAIL for subclasses overriding removed methods:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 13: Adapt GLB

**Files:** Modify: `OperationGuidance_new/Views/WorkplaceMissionView_GLB.cs`

- [ ] Step 1: Remove `StoreTighteningData` override (lines 47-50)

- [ ] Step 2: Add `OnTighteningDataStored` hook:
```csharp
protected override Task OnTighteningDataStored(OperationDataDTO dto) {
    _operationDatasCached.Add(dto);
    return Task.CompletedTask;
}
```

- [ ] Step 3: Reorder `TerminateMission` — batch write AFTER `base.TerminateMission`:
```csharp
public override async Task TerminateMission(WorkplaceProcessStatus status) {
    await base.TerminateMission(status);  // 排空 → 钩子全部触发
    StoreTighteningDataToOuterDatabase(); // 数据完整
    // ... plc signals ...
}
```

- [ ] Step 4: Remove `StoreTighteningDataToOuterDatabase()` from `OnHandleDestroyed` (avoid double-write)

- [ ] Build:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 14: Adapt SCII_XT

**Files:** Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII_XT.cs`

- [ ] Step 1: Remove `StoreTighteningData` override (lines 281-299)

- [ ] Step 2: Add `OnTighteningDataStored` hook:
```csharp
protected override Task OnTighteningDataStored(OperationDataDTO dto) {
    _operationDataDTOs.Add(dto);
    return Task.CompletedTask;
}
```

- [ ] Step 3: Reorder `TerminateMission` — MES send AFTER `base.TerminateMission`:
```csharp
public override async Task TerminateMission(WorkplaceProcessStatus status) {
    SetPset();
    HandleScrewBitCounter();
    ResizeChildren();
    await base.TerminateMission(status);  // 排空 → 钩子全部触发

    if (status == WorkplaceProcessStatus.FINISHED_OK)
        DelayedReconcileTodayData();
    else if (status == WorkplaceProcessStatus.FINISHED_NG)
        DelayedRefreshTodayData();

    // CRITICAL: guard with isPointInspection per Spec Section 8
    if (!isPointInspection) {
        await SendDataToMES(_operationDataDTOs);
        _inBoundStationOk = false;
        _lidCodePrinted = false;
        _lastPrintedConfig = null;
        if (await OutBound())
            SwitchMissionByRecipe(_getRecipeCode());
    }
}
```

- [ ] Build:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 15: Adapt SCII and YF

**Files:** Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`, `WorkplaceMissionView_YF.cs`

- [ ] In both files, replace `StoreTighteningData(dataDTO)` with:
```csharp
try {
    await _messageQueue.EnqueueAsync(new TighteningDataMessage(dataDTO));
} catch (ChannelClosedException) {
    logger.Warn("Channel closed, data discarded");
}
```

- [ ] Build:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

---

### Task 16: Adapt WHYC and TZYX

**Files:** Modify: `OperationGuidance_new/Views/WorkplaceMissionView_WHYC.cs`, `WorkplaceMissionView_TZYX.cs`

- [ ] Step 1: Remove `StoreTighteningData` overrides that only call `base.StoreTighteningData` (WHYC line 579, TZYX line 518)

- [ ] Step 2: Replace `StoreTighteningData(dataDTO)` calls within `DoAfterRecevingTighteningDataAsync` overrides with `EnqueueAsync` (same pattern as Task 15)

- [ ] Build all 7 versions:
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: 零错误，零警告

---

### Task 17: Full build and smoke test

- [ ] Clean build:
```bash
dotnet clean OperationGuidance_new/OperationGuidance_new.csproj && dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] Run existing tests:
```bash
dotnet test OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] Manual smoke test: 启动 → 选择任务 → 激活 → 拧紧操作 → 数据面板刷新 → 曲线正确关联 → 任务完成导出 → 窗体关闭不崩溃

---

### Task 18: Verify CONTEXT.md

- [ ] Confirm "Single-Tool Assumption" section exists in `CONTEXT.md`:
```bash
grep "Single-Tool" CONTEXT.md
```
