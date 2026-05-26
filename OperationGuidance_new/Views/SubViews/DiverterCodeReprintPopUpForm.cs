using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;
using log4net;

namespace OperationGuidance_new.Views.SubViews {
    public class DiverterCodeReprintPopUpForm: CustomPopUpForm {
        private static readonly ILog log = LogManager.GetLogger(typeof(DiverterCodeReprintPopUpForm));

        private readonly WorkplaceContentPanel_SCII_XT _workplace;
        private SciiXtPrinterConfig _config;
        private TableLayoutPanel _tablePanel;
        private CustomTextBoxGroup _inputBox;
        private FunctionButton _btnConfirm;

        public CustomTextBoxGroup InputBox { get => _inputBox; }

        public DiverterCodeReprintPopUpForm(WorkplaceContentPanel_SCII_XT workplace) {
            _workplace = workplace;
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "分流器码重打";

            _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                RowCount = 1,
                Parent = ContentPanel,
            };
            _tablePanel.ColumnStyles.Add(new(SizeType.Percent, 100F));
            _tablePanel.ColumnStyles.Add(new(SizeType.Absolute, WidgetUtils.TextOrComboBoxHeight() * 3));

            _inputBox = new("二维码内容") {
                Parent = _tablePanel,
            };
            _inputBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ProcessAndPrint();
                }
            };

            _btnConfirm = new() {
                Label = "确定",
                Parent = _tablePanel,
            };
            _btnConfirm.Click += (s, e) => ProcessAndPrint();

            // Disable if second printer not configured
            if (_config.enabled_second != (int) YesOrNo.YES
                || string.IsNullOrEmpty(_config.second_printer_name)) {
                _inputBox.Enabled = false;
                _btnConfirm.Enabled = false;
            }

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        public void FillBarcode(string barcode) {
            if (IsDisposed || _inputBox.IsDisposed) return;
            if (!_inputBox.Enabled) return;
            _inputBox.GetTextBox(0).Box.Text = barcode;
            ProcessAndPrint();
        }

        private void ProcessAndPrint() {
            string barcode = _inputBox.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(barcode)) return;

            _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            if (_config.enabled_second == (int) YesOrNo.YES && _config.second_barcode_length > 0) {
                if (barcode.Length != _config.second_barcode_length) {
                    WidgetUtils.ShowWarningPopUp(
                        $"条码长度不匹配！当前长度为 {barcode.Length}，要求长度为 {_config.second_barcode_length}。");
                    return;
                }
            }

            try {
                _ = _workplace.SendQRCodeToPrinter(barcode);
                WidgetUtils.ShowNoticePopUp("打印指令已发送", 2);
            } catch (Exception ex) {
                log.Error("分流器码重打发送失败", ex);
                WidgetUtils.ShowWarningPopUp("发送指令至打印机失败");
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width,
                                    ContentPanel.Height - ContentPanel.Padding.Size.Height);

            int rowHeight = WidgetUtils.TextOrComboBoxHeight();
            int col2Width = WidgetUtils.TextOrComboBoxHeight() * 3;
            int col1Width = _tablePanel.Width - col2Width;
            int btnMargin = rowHeight / 6;

            _inputBox.Size = new(col1Width, rowHeight);
            _inputBox.Margin = new(0, 0, 0, 0);

            _btnConfirm.Size = new(col2Width - btnMargin, rowHeight);
            _btnConfirm.Margin = new(btnMargin, 0, 0, 0);
        }
    }
}
