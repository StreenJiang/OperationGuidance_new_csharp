# SendPSet 重连重试优化设计

## 问题

任务激活后下发 PSet 到控制器时，偶尔出现连续 3 次重试全部失败。根因：

- `SendPSetAsync` 超时后只返回 false，不切断连接
- 下次重试在可能已损坏的连接上进行，必然再次失败
- 全部失败后弹 `ShowWarningPopUp`（仅 OK），用户无法选择重试

## 方案

### 核心思路

每次重试前切断旧连接、等待重连建立、在新连接上发送 PSet，保证每次尝试都是干净连接。

弹窗从纯警告改为 Yes/No 选择，用户可触发额外重试。

### 新增 `ToolTask.ReconnectAndResendPset`

```csharp
// Tasks/ToolTask.cs
public async Task<bool> ReconnectAndResendPset(int pSetNumber, CancellationToken token)
```

流程：

1. `Status = CONNECTING` — 阻止 TaskCheckingLoop 并发重连
2. `CloseToTriggerReconnection()` — 关闭旧 socket
3. `await Task.Delay(100, token)` — 等待 RunTask 主循环感知断连并退出。取消时 `Status = DISCONNECTED` 让 TaskCheckingLoop 接管。
4. `Connect()` — 启动后台重连循环
5. 轮询 `Connected`（200ms × 50 = 10s），每次检查 `token`。取消时 `Status = DISCONNECTED`。
6. 超时未连上 → `return false`
7. `_currentPSet = -1` — 重连后缓存不可信，强制真实下发
8. `await SendPSetAsync(pSetNumber)` — 新连接上单次发送
9. 返回结果

### 去掉 `SendPSetAsync` 内部的 `CloseToTriggerReconnection()`

原 line 495：`SendCommand` 失败时调用 `CloseToTriggerReconnection()`。去掉该行，
连接生命周期统一由外层 `ReconnectAndResendPset` 管理。

### 修改 `SendPSet` 层

```csharp
// Views/AbstractViews/AWorkplaceContentPanel.cs — SendPSet 方法
```

**RetryStrategy**：`FixedDelay(3, 0)` — 去掉递增延迟，重连本身就是间隔。

**自动重试阶段**（3 次）：

每次 attempt 调用 `task.ReconnectAndResendPset(pset.Value, token)`，替代原来的 `task.SendPSetAsync(pset.Value)`。

成功 → 结束。失败 → UI 显示 `"程序号[{pset}]下发失败，正在重连重试...（第{n}次）"`。

**全部失败后弹窗**：

```csharp
// token 已取消则不弹窗，静默退出
if (token.IsCancellationRequested) return;

bool userRetry = WidgetUtils.ShowConfirmPopUp(
    $"程序号{pset}下发失败，已自动重试{_resendPsetMaxTimes}次。是否重新尝试？");
```

**用户选择后的循环**：

```csharp
while (!success && !token.IsCancellationRequested) {
    if (!WidgetUtils.ShowConfirmPopUp("...是否重新尝试？"))
        break;  // 用户选 No
    success = await retryStrategy.ExecuteAsync(
        async () => await task.ReconnectAndResendPset(pset.Value, token),
        ...
    );
}
```

用户每次点"是"→额外一轮（3次），全部失败继续弹窗。点"否"或 token 取消→停止。

## 完整流程

```
ChangeBoltStatusToWorking
  └─ SendPSet(boltButton, task, pset)
       │
       ├─ 自动重试阶段 (RetryStrategy FixedDelay(3, 0))
       │   for attempt = 1..3:
       │     success = await task.ReconnectAndResendPset(pset, token)
       │     if success → 成功，结束
       │     else → UI显示"下发失败，正在重连重试...（第{n}次）"
       │
       ├─ 自动重试全部失败
       │   if token.IsCancellationRequested → 静默退出
       │   ↓
       │   while (!success && !token.IsCancellationRequested):
       │     弹 ShowConfirmPopUp → 用户选择
       │     if No  → break（保持失败状态，流程继续）
       │     if Yes → 额外一轮重试（3次，含重连）
       │
       └─ 终态：成功 或 用户放弃
```

## 变更清单

| 文件 | 变更 |
|------|------|
| `Tasks/ToolTask.cs` | 新增 `ReconnectAndResendPset` / 去掉 `SendPSetAsync` 内部 `CloseToTriggerReconnection()` |
| `Views/AbstractViews/AWorkplaceContentPanel.cs` | `SendPSet`：调用新方法 + `FixedDelay(3,0)` + while 循环弹窗 |

## 不改动

- `SendPSetAsync` 其余逻辑 — 保持单次发送语义
- `Connect` / `ConnectAsync` — 保持现有重连循环
- `RetryStrategy` — 保持不变
- `ShowConfirmPopUp` — 已存在（`WidgetUtils.cs:611`），直接复用
- `CloseToTriggerReconnection` — 保持不变
