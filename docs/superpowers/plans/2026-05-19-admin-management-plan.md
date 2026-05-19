# Admin Management Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add admin management interface with password change and parts-barcode re-import, accessible via a new login button.

**Architecture:** Custom CardPanel GDI+ control for card UI → AdminManagementView hosts two cards → two new APIs on OperationGuidanceApis (ChangeAdminPassword, ReimportPartsBarcode) → LoginView gains third button + admin callback → MainForm.Designer.cs branches on admin login to load AdminManagementView instead of normal menu layout.

**Tech Stack:** WinForms, GDI+, C# (.NET)

---

### Task 1: CardPanel — GDI+ Custom Card Control

**Files:**
- Create: `CustomLibrary/Panels/CardPanel.cs`

- [ ] **Step 1: Create CardPanel class**

```csharp
using System.Drawing.Drawing2D;

namespace CustomLibrary.Panels {
    public class CardPanel: Panel {
        private GraphicsPath? _cardPath;
        private GraphicsPath? _shadowPath;
        private const int RADIUS = 8;
        private const int SHADOW_OFFSET = 4;
        private static readonly Color SHADOW_COLOR = Color.FromArgb(208, 208, 208);
        private static readonly Color CARD_BG = Color.White;
        private static readonly Color CARD_BORDER = Color.FromArgb(224, 224, 224);

        public CardPanel() {
            DoubleBuffered = true;
            ResizeRedraw = true;
            base.BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs eventargs) {
            base.OnResize(eventargs);
            BuildPaths();
        }

        private void BuildPaths() {
            _cardPath?.Dispose();
            _shadowPath?.Dispose();

            int cardW = Width - SHADOW_OFFSET - 2;
            int cardH = Height - SHADOW_OFFSET - 2;
            _cardPath = CreateRoundedPath(1, 1, cardW, cardH, RADIUS);
            _shadowPath = CreateRoundedPath(SHADOW_OFFSET + 1, SHADOW_OFFSET + 1, cardW, cardH, RADIUS);
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (_cardPath == null || _shadowPath == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var shadowBrush = new SolidBrush(SHADOW_COLOR);
            e.Graphics.FillPath(shadowBrush, _shadowPath);

            using var cardBrush = new SolidBrush(CARD_BG);
            e.Graphics.FillPath(cardBrush, _cardPath);

            using var borderPen = new Pen(CARD_BORDER, 1);
            e.Graphics.DrawPath(borderPen, _cardPath);

            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cardPath?.Dispose();
                _shadowPath?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static GraphicsPath CreateRoundedPath(int x, int y, int w, int h, int r) {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds (CardPanel exists but unused).

- [ ] **Step 3: Commit**

```bash
git add CustomLibrary/Panels/CardPanel.cs
git commit -m "feat: add CardPanel GDI+ custom control with rounded corners and shadow

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 2: ChangeAdminPassword API

**Files:**
- Create: `OperationGuidance_service/Models/Requests/ChangeAdminPasswordReq.cs`
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Create request DTO**

```csharp
namespace OperationGuidance_service.Models.Requests {
    public class ChangeAdminPasswordReq {
        public string? Password { get; set; }
        public string? OperationPassword { get; set; }
    }
}
```

- [ ] **Step 2: Add ChangeAdminPassword method to OperationGuidanceApis.cs**

Insert after the `AdminPasswordValidate` method (after line 215):

```csharp
// 修改admin密码
public string ChangeAdminPassword(ChangeAdminPasswordReq req) {
    if (!SystemUtils.IsAdmin) {
        return "权限不足";
    }

    string sql = $"select * from {_userAccountInfoService.TableName} where {_userAccountInfoService.ConditionWithoutUserId} and account = 'admin'";
    List<UserAccountInfo> users = _userAccountInfoService.FindBySql(sql);

    if (users.Count == 0) {
        return "未找到admin账户";
    }

    UserAccountInfo admin = users[0];

    if (!string.IsNullOrEmpty(req.Password)) {
        admin.password = SystemUtils.ToMD5String(req.Password);
    }
    if (!string.IsNullOrEmpty(req.OperationPassword)) {
        admin.operation_password = SystemUtils.ToMD5String(req.OperationPassword);
    }

    _userAccountInfoService.UpdateEntity(admin);
    return "";
}
```

