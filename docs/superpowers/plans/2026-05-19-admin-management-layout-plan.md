# Admin Management Layout Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix FlowLayoutPanel layout conflict by wrapping content in a plain Panel, redesign the header (back link + page title), and apply a rhythmic spacing system to card internals.

**Architecture:** Add `_contentArea` (plain `Panel`, `Dock=Fill`) as the single direct child of `AdminManagementView`. All other controls become grandchildren inside `_contentArea`, bypassing the `FlowLayoutPanel` base class layout engine. Absolute positioning within `_contentArea` via `LayoutCards()`.

**Tech Stack:** .NET 6 WinForms, GDI+, CustomLibrary

---

### Task 1: Add _contentArea container and rewrite field declarations

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs` (lines 11-19, fields)

- [ ] **Step 1: Replace field declarations**

Replace lines 11-19:

```csharp
public class AdminManagementView : CustomContentPanel {
    private CardPanel _passwordCard;
    private CardPanel _reimportCard;
    private TextBox _passwordBox;
    private TextBox _operationPasswordBox;
    private Button _savePwdBtn;
    private Button _reimportBtn;
    private Panel _loadingOverlay;
    private Panel _topBar;
```

With:

```csharp
public class AdminManagementView : CustomContentPanel {
    private Panel _contentArea;
    private CardPanel _passwordCard;
    private CardPanel _reimportCard;
    private TextBox _passwordBox;
    private TextBox _operationPasswordBox;
    private Button _savePwdBtn;
    private Button _reimportBtn;
    private Panel _loadingOverlay;
```

- [ ] **Step 2: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: build error (constructor references `_topBar` and `this` as parent — not yet updated). This confirms the field dependency chain.

---

### Task 2: Add _contentArea initialization and rewrite constructor

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs` (constructor, lines 21-98)

- [ ] **Step 1: Replace the entire constructor**

Replace lines 21-98 (from `public AdminManagementView() {` through `LayoutCards();`):

```csharp
        public AdminManagementView() {
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            AutoPadding = false;
            PaddingWithoutBorder = true;

            // Plain Panel container — bypasses FlowLayoutPanel from base class
            _contentArea = new Panel {
                Parent = this,
                Dock = DockStyle.Fill,
            };

            // Header: back link
            var backLink = new Label {
                Parent = _contentArea,
                Text = "← 返回",
                Cursor = Cursors.Hand,
                AutoSize = true,
                ForeColor = Color.FromArgb(0x88, 0x88, 0x88),
            };
            backLink.Font = new Font(WidgetsConfigs.SystemFontFamily, 12F, FontStyle.Regular, GraphicsUnit.Pixel);
            backLink.MouseEnter += (s, e) => backLink.ForeColor = Color.FromArgb(0xE8, 0x6C, 0x10);
            backLink.MouseLeave += (s, e) => backLink.ForeColor = Color.FromArgb(0x88, 0x88, 0x88);
            backLink.Click += (s, e) => WidgetUtils.BackToLoginView?.Invoke(false);

            // Header: page title
            var pageTitle = new Label {
                Parent = _contentArea,
                Text = "后台管理",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x33, 0x33, 0x33),
            };
            pageTitle.Font = new Font(WidgetsConfigs.SystemFontFamily, 22F, FontStyle.Bold, GraphicsUnit.Pixel);

            // Card 1: Change admin password
            _passwordCard = new CardPanel {
                Parent = _contentArea,
                Title = "修改管理员密码",
                Width = 640,
                Height = 220,
            };
            BuildPasswordCard();

            // Card 2: Re-import parts barcode
            _reimportCard = new CardPanel {
                Parent = _contentArea,
                Title = "重新导入物料码",
                Width = 640,
                Height = 170,
            };
            BuildReimportCard();

            // Loading overlay
            _loadingOverlay = new Panel {
                Parent = _contentArea,
                Visible = false,
                BackColor = Color.FromArgb(180, 0, 0, 0),
            };
            var loadingLabel = new Label {
                Parent = _loadingOverlay,
                Text = "正在重新导入物料码，请稍候...",
                ForeColor = Color.White,
                AutoSize = true,
            };
            var marquee = new ProgressBar {
                Parent = _loadingOverlay,
                Style = ProgressBarStyle.Marquee,
                Width = 300,
                Height = 24,
                MarqueeAnimationSpeed = 30,
            };
            _loadingOverlay.Resize += (s, e) => {
                loadingLabel.Location = new Point((_loadingOverlay.Width - loadingLabel.Width) / 2, _loadingOverlay.Height / 2 - 30);
                marquee.Location = new Point((_loadingOverlay.Width - marquee.Width) / 2, _loadingOverlay.Height / 2 + 10);
            };

            LayoutCards();

            // Register after all children exist — do not move above LayoutCards()
            _contentArea.SizeChanged += (s, e) => LayoutCards();
        }
```

- [ ] **Step 2: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors. The old `_topBar` and `SizeChanged += (s, e) => LayoutCards()` are removed.

---

### Task 3: Rewrite LayoutCards to use _contentArea

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs` (LayoutCards method, lines 100-110)

- [ ] **Step 1: Replace LayoutCards**

Replace lines 100-110:

```csharp
        private void LayoutCards() {
            int padding = WidgetUtils.ContentInnerBorderMargin();
            int cardWidth = Math.Min(480, Width - padding * 4);
            int topY = _topBar.Bottom + 20;

            _passwordCard.Width = cardWidth;
            _passwordCard.Location = new Point((Width - cardWidth) / 2, topY);

            _reimportCard.Width = cardWidth;
            _reimportCard.Location = new Point((Width - cardWidth) / 2, _passwordCard.Bottom + 24);
        }
```

With:

```csharp
        private void LayoutCards() {
            int areaW = _contentArea.Width;
            int areaH = _contentArea.Height;
            int hPad = WidgetUtils.ContentInnerBorderMargin(areaW, areaH);
            int cardWidth = Math.Min(640, areaW - hPad * 2);

            // Header: back link and page title
            Control backLink = _contentArea.Controls[0];
            Control pageTitle = _contentArea.Controls[1];

            backLink.Location = new Point(hPad, hPad);
            pageTitle.Location = new Point(hPad, backLink.Bottom + 8);

            // Cards
            int topY = pageTitle.Bottom + 28;
            int cardX = (areaW - cardWidth) / 2;

            _passwordCard.Width = cardWidth;
            _passwordCard.Location = new Point(cardX, topY);

            _reimportCard.Width = cardWidth;
            _reimportCard.Location = new Point(cardX, _passwordCard.Bottom + 24);
        }
```

Note: `backLink` and `pageTitle` are accessed by index because we don't store them in fields. They are always Controls[0] and Controls[1] of `_contentArea` since we add nothing else before them.

- [ ] **Step 2: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors.

---

### Task 4: Update card internal spacing

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs` (BuildPasswordCard: line 117 gap; BuildReimportCard: line 177)

- [ ] **Step 1: Update password card row spacing**

In `BuildPasswordCard()`, change `int gap = 12;` to `int rowGap = 16;` and add a separate button gap:

Replace lines 112-158:

```csharp
        private void BuildPasswordCard() {
            var pad = _passwordCard.ContentPadding;
            int labelW = 80;
            int inputW = 260;
            int rowH = WidgetUtils.TextOrComboBoxHeight();
            int rowGap = 16;
            int btnGap = 20;
            int y = pad.Top;

            new Label {
                Parent = _passwordCard,
                Text = "登录密码",
                AutoSize = true,
                Location = new Point(pad.Left, y + 4),
            };
            _passwordBox = new TextBox {
                Parent = _passwordCard,
                Width = inputW,
                Height = rowH,
                Location = new Point(pad.Left + labelW, y),
                PasswordChar = '*',
            };

            y += rowH + rowGap;

            new Label {
                Parent = _passwordCard,
                Text = "操作密码",
                AutoSize = true,
                Location = new Point(pad.Left, y + 4),
            };
            _operationPasswordBox = new TextBox {
                Parent = _passwordCard,
                Width = inputW,
                Height = rowH,
                Location = new Point(pad.Left + labelW, y),
                PasswordChar = '*',
            };

            y += rowH + btnGap;

            _savePwdBtn = new Button {
                Parent = _passwordCard,
                Text = "保存修改",
                AutoSize = true,
                Location = new Point(pad.Left + labelW + inputW - 80, y),
            };
            _savePwdBtn.Click += OnSavePassword;
        }
```

- [ ] **Step 2: Update reimport card button spacing**

Replace lines 161-179:

```csharp
        private void BuildReimportCard() {
            var pad = _reimportCard.ContentPadding;
            int y = pad.Top;

            var desc = new Label {
                Parent = _reimportCard,
                Text = "将清空 parts_bar_code 表，并从 mission_record 表\n重新拆分导入物料码数据。数据量大时可能耗时较长。",
                AutoSize = true,
                Font = new Font(WidgetsConfigs.SystemFontFamily, 14F, FontStyle.Regular, GraphicsUnit.Pixel),
                Location = new Point(pad.Left, y),
            };

            _reimportBtn = new Button {
                Parent = _reimportCard,
                Text = "重新导入物料码",
                AutoSize = true,
                Location = new Point(pad.Left, desc.Bottom + 20),
            };
            _reimportBtn.Click += OnReimport;
        }
```

- [ ] **Step 3: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors.

---

### Task 5: Verify no CardPanel changes needed

**Files:**
- Read: `CustomLibrary/Panels/CardPanel.cs`

- [ ] **Step 1: Confirm CardPanel is unchanged**

```bash
git diff CustomLibrary/Panels/CardPanel.cs
```

Expected: no diff (CardPanel.cs was modified in previous commits but should remain as-is for this plan).

If there are uncommitted changes to CardPanel.cs from the previous brainstorming session, they should have been committed already. If not, they stay as-is — this plan only modifies AdminManagementView.cs.

---

### Task 6: Full build verification

- [ ] **Step 1: Clean build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: 0 errors, 0 new warnings.

- [ ] **Step 2: Verify final file structure**

Check that `AdminManagementView.cs` has:
- No `_topBar` field
- `_contentArea` Panel as sole direct child of `this`
- `backLink` and `pageTitle` as Controls[0] and Controls[1] of `_contentArea`
- Two `CardPanel` children of `_contentArea`
- `LayoutCards()` computing positions from `_contentArea.Width`
- `rowGap = 16`, `btnGap = 20` in `BuildPasswordCard`
- `desc.Bottom + 20` in `BuildReimportCard`

---

### Task 7: Commit

- [ ] **Step 1: Stage and commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "refine: redesign admin management layout with content-area wrapper and rhythmic spacing

Fix FlowLayoutPanel overriding manual card positioning by wrapping all
content in a plain Panel (_contentArea) — bypasses the flow layout
engine from the base class. Replace top-bar with in-content back link
and page title. Apply differentiated row-gap (16px) vs button-gap (20px)
for visual rhythm.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```
