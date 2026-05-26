using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using log4net;

namespace OperationGuidance_new.Views.SubViews {
    public class LidCodeReprintPopUpForm: CustomPopUpForm {
        private static readonly ILog log = LogManager.GetLogger(typeof(LidCodeReprintPopUpForm));

        private readonly WorkplaceContentPanel_SCII_XT _workplace;
        private readonly bool _hasQuickReprint;
        private readonly SciiXtPrinterConfig? _lastPrintedConfig;
        private SciiXtPrinterConfig _config;

        private TableLayoutPanel _tablePanel;
        private FunctionButton? _btnQuickReprint;
        private CustomTextBoxGroup _traceCodeBox;
        private FunctionButton _btnConfirm;

        public int ContentWidth {
            get {
                int btnH = WidgetUtils.TextOrComboBoxHeight();
                using (var font = new Font(WidgetsConfigs.SystemFontFamily,
                                           btnH * 0.425f, FontStyle.Bold, GraphicsUnit.Pixel)) {
                    int wInput = (TextRenderer.MeasureText(new string('0', 20), font).Width + btnH) * 2;
                    int wButton = btnH * 2;
                    return wInput + wButton + btnH;
                }
            }
        }

        public LidCodeReprintPopUpForm(WorkplaceContentPanel_SCII_XT workplace,
                                        bool hasQuickReprint,
                                        SciiXtPrinterConfig? lastPrintedConfig) {
            _workplace = workplace; // held for potential future use by external callers
            _hasQuickReprint = hasQuickReprint;
            _lastPrintedConfig = lastPrintedConfig;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "上盖码重打";

            _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            int rowCount = _hasQuickReprint && _lastPrintedConfig != null ? 2 : 1;
            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                RowCount = rowCount,
                Parent = ContentPanel,
            };
            _tablePanel.ColumnStyles.Add(new(SizeType.Percent, 100F));
            _tablePanel.ColumnStyles.Add(new(SizeType.Absolute, WidgetUtils.TextOrComboBoxHeight() * 3));

            if (_hasQuickReprint && _lastPrintedConfig != null) {
                _btnQuickReprint = new() {
                    Label = "快速重打当前产品上盖码",
                    Parent = _tablePanel,
                };
                _tablePanel.SetColumnSpan(_btnQuickReprint, 2);
                _btnQuickReprint.Click += (s, e) => QuickReprint();
            }

            _traceCodeBox = new("产品码") {
                Parent = _tablePanel,
            };
            _traceCodeBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) ReprintByInput();
            };

            _btnConfirm = new() {
                Label = "确定",
                Parent = _tablePanel,
            };
            _btnConfirm.Click += (s, e) => ReprintByInput();

            // Disable if printer not configured
            if (string.IsNullOrEmpty(_config.printer_name)) {
                _traceCodeBox.Enabled = false;
                _btnConfirm.Enabled = false;
            }

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        private async void QuickReprint() {
            if (_lastPrintedConfig == null) return;
            bool ok = await Task.Run(() => {
                try {
                    using ZplQrCodePrinter printer = new();
                    return printer.QuickPrint(_lastPrintedConfig);
                } catch (Exception ex) {
                    log.Error("快速重打上盖码失败", ex);
                    return false;
                }
            });
            if (ok)
                WidgetUtils.ShowNoticePopUp("打印成功", 2);
            else
                WidgetUtils.ShowWarningPopUp("打印失败");
        }

        private async void ReprintByInput() {
            string productCode = _traceCodeBox.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(productCode)) {
                WidgetUtils.ShowWarningPopUp("请输入或扫描产品码");
                return;
            }

            string? traceCode = await Workflow_SCII_XT.GetUpperCode(productCode);
            if (string.IsNullOrEmpty(traceCode)) {
                WidgetUtils.ShowWarningPopUp("获取追溯码失败，请检查产品码是否正确或稍后重试。");
                return;
            }

            var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            bool ok = await Task.Run(() => {
                using ZplQrCodePrinter printer = new();
                return printer.PrintWithTraceCode(config, traceCode);
            });
            if (ok)
                WidgetUtils.ShowNoticePopUp("打印成功", 2);
            else
                WidgetUtils.ShowWarningPopUp("打印失败");
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width,
                                    ContentPanel.Height - ContentPanel.Padding.Size.Height);

            _tablePanel.RowStyles.Clear();
            int col2Width = WidgetUtils.TextOrComboBoxHeight() * 3;
            int col1Width = _tablePanel.Width - col2Width;

            if (_hasQuickReprint && _btnQuickReprint != null) {
                int gap = _tablePanel.Height / 8;
                int rowHeight = (_tablePanel.Height - gap) / 2;

                _tablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight + gap));
                _tablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

                _btnQuickReprint.Size = new(_tablePanel.Width, rowHeight);
                _btnQuickReprint.Margin = new(0);

                int btnMargin = rowHeight / 6;
                _traceCodeBox.Margin = new(0);
                _traceCodeBox.Size = new(col1Width, rowHeight);

                _btnConfirm.Margin = new(btnMargin, 0, 0, 0);
                _btnConfirm.Size = new(col2Width - btnMargin, rowHeight);
            } else {
                _tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                int rowHeight = _tablePanel.Height;
                int btnMargin = rowHeight / 6;

                _traceCodeBox.Margin = new(0);
                _traceCodeBox.Size = new(col1Width, rowHeight);

                _btnConfirm.Margin = new(btnMargin, 0, 0, 0);
                _btnConfirm.Size = new(col2Width - btnMargin, rowHeight);
            }
        }
    }
}