- [ ] **Step 3: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/Requests/ChangeAdminPasswordReq.cs OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add ChangeAdminPassword API for admin password management

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 3: ReimportPartsBarcode API

**Files:**
- Create: `OperationGuidance_service/Models/Requests/ReimportPartsBarcodeReq.cs`
- Create: `OperationGuidance_service/Models/Responses/ReimportPartsBarcodeRsp.cs`
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs`

- [ ] **Step 1: Create request and response DTOs**

```csharp
namespace OperationGuidance_service.Models.Requests {
    public class ReimportPartsBarcodeReq {
    }
}
```

```csharp
namespace OperationGuidance_service.Models.Responses {
    public class ReimportPartsBarcodeRsp {
        public int DeletedRows { get; set; }
        public int InsertedRows { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
```

- [ ] **Step 2: Autowire SqlExecuteRecordService and add ReimportPartsBarcode method**

Add `using OperationGuidance_service.Configurations;` at top of `OperationGuidanceApis.cs`.

Add autowired field after line 63 (`_partsBarCodeService`):

```csharp
[Autowired]
private SqlExecuteRecordService _sqlExecuteRecordService;
```

Add method after `ChangeAdminPassword`:

```csharp
// 重新导入物料码
public ReimportPartsBarcodeRsp ReimportPartsBarcode(ReimportPartsBarcodeReq req) {
    ReimportPartsBarcodeRsp rsp = new();

    if (!SystemUtils.IsAdmin) {
        rsp.ErrorMessage = "权限不足";
        return rsp;
    }

    try {
        // 1. 删除 parts_bar_code 表数据
        string deleteSql = $"delete from parts_bar_code where deleted = {(int) YesOrNo.NO}";
        rsp.DeletedRows = _partsBarCodeService.ExecuteSql(deleteSql);

        // 2. 查询 mission_record 中有 parts_bar_code 的记录
        string selectSql = $"select * from {_missionRecordService.TableName} where {_missionRecordService.ConditionWithoutUserId} and parts_bar_code is not null and parts_bar_code != ''";
        List<MissionRecord> records = _missionRecordService.FindBySql(selectSql);

        // 3. 拆分逗号分隔的条码，逐行插入 parts_bar_code
        int inserted = 0;
        foreach (MissionRecord record in records) {
            if (string.IsNullOrEmpty(record.parts_bar_code)) continue;

            string[] barcodes = record.parts_bar_code.Split(',');
            foreach (string barcode in barcodes) {
                string trimmed = barcode.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                PartsBarCode entity = new() {
                    mission_record_id = record.id,
                    parts_bar_code = trimmed,
                };
                _partsBarCodeService.AddEntity(entity);
                inserted++;
            }
        }
        rsp.InsertedRows = inserted;

        // 4. 检查 sql_execute_record 是否有 20250625_1 记录，没有则补上
        DBTypes dbType = SystemUtils.GetDBTypes();
        string fileName = dbType switch {
            DBTypes.MYSQL => "modify_mysql_20250625_1",
            DBTypes.SQLSERVER => "modify_sqlserver_20250625_1",
            _ => "modify_sqlite_20250625_1",
        };

        string checkSql = $"select * from sql_execute_record where file_name = '{fileName}' and deleted = {(int) YesOrNo.NO}";
        List<SqlExecuteRecord> existingRecords = _sqlExecuteRecordService.FindBySql(checkSql);

        if (existingRecords.Count == 0) {
            SqlExecuteRecord newRecord = new() {
                file_name = fileName,
            };
            _sqlExecuteRecordService.AddEntity(newRecord);
        }
    } catch (Exception ex) {
        rsp.ErrorMessage = ex.Message;
    }

    return rsp;
}
```

- [ ] **Step 3: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_service/Models/Requests/ReimportPartsBarcodeReq.cs OperationGuidance_service/Models/Responses/ReimportPartsBarcodeRsp.cs OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "feat: add ReimportPartsBarcode API with C# split-based re-import

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 4: LoginView — Add Admin Login Button

**Files:**
- Modify: `OperationGuidance_new/Views/LoginView.cs`

- [ ] **Step 1: Add admin login fields and callback**

Change the `_isLoggedIn` field (line 22) to include `_isAdminLogin`:

```csharp
private bool _isLoggedIn = false;
private bool _isAdminLogin = false;
```

Add the admin login callback property after line 29 (`AfterLogin` property):

```csharp
public Action<Size>? AfterAdminLogin { get; set; }
```

- [ ] **Step 2: Add ClickAdminLogin method and admin button**

Replace lines 72-73 in `ShowLoginForm`:

```csharp
_loginForm.AddButton("后台管理").Click += (s, e) => ClickAdminLogin();
_loginForm.AddButton("登录").Click += (s, e) => ClickLogin();
_loginForm.AddButton("退出").Click += (s, e) => _loginForm.Close();
```

Add the `ClickAdminLogin` local function inside `ShowLoginForm`, after `ClickLogin` (after line 83):

```csharp
void ClickAdminLogin() {
    _isAdminLogin = true;
    string account = _loginForm.AccountBox.GetTextBox(0).Box.Text;
    string password = _loginForm.PasswordBox.GetTextBox(0).Box.Text;
    CheckLoginByApi(account, password);
}
```

- [ ] **Step 3: Route to admin callback in ActionAfterLogin**

Replace the `ActionAfterLogin` method (lines 94-108):

```csharp
protected void ActionAfterLogin() {
    _isLoggedIn = true;
    _loginForm.Dispose();
    Hide();
    MainUtils.LoginFlag = true;

    if (_isAdminLogin) {
        _afterAdminLogin?.Invoke(_mainFormSize);
    } else {
        _afterLogin(_mainFormSize);

        if (MainUtils.IsAutoLoginEnabled()) {
            String loginInfo = $"{SystemUtils.UserInfo.account},{SystemUtils.UserInfo.password}";
            MainUtils.SetAutoLoginInfo(loginInfo);
        }
    }
}
```

- [ ] **Step 4: Reset _isAdminLogin in ShowLoginForm**

In `ShowLoginForm`, reset the flag at the start (after line 53, `_isLoggedIn = false`):

```csharp
_isLoggedIn = false;
_isAdminLogin = false;
```

- [ ] **Step 5: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/LoginView.cs
git commit -m "feat: add admin login button and callback to LoginView

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 5: AdminManagementView — Admin Management UI

**Files:**
- Create: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Create AdminManagementView**

```csharp
using System.Diagnostics;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class AdminManagementView: CustomContentPanel {
        private CardPanel _passwordCard;
        private CardPanel _reimportCard;
        private TextBox _passwordBox;
        private TextBox _operationPasswordBox;
        private Button _savePwdBtn;
        private Button _reimportBtn;
        private Panel _loadingOverlay;
        private Panel _topBar;

        public AdminManagementView() {
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            AutoPadding = false;
            PaddingWithoutBorder = true;

            // Top bar
            _topBar = new Panel {
                Parent = this,
                Height = 44,
            };
            _topBar.Dock = DockStyle.Top;

            var backBtn = new Button {
                Parent = _topBar,
                Text = " ← 返回",
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
            };
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.Click += (s, e) => WidgetUtils.BackToLoginView?.Invoke(false);
            backBtn.Location = new Point(8, 8);

            var title = new Label {
                Parent = _topBar,
                Text = "后台管理",
                AutoSize = true,
            };
            title.Font = new Font(WidgetsConfigs.SystemFontFamily, 16F, FontStyle.Bold, GraphicsUnit.Pixel);
            title.Location = new Point(8, 8);
            _topBar.Resize += (s, e) => {
                title.Location = new Point((_topBar.Width - title.Width) / 2, 8);
            };

            // Card 1: Change admin password
            _passwordCard = new CardPanel {
                Parent = this,
                Width = 480,
                Height = 200,
            };
            BuildPasswordCard();

            // Card 2: Re-import parts barcode
            _reimportCard = new CardPanel {
                Parent = this,
                Width = 480,
                Height = 170,
            };
            BuildReimportCard();

            // Loading overlay (hidden initially)
            _loadingOverlay = new Panel {
                Parent = this,
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

            SizeChanged += (s, e) => LayoutCards();
            LayoutCards();
        }

        private void LayoutCards() {
            int padding = WidgetUtils.ContentInnerBorderMargin();
            int cardWidth = Math.Min(480, Width - padding * 4);
            int topY = _topBar.Bottom + 20;

            _passwordCard.Width = cardWidth;
            _passwordCard.Location = new Point((Width - cardWidth) / 2, topY);

            _reimportCard.Width = cardWidth;
            _reimportCard.Location = new Point((Width - cardWidth) / 2, _passwordCard.Bottom + 24);
        }

        private void BuildPasswordCard() {
            var title = new Label {
                Parent = _passwordCard,
                Text = "修改管理员密码",
                AutoSize = true,
            };
            title.Font = new Font(WidgetsConfigs.SystemFontFamily, 14F, FontStyle.Bold, GraphicsUnit.Pixel);
            title.Location = new Point(24, 20);

            int y = 56;
            int labelW = 80;
            int inputW = 260;
            int inputH = WidgetUtils.TextOrComboBoxHeight();

            new Label {
                Parent = _passwordCard, Text = "登录密码", AutoSize = true,
                Location = new Point(24, y + 4),
            };
            _passwordBox = new TextBox {
                Parent = _passwordCard, Width = inputW, Height = inputH,
                Location = new Point(24 + labelW, y),
                PasswordChar = '*',
            };

            y += inputH + 12;
            new Label {
                Parent = _passwordCard, Text = "操作密码", AutoSize = true,
                Location = new Point(24, y + 4),
            };
            _operationPasswordBox = new TextBox {
                Parent = _passwordCard, Width = inputW, Height = inputH,
                Location = new Point(24 + labelW, y),
                PasswordChar = '*',
            };

            _savePwdBtn = new Button {
                Parent = _passwordCard, Text = "保存修改", AutoSize = true,
                Location = new Point(24 + labelW + inputW - 80, y + inputH + 16),
            };
            _savePwdBtn.Click += OnSavePassword;
        }

        private void BuildReimportCard() {
            var title = new Label {
                Parent = _reimportCard,
                Text = "重新导入物料码",
                AutoSize = true,
            };
            title.Font = new Font(WidgetsConfigs.SystemFontFamily, 14F, FontStyle.Bold, GraphicsUnit.Pixel);
            title.Location = new Point(24, 20);

            var desc = new Label {
                Parent = _reimportCard,
                Text = "将清空 parts_bar_code 表，并从 mission_record 表\n重新拆分导入物料码数据。数据量大时可能耗时较长。",
                AutoSize = true,
                Location = new Point(24, 56),
            };

            _reimportBtn = new Button {
                Parent = _reimportCard,
                Text = "重新导入物料码",
                AutoSize = true,
                Location = new Point(24, 110),
            };
            _reimportBtn.Click += OnReimport;
        }

        private void OnSavePassword(object? sender, EventArgs e) {
            string pwd = _passwordBox.Text.Trim();
            string opPwd = _operationPasswordBox.Text.Trim();

            if (string.IsNullOrEmpty(pwd) && string.IsNullOrEmpty(opPwd)) {
                WidgetUtils.ShowWarningPopUp("请至少输入一项密码");
                return;
            }

            OperationGuidanceApis apis = SystemUtils.GetApis();
            string result = apis.ChangeAdminPassword(new() {
                Password = string.IsNullOrEmpty(pwd) ? null : pwd,
                OperationPassword = string.IsNullOrEmpty(opPwd) ? null : opPwd,
            });

            if (string.IsNullOrEmpty(result)) {
                _passwordBox.Text = "";
                _operationPasswordBox.Text = "";
                WidgetUtils.ShowNoticePopUp("密码修改成功");
            } else {
                WidgetUtils.ShowErrorPopUp(result);
            }
        }

        private async void OnReimport(object? sender, EventArgs e) {
            DialogResult confirm = MessageBox.Show(
                null,
                "此操作将清空并重新导入物料码数据，可能需要较长时间，确定继续？",
                "确认操作",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _reimportBtn.Enabled = false;
            ShowLoadingOverlay(true);

            var stopwatch = Stopwatch.StartNew();
            OperationGuidanceApis apis = SystemUtils.GetApis();

            try {
                ReimportPartsBarcodeRsp rsp = await Task.Run(() => apis.ReimportPartsBarcode(new()));

                stopwatch.Stop();
                if (rsp.ErrorMessage != null) {
                    WidgetUtils.ShowErrorPopUp($"重新导入失败：{rsp.ErrorMessage}");
                } else {
                    WidgetUtils.ShowNoticePopUp(
                        $"重新导入完成！\n删除 {rsp.DeletedRows} 条旧记录\n插入 {rsp.InsertedRows} 条新记录\n耗时 {stopwatch.Elapsed.TotalSeconds:F1} 秒");
                }
            } catch (Exception ex) {
                WidgetUtils.ShowErrorPopUp($"重新导入异常：{ex.Message}");
            } finally {
                ShowLoadingOverlay(false);
                _reimportBtn.Enabled = true;
            }
        }

        private void ShowLoadingOverlay(bool show) {
            _loadingOverlay.Visible = show;
            _loadingOverlay.BringToFront();
            if (show) {
                _loadingOverlay.Size = Size;
                _loadingOverlay.Location = Point.Empty;
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "feat: add AdminManagementView with password and re-import cards

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 6: MainForm — Admin Login Flow Integration

**Files:**
- Modify: `OperationGuidance_new/MainForm.Designer.cs`

- [ ] **Step 1: Add AfterAdminLogin method and wire to LoginView**

After the loginView creation block (after line 128), add:

```csharp
loginView.AfterAdminLogin = AfterAdminLogin;
```

- [ ] **Step 2: Add AfterAdminLogin method**

Insert before `AfterLogin` method (before line 225):

```csharp
private async void AfterAdminLogin(Size mainFormSize) {
    // Check if user is admin
    if (!SystemUtils.IsAdmin) {
        WidgetUtils.ShowErrorPopUp("权限不足，仅管理员可访问后台管理");
        WidgetUtils.BackToLoginView?.Invoke(false);
        return;
    }

    BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;

    // Dispose all controls except LoginView
    foreach (Control c in WidgetUtils.MainForm.Controls) {
        if (c is LoginView) continue;
        c.Dispose();
    }

    // Create admin management view
    AdminManagementView adminView = new() {
        Parent = this,
    };

    // Resize MainForm
    Size screenSize = WidgetUtils.GetScreenResolution();
    if (mainFormSize == screenSize) {
        WindowState = FormWindowState.Maximized;
    } else {
        Size = mainFormSize;
        ClientSize = mainFormSize;
        CenterToScreen();
    }
    MinimumSize = new Size(400, 300);
    MaximumSize = screenSize;

    // Size & position adminView
    adminView.Size = ClientSize;
    adminView.Location = Point.Empty;

    // SizeChanged handler
    SizeChanged += (sender, eventArgs) => {
        if (WindowState == FormWindowState.Minimized) return;
        adminView.Size = ((Form) sender!).ClientSize;
    };
}
```

- [ ] **Step 3: Handle back-to-login for admin view**

The `BackToLoginView` delegate (set in `MainForm.Designer.cs` line 132) already handles returning to the login view. The `AdminManagementView` calls it via `WidgetUtils.BackToLoginView?.Invoke(false)`. However, the existing delegate's `AfterLogin` callback needs to handle both normal and admin re-login.

The existing `WidgetUtils.BackToLoginView` in `MainForm.Designer.cs` (lines 132-218) resets `AfterLogin` to the normal `AfterLogin` method. This is fine — when the user comes back from admin management, they can log in normally or as admin again. The `AfterAdminLogin` is set once at startup (line 123 area) and not overwritten in `BackToLoginView`.

No change needed here — the existing mechanism works correctly.

- [ ] **Step 4: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add OperationGuidance_new/MainForm.Designer.cs
git commit -m "feat: wire admin login flow in MainForm with AdminManagementView

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

### Task 7: Final Verification Build

- [ ] **Step 1: Full build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: Build succeeds with no warnings.

- [ ] **Step 2: Verify all files exist**

```bash
ls -la CustomLibrary/Panels/CardPanel.cs
ls -la OperationGuidance_new/Views/AdminManagementView.cs
ls -la OperationGuidance_service/Models/Requests/ChangeAdminPasswordReq.cs
ls -la OperationGuidance_service/Models/Requests/ReimportPartsBarcodeReq.cs
ls -la OperationGuidance_service/Models/Responses/ReimportPartsBarcodeRsp.cs
```
Expected: All five new files exist.

- [ ] **Step 3: Final commit if any changes from build fixes**

No commit needed if build already succeeded in Step 1.
