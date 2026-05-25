# Admin Management Overlay Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix z-order (remove TopMost, Owner chain + WndProc), add real-time progress log with elapsed time to the reimport overlay.

**Architecture:** Three concerns: (1) `OverlayBackdropForm` inner class with `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` + Owner chain for z-order; (2) `Action<ReimportProgressInfo>? OnProgress` callback on `ReimportPartsBarcodeReq` bridged to UI via `lock`-protected `_latestProgress` + 5s `System.Windows.Forms.Timer`; (3) expanded popup with multiline `TextBox` log, marquee `ProgressBar`, and elapsed-time label.

**Tech Stack:** C# WinForms, GDI+, .NET 8

---

### Task 1: Create `ReimportProgressInfo` DTO

**Files:**
- Create: `OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs`

- [ ] **Step 1: Write the DTO**

```csharp
namespace OperationGuidance_service.Models.Responses {
    public class ReimportProgressInfo {
        public int BatchCount { get; set; }
        public int TotalInserted { get; set; }
        public int LastId { get; set; }
        public int TotalBatches { get; set; }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds (new file compiles).

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/Responses/ReimportProgressInfo.cs
git commit -m "feat: add ReimportProgressInfo DTO for reimport progress reporting"
```

---

### Task 2: Add `OnProgress` callback to `ReimportPartsBarcodeReq`

**Files:**
- Modify: `OperationGuidance_service/Models/Requests/ReimportPartsBarcodeReq.cs`

- [ ] **Step 1: Add the callback field**

Replace the entire file content:

```csharp
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_service.Models.Requests {
    public class ReimportPartsBarcodeReq {
        public Action<ReimportProgressInfo>? OnProgress { get; set; }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Models/Requests/ReimportPartsBarcodeReq.cs
git commit -m "feat: add OnProgress callback to ReimportPartsBarcodeReq"
```

---

### Task 3: Add COUNT query + invoke `OnProgress` in `ReimportPartsBarcode` API

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:244-339`

- [ ] **Step 1: Add COUNT query and TotalBatches calculation before the batch loop**

After line 265 (`int batchCount = 0;`), insert:

```csharp
                // 0. count total mission_record rows for progress estimation
                string countSql = $"select count(*) from {_missionRecordService.TableName} where {_missionRecordService.ConditionWithoutUserId} and parts_bar_code is not null and parts_bar_code != ''";
                int totalRows = _missionRecordService.FindBySql(countSql).Count;
                int totalBatches = (int)Math.Ceiling((double)totalRows / batchSize);
```

- [ ] **Step 2: Remove `logInterval` constant**

Remove line 265:
```csharp
                const int logInterval = 100;
```

- [ ] **Step 3: Replace the logger.Info progress block with req.OnProgress invoke**

Old (line 303-305):
```csharp
                    if (batchCount % logInterval == 0) {
                        logger.Info($"ReimportPartsBarcode progress: {batchCount} batches, {totalInserted} rows inserted, lastId={lastId}");
                    }
```

New (replace lines 303-305):
```csharp
                    req.OnProgress?.Invoke(new ReimportProgressInfo {
                        BatchCount = batchCount,
                        TotalInserted = totalInserted,
                        LastId = lastId,
                        TotalBatches = totalBatches,
                    });
```

Fires every batch so the UI always has fresh data on its 5-second polling cycle.

- [ ] **Step 4: Note on COUNT approach**

`_missionRecordService.FindBySql(countSql).Count` fetches all rows then counts — acceptable for this one-shot admin operation. No need to add a new service method.

- [ ] **Step 5: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add COUNT query and invoke OnProgress each batch in ReimportPartsBarcode"
```

---

### Task 4: Z-order fix — `OverlayBackdropForm` + Owner chain

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add `OverlayBackdropForm` inner class at the bottom of the file (before the closing `}` of the namespace)**

Insert after line 307 (`}` that closes `AdminManagementView` class) and before line 308 (`}` that closes `namespace`):

