# SendPSet 重连重试优化实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SendPSet 每次重试前切断旧连接并重连，保证每次尝试都在干净连接上进行；失败弹窗改为 Yes/No 可重试。

**Architecture:** `ToolTask` 新增 `ReconnectAndResendPset` 组合断连+重连+单次发送；`SendPSet` 层改用 `FixedDelay(3,0)` + while 循环弹窗，移除 `SendPSetAsync` 内部的 `CloseToTriggerReconnection()` 调用。

**Tech Stack:** C# / .NET, WinForms MessageBox

---

### Task 1: 去掉 `SendPSetAsync` 内部的 `CloseToTriggerReconnection()`

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs:495`

- [ ] **Step 1: 删除 line 495 的 `CloseToTriggerReconnection()` 调用**

当前代码（line 491-496）：
```csharp
                    } else {
                        isSuccess = false;
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send failed");

                        CloseToTriggerReconnection();
                    }
```

改为：
```csharp
                    } else {
                        isSuccess = false;
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send failed");
                    }
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
预期：BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "refactor(sendpset): remove CloseToTriggerReconnection from SendPSetAsync

Connection lifecycle now managed by outer ReconnectAndResendPset."
```

---

### Task 2: 新增 `ReconnectAndResendPset` 方法

**Files:**
- Modify: `OperationGuidance_new/Tasks/ToolTask.cs` — 在 `SendPSetAsync` 之后、`SendLock` 之前插入新方法
- Add using: `System.Threading`（line 4 之后）

- [ ] **Step 1: 添加 `using System.Threading;`**

当前 using 块（line 1-7）：
```csharp
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;
```

改为：
```csharp
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
```

- [ ] **Step 2: 在 line 509（`SendPSetAsync` 结束 `}` 之后）和 line 511（`SendLock`）之间插入新方法**

```csharp
        /// <summary>
        /// 断开当前连接 → 重连 → 单次发送 PSet，作为一次原子重试
        /// </summary>
        public async Task<bool> ReconnectAndResendPset(int pSetNumber, CancellationToken token) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset pSetNumber={pSetNumber} start");

            // 1. 阻止 TaskCheckingLoop 并发重连
            Status = CONNECTING;

            // 2. 关闭旧连接
            CloseToTriggerReconnection();

            // 3. 等待 RunTask 主循环感知断连并退出
            try {
                await Task.Delay(100, token);
            } catch (OperationCanceledException) {
                Status = DISCONNECTED;  // 恢复状态，让 TaskCheckingLoop 接管
                return false;
            }

            // 4. 启动重连
            Connect();

            // 5. 轮询等待重连完成（200ms × 50 = 10s）
            int pollCount = 0;
            int pollMax = 50;
            while (!Connected && pollCount < pollMax && !token.IsCancellationRequested) {
                pollCount++;
                try {
                    await Task.Delay(200, token);
                } catch (OperationCanceledException) {
                    Status = DISCONNECTED;  // 恢复状态，让 TaskCheckingLoop 接管
                    return false;
                }
            }

            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset reconnect timeout after {pollMax * 200}ms");
                return false;
            }

            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset reconnected after {pollCount * 200}ms");

            // 6. 重连后 _currentPSet 缓存不可信，重置以强制真实下发
            _currentPSet = -1;

            // 7. 在新连接上单次发送 PSet
            return await SendPSetAsync(pSetNumber);
        }
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
预期：BUILD SUCCEEDED

- [ ] **Step 4: 提交**

```bash
git add OperationGuidance_new/Tasks/ToolTask.cs
git commit -m "feat(sendpset): add ReconnectAndResendPset for connection-reset retry"
```

---

### Task 3: 修改 `SendPSet` 层 — 调用新方法 + FixedDelay(3,0) + while 循环弹窗

**Files:**
- Modify: `OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs:2039-2090`

- [ ] **Step 1: 替换 RetryStrategy 和 ExecuteAsync 调用**

当前（line 2039-2077）：
```csharp
                // === 使用新的通用重试策略 ===
                var retryStrategy = RetryStrategy.IncrementalDelay(_resendPsetMaxTimes, 200);

                bool success = false;
                try {
                    success = await retryStrategy.ExecuteAsync(
                        async () => await task.SendPSetAsync(pset.Value),
                        (currentAttempt, maxAttempts) => {
                            // === 实时显示重试进度 ===
                            this.SafeInvoke(() => {
                                _pset.SetValue(0, $"程序号[{pset}]下发中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                                logger.Info($"【工作台】程序号[{pset}]下发中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                            });
                        },
                        () => {
                            this.SafeInvoke(() => {
                                // === 下发成功 ===
                                RemoveLockMsg(lockFailedMsg);
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                boltButton.CurrentParameterSet = pset;
                                _pset.SetValue(0, $"程序号 {pset} (发送成功)");
                                logger.Info($"【工作台】程序号[{pset}]发送成功");
                            });
                        },
                        () => {
                            // === 每次失败时更新UI（但不阻塞） ===
                            this.SafeInvoke(() => {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                AddLockMsg(lockFailedMsg);
                                _pset.SetValue(0, $"程序号[{pset}]下发失败...");
                                logger.Info($"【工作台】程序号[{pset}]下发失败...");
                            });
                        },
                        _activeMissionCts.Token);
                } catch (RetryException ex) {
                    // 处理重试异常
                    logger.Warn($"程序号{pset}发送失败，已重试{_resendPsetMaxTimes}次: {ex.Message}");
                    success = false;
                }
```

