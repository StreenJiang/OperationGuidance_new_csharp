# Admin Overlay Minimize & Lifecycle Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix borderless form minimize cascade (WS_MINIMIZEBOX), replace Hide() with Close() lifecycle, and add on-demand form recreation to prevent null-reference on second click.

**Architecture:** Single-file change to `AdminManagementView.cs` — rename `OverlayBackdropForm` → `OverlayForm` with `CreateParams` adding `WS_MINIMIZEBOX`, extract overlay creation into `CreateOverlayForms()`, switch `ShowLoadingOverlay` to Close-based lifecycle with recreate-on-demand.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Rename OverlayBackdropForm → OverlayForm + add CreateParams

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Rename the inner class and add CreateParams**

Find the `OverlayBackdropForm` inner class (near the end of the file). Replace it entirely:

Old:
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

New:
```csharp
    internal sealed class OverlayForm : Form {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WS_MINIMIZEBOX = 0x20000;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX;
                return cp;
            }
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_MOUSEACTIVATE) {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
    }
```

- [ ] **Step 2: Update all references from OverlayBackdropForm to OverlayForm**

Find and replace ALL occurrences of `OverlayBackdropForm` with `OverlayForm` in the file:
- Field declaration: `private OverlayForm _overlayBackdrop;`
- Constructor instantiation: `new OverlayForm {`

Also change `_overlayPopup` from `new Form {` to `new OverlayForm {` so the popup also gets `WS_MINIMIZEBOX`.

- [ ] **Step 3: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "fix: rename OverlayBackdropForm to OverlayForm, add WS_MINIMIZEBOX via CreateParams"
```

---

### Task 2: Extract overlay creation to CreateOverlayForms + Close lifecycle

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Create the CreateOverlayForms method**

Insert before the constructor. Find `public AdminManagementView() {` and insert this method before it:

```csharp
        private void CreateOverlayForms() {
            if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) return;

            _overlayBackdrop = new OverlayForm {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.4,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
            };
            _overlayBackdrop.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            };

            _overlayPopup = new OverlayForm {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White,
                Opacity = 1.0,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Width = 460,
                Height = 370,
            };
            _overlayPopup.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            };
            _overlayPopup.Resize += (s, e) => {
                if (_overlayPopup.Width > 0 && _overlayPopup.Height > 0) {
                    _overlayPopup.Region = new Region(
                        WidgetUtils.RoundedRect(
                            new Rectangle(0, 0, _overlayPopup.Width - 1, _overlayPopup.Height - 1), 8));
                }
            };

            _overlayPopup.Owner = _overlayBackdrop;

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
                Font = _logFont,
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
                Font = _progressInfoFont,
            };

            _percentLabel = new Label {
                Parent = _overlayPopup,
                Text = "进度: 0%",
                AutoSize = true,
                ForeColor = Color.FromArgb(0xE8, 0x6C, 0x10),
                Font = _progressPercentFont,
            };

            _etaLabel = new Label {
                Parent = _overlayPopup,
                Text = "预计剩余: --:--:--，预计结束: --:--:--",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x88, 0x88, 0x88),
                Font = _progressInfoFont,
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
        }
```

IMPORTANT: The `Font` references use the static readonly fields: `_logFont`, `_progressInfoFont`, `_progressPercentFont`. These were already defined in a previous commit and must be used (not `new Font(...)`).

- [ ] **Step 2: Remove overlay creation from constructor**

In the constructor, remove the ENTIRE block that creates `_overlayBackdrop`, `_overlayPopup`, all child controls, and the Resize handler. This is the block from `_overlayBackdrop = new OverlayForm {` through the end of the Resize handler's `};`.

Replace it with a single call:
```csharp
            CreateOverlayForms();
```

Keep everything else in the constructor intact (backLink, pageTitle, passwordCard, reimportCard, timers, LayoutCards).

- [ ] **Step 3: Update ShowLoadingOverlay — Close not Hide, recreate on demand**

Replace the `ShowLoadingOverlay` method:

```csharp
        private void ShowLoadingOverlay(bool show) {
            if (show) {
                CreateOverlayForms();
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
                _overlayPopup.Close();
                _overlayBackdrop.Close();
            }
        }
```

Key differences from current:
- First line: `CreateOverlayForms()` — recreates if previously Closed
- `_overlayPopup.Owner = _overlayBackdrop` is set AFTER `_overlayBackdrop.Show()` — handle exists
- `Close()` instead of `Hide()` → fully destroys forms

- [ ] **Step 4: Build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "fix: Close lifecycle with on-demand recreation via CreateOverlayForms"
```

---

### Task 3: Final build and verify

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: 0 errors.

- [ ] **Step 2: Commit log**

Run: `git log --oneline -4`

- [ ] **Step 3: Smoke test checklist**

1. Login as admin, navigate to "后台管理"
2. Click "重新导入物料码" → overlay appears
3. Click taskbar icon to minimize → BOTH backdrop AND popup minimize together
4. Click taskbar icon to restore → both restore correctly
5. Let import complete → completion popup appears, click OK
6. Verify no lingering backdrop or popup
7. Click "重新导入物料码" again → overlay appears normally (no null reference)
8. Verify second import works correctly through to completion
