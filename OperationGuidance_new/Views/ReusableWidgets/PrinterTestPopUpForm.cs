using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public enum PrinterTestMode { Printer1, Printer2 }

    public class PrinterTestPopUpForm: CustomPopUpForm {
        private readonly PrinterTestMode _mode;
        private SciiXtPrinterConfig _config = null!;
        private List<string> _printerList = new();
        private bool _loaded;

        private CustomTextBoxGroup _inputBox = null!;
        private CustomComboBoxGroup<string> _printerNameBox = null!;

        public PrinterTestPopUpForm(PrinterTestMode mode) {
            _mode = mode;
            Title = mode == PrinterTestMode.Printer1 ? "打印机1测试" : "打印机2测试";
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;

            string inputLabel = mode == PrinterTestMode.Printer1 ? "SN" : "二维码内容";

            _inputBox = new(inputLabel) {
                Parent = ContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = mode == PrinterTestMode.Printer1,
            };

            _printerNameBox = new("打印机名称") {
                Parent = ContentPanel,
                Ratio = 6.95,
            };

            AddButton("打印测试").Click += PrintTest;
            AddButton("关闭").Click += (s, e) => Dispose();
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);

            try {
                _config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
            } catch (Exception ex) {
                LogManager.GetLogger(GetType()).Error("加载打印机配置失败", ex);
                WidgetUtils.ShowErrorPopUp("加载打印机配置失败");
                Dispose();
                return;
            }

            using ZplQrCodePrinter printer = new();
            _printerList = printer.GetAvailablePrinters();

            foreach (string p in _printerList) {
                _printerNameBox.AddItem(p, p);
            }

            if (_mode == PrinterTestMode.Printer1) {
                _inputBox.SetValue(0, _config.sn > 0 ? _config.sn.ToString() : "");
                int idx = _printerNameBox.IndexOf(_config.printer_name);
                if (idx >= 0) _printerNameBox.SetCurrent(idx);
            } else {
                int idx = _printerNameBox.IndexOf(_config.second_printer_name);
                if (idx >= 0) _printerNameBox.SetCurrent(idx);
            }

            _loaded = true;
        }

        public void ResizeSelf() {
            CalculateDetailProperties();

            Padding contentPadding = ContentPanel.Padding;
            int boxHeight = WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            int contentWidth = (int) (WidgetUtils.MainSize.Width * .45);
            int boxWidth = contentWidth - contentPadding.Size.Width - boxMargin * 2;

            _inputBox.Size = new(boxWidth, boxHeight);
            _inputBox.Margin = new(boxMargin, boxMargin, boxMargin, boxMargin / 2);

            _printerNameBox.Size = new(boxWidth, boxHeight);
            _printerNameBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin);

            int contentHeight = boxHeight * 2 + boxMargin * 2 + contentPadding.Size.Height;
            SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
        }

        private async void PrintTest(object? sender, EventArgs e) {
            if (!_loaded) return;

            bool valid = true;

            if (string.IsNullOrEmpty(_printerNameBox.Key) || _printerNameBox.IsDefaultValue()) {
                _printerNameBox.SetError(true);
                valid = false;
            } else {
                _printerNameBox.SetError(false);
            }

            string printerName = _printerNameBox.Key;

            if (_mode == PrinterTestMode.Printer1) {
                string snText = _inputBox.GetTextBox(0).Box.Text;
                int snVal = 0;
                if (string.IsNullOrEmpty(snText) || !int.TryParse(snText, out snVal) || snVal <= 0) {
                    _inputBox.CheckError(0, true);
                    valid = false;
                } else {
                    _inputBox.CheckError(0, false);
                }

                if (valid) {
                    bool ok = await Task.Run(() => {
                        using ZplQrCodePrinter printer = new();
                        return printer.PrintWithSn(_config, snVal, printerName);
                    });
                    if (ok) {
                        WidgetUtils.ShowNoticePopUp("打印成功");
                    } else {
                        WidgetUtils.ShowWarningPopUp("打印失败");
                    }
                }
            } else {
                string content = _inputBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(content)) {
                    _inputBox.CheckError(0, true);
                    valid = false;
                } else {
                    _inputBox.CheckError(0, false);
                }

                if (valid) {
                    bool ok = await Task.Run(() => {
                        using ZplQrCodePrinter printer = new();
                        return printer.PrintQrContent(content, printerName);
                    });
                    if (ok) {
                        WidgetUtils.ShowNoticePopUp("打印成功");
                    } else {
                        WidgetUtils.ShowWarningPopUp("打印失败");
                    }
                }
            }
        }
    }
}