```csharp
    internal sealed class OverlayBackdropForm : Form {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_MOUSEACTIVATE) {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
    }
```

- [ ] **Step 2: Change `_overlayBackdrop` field type from `Form` to `OverlayBackdropForm`**

Line 20 — change:
```csharp
private Form _overlayBackdrop;
```
to:
```csharp
private OverlayBackdropForm _overlayBackdrop;
```

- [ ] **Step 3: Change `_overlayBackdrop` instantiation + remove `TopMost = true` from both overlay forms**

Lines 74-82 — change `new Form` to `new OverlayBackdropForm` and remove `TopMost = true`:

```csharp
            _overlayBackdrop = new OverlayBackdropForm {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.4,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
            };
```

Line 90 — remove `TopMost = true,` from `_overlayPopup` initializer.

Line 90 — remove:
```csharp
TopMost = true,
```
from `_overlayPopup` initializer.

- [ ] **Step 4: Replace `ShowLoadingOverlay` method body**

Replace lines 290-306 (the entire `ShowLoadingOverlay` method):

```csharp
        private void ShowLoadingOverlay(bool show) {
            if (show) {
                Form mainForm = (Form)TopLevelControl!;
                _overlayBackdrop.Owner = mainForm;
                _overlayBackdrop.Location = mainForm.PointToScreen(Point.Empty);
                _overlayBackdrop.Size = mainForm.ClientSize;
                _overlayBackdrop.Show();
                _overlayPopup.Owner = _overlayBackdrop;
                _overlayPopup.Location = new Point(
                    _overlayBackdrop.Location.X + (_overlayBackdrop.Width - _overlayPopup.Width) / 2,
                    _overlayBackdrop.Location.Y + (_overlayBackdrop.Height - _overlayPopup.Height) / 2);
                _overlayPopup.Show();
            } else {
                _overlayPopup.Hide();
                _overlayBackdrop.Hide();
            }
        }
```

Note: removes `.Dispose()` calls — forms are created once in the constructor and reused via Show/Hide. Previous code disposed them, which would throw `ObjectDisposedException` on the second reimport click.

- [ ] **Step 5: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "fix: remove TopMost, use Owner chain + WM_MOUSEACTIVATE block for reimport overlay"
```

---

### Task 5: Popup UI rebuild — log TextBox, marquee, elapsed label

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Add new UI field declarations**

After line 23 (`private Panel _contentArea;`), add:

```csharp
        private System.Windows.Forms.Timer _progressTimer;
        private ReimportProgressInfo? _latestProgress;
        private readonly object _progressLock = new();
        private Stopwatch? _reimportStopwatch;
        private TextBox _reimportLogBox;
        private Label _elapsedLabel;
        private Label _percentLabel;
        private Label _etaLabel;
        private ProgressBar _reimportProgressBar;
```

- [ ] **Step 2: Add using for the new DTO at the top**

After line 10 (`using OperationGuidance_service.Models.Responses;`) — already imported, no change needed for the DTO path. But verify `ReimportProgressInfo` is in the same namespace as `ReimportPartsBarcodeRsp`. If not, add:
```csharp
using OperationGuidance_service.Models.Responses;
```
(Already present at line 9, no change required.)

- [ ] **Step 3: Replace the `_overlayPopup` content in the constructor**

In the constructor (lines 84-123), replace the popup creation block. Old (lines 84-123):

```csharp
            _overlayPopup = new Form {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White,
                Opacity = 1.0,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                Width = 400,
                Height = 120,
            };
            _overlayPopup.FormClosing += (s, e) => e.Cancel = true;
            void ApplyPopupRegion() {
                if (_overlayPopup.Width > 0 && _overlayPopup.Height > 0) {
                    _overlayPopup.Region = new Region(
                        WidgetUtils.RoundedRect(
                            new Rectangle(0, 0, _overlayPopup.Width - 1, _overlayPopup.Height - 1), 8));
                }
            }
            ApplyPopupRegion();
            _overlayPopup.Resize += (s, e) => ApplyPopupRegion();

            var loadingLabel = new Label {
                Parent = _overlayPopup,
                Text = "正在重新导入物料码，请稍候...",
                ForeColor = Color.FromArgb(0x44, 0x44, 0x44),
                AutoSize = true,
            };
            var marquee = new ProgressBar {
                Parent = _overlayPopup,
                Style = ProgressBarStyle.Marquee,
                Width = 300,
                Height = 24,
                MarqueeAnimationSpeed = 30,
            };
            _overlayPopup.Resize += (s, e) => {
                loadingLabel.Location = new Point(
                    (_overlayPopup.Width - loadingLabel.Width) / 2, 28);
                marquee.Location = new Point(
                    (_overlayPopup.Width - marquee.Width) / 2, 60);
            };
