# Phase Progress Labels & Per-Phase ETA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `[删除]`/`[导入]` phase labels to progress and ETA, fix ETA accuracy by using per-phase elapsed time instead of total elapsed.

**Architecture:** Single-file change to `AdminManagementView.cs` — add `_phaseStartElapsed` field, capture stopwatch on phase transition, add phase prefix to percent/ETA labels, use `elapsed - _phaseStartElapsed` for ETA calculation.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Phase labels + per-phase ETA

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add _phaseStartElapsed field**

After `private string? _lastPhase;` add:
```csharp
        private double _phaseStartElapsed;
```

- [ ] **Step 2: Initialize _phaseStartElapsed in OnReimport**

After `_lastPhase = null;` add:
```csharp
            _phaseStartElapsed = 0;
```

- [ ] **Step 3: Replace OnProgressTimerTick**

Replace the entire `OnProgressTimerTick` method with:

```csharp
        private void OnProgressTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            ReimportProgressInfo? progress;
            lock (_progressLock) {
                progress = _latestProgress;
            }

            if (progress != null && progress.Phase == "deleting") {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                string ratio = progress.TotalToDelete > 0
                    ? $"{progress.DeletedCount}/{progress.TotalToDelete} 行 ({Math.Min((int)((double)progress.DeletedCount / progress.TotalToDelete * 100), 99)}%)"
                    : $"{progress.DeletedCount} 行";
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已删除 {ratio}, 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();

                if (progress.DeletedCount > 0 && progress.TotalToDelete > 0) {
                    double phaseElapsed = _reimportStopwatch.Elapsed.TotalSeconds - _phaseStartElapsed;
                    double etaSec = Math.Max(0, phaseElapsed / progress.DeletedCount * (progress.TotalToDelete - progress.DeletedCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    _percentLabel.Text = $"[删除] 进度: {Math.Min((int)((double)progress.DeletedCount / progress.TotalToDelete * 100), 99)}%";
                    _etaLabel.Text = $"[删除] 预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
                }
            } else if (progress != null && progress.TotalBatches > 0) {
                if (_lastPhase == "deleting") {
                    _phaseStartElapsed = _reimportStopwatch.Elapsed.TotalSeconds;
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 删除完成，开始导入物料码...\r\n");
                    _reimportLogBox.ScrollToCaret();
                }

                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 已处理 {progress.BatchCount}/{progress.TotalBatches} 批, 插入 {progress.TotalInserted} 行, 耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();

                if (progress.BatchCount > 0) {
                    double phaseElapsed = _reimportStopwatch.Elapsed.TotalSeconds - _phaseStartElapsed;
                    double etaSec = Math.Max(0, phaseElapsed / progress.BatchCount * (progress.TotalBatches - progress.BatchCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    _percentLabel.Text = $"[导入] 进度: {Math.Min((int)((double)progress.BatchCount / progress.TotalBatches * 100), 99)}%";
                    _etaLabel.Text = $"[导入] 预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
                }
            }

            if (progress != null) {
                _lastPhase = progress.Phase;
            }

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
        }
```

- [ ] **Step 4: Replace OnStatusTimerTick**

Replace the entire `OnStatusTimerTick` method with:

```csharp
        private void OnStatusTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";

            var progress = _latestProgress;
            if (progress == null) return;

            double completed = 0, total = 0;
            string phaseLabel;

            if (progress.Phase == "deleting" && progress.TotalToDelete > 0) {
                completed = progress.DeletedCount;
                total = progress.TotalToDelete;
                phaseLabel = "[删除]";
            } else if (progress.TotalBatches > 0) {
                completed = progress.BatchCount;
                total = progress.TotalBatches;
                phaseLabel = "[导入]";
            } else {
                return;
            }

            if (total > 0 && completed > 0) {
                double phaseElapsed = _reimportStopwatch.Elapsed.TotalSeconds - _phaseStartElapsed;
                int pct = Math.Min((int)(completed / total * 100), 99);
                _percentLabel.Text = $"{phaseLabel} 进度: {pct}%";
                double etaSec = Math.Max(0, phaseElapsed / completed * (total - completed));
                TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                _etaLabel.Text = $"{phaseLabel} 预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
            }
        }
```

- [ ] **Step 5: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: add phase labels to progress/ETA, use per-phase elapsed time for ETA"
```

---

### Task 2: Final build and verify

- [ ] **Step 1: Full build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: 0 errors.

- [ ] **Step 2: Smoke test**

1. Start import, verify deleting phase shows `[删除] 进度: 50%` and `[删除] 预计剩余: ...`
2. Verify ETA during delete is reasonable (not inflated)
3. After phase switch, verify `[导入] 进度: ...` and `[导入] 预计剩余: ...`
4. Verify import ETA is reasonable (not including delete time)
5. Verify `已运行` shows total time throughout
