using System.Diagnostics;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class AdminManagementView : CustomContentPanel {
        private CardPanel _passwordCard;
        private CardPanel _reimportCard;
        private TextBox _passwordBox;
        private TextBox _operationPasswordBox;
        private Button _savePwdBtn;
        private Button _reimportBtn;
        private Form _loadingOverlay;
        private Panel _contentArea;

        public AdminManagementView() {
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            AutoPadding = false;
            PaddingWithoutBorder = true;

            _contentArea = new Panel();
            _contentArea.Parent = this;

            // Header: back link + page title on one line
            var backLink = new Label {
                Parent = _contentArea,
                Text = "← 返回",
                Cursor = Cursors.Hand,
                AutoSize = true,
                ForeColor = Color.FromArgb(0x88, 0x88, 0x88),
            };
            backLink.Font = new Font(WidgetsConfigs.SystemFontFamily, 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            backLink.MouseEnter += (s, e) => backLink.ForeColor = Color.FromArgb(0xE8, 0x6C, 0x10);
            backLink.MouseLeave += (s, e) => backLink.ForeColor = Color.FromArgb(0x88, 0x88, 0x88);
            backLink.Click += (s, e) => WidgetUtils.BackToLoginView?.Invoke(false);

            var pageTitle = new Label {
                Parent = _contentArea,
                Text = "后台管理",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x33, 0x33, 0x33),
            };
            pageTitle.Font = new Font(WidgetsConfigs.SystemFontFamily, 22F, FontStyle.Bold, GraphicsUnit.Pixel);

            // Card 1
            _passwordCard = new CardPanel {
                Parent = _contentArea,
                Title = "修改管理员密码",
                Width = 800,
                Height = 260,
            };
            BuildPasswordCard();

            // Card 2
            _reimportCard = new CardPanel {
                Parent = _contentArea,
                Title = "重新导入物料码",
                Width = 800,
                Height = 190,
            };
            BuildReimportCard();

            // Loading overlay — borderless semi-transparent Form
            _loadingOverlay = new Form {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.4,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
            };
            var loadingPopup = new Panel {
                Parent = _loadingOverlay,
                BackColor = Color.White,
                Width = 400,
                Height = 120,
            };
            void ApplyPopupRegion() {
                if (loadingPopup.Width > 0 && loadingPopup.Height > 0) {
                    loadingPopup.Region = new Region(
                        WidgetUtils.RoundedRect(
                            new Rectangle(0, 0, loadingPopup.Width - 1, loadingPopup.Height - 1), 8));
                }
            }
            ApplyPopupRegion();
            loadingPopup.Resize += (s, e) => ApplyPopupRegion();

            var loadingLabel = new Label {
                Parent = loadingPopup,
                Text = "正在重新导入物料码，请稍候...",
                ForeColor = Color.FromArgb(0x44, 0x44, 0x44),
                AutoSize = true,
            };
            var marquee = new ProgressBar {
                Parent = loadingPopup,
                Style = ProgressBarStyle.Marquee,
                Width = 300,
                Height = 24,
                MarqueeAnimationSpeed = 30,
            };
            _loadingOverlay.Resize += (s, e) => {
                loadingPopup.Location = new Point(
                    (_loadingOverlay.Width - loadingPopup.Width) / 2,
                    (_loadingOverlay.Height - loadingPopup.Height) / 2);
                loadingLabel.Location = new Point(
                    (loadingPopup.Width - loadingLabel.Width) / 2, 28);
                marquee.Location = new Point(
                    (loadingPopup.Width - marquee.Width) / 2, 60);
            };

            LayoutCards();

            _contentArea.SizeChanged += (s, e) => LayoutCards();
        }

        protected override void OnLayout(LayoutEventArgs e) {
            base.OnLayout(e);
            _contentArea.Size = ClientSize;
            _contentArea.Location = Point.Empty;
        }

        private void LayoutCards() {
            int areaW = _contentArea.Width;
            int areaH = _contentArea.Height;
            int hPad = WidgetUtils.ContentInnerBorderMargin(areaW, areaH);
            int cardWidth = Math.Min(800, areaW - hPad * 2);
            int cardX = hPad;

            // Header: back link left, page title right-aligned with card edge
            Control backLink = _contentArea.Controls[0];
            Control pageTitle = _contentArea.Controls[1];

            pageTitle.Location = new Point(cardX + cardWidth - pageTitle.Width, hPad);
            backLink.Location = new Point(hPad, pageTitle.Top + (pageTitle.Height - backLink.Height) / 2);

            // Cards: left-aligned with header
            int topY = pageTitle.Bottom + 28;

            _passwordCard.Width = cardWidth;
            _passwordCard.Location = new Point(cardX, topY);

            _reimportCard.Width = cardWidth;
            _reimportCard.Location = new Point(cardX, _passwordCard.Bottom + 24);
        }

        private void BuildPasswordCard() {
            var pad = _passwordCard.ContentPadding;
            int labelW = 80;
            int inputW = 260;
            int rowH = WidgetUtils.TextOrComboBoxHeight();
            int rowGap = 18;
            int btnGap = 22;
            int y = pad.Top;

            var label1 = new Label {
                Parent = _passwordCard,
                Text = "登录密码",
                AutoSize = true,
            };
            label1.Location = new Point(pad.Left, y + (rowH - label1.Height) / 2);
            _passwordBox = new TextBox {
                Parent = _passwordCard,
                Width = inputW,
                Height = rowH,
                Location = new Point(pad.Left + labelW, y),
                PasswordChar = '*',
            };

            y += rowH + rowGap;

            var label2 = new Label {
                Parent = _passwordCard,
                Text = "操作密码",
                AutoSize = true,
            };
            label2.Location = new Point(pad.Left, y + (rowH - label2.Height) / 2);
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
            };
            _savePwdBtn.Location = new Point(pad.Left + labelW + inputW - _savePwdBtn.Width, y);
            _savePwdBtn.Click += OnSavePassword;
        }

        private void BuildReimportCard() {
            var pad = _reimportCard.ContentPadding;

            var desc = new Label {
                Parent = _reimportCard,
                Text = "将清空 parts_bar_code 表，并从 mission_record 表\n重新拆分导入物料码数据。数据量大时可能耗时较长。",
                AutoSize = true,
                Font = new Font(WidgetsConfigs.SystemFontFamily, 14F, FontStyle.Regular, GraphicsUnit.Pixel),
                Location = new Point(pad.Left, pad.Top),
            };

            _reimportBtn = new Button {
                Parent = _reimportCard,
                Text = "重新导入物料码",
                AutoSize = true,
            };
            _reimportBtn.Location = new Point(pad.Left, desc.Bottom + 22);
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
            if (show) {
                Form mainForm = (Form) TopLevelControl!;
                _loadingOverlay.Location = mainForm.PointToScreen(Point.Empty);
                _loadingOverlay.Size = mainForm.ClientSize;
                _loadingOverlay.Show();
            } else {
                _loadingOverlay.Hide();
            }
        }
    }
}
