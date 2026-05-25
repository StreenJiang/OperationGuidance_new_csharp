# Admin Overlay UI Experience Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix popup minimize behavior, split timer for 1s status refresh, replace auto-close with user-confirmation popups, and move lock-error detection to UI layer.

**Architecture:** Four independent fixes in two files: (1) API catch block simplified to guarded rollback only; (2) `AdminManagementView` gets Owner in constructor, dual timer, `rsp.ErrorMessage` check, and completion popups via `WidgetUtils.ShowNoticePopUp`/`ShowErrorPopUp`/`ShowWarningPopUp`.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Clean up API catch block — remove IsTableLockError

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Remove IsTableLockError + ShowWarningPopUp from catch block**

The catch block in `ReimportPartsBarcode` currently reads:
```csharp
            } catch (Exception ex) {
                if (conn.State == ConnectionState.Open) {
                    transaction.Rollback();
                }
                rsp.ErrorMessage = ex.Message;

                if (IsTableLockError(ex)) {
                    SystemUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）");
                }
            }
```

Replace with:
```csharp
            } catch (Exception ex) {
                if (conn.State == ConnectionState.Open) {
                    transaction.Rollback();
                }
                rsp.ErrorMessage = ex.Message;
            }
```

- [ ] **Step 2: Remove IsTableLockError helper method**

Find and delete the method (around line 353-358):
```csharp
        private static bool IsTableLockError(Exception ex) {
            string msg = ex.Message;
            return msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("busy", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }
```

- [ ] **Step 3: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "refactor: move IsTableLockError from API to UI layer"
```

---

### Task 2: Owner fix + dual timer + IsTableLockError in AdminManagementView

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add `_statusTimer` field declaration**

After the `_progressTimer` field (around line 24), add:
```csharp
        private System.Windows.Forms.Timer _statusTimer;
