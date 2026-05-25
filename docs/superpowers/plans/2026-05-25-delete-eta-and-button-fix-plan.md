# DELETE ETA & Close Button Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add TotalToDelete for DELETE ratio/ETA, unified ETA across delete+import, wider overlay, transition log, and fix close button.

**Architecture:** Three files: DTO adds TotalToDelete; API adds COUNT before DELETE and includes TotalToDelete in progress; UI changes popup to Form (removes WM_MOUSEACTIVATE block), inline close logic, wider layout, unified ETA on both phases, and transition log detection.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Add TotalToDelete to DTO + API changes

**Files:**
- Modify: `OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs`
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Add TotalToDelete to DTO**

Add after `DeletedCount`:

```csharp
        public int TotalToDelete { get; set; }
```

- [ ] **Step 2: API — add COUNT before DELETE, include TotalToDelete**

In `ReimportPartsBarcode`, before the delete loop (before `const int deleteBatchSize`), add a COUNT query:

```csharp
                // 0. count total rows to delete for progress estimation
                string deleteCountSql = $"select count(*) from parts_bar_code where deleted = {(int)YesOrNo.NO}";
                int totalToDelete = _partsBarCodeService.ExecuteScalar(deleteCountSql);

                // 1. batch delete all rows in parts_bar_code (avoids 10s command timeout on large tables)
                const int deleteBatchSize = 1000;
                int deletedTotal = 0;
                while (true) {
                    string deleteSql = SystemUtils.GetDBTypes() switch {
                        DBTypes.SQLSERVER => $"delete top({deleteBatchSize}) from parts_bar_code where deleted = {(int)YesOrNo.NO}",
                        _ => $"delete from parts_bar_code where deleted = {(int)YesOrNo.NO} limit {deleteBatchSize}",
                    };
                    int affected = _partsBarCodeService.ExecuteSql(deleteSql);
                    if (affected == 0) break;
                    deletedTotal += affected;
                    req.OnProgress?.Invoke(new ReimportProgressInfo {
                        Phase = "deleting",
                        DeletedCount = deletedTotal,
                        TotalToDelete = totalToDelete,
                    });
                }
                rsp.DeletedRows = deletedTotal;
```

