# okSumToday 实时性优化 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `WorkplaceContentPanel_SCII` 中实现乐观更新 + 延迟对账，确保 `_okSumPerDay` 在任务 OK 完成后即时更新，不受 DB 异步写入延迟影响。

**Architecture:** 两阶段：阶段一在 `TerminateMission(FINISHED_OK)` 前本地 +1（乐观更新）；阶段二在 `TerminateMission` 完成后 fire-and-forget 延迟对账，等待 DB 写入完成后用 API 真实数据覆盖。

**Tech Stack:** C# WinForms, async/await, Task.Delay

**改动范围:** `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`（仅 `WorkplaceContentPanel_SCII` 类，XT 继承自动受益）

---

### Task 1: 新增 `IncrementTodayCountersOptimistically` 方法

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` — 在 `WorkplaceContentPanel_SCII` 类中新增方法

- [ ] **Step 1: 在 `SetTodayData()` 方法之后添加新方法**

在 line 590（`SetTodayData` 闭合大括号）之后插入：

```csharp
private void IncrementTodayCountersOptimistically() {
    if (InvokeRequired) {
        Invoke(() => IncrementTodayCountersOptimistically());
        return;
    }
    logger.Debug($"[SCII:IncrementTodayCountersOptimistically] Incrementing today counters optimistically");

    int sum = 0;
    int okSum = 0;
    int.TryParse(_productSumPerDay.GetTextBox(0).Box.Text, out sum);
    int.TryParse(_okSumPerDay.GetTextBox(0).Box.Text, out okSum);

    sum++;
    okSum++;

    double ngRate = 0;
    if (sum > 0) {
        ngRate = (sum - okSum) / (double)sum * 100;
    }

    _productSumPerDay.SetValue(0, sum + "");
    _okSumPerDay.SetValue(0, okSum + "");
    _ngRatePerDay.SetValue(0, $"{ngRate.ToString("F2")}%");
    logger.Debug($"[SCII:IncrementTodayCountersOptimistically] Optimistic update - Sum: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
}
```

- [ ] **Step 2: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: add IncrementTodayCountersOptimistically for optimistic today counter updates

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: 在 OK 完成分支调用乐观更新

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:1424-1438`

- [ ] **Step 1: 在 `TerminateMission(FINISHED_OK)` 之前插入乐观更新调用**

将 line 1424-1438：
```csharp
                                    } else {
                                        logger.Info($"[SCII:DoAfterRecevingTighteningDataAsync] All bolts completed, mission finished successfully");

                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Mission record updated with OK result");

                                        // Checks for challenge mission
                                        if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                            AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Challenge mission result added");
                                        }

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
```

改为：
```csharp
                                    } else {
                                        logger.Info($"[SCII:DoAfterRecevingTighteningDataAsync] All bolts completed, mission finished successfully");

                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Mission record updated with OK result");

                                        // Checks for challenge mission
                                        if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                            AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Challenge mission result added");
                                        }

                                        // 乐观更新当日计数，不等 DB 写入
                                        IncrementTodayCountersOptimistically();

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
```

- [ ] **Step 2: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: call IncrementTodayCountersOptimistically before TerminateMission OK

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: 新增 `DelayedReconcileTodayData` 方法

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs` — 在 `WorkplaceContentPanel_SCII` 类中新增方法

- [ ] **Step 1: 在 `IncrementTodayCountersOptimistically` 方法之后添加延迟对账方法**

```csharp
private async void DelayedReconcileTodayData() {
    // 读取乐观值（此时在 UI 线程）
    int optimisticOkSum = 0;
    int.TryParse(_okSumPerDay.GetTextBox(0).Box.Text, out optimisticOkSum);
    logger.Debug($"[SCII:DelayedReconcileTodayData] Starting delayed reconcile, optimistic OK sum: {optimisticOkSum}");

    // 等 DB 写入完成
    await Task.Delay(1500);

    for (int i = 0; i < 3; i++) {
        if (IsDisposed) {
            logger.Debug($"[SCII:DelayedReconcileTodayData] Disposed, stopping reconcile");
            return;
        }

        // SetTodayData 内部处理 InvokeRequired
        SetTodayData();
        logger.Debug($"[SCII:DelayedReconcileTodayData] Reconcile attempt {i + 1} completed");

        if (IsDisposed) return;

        // 读回真实值
        int realOkSum = 0;
        if (InvokeRequired) {
            Invoke(() => int.TryParse(_okSumPerDay.GetTextBox(0).Box.Text, out realOkSum));
        } else {
            int.TryParse(_okSumPerDay.GetTextBox(0).Box.Text, out realOkSum);
        }

        if (realOkSum >= optimisticOkSum) {
            logger.Info($"[SCII:DelayedReconcileTodayData] Reconcile succeeded - real: {realOkSum} >= optimistic: {optimisticOkSum}");
            return;
        }

        logger.Warn($"[SCII:DelayedReconcileTodayData] Real OK sum ({realOkSum}) < optimistic ({optimisticOkSum}), retrying...");
        await Task.Delay(3000);
    }

    logger.Warn($"[SCII:DelayedReconcileTodayData] Reconcile gave up after max retries");
}
```

- [ ] **Step 2: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: add DelayedReconcileTodayData for background API data reconciliation

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 4: 修改 `TerminateMission`，移除立即 `SetTodayData` 并启动延迟对账

**Files:**
- Modify: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs:1518-1534`

- [ ] **Step 1: 重写 `TerminateMission`**

将 line 1518-1534：
```csharp
        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            logger.Info($"[SCII:TerminateMission] Terminating mission with status: {status}");

            ResetMissionDetails();

            await base.TerminateMission(status);

            logger.Debug($"[SCII:TerminateMission] Base termination completed");

            // // If it's challenge mission, then switch mission automatically
            // if (_mission.is_challenge_mission == (int) YesOrNo.YES
            //         && _missionRecord != null
            //         && _missionRecord.mission_result == (int) TighteningStatus.OK) {
            //     _view.OpenWorkplaceView(_mission.challenge_mission_id);
            // }
            logger.Debug($"[SCII:TerminateMission] Mission termination completed");
        }
```

改为：
```csharp
        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            logger.Info($"[SCII:TerminateMission] Terminating mission with status: {status}");

            // 不调 SetTodayData()，避免用可能过时的 API 数据覆盖乐观值
            // 只做不依赖今日数据的操作
            SetPset();
            HandleScrewBitCounter();
            ResizeChildren();

            await base.TerminateMission(status);

            logger.Debug($"[SCII:TerminateMission] Base termination completed");

            // 后台对账：等 DB 写入完成后再拉真实数据覆盖乐观值
            if (status == WorkplaceProcessStatus.FINISHED_OK) {
                _ = DelayedReconcileTodayData();
            }

            logger.Debug($"[SCII:TerminateMission] Mission termination completed");
        }
```

**说明:** `ResetMissionDetails()` 被拆开，只调用不依赖今日 API 数据的三个操作（`SetPset`, `HandleScrewBitCounter`, `ResizeChildren`），跳过 `SetTodayData()`。OK 终止时启动延迟对账。

- [ ] **Step 2: Build 验证编译通过**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "feat: replace immediate SetTodayData with delayed reconcile in TerminateMission

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 5: 验证 `ResetMissionDetails` 仍被其他路径正确调用

**Files:**
- Read-only check: `OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs`

- [ ] **Step 1: 确认 `ResetMissionDetails` 的其他调用点**

`ResetMissionDetails` 除了在 `TerminateMission` 中被调用外，还在以下位置被调用：

1. `ActionAfterSwitchMission` (line 544) — 切换任务时，需要立即从 API 拉当日数据 ✓
2. `SetMissionDetails` (line 683) — 初始加载任务详情时 ✓

这两个路径不涉及 DB 写入竞态，保留原有 `ResetMissionDetails` → `SetTodayData` 行为是正确的。

- [ ] **Step 2: 验证无其他调用点**

```bash
grep -n "ResetMissionDetails" OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
```

Expected 输出: 只有上述几处引用。

- [ ] **Step 3: 最终 Build 验证**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: 最终 Commit**

```bash
git add OperationGuidance_new/Views/WorkplaceMissionView_SCII.cs
git commit -m "chore: verify ResetMissionDetails caller correctness

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```
