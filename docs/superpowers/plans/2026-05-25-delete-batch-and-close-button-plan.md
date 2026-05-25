# DELETE Batch Progress & Overlay Close Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix DELETE timeout by batching with progress, replace programmatic overlay dismissal with user-controlled close button.

**Architecture:** Three files changed: (1) Add `DeletedCount` to DTO; (2) Loop DELETE with LIMIT/TOP 1000 in API, reporting progress per batch; (3) Add close button to overlay, remove all popups, close both forms explicitly on button click.

**Tech Stack:** C# WinForms, .NET 8, MySQL/SQL Server

---

### Task 1: Add DeletedCount to ReimportProgressInfo

**Files:**
- Modify: `OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs`

- [ ] **Step 1: Add property**

Replace the file content:

```csharp
namespace OperationGuidance_service.Models.Responses {
    public class ReimportProgressInfo {
        public int BatchCount { get; set; }
        public int TotalInserted { get; set; }
        public int LastId { get; set; }
        public int TotalBatches { get; set; }
        public int DeletedCount { get; set; }
        public string? Phase { get; set; }
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs
git commit -m "feat: add DeletedCount to ReimportProgressInfo DTO"
```

---

### Task 2: Batch DELETE with progress in API

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Replace single DELETE with batch loop**

In `ReimportPartsBarcode`, replace lines 258-261:

Old:
```csharp
                req.OnProgress?.Invoke(new ReimportProgressInfo { Phase = "deleting" });
                // 1. delete all rows in parts_bar_code
                string deleteSql = $"delete from parts_bar_code where deleted = {(int) YesOrNo.NO}";
                rsp.DeletedRows = _partsBarCodeService.ExecuteSql(deleteSql);
```

New:
```csharp
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
                    });
                }
                rsp.DeletedRows = deletedTotal;
```

- [ ] **Step 2: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: batch DELETE with progress to avoid command timeout"
```

---

### Task 3: Close button, timer update, remove popups, close both forms

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add `_closeBtn` field**

Add after `private ProgressBar _reimportProgressBar;` (line 40):

```csharp
        private Button _closeBtn;
```

- [ ] **Step 2: Add close button to CreateOverlayForms**

In `CreateOverlayForms`, increase popup height from 370 to 410 (to fit the button). Add the close button after line 138 (after the Resize handler `};`):

Old (line 63):
```csharp
                Height = 370,
```

New:
```csharp
                Height = 410,
```

Then insert the close button after the Resize handler block (after line 138's `};`), before the closing `}` of `CreateOverlayForms`:

```csharp

            _closeBtn = new Button {
                Parent = _overlayPopup,
                Text = "正在导入...",
                Enabled = false,
                AutoSize = true,
            };
            _closeBtn.Click += (s, e) => ShowLoadingOverlay(false);

            // update the existing Resize handler to also position _closeBtn
            // replace the existing Resize handler entirely
```

Wait — the existing Resize handler is already added. Let's just append a second Resize handler to also position the button. Add after the first Resize handler's `};`:

```csharp
            _overlayPopup.Resize += (s, e) => {
                _closeBtn.Location = new Point((_overlayPopup.Width - _closeBtn.Width) / 2, _etaLabel.Bottom + 8);
            };
```

- [ ] **Step 3: Enable close button on completion**

In `OnReimport`, replace the entire method body from line 359 (`try {`) to line 419 (`}`) with the new flow that enables `_closeBtn` instead of calling `ShowLoadingOverlay(false)` + popups.

Replace lines 359-419:

Old:
```csharp
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
                _elapsedLabel.Text = $"已停止 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
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
```

New:
```csharp
            try {
                ReimportPartsBarcodeRsp rsp = await Task.Run(() => apis.ReimportPartsBarcode(req));
                _reimportStopwatch.Stop();
                _progressTimer.Stop();
                _statusTimer.Stop();

                if (rsp.ErrorMessage != null) {
                    string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 导入失败：{rsp.ErrorMessage}\r\n");
                    _reimportLogBox.ScrollToCaret();
                    _elapsedLabel.Text = $"已停止 {elapsed}";
                    _percentLabel.Text = "进度: 异常";
                    _etaLabel.Text = "预计剩余: --:--:--，预计结束: --:--:--";
                    _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                    _reimportProgressBar.Value = 0;
                } else {
                    string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                    _reimportLogBox.AppendText(
                        $"[{DateTime.Now:HH:mm:ss}] 导入完成！删除 {rsp.DeletedRows} 条旧记录, 插入 {rsp.InsertedRows} 条新记录, 总耗时 {elapsed}\r\n");
                    _reimportLogBox.ScrollToCaret();
                    _elapsedLabel.Text = $"已完成 {elapsed}";
                    _percentLabel.Text = "进度: 100%";
                    _etaLabel.Text = "预计剩余: 00:00:00，预计结束: 已完成";
                    _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                    _reimportProgressBar.Value = 100;
                }
            } catch (Exception ex) {
                _reimportStopwatch.Stop();
                _progressTimer.Stop();
                _statusTimer.Stop();
                _elapsedLabel.Text = $"已停止 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 导入异常：{ex.Message}\r\n");
                _reimportLogBox.ScrollToCaret();
                _percentLabel.Text = "进度: 异常";
                _etaLabel.Text = "预计剩余: --:--:--，预计结束: --:--:--";
                _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                _reimportProgressBar.Value = 0;
            } finally {
                _closeBtn.Text = "关闭";
                _closeBtn.Enabled = true;
                _reimportBtn.Enabled = true;
            }
```

Key changes:
- Removed ALL `ShowLoadingOverlay(false)` calls from completion paths
- Removed ALL `ShowNoticePopUp`/`ShowErrorPopUp`/`ShowWarningPopUp` calls
- Removed `IsTableLockError` checks
- Added `_closeBtn.Text = "关闭"` / `_closeBtn.Enabled = true` in finally block
- Every path (success, API error, exception) now enables the close button

- [ ] **Step 4: Update ShowLoadingOverlay(false) to close both forms**

Replace the else branch in `ShowLoadingOverlay` (lines 436-439):

Old:
```csharp
            } else {
                _overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
                _overlayBackdrop.Close();
            }
```

New:
```csharp
            } else {
                _overlayBackdrop.VisibleChanged -= OnBackdropVisibleChanged;
                if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
                    _overlayPopup.Owner = null;
                    _overlayPopup.Close();
                }
                if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
                    _overlayBackdrop.Close();
                }
            }
```

- [ ] **Step 5: Update OnProgressTimerTick to show DeletedCount**

Replace the "deleting" phase log line (lines 458-462):

Old:
```csharp
            if (progress != null && progress.Phase == "deleting") {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();
```

New:
```csharp
            if (progress != null && progress.Phase == "deleting") {
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 正在清空旧数据... 已删除 {progress.DeletedCount} 行, 已耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();
```

- [ ] **Step 6: Remove IsTableLockError method**

Delete lines 504-508:
```csharp
        private static bool IsTableLockError(string msg) {
            return msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("busy", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }
```

- [ ] **Step 7: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: add close button, remove popups, close both forms, show delete count"
```

---

### Task 4: Final build and verify

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
2. Verify close button shows "正在导入..." and is disabled
3. Verify log shows "正在清空旧数据... 已删除 N 行" (with running count, not just elapsed time)
4. Verify log shows batch progress after DELETE completes
5. On success: close button changes to "关闭", enabled; no extra popup
6. Click "关闭" → both popup and backdrop close
7. On error (e.g., table locked): log shows error, close button enables
8. Click "关闭" → both forms close, no lingering overlay
