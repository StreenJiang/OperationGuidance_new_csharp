# okSumToday 实时性优化设计

## 问题

SCII/XT 版本中，`_okSumPerDay`（当日 OK 计数）偶发不准确。

**根因：** `DoAfterRecevingTighteningDataAsync` 中 `StoreTighteningData` 和 `TerminateMission` 都是 fire-and-forget 调用。`StoreTighteningData` 内部的异步 DB 写入与 `TerminateMission` → `SetTodayData()` → `GetRecoreds()` 的 API 查询并发执行，DB 写入未完成时查询读到旧值。

## 方案：乐观更新 + 延迟对账

### 阶段一：立即乐观更新

最后一个螺栓 OK 后、`TerminateMission` 之前，直接本地 +1：

- `_productSumPerDay` +1
- `_okSumPerDay` +1
- `_ngRatePerDay` 用新值重算

用户即时看到更新，不阻塞。

### 阶段二：延迟对账

`TerminateMission` 完成后，fire-and-forget 启动后台对账：

1. `Task.Delay(1500)` — 等 DB 写入完成
2. 调用 `SetTodayData()` 从 API 拉真实数据覆盖
3. 如果真实 `okSum < 乐观 okSum` — DB 仍未同步，等 3 秒重试
4. 最多重试 3 次，`IsDisposed` 保护，异常静默 catch

## 改动范围

仅改 `WorkplaceContentPanel_SCII`。XT 继承 SCII，自动受益。

### WorkplaceContentPanel_SCII

**1. `DoAfterRecevingTighteningDataAsync` — 任务 OK 完成分支**

在 `TerminateMission(FINISHED_OK)` 之前插入：

```csharp
IncrementTodayCountersOptimistically();
```

**2. 新增 `IncrementTodayCountersOptimistically()`**

本地 +1，重算 ngRate。

**3. `TerminateMission` override**

移除 `ResetMissionDetails()` 调用（避免过时 API 数据覆盖乐观值），只做不依赖 API 的操作（SetPset, HandleScrewBitCounter, ResizeChildren），最后 fire-and-forget 延迟对账。

**4. 新增 `DelayedReconcileTodayData()`**

延迟 1.5s → SetTodayData() → 校验 → 最多 3 次重试。

### 不改的内容

- `ResetMissionDetails()` — 切换任务、初始加载仍用它，不涉及 DB 竞态
- NG 路径 — `StoreTighteningData` 和 NG 确认弹窗是串行的（弹窗阻塞），无竞态
- XT — 自动继承

## 时序

```
改动后:
  拧紧OK → 乐观+1 → TerminateMission → 用户即时看到结果
                              └→ 1.5s后GetRecoreds() → 覆盖为真实值
```
