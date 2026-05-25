using System.Diagnostics;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
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
        private OverlayForm _overlayBackdrop;
        private Form _overlayPopup;
        private Panel _contentArea;

        private static readonly Font _titleFont = new(WidgetsConfigs.SystemFontFamily, 22F, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font _backLinkFont = new(WidgetsConfigs.SystemFontFamily, 16F, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _descFont = new(WidgetsConfigs.SystemFontFamily, 14F, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _progressInfoFont = new(WidgetsConfigs.SystemFontFamily, 13F, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _progressPercentFont = new(WidgetsConfigs.SystemFontFamily, 13F, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font _logFont = new("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Pixel);

        private System.Windows.Forms.Timer _progressTimer;
        private System.Windows.Forms.Timer _statusTimer;
        private ReimportProgressInfo? _latestProgress;
        private readonly object _progressLock = new();
        private Stopwatch? _reimportStopwatch;
        private TextBox _reimportLogBox;
        private Label _elapsedLabel;
        private Label _percentLabel;
        private Label _etaLabel;
        private ProgressBar _reimportProgressBar;
        private Button _closeBtn;
        private string? _lastPhase;
        private double _phaseStartElapsed;
        private int _lastDeletedCount;

        private void CreateOverlayForms() {
            if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) return;

            _overlayBackdrop = new OverlayForm {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.4,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
            };
            _overlayBackdrop.FormClosing += OnOverlayFormClosing;

            _overlayPopup = new Form {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White,
                Opacity = 1.0,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Width = 700,
                Height = 410,
            };
            _overlayPopup.FormClosing += OnPopupFormClosing;
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
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Width = 636,
                Height = 170,
            };

            _reimportProgressBar = new ProgressBar {
                Parent = _overlayPopup,
                Style = ProgressBarStyle.Marquee,
                Width = 636,
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
        }

        private void OnOverlayFormClosing(object? sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        }

        private void OnPopupFormClosing(object? sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        }

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
            backLink.Font = _backLinkFont;
            backLink.MouseEnter += (s, e) => backLink.ForeColor = Color.FromArgb(0xE8, 0x6C, 0x10);
            backLink.MouseLeave += (s, e) => backLink.ForeColor = Color.FromArgb(0x88, 0x88, 0x88);
            backLink.Click += (s, e) => WidgetUtils.BackToLoginView?.Invoke(false);

            var pageTitle = new Label {
                Parent = _contentArea,
                Text = "后台管理",
                AutoSize = true,
                ForeColor = Color.FromArgb(0x33, 0x33, 0x33),
            };
            pageTitle.Font = _titleFont;

            // Card 1
            _passwordCard = new CardPanel {
                Parent = _contentArea,
                Title = "修改管理员密码",
                Width = 800,
                Height = 260,
            };
            BuildPasswordCard();

            // Card 2 — 仅非 SCII XT 版本开放物料重新导入
            if (MainUtils.GetVersion() != AppVersion.SCII_XT) {
                _reimportCard = new CardPanel {
                    Parent = _contentArea,
                    Title = "重新导入物料码",
                    Width = 800,
                    Height = 190,
                };
                BuildReimportCard();
            }

            CreateOverlayForms();

            _progressTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _progressTimer.Tick += OnProgressTimerTick;

            _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusTimer.Tick += OnStatusTimerTick;

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

            if (_reimportCard != null) {
                _reimportCard.Width = cardWidth;
                _reimportCard.Location = new Point(cardX, _passwordCard.Bottom + 24);
            }
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
                Font = _descFont,
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

            CreateOverlayForms();
            _reimportBtn.Enabled = false;
            _reimportLogBox.Text = "";
            _latestProgress = null;
            _lastPhase = null;
            _phaseStartElapsed = 0;
            _reimportProgressBar.Style = ProgressBarStyle.Marquee;
            ShowLoadingOverlay(true);
            _progressTimer.Interval = 1000;
            _progressTimer.Start();
            _statusTimer.Start();
            _reimportStopwatch = Stopwatch.StartNew();

            var req = new ReimportPartsBarcodeReq {
                OnProgress = info => {
                    lock (_progressLock) {
                        if (info.Phase == "deleting") {
                            _lastDeletedCount = info.DeletedCount;
                        }
                        if (_lastPhase == "deleting" && info.Phase == "importing") {
                            _phaseStartElapsed = _reimportStopwatch?.Elapsed.TotalSeconds ?? 0;
                            int deleted = _lastDeletedCount;
                            BeginInvoke(() => {
                                _reimportLogBox.AppendText(
                                    $"[{DateTime.Now:HH:mm:ss}] 删除完成，共删除 {deleted} 条，开始导入物料码...\r\n");
                                _reimportLogBox.ScrollToCaret();
                            });
                        }
                        _lastPhase = info.Phase;
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
                _overlayBackdrop.FormClosing -= OnOverlayFormClosing;
                _overlayPopup.FormClosing -= OnPopupFormClosing;
                _overlayPopup.FormClosing += (s, e) => {
                    if (_overlayBackdrop != null && !_overlayBackdrop.IsDisposed) {
                        _overlayPopup.Owner = null;
                        _overlayBackdrop.Close();
                    }
                };
                _closeBtn.Text = "关闭";
                _closeBtn.Enabled = true;
                _reimportBtn.Enabled = true;
            }
        }

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
                _overlayBackdrop.VisibleChanged += OnBackdropVisibleChanged;
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
        }

        private void OnBackdropVisibleChanged(object? sender, EventArgs e) {
            if (!_overlayBackdrop.Visible) {
                if (_overlayPopup != null && !_overlayPopup.IsDisposed) _overlayPopup.Hide();
            } else if (_overlayPopup != null && !_overlayPopup.IsDisposed) {
                _overlayPopup.Show();
            }
        }

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
                _lastDeletedCount = progress.DeletedCount;
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

            _elapsedLabel.Text = $"已运行 {_reimportStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}";

            if (_progressTimer.Interval != 5000) {
                _progressTimer.Interval = 5000;
            }
        }

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
    }
}
