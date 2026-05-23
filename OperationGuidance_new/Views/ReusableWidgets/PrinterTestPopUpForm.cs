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
        private SecondPrinterDetailConfig _detailConfig = null!;
        private List<string> _printerList = new();
        private bool _loaded;

        private CustomTextBoxGroup _inputBox = null!;
        private CustomComboBoxGroup<string> _printerNameBox = null!;

        // Printer2 detail fields
        private CustomComboBoxGroup<string> _dpiBox = null!;
        private CustomTextBoxGroup _labelSizeBox = null!;
        private CustomTextBoxGroup _qrSizeBox = null!;
        private CustomTextBoxGroup _marginXFactorBox = null!;
        private CustomTextBoxGroup _marginYFactorBox = null!;

        public PrinterTestPopUpForm(PrinterTestMode mode) {
            _mode = mode;
            Title = mode == PrinterTestMode.Printer1 ? "大二维码打印测试" : "小二维码打印测试";
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

            if (_mode == PrinterTestMode.Printer2) {
                _dpiBox = new("DPI") {
                    Parent = ContentPanel,
                    Ratio = 6.95,
                };
                _labelSizeBox = new("标签尺寸(mm)") {
                    Parent = ContentPanel,
                    Ratio = 6.95,
                };
                _qrSizeBox = new("QR尺寸(mm)") {
                    Parent = ContentPanel,
                    Ratio = 6.95,
                };
                _marginXFactorBox = new("X边距系数(0-2)") {
                    Parent = ContentPanel,
                    Ratio = 6.95,
                };
                _marginYFactorBox = new("Y边距系数(0-2)") {
                    Parent = ContentPanel,
                    Ratio = 6.95,
                };
            }

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

                try {
                    _detailConfig = ConfigUtils.LoadConfig<SecondPrinterDetailConfig>();
                } catch (Exception ex) {
                    LogManager.GetLogger(GetType()).Error("加载第二打印机详细配置失败", ex);
                    _detailConfig = new SecondPrinterDetailConfig();
                }

                _dpiBox.AddItem("203 dpi", ZplQrCodePrinter.DPMM_203DPI.ToString());
                _dpiBox.AddItem("300 dpi", ZplQrCodePrinter.DPMM_300DPI.ToString());
                _dpiBox.AddItem("600 dpi", ZplQrCodePrinter.DPMM_600DPI.ToString());
                int dpiIdx = _dpiBox.IndexOf(_detailConfig.dpmm.ToString());
                if (dpiIdx >= 0) _dpiBox.SetCurrent(dpiIdx);

                _labelSizeBox.SetValue(0, _detailConfig.label_size_mm.ToString());
                _qrSizeBox.SetValue(0, _detailConfig.qr_size_mm.ToString());
                _marginXFactorBox.SetValue(0, _detailConfig.margin_x_factor.ToString());
                _marginYFactorBox.SetValue(0, _detailConfig.margin_y_factor.ToString());
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

            int rowCount = 2;
            if (_mode == PrinterTestMode.Printer2) {
                _printerNameBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

                _dpiBox.Size = new(boxWidth, boxHeight);
                _dpiBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

                _labelSizeBox.Size = new(boxWidth, boxHeight);
                _labelSizeBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

                _qrSizeBox.Size = new(boxWidth, boxHeight);
                _qrSizeBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

                _marginXFactorBox.Size = new(boxWidth, boxHeight);
                _marginXFactorBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin / 2);

                _marginYFactorBox.Size = new(boxWidth, boxHeight);
                _marginYFactorBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin);

                rowCount = 7;
            } else {
                _printerNameBox.Margin = new(boxMargin, boxMargin / 2, boxMargin, boxMargin);
            }

            int contentHeight = boxHeight * rowCount + boxMargin * rowCount + contentPadding.Size.Height;
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
                        WidgetUtils.ShowNoticePopUp("打印成功", 2);
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

                if (!TryValidateDouble(_labelSizeBox, v => v > 0, null, out double labelSizeMm)) valid = false;
                if (!TryValidateDouble(_qrSizeBox, v => v > 0, null, out double qrSizeMm)) valid = false;
                if (!TryValidateDouble(_marginXFactorBox, v => v >= SecondPrinterDetailConfig.MarginFactorMin && v <= SecondPrinterDetailConfig.MarginFactorMax, "X边距系数超出范围，请输入0~2之间的数值", out double marginXFactor)) valid = false;
                if (!TryValidateDouble(_marginYFactorBox, v => v >= SecondPrinterDetailConfig.MarginFactorMin && v <= SecondPrinterDetailConfig.MarginFactorMax, "Y边距系数超出范围，请输入0~2之间的数值", out double marginYFactor)) valid = false;

                if (valid) {
                    double dpmm = double.TryParse(_dpiBox.Value, out double d) ? d : ZplQrCodePrinter.DPMM_300DPI;

                    _detailConfig.dpmm = dpmm;
                    _detailConfig.label_size_mm = labelSizeMm;
                    _detailConfig.qr_size_mm = qrSizeMm;
                    _detailConfig.margin_x_factor = marginXFactor;
                    _detailConfig.margin_y_factor = marginYFactor;
                    ConfigUtils.SaveConfig(_detailConfig);

                    bool ok = await Task.Run(() => {
                        using ZplQrCodePrinter printer = new();
                        return printer.PrintQrContent(content, printerName, dpmm, labelSizeMm, qrSizeMm, marginXFactor, marginYFactor);
                    });
                    if (ok) {
                        WidgetUtils.ShowNoticePopUp("打印成功", 2);
                    } else {
                        WidgetUtils.ShowWarningPopUp("打印失败");
                    }
                }
            }
        }
        private bool TryValidateDouble(CustomTextBoxGroup box, Func<double, bool> predicate, string? warningMessage, out double value) {
            value = double.TryParse(box.GetTextBox(0).Box.Text, out double parsed) && predicate(parsed) ? parsed : -1;
            if (value < 0) {
                if (warningMessage != null)
                    WidgetUtils.ShowWarningPopUp(warningMessage);
                box.CheckError(0, true);
                return false;
            }
            box.CheckError(0, false);
            return true;
        }
    }
}