```

New:

```csharp
            _overlayPopup = new Form {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White,
                Opacity = 1.0,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Width = 460,
                Height = 370,
            };
            _overlayPopup.FormClosing += (s, e) => e.Cancel = true;
            void ApplyPopupRegion() {
                if (_overlayPopup.Width > 0 && _overlayPopup.Height > 0) {
                    _overlayPopup.Region = new Region(
                        WidgetUtils.RoundedRect(
                            new Rectangle(0, 0, _overlayPopup.Width - 1, _overlayPopup.Height - 1), 8));
                }
            }
            ApplyPopupRegion();
            _overlayPopup.Resize += (s, e) => ApplyPopupRegion();

            var titleLabel = new Label {
                Parent = _overlayPopup,
                Text = "正在重新导入物料码...",
                ForeColor = Color.FromArgb(0x44, 0x44, 0x44),
                AutoSize = true,
            };

            _reimportLogBox = new TextBox {
                Parent = _overlayPopup,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Pixel),
                BackColor = Color.FromArgb(0xFA, 0xFA, 0xFA),
                ForeColor = Color.FromArgb(0x33, 0x33, 0x33),
                BorderStyle = BorderStyle.None,
                Width = 396,
                Height = 170,
            };

            _reimportProgressBar = new ProgressBar {
                Parent = _overlayPopup,
                Style = ProgressBarStyle.Marquee,
                Width = 396,
                Height = 24,
                MarqueeAnimationSpeed = 30,
            };

            _elapsedLabel = new Label {
                Parent = _overlayPopup,
                Text = "已运行 00:00:00",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x88, 0x88, 0x88),
                Font = new Font(WidgetsConfigs.SystemFontFamily, 13F, FontStyle.Regular, GraphicsUnit.Pixel),
            };

            _percentLabel = new Label {
                Parent = _overlayPopup,
                Text = "进度: 0%",
                AutoSize = true,
                ForeColor = Color.FromArgb(0xE8, 0x6C, 0x10),
                Font = new Font(WidgetsConfigs.SystemFontFamily, 13F, FontStyle.Bold, GraphicsUnit.Pixel),
            };

            _etaLabel = new Label {
                Parent = _overlayPopup,
                Text = "预计剩余: --:--:--，预计结束: --:--:--",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x88, 0x88, 0x88),
                Font = new Font(WidgetsConfigs.SystemFontFamily, 13F, FontStyle.Regular, GraphicsUnit.Pixel),
            };

            _overlayPopup.Resize += (s, e) => {
                int padH = 32;
                titleLabel.Location = new Point(padH, 22);
                _reimportLogBox.Location = new Point(padH, titleLabel.Bottom + 12);
                _reimportProgressBar.Location = new Point(padH, _reimportLogBox.Bottom + 10);
                _elapsedLabel.Location = new Point(padH, _reimportProgressBar.Bottom + 8);
                _percentLabel.Location = new Point(padH, _elapsedLabel.Bottom + 4);
                _etaLabel.Location = new Point(padH, _percentLabel.Bottom + 2);
            };