改为：
```csharp
                // === 每次尝试前断连+重连，保证干净连接 ===
                var retryStrategy = RetryStrategy.FixedDelay(_resendPsetMaxTimes, 0);

                bool success = false;
                try {
                    success = await retryStrategy.ExecuteAsync(
                        async () => await task.ReconnectAndResendPset(pset.Value, _activeMissionCts.Token),
                        (currentAttempt, maxAttempts) => {
                            this.SafeInvoke(() => {
                                _pset.SetValue(0, $"程序号[{pset}]下发中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                                logger.Info($"【工作台】程序号[{pset}]下发中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                            });
                        },
                        () => {
                            this.SafeInvoke(() => {
                                RemoveLockMsg(lockFailedMsg);
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                boltButton.CurrentParameterSet = pset;
                                _pset.SetValue(0, $"程序号 {pset} (发送成功)");
                                logger.Info($"【工作台】程序号[{pset}]发送成功");
                            });
                        },
                        () => {
                            this.SafeInvoke(() => {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                AddLockMsg(lockFailedMsg);
                                _pset.SetValue(0, $"程序号[{pset}]下发失败，正在重连重试...");
                                logger.Info($"【工作台】程序号[{pset}]下发失败，正在重连重试...");
                            });
                        },
                        _activeMissionCts.Token);
                } catch (RetryException ex) {
                    logger.Warn($"程序号{pset}发送失败，已重试{_resendPsetMaxTimes}次: {ex.Message}");
                    success = false;
                }
```

- [ ] **Step 2: 替换失败后处理为 while 循环弹窗**

当前（line 2079-2090）：
```csharp
                // === 失败后处理（无对话框，改为状态提示） ===
                if (!success && boltButton.CurrentParameterSet == null) {
                    RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                    AddLockMsg(lockFailedMsg);

                    logger.Info($"程序号 {pset} 下发失败，添加阻塞失败提示");

                    this.SafeInvoke(() => {
                        _pset.SetValue(0, $"程序号 {pset} (失败 - 已达最大重试次数)");
                        WidgetUtils.ShowWarningPopUp($"程序号{pset}下发失败，已自动重试{_resendPsetMaxTimes}次，请检查设备连接");
                    });
                }
```

改为：
```csharp
                // === 自动重试全部失败 → 弹窗让用户选择是否继续 ===
                while (!success && boltButton.CurrentParameterSet == null && !_activeMissionCts.Token.IsCancellationRequested) {
                    bool userRetry = false;
                    this.SafeInvoke(() => {
                        _pset.SetValue(0, $"程序号 {pset} (失败 - 已达最大重试次数)");
                        userRetry = WidgetUtils.ShowConfirmPopUp(
                            $"程序号{pset}下发失败，已自动重试{_resendPsetMaxTimes}次。是否重新尝试？");
                    });

                    if (!userRetry) {
                        RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                        AddLockMsg(lockFailedMsg);
                        logger.Info($"程序号 {pset} 下发失败，用户选择放弃");
                        break;
                    }

                    logger.Info($"程序号 {pset} 用户选择重试，开始额外一轮...");
                    try {
                        success = await retryStrategy.ExecuteAsync(
                            async () => await task.ReconnectAndResendPset(pset.Value, _activeMissionCts.Token),
                            (currentAttempt, maxAttempts) => {
                                this.SafeInvoke(() => {
                                    _pset.SetValue(0, $"程序号[{pset}]重试中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                                    logger.Info($"【工作台】程序号[{pset}]重试中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                                });
                            },
                            () => {
                                this.SafeInvoke(() => {
                                    RemoveLockMsg(lockFailedMsg);
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                    boltButton.CurrentParameterSet = pset;
                                    _pset.SetValue(0, $"程序号 {pset} (发送成功)");
                                    logger.Info($"【工作台】程序号[{pset}]重试后发送成功");
                                });
                            },
                            () => {
                                this.SafeInvoke(() => {
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                    AddLockMsg(lockFailedMsg);
                                    _pset.SetValue(0, $"程序号[{pset}]重试失败...");
                                    logger.Info($"【工作台】程序号[{pset}]重试失败...");
                                });
                            },
                            _activeMissionCts.Token);
                    } catch (RetryException ex) {
                        logger.Warn($"程序号{pset}额外轮重试失败: {ex.Message}");
                        success = false;
                    }
                }
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
预期：BUILD SUCCEEDED

- [ ] **Step 4: 提交**

```bash
git add OperationGuidance_new/Views/AbstractViews/AWorkplaceContentPanel.cs
git commit -m "feat(sendpset): use ReconnectAndResendPset + confirm dialog with retry loop"
```

---

## 验证检查点

全部 Task 完成后：
```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
预期：BUILD SUCCEEDED，0 errors，0 warnings（与改动无关的已有 warning 除外）
