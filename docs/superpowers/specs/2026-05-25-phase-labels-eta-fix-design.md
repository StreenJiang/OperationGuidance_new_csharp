# Phase Progress Labels & Per-Phase ETA

2026-05-25

## Overview

Two refinements to progress display:

1. Add phase labels (`[删除]` / `[导入]`) to progress percentage and ETA so it's clear which phase they refer to
2. Fix ETA accuracy by using per-phase elapsed time instead of total elapsed time (import ETA was wildly inflated because it included delete phase duration)

## Fix 1: Phase Labels

`_percentLabel` and `_etaLabel` get phase prefixes:

- **Deleting:** `[删除] 进度: 50%` / `[删除] 预计剩余: 00:01:00，预计结束: 14:30:00`
- **Importing:** `[导入] 进度: 75%` / `[导入] 预计剩余: 00:00:30，预计结束: 14:30:30`

`_elapsedLabel` stays as is (`已运行 hh:mm:ss` — already clear it's total runtime).

## Fix 2: Per-Phase ETA

**Root cause:** Import ETA uses `_reimportStopwatch.Elapsed.TotalSeconds` (total time including delete phase). If delete took 60s, the first import batch ETA is `60/1*99 = 5940s` — completely wrong.

**Fix:** Add `_phaseStartElapsed` field (double, seconds). When phase transitions, capture the stopwatch reading at that point. ETA formula uses `currentElapsed - _phaseStartElapsed` as the per-phase elapsed.

```
phaseElapsed = stopwatch.Elapsed.TotalSeconds - _phaseStartElapsed
etaSec = phaseElapsed / completed * (total - completed)
```

In `OnReimport`, initialize `_phaseStartElapsed = 0`.

In `OnProgressTimerTick`, on phase transition (deleting→importing):
```csharp
_phaseStartElapsed = _reimportStopwatch.Elapsed.TotalSeconds;
```

Both `OnProgressTimerTick` and `OnStatusTimerTick` use the same per-phase ETA logic.

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `AdminManagementView.cs` | Modify | `_phaseStartElapsed` field, phase labels, per-phase ETA |
