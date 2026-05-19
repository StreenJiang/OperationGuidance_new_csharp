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