```

- [ ] **Step 2: Set `_overlayPopup.Owner` in constructor**

Find where `_overlayPopup` is created in the constructor (look for `_overlayPopup = new Form {`. After the `FormClosing` handler and `SizeChanged` handler are attached, add:
```csharp
            _overlayPopup.Owner = _overlayBackdrop;
```

This goes after the `_overlayPopup` setup block — insert it right before the title label creation (before `var titleLabel = new Label {`).

- [ ] **Step 3: Remove Owner from ShowLoadingOverlay**

In `ShowLoadingOverlay`, find and remove the line:
```csharp
                _overlayPopup.Owner = _overlayBackdrop;
```

(This line should be around line 352 in the current file — delete it.)

- [ ] **Step 4: Add 1-second status timer creation in constructor**

Find where `_progressTimer` is created (around `_progressTimer = new System.Windows.Forms.Timer { Interval = 5000 };`). After that block, add:
```csharp
            _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusTimer.Tick += OnStatusTimerTick;
```

- [ ] **Step 5: Add `OnStatusTimerTick` handler**

Insert after `OnProgressTimerTick` method:

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

- [ ] **Step 6: Add `IsTableLockError` private method to AdminManagementView**

Insert after the new `OnStatusTimerTick` method:

```csharp
        private static bool IsTableLockError(string msg) {
            return msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("busy", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }
```

Note: takes `string` not `Exception` — callers pass `rsp.ErrorMessage` or `ex.Message` directly.

- [ ] **Step 7: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds (may have warning about `_statusTimer` not used yet — resolved in Task 3).

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: add Owner fix, dual timer, and IsTableLockError in AdminManagementView"
```

---

### Task 3: Replace OnReimport — rsp.ErrorMessage check + completion popup

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Replace the OnReimport method**

Find the current `OnReimport` method and replace it entirely:

```csharp
        private async void OnReimport(object? sender, EventArgs e) {
            DialogResult confirm = MessageBox.Show(
                null,
                "此操作将清空并重新导入物料码数据，可能需要较长时间，确定继续？",
                "确认操作",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _reimportBtn.Enabled = false;
            _reimportLogBox.Text = "";
            _latestProgress = null;
            _reimportProgressBar.Style = ProgressBarStyle.Marquee;
            ShowLoadingOverlay(true);
            _progressTimer.Start();
            _statusTimer.Start();
            _reimportStopwatch = Stopwatch.StartNew();

            var req = new ReimportPartsBarcodeReq {
                OnProgress = info => {
                    lock (_progressLock) {
                        _latestProgress = info;
                    }
                },
            };

            OperationGuidanceApis apis = SystemUtils.GetApis();

            try {
                ReimportPartsBarcodeRsp rsp = await Task.Run(() => apis.ReimportPartsBarcode(req));
                _reimportStopwatch.Stop();
                _progressTimer.Stop();
                _statusTimer.Stop();

                if (rsp.ErrorMessage != null) {
                    // API-level error
                    string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 导入失败：{rsp.ErrorMessage}\r\n");
                    _reimportLogBox.ScrollToCaret();
                    _elapsedLabel.Text = $"已停止 {elapsed}";
                    _percentLabel.Text = "进度: 异常";
                    _etaLabel.Text = "预计剩余: --:--:--，预计结束: --:--:--";
                    _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                    _reimportProgressBar.Value = 0;
                    ShowLoadingOverlay(false);

                    if (IsTableLockError(rsp.ErrorMessage)) {
                        WidgetUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）");
                    } else {
                        WidgetUtils.ShowErrorPopUp($"重新导入失败：{rsp.ErrorMessage}");
                    }
                } else {
                    // Success
                    string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 导入完成！删除 {rsp.DeletedRows} 条旧记录, 插入 {rsp.InsertedRows} 条新记录, 总耗时 {elapsed}\r\n");
                    _reimportLogBox.ScrollToCaret();
                    _elapsedLabel.Text = $"已完成 {elapsed}";
                    _percentLabel.Text = "进度: 100%";
                    _etaLabel.Text = "预计剩余: 00:00:00，预计结束: 已完成";
                    _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                    _reimportProgressBar.Value = 100;
                    ShowLoadingOverlay(false);

                    WidgetUtils.ShowNoticePopUp($"导入完成！\n删除 {rsp.DeletedRows} 条旧记录\n插入 {rsp.InsertedRows} 条新记录\n耗时 {elapsed}");
                }
            } catch (Exception ex) {
                _reimportStopwatch.Stop();
                _progressTimer.Stop();
                _statusTimer.Stop();
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 导入异常：{ex.Message}\r\n");
                _reimportLogBox.ScrollToCaret();
                _percentLabel.Text = "进度: 异常";
                _etaLabel.Text = "预计剩余: --:--:--，预计结束: --:--:--";
                _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                _reimportProgressBar.Value = 0;
                ShowLoadingOverlay(false);

                if (IsTableLockError(ex.Message)) {
                    WidgetUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）");
                } else {
                    WidgetUtils.ShowErrorPopUp($"重新导入异常：{ex.Message}");
                }
            } finally {
                _reimportBtn.Enabled = true;
            }
        }
```

- [ ] **Step 2: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: check rsp.ErrorMessage, show completion/error popups instead of auto-close"
```

---

### Task 4: Final build and smoke test

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 2: Verify changes summary**

Run: `git log --oneline -4`
Expected: 3 new commits covering the 3 tasks on branch `v2.1.x`.

- [ ] **Step 3: Manual smoke test**

1. Login as admin, navigate to "后台管理"
2. Click "重新导入物料码" → verify overlay appears
3. Minimize main app via taskbar → verify BOTH backdrop AND popup minimize together
4. Restore main app → verify overlay restores correctly
5. Verify "已运行" label updates every 1 second (not 5)
6. Verify "进度" percentage and ETA update every 1 second
7. Verify log lines still append every ~5 seconds
8. Wait for completion → verify `ShowNoticePopUp` appears with result, must click OK to dismiss
9. Test error path: trigger a table-lock scenario → verify `ShowWarningPopUp` with friendly message