- [ ] **Step 3: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add TotalToDelete to DTO and include in DELETE progress"
```

---

### Task 2: UI changes — close button fix, wider, unified ETA, transition log

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Change _overlayPopup from OverlayForm to Form**

In `CreateOverlayForms`, replace `new OverlayForm {` for `_overlayPopup` (line 57) with `new Form {`:

Old:
```csharp
            _overlayPopup = new OverlayForm {
```

New:
```csharp
            _overlayPopup = new Form {
```

Keep all property assignments the same (FormBorderStyle, BackColor, Opacity, ShowInTaskbar, StartPosition, Width, Height, FormClosing, Resize rounded rect, Owner).

- [ ] **Step 2: Widen overlay**

- Popup `Width = 460` → `Width = 600` (line 63)
- `_reimportLogBox.Width = 396` → `Width = 536` (line 95)
- `_reimportProgressBar.Width = 396` → `Width = 536` (line 102)

- [ ] **Step 3: Inline close button click handler**

Replace the close button creation (lines 141-151):

Old:
```csharp
            _closeBtn = new Button {
                Parent = _overlayPopup,
                Text = "正在导入...",
                Enabled = false,
                AutoSize = true,
            };
            _closeBtn.Click += (s, e) => ShowLoadingOverlay(false);

            _overlayPopup.Resize += (s, e) => {
                _closeBtn.Location = new Point((_overlayPopup.Width - _closeBtn.Width) / 2, _etaLabel.Bottom + 8);
            };
```

New:
```csharp
            _closeBtn = new Button {
                Parent = _overlayPopup,
                Text = "正在导入...",
                Enabled = false,
                AutoSize = true,
            };
            _closeBtn.Click += (s, e) => {
                if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
                    _overlayPopup.Owner = null;
                    _overlayPopup.Close();
                }
                if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
                    _overlayBackdrop.Close();
                }
            };

            _overlayPopup.Resize += (s, e) => {
                _closeBtn.Location = new Point((_overlayPopup.Width - _closeBtn.Width) / 2, _etaLabel.Bottom + 8);
            };
```

- [ ] **Step 4: Add _lastPhase field for transition detection**

Add after `private Button _closeBtn;`:

```csharp
        private string? _lastPhase;
```

In `OnReimport`, reset `_lastPhase` alongside `_latestProgress` (line 355). Add:

Old:
```csharp
            _latestProgress = null;
```

New:
```csharp
            _latestProgress = null;
            _lastPhase = null;
```

- [ ] **Step 5: Update OnProgressTimerTick — deleting ratio + ETA + transition log**

Replace the entire `OnProgressTimerTick` method body (lines 452-485):

Old:
```csharp
        private void OnProgressTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            ReimportProgressInfo? progress;
            lock (_progressLock) {
                progress = _latestProgress;
            }

            if (progress != null && progress.Phase == "deleting") {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已删除 {progress.DeletedCount} 行, 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();
            } else if (progress != null && progress.TotalBatches > 0) {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 已处理 {progress.BatchCount}/{progress.TotalBatches} 批, 插入 {progress.TotalInserted} 行, 耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();

                // percentage
                int pct = Math.Min((int)((double)progress.BatchCount / progress.TotalBatches * 100), 99);
                _percentLabel.Text = $"进度: {pct}%";

                // ETA
                if (progress.BatchCount > 0) {
                    double elapsedSec = _reimportStopwatch.Elapsed.TotalSeconds;
                    double etaSec = Math.Max(0, elapsedSec / progress.BatchCount * (progress.TotalBatches - progress.BatchCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    DateTime endTime = DateTime.Now.Add(eta);
                    _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {endTime:HH:mm:ss}";
                }
            }

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
        }
```

New:
```csharp
        private void OnProgressTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            ReimportProgressInfo? progress;
            lock (_progressLock) {
                progress = _latestProgress;
            }

            if (progress != null && progress.Phase == "deleting") {
                // transition log
                if (_lastPhase != "deleting") {
                    if (_lastPhase == null) {
                        // first progress tick, begin deleting
                    } else {
                        // shouldn't happen, but handle gracefully
                    }
                }

                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                string ratio = progress.TotalToDelete > 0
                    ? $"{progress.DeletedCount}/{progress.TotalToDelete} 行 ({Math.Min((int)((double)progress.DeletedCount / progress.TotalToDelete * 100), 99)}%)"
                    : $"{progress.DeletedCount} 行";
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已删除 {ratio}, 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();

                // unified ETA
                if (progress.DeletedCount > 0 && progress.TotalToDelete > 0) {
                    double elapsedSec = _reimportStopwatch.Elapsed.TotalSeconds;
                    double etaSec = Math.Max(0, elapsedSec / progress.DeletedCount * (progress.TotalToDelete - progress.DeletedCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    _percentLabel.Text = $"进度: {Math.Min((int)((double)progress.DeletedCount / progress.TotalToDelete * 100), 99)}%";
                    _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
                }
            } else if (progress != null && progress.TotalBatches > 0) {
                // transition log: deleting → importing
                if (_lastPhase == "deleting") {
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 删除完成，开始导入物料码...\r\n");
                    _reimportLogBox.ScrollToCaret();
                }

                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 已处理 {progress.BatchCount}/{progress.TotalBatches} 批, 插入 {progress.TotalInserted} 行, 耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();

                // unified ETA
                if (progress.BatchCount > 0) {
                    double elapsedSec = _reimportStopwatch.Elapsed.TotalSeconds;
                    double etaSec = Math.Max(0, elapsedSec / progress.BatchCount * (progress.TotalBatches - progress.BatchCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    _percentLabel.Text = $"进度: {Math.Min((int)((double)progress.BatchCount / progress.TotalBatches * 100), 99)}%";
                    _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
                }
            }

            if (progress != null) {
                _lastPhase = progress.Phase;
            }

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
        }
```

- [ ] **Step 6: Update OnStatusTimerTick — unified ETA for deleting phase**

Replace the `OnStatusTimerTick` method body (lines 488-504):

Old:
```csharp
        private void OnStatusTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";

            var progress = _latestProgress;
            if (progress != null && progress.TotalBatches > 0) {
                int pct = Math.Min((int)((double)progress.BatchCount / progress.TotalBatches * 100), 99);
                _percentLabel.Text = $"进度: {pct}%";

                if (progress.BatchCount > 0) {
                    double etaSec = Math.Max(0, _reimportStopwatch.Elapsed.TotalSeconds / progress.BatchCount * (progress.TotalBatches - progress.BatchCount));
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
                }
            }
        }
```

New:
```csharp
        private void OnStatusTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";

            var progress = _latestProgress;
            if (progress == null) return;

            double completed = 0, total = 0;

            if (progress.Phase == "deleting" && progress.TotalToDelete > 0) {
                completed = progress.DeletedCount;
                total = progress.TotalToDelete;
            } else if (progress.TotalBatches > 0) {
                completed = progress.BatchCount;
                total = progress.TotalBatches;
            }

            if (total > 0 && completed > 0) {
                int pct = Math.Min((int)(completed / total * 100), 99);
                _percentLabel.Text = $"进度: {pct}%";
                double etaSec = Math.Max(0, _reimportStopwatch.Elapsed.TotalSeconds / completed * (total - completed));
                TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {DateTime.Now.Add(eta):HH:mm:ss}";
            }
        }
```

- [ ] **Step 7: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: fix close button, wider overlay, unified ETA, transition log"
```

---

### Task 3: Final build and verify

- [ ] **Step 1: Full build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors.

- [ ] **Step 2: Commit log**

```bash
git log --oneline -5
```

- [ ] **Step 3: Smoke test checklist**

1. Login as admin, click "重新导入物料码"
2. Verify close button shows "正在导入..." disabled
3. Verify log shows `已删除 5000/10000 行 (50%)` with ratio during DELETE
4. Verify percentage and ETA update during DELETE phase
5. Verify transition log "删除完成，开始导入物料码..." appears
6. Verify log lines fit on single line (wider overlay)
7. On completion: close button becomes "关闭" enabled
8. **Click "关闭" — verify BOTH popup and backdrop close**
9. Verify no lingering overlay forms