```

- [ ] **Step 4: Add the 5-second Timer creation at the end of the constructor**

Insert before `LayoutCards();` (line 125):

```csharp
            _progressTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _progressTimer.Tick += OnProgressTimerTick;
```

- [ ] **Step 5: Add `OnProgressTimerTick` handler method**

Insert after `ShowLoadingOverlay` method (after line 306 in the updated file):

```csharp
        private void OnProgressTimerTick(object? sender, EventArgs e) {
            if (_reimportStopwatch == null) return;

            ReimportProgressInfo? progress;
            lock (_progressLock) {
                progress = _latestProgress;
            }

            if (progress != null && progress.TotalBatches > 0) {
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
                    double etaSec = elapsedSec / progress.BatchCount * (progress.TotalBatches - progress.BatchCount);
                    TimeSpan eta = TimeSpan.FromSeconds(etaSec);
                    DateTime endTime = DateTime.Now.Add(eta);
                    _etaLabel.Text = $"预计剩余: {eta.ToString(@"hh\:mm\:ss")}，预计结束: {endTime:HH:mm:ss}";
                }
            }

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";
        }
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: rebuild reimport popup with log TextBox, marquee, and elapsed timer"
```

---

### Task 6: Progress bridge + completion flow in `OnReimport`

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Replace `OnReimport` method**

Replace lines 256-288 (the entire `OnReimport` method):

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
                string elapsed = _reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 导入完成！删除 {rsp.DeletedRows} 条旧记录, 插入 {rsp.InsertedRows} 条新记录, 总耗时 {elapsed}\r\n");
                _reimportLogBox.ScrollToCaret();
                _elapsedLabel.Text = $"已完成 {elapsed}";
                _percentLabel.Text = "进度: 100%";
                _etaLabel.Text = "预计剩余: 00:00:00，预计结束: 已完成";
                _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                _reimportProgressBar.Value = 100;
                await Task.Delay(2000);
            } catch (Exception ex) {
                _reimportStopwatch.Stop();
                _reimportLogBox.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] 导入异常：{ex.Message}\r\n");
                _reimportLogBox.ScrollToCaret();
                _percentLabel.Text = "进度: 异常";
                _etaLabel.Text = "预计剩余: --:--:--，预计结束: --:--:--";
                _reimportProgressBar.Style = ProgressBarStyle.Blocks;
                _reimportProgressBar.Value = 100;
                await Task.Delay(2000);
            } finally {
                _progressTimer.Stop();
                ShowLoadingOverlay(false);
                _reimportBtn.Enabled = true;
            }
        }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 3: Quick review — verify `System.Diagnostics` using exists**

`Stopwatch` requires `using System.Diagnostics`. Confirm line 1 of AdminManagementView.cs already has it. (It does, from the existing `Stopwatch` usage.)

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: wire progress callback and completion flow into OnReimport"
```

---

### Task 7: Final build and smoke test

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds with zero warnings.

- [ ] **Step 2: Verify changes summary**

Run: `git log --oneline -6`
Expected: 6 new commits on the current branch covering all tasks.

- [ ] **Step 3: Manual smoke test checklist** (run the app)

1. Login as admin
2. Navigate to "后台管理"
3. Click "重新导入物料码"
4. Confirm the dialog appears
5. Verify: marquee progress bar is animating
6. Verify: elapsed time label updates every 5 seconds
7. Verify: log lines appear every 5 seconds with "X/Total" batch counts
8. Verify: percentage increases over time (progress bar area shows "进度: X%")
9. Verify: ETA shows reasonable estimates ("预计剩余: hh:mm:ss，预计结束: HH:mm:ss")
10. Wait for completion: verify result line in log, percentage → 100%, ETA → completed
11. Verify: progress bar fills to 100%, then overlay disappears after 2 seconds
12. Verify: button is re-enabled
13. Alt+Tab to another app: verify overlay does NOT cover other apps (TopMost removed)
