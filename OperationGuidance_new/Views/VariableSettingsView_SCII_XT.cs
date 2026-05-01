using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII_XT: VariableSettingsView_SCII {
        // Printer settings panel
        private CustomContentPanel _printerSettingsPanel;
        private TitlePanel _printerSettingsTitlePanel;
        private CustomContentPanel _printerSettingsContentPanel;

        private ToggleButtonGroup _enablePrinter;
        private bool _enablePrinterOriginal;
        private ToggleButtonGroup _enableSecondPrinter;
        private bool _enableSecondPrinterOriginal;

        private CustomComboBoxButtonGroup<string> _printerName;
        private string _printerNameOriginal;
        private CustomComboBoxButtonGroup<string> _secondPrinterName;
        private string _secondPrinterNameOriginal;
        private CustomTextBoxGroup _secondBarcodeLength;
        private int _secondBarcodeLengthOriginal;

        // Printer info fields
        private CustomTextBoxGroup _partCodeBox;
        private string _partCodeOriginal;
        private CustomTextBoxGroup _venderCodeBox;
        private string _venderCodeOriginal;
        private CustomTextBoxGroup _softVersionBox;
        private string _softVersionOriginal;
        private CustomTextBoxGroup _hardVersionBox;
        private string _hardVersionOriginal;

        // MES settings panel
        private CustomContentPanel _mesSettingsPanel;
        private TitlePanel _mesSettingsTitlePanel;
        private CustomContentPanel _mesSettingsContentPanel;

        // MES settings fields
        private CustomTextBoxGroup _httpHostBox;
        private string _httpHostOriginal;
        private CustomTextBoxGroup _procedureCodeBox;
        private string _procedureCodeOriginal;
        private CustomTextBoxGroup _equipmentCodeBox;
        private string _equipmentCodeOriginal;
        private CustomTextBoxGroup _batchNoBox;
        private string _batchNoOriginal;
        private CustomTextBoxGroup _recipeCodeBox;
        private string _recipeCodeOriginal;
        private CustomTextBoxGroup _plcIsReadyAddrBox;
        private string _plcIsReadyAddrOriginal;
        private CustomTextBoxGroup _plcRegisterAddrBox;
        private string _plcRegisterAddrOriginal;

        public VariableSettingsView_SCII_XT() {
            MissionSelfLoopingModeToggle.Hide();
            StoreLooseningDataToggle.Hide();
            AutoLockToolToggle.Hide();
            EnableArmLocatingToggle.Hide();
            ArmLocatingAccuracyBox.Hide();
            ErrorPromptForArmToggle.Show();
        }

        protected ToggleButtonGroup EnablePrinterToggle { get => _enablePrinter; set => _enablePrinter = value; }
        protected ToggleButtonGroup EnableSecondPrinterToggle { get => _enableSecondPrinter; set => _enableSecondPrinter = value; }
        protected CustomComboBoxButtonGroup<string> PrinterNameBox { get => _printerName; set => _printerName = value; }
        protected CustomComboBoxButtonGroup<string> SecondPrinterNameBox { get => _secondPrinterName; set => _secondPrinterName = value; }

        public override void VisibleToTrue() {
            base.VisibleToTrue();
            // Set as current panel for unsaved check
            WidgetUtils.CurrentPanel = this;
        }

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();
            InitializePrinterSettingsPanel();
            InitializeMesSettingsPanel();
        }

        protected virtual void InitializeMesSettingsPanel() {
            _mesSettingsPanel = new() {
                Parent = WorkPanel,
                FlowDirection = FlowDirection.TopDown,
            };
            _mesSettingsTitlePanel = new("MES交互配置") {
                Parent = _mesSettingsPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _mesSettingsContentPanel = new() {
                Parent = _mesSettingsPanel,
            };

            _httpHostBox = new("HTTP地址") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _procedureCodeBox = new("工序编码") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _equipmentCodeBox = new("设备编码") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _batchNoBox = new("批次号") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _recipeCodeBox = new("配方编码") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _plcIsReadyAddrBox = new("PLC就绪地址") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };

            _plcRegisterAddrBox = new("PLC寄存器地址") {
                Parent = _mesSettingsContentPanel,
                Ratio = 6.95,
            };
        }

        protected virtual void InitializePrinterSettingsPanel() {
            _printerSettingsPanel = new() {
                Parent = WorkPanel,
                FlowDirection = FlowDirection.TopDown,
            };
            _printerSettingsTitlePanel = new("打印机配置") {
                Parent = _printerSettingsPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _printerSettingsContentPanel = new() {
                Parent = _printerSettingsPanel,
            };

            _enablePrinter = new("启用打印机") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _printerName = new("打印机名称") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _enableSecondPrinter = new("启用第二打印机") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _secondPrinterName = new("第二打印机名称") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _secondBarcodeLength = new("第二条码长度") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };

            _partCodeBox = new("零部件编码") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _venderCodeBox = new("供应商编码") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _softVersionBox = new("软件版本") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            _hardVersionBox = new("硬件版本") {
                Parent = _printerSettingsContentPanel,
                Ratio = 6.95,
            };

            // Bind toggle events to control combobox enable state
            _enablePrinter.CheckedChanged += (s, e) => {
                _printerName.Enabled = _enablePrinter.Checked;
            };
            _enableSecondPrinter.CheckedChanged += (s, e) => {
                _secondPrinterName.Enabled = _enableSecondPrinter.Checked;
                _secondBarcodeLength.Enabled = _enableSecondPrinter.Checked;
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            var printerConfig = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
            printerConfig.enabled = _enablePrinter.Checked.ToYesOrNoInt();
            printerConfig.enabled_second = _enableSecondPrinter.Checked.ToYesOrNoInt();

            // Always save current value, if toggle is off, save empty string
            printerConfig.printer_name = _enablePrinter.Checked ? (_printerName.Value ?? "") : "";
            printerConfig.second_printer_name = _enableSecondPrinter.Checked ? (_secondPrinterName.Value ?? "") : "";
            printerConfig.second_barcode_length = int.TryParse(_secondBarcodeLength?.GetTextBox(0)?.Box?.Text, out int length) ? length : 0;

            // Save printer info
            printerConfig.part_code = _partCodeBox.GetTextBox(0).Box.Text;
            printerConfig.vender_code = _venderCodeBox.GetTextBox(0).Box.Text;
            printerConfig.soft_version = _softVersionBox.GetTextBox(0).Box.Text;
            printerConfig.hard_version = _hardVersionBox.GetTextBox(0).Box.Text;

            ConfigUtils.SaveConfig(printerConfig);

            _enablePrinterOriginal = printerConfig.enabled.ToYesOrNoBool();
            _enableSecondPrinterOriginal = printerConfig.enabled_second.ToYesOrNoBool();
            _printerNameOriginal = printerConfig.printer_name;
            _secondPrinterNameOriginal = printerConfig.second_printer_name;
            _secondBarcodeLengthOriginal = printerConfig.second_barcode_length;

            // Update original values for unsaved change detection
            _partCodeOriginal = printerConfig.part_code;
            _venderCodeOriginal = printerConfig.vender_code;
            _softVersionOriginal = printerConfig.soft_version;
            _hardVersionOriginal = printerConfig.hard_version;

            // Save MES settings
            SaveMesSettings();

            // Reset unsaved changes check state
            WidgetUtils.CheckSaved = true;
        }

        protected virtual void SaveMesSettings() {
            var mesConfig = ConfigUtils.LoadConfig<SciiXtConfig>();
            mesConfig.http_host = _httpHostBox.GetTextBox(0).Box.Text;
            mesConfig.procedure_code = _procedureCodeBox.GetTextBox(0).Box.Text;
            mesConfig.equipment_code = _equipmentCodeBox.GetTextBox(0).Box.Text;
            mesConfig.batch_no = _batchNoBox.GetTextBox(0).Box.Text;
            mesConfig.recipe_code = _recipeCodeBox.GetTextBox(0).Box.Text;
            mesConfig.plc_is_ready_addr = _plcIsReadyAddrBox.GetTextBox(0).Box.Text;
            mesConfig.plc_register_addr = _plcRegisterAddrBox.GetTextBox(0).Box.Text;
            ConfigUtils.SaveConfig(mesConfig);

            // Update original values
            _httpHostOriginal = mesConfig.http_host;
            _procedureCodeOriginal = mesConfig.procedure_code;
            _equipmentCodeOriginal = mesConfig.equipment_code;
            _batchNoOriginal = mesConfig.batch_no;
            _recipeCodeOriginal = mesConfig.recipe_code;
            _plcIsReadyAddrOriginal = mesConfig.plc_is_ready_addr;
            _plcRegisterAddrOriginal = mesConfig.plc_register_addr;
        }

        protected override void ResizeStoragePanel() {
            base.ResizeStoragePanel();

            int boxVMargin = BoxNBtnHeight / 2;
            // Resize Content
            int contentHeight = BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2;
            StorageContentPanel.Size = new(Width, contentHeight);

            // Resize outer panel
            StoragePanel.Size = new(Width, StorageTitlePanel.Height + StorageContentPanel.Height);
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            // Resize printer settings panel - calculate location after work panel content
            _printerSettingsPanel.Location = new(0, WorkContentPanel.Location.Y + WorkContentPanel.Height + ContentVPadding);
            _printerSettingsPanel.Size = new(Width, 0);

            // Resize title panel - needs height to render
            _printerSettingsTitlePanel.Size = new(Width, TitleHeight);

            // Resize box - first row
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            _enablePrinter.Size = new(boxWidth, BoxNBtnHeight);
            _enablePrinter.Margin = new(0, 0, ContentHGap / 2, 0);
            _printerName.Size = new(boxWidth, BoxNBtnHeight);
            _printerName.Margin = new(0, 0, 0, 0);
            // Resize box - second row
            _enableSecondPrinter.Size = new(boxWidth, BoxNBtnHeight);
            _enableSecondPrinter.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _secondPrinterName.Size = new(boxWidth, BoxNBtnHeight);
            _secondPrinterName.Margin = new(0, boxVMargin, 0, 0);
            // Resize box - third row
            _secondBarcodeLength.Size = new(boxWidth, BoxNBtnHeight);
            _secondBarcodeLength.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _partCodeBox.Size = new(boxWidth, BoxNBtnHeight);
            _partCodeBox.Margin = new(0, boxVMargin, 0, 0);
            // Resize box - fourth row
            _venderCodeBox.Size = new(boxWidth, BoxNBtnHeight);
            _venderCodeBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _softVersionBox.Size = new(boxWidth, BoxNBtnHeight);
            _softVersionBox.Margin = new(0, boxVMargin, 0, 0);
            // Resize box - fifth row
            _hardVersionBox.Size = new(boxWidth, BoxNBtnHeight);
            _hardVersionBox.Margin = new(0, boxVMargin, 0, 0);
            // Resize Content with padding (5 rows now)
            _printerSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 5 + ContentVPadding * 2 + boxVMargin * 4);
            _printerSettingsContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);
            // Resize outer panel
            _printerSettingsPanel.Size = new(Width, _printerSettingsTitlePanel.Height + _printerSettingsContentPanel.Height);

            // Resize WorkPanel to include printer settings (MES panel will be added in ResizeMesSettings)
            // Note: WorkPanel final size will be set in ResizeMesSettings

            // Call ResizeMesSettings to position and size MES panel and finalize WorkPanel size
            ResizeMesSettings();
        }

        protected virtual void ResizeMesSettings() {
            // Position after printer settings panel
            _mesSettingsPanel.Location = new(0, _printerSettingsPanel.Location.Y + _printerSettingsPanel.Height + ContentVPadding);
            _mesSettingsPanel.Size = new(Width, 0);

            // Title panel
            _mesSettingsTitlePanel.Size = new(Width, TitleHeight);

            // Box layout
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;

            // Row 1 - HTTP address + procedure code (two controls side by side)
            _httpHostBox.Size = new(boxWidth, BoxNBtnHeight);
            _httpHostBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _procedureCodeBox.Size = new(boxWidth, BoxNBtnHeight);
            _procedureCodeBox.Margin = new(0, boxVMargin, 0, 0);

            // Row 2 - equipment code + batch no
            _equipmentCodeBox.Size = new(boxWidth, BoxNBtnHeight);
            _equipmentCodeBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _batchNoBox.Size = new(boxWidth, BoxNBtnHeight);
            _batchNoBox.Margin = new(0, boxVMargin, 0, 0);

            // Row 3 - recipe code + PLC addresses
            _recipeCodeBox.Size = new(boxWidth, BoxNBtnHeight);
            _recipeCodeBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _plcIsReadyAddrBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcIsReadyAddrBox.Margin = new(0, boxVMargin, 0, 0);

            // Row 4 - PLC register address + (empty to complete the row)
            _plcRegisterAddrBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcRegisterAddrBox.Margin = new(0, boxVMargin, 0, 0);

            // Content panel height (4 rows)
            _mesSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 4 + ContentVPadding * 2 + boxVMargin * 3);
            _mesSettingsContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);

            // Outer panel
            _mesSettingsPanel.Size = new(Width, _mesSettingsTitlePanel.Height + _mesSettingsContentPanel.Height);

            // Update WorkPanel to include MES settings
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height + _printerSettingsPanel.Height + _mesSettingsPanel.Height + ContentVPadding * 2);
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                base.LoadSettings();

                // Load printer list
                using ZplQrCodePrinter printer = new();
                var printerList = printer.GetAvailablePrinters();
                var printerConfig = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

                BeginInvoke(() => {
                    // Add printer items to combobox
                    _printerName.Reset();
                    _secondPrinterName.Reset();
                    _printerName.ClearItem();
                    _secondPrinterName.ClearItem();
                    foreach (var printer in printerList) {
                        _printerName.AddItem(printer, printer);
                        _secondPrinterName.AddItem(printer, printer);
                    }

                    _enablePrinter.Checked = printerConfig.enabled.ToYesOrNoBool();
                    _enableSecondPrinter.Checked = printerConfig.enabled_second.ToYesOrNoBool();

                    // Set combobox enabled state based on toggle
                    _printerName.Enabled = _enablePrinter.Checked;
                    _secondPrinterName.Enabled = _enableSecondPrinter.Checked;
                    _secondBarcodeLength.Enabled = _enableSecondPrinter.Checked;

                    // Select saved printer by name
                    int printerIndex = _printerName.Names.IndexOf(printerConfig.printer_name);
                    if (printerIndex >= 0) {
                        _printerName.SetCurrent(printerIndex);
                    }
                    int secondPrinterIndex = _secondPrinterName.Names.IndexOf(printerConfig.second_printer_name);
                    if (secondPrinterIndex >= 0) {
                        _secondPrinterName.SetCurrent(secondPrinterIndex);
                    }

                    // Load second barcode length
                    _secondBarcodeLength.GetTextBox(0).Box.Text = printerConfig.second_barcode_length > 0
                        ? printerConfig.second_barcode_length.ToString()
                        : "";

                    // Load printer info
                    _partCodeBox.GetTextBox(0).Box.Text = printerConfig.part_code;
                    _venderCodeBox.GetTextBox(0).Box.Text = printerConfig.vender_code;
                    _softVersionBox.GetTextBox(0).Box.Text = printerConfig.soft_version;
                    _hardVersionBox.GetTextBox(0).Box.Text = printerConfig.hard_version;

                    // Initialize Original values for unsaved change detection
                    _enablePrinterOriginal = printerConfig.enabled.ToYesOrNoBool();
                    _enableSecondPrinterOriginal = printerConfig.enabled_second.ToYesOrNoBool();
                    _printerNameOriginal = printerConfig.printer_name;
                    _secondPrinterNameOriginal = printerConfig.second_printer_name;
                    _secondBarcodeLengthOriginal = printerConfig.second_barcode_length;
                    _partCodeOriginal = printerConfig.part_code;
                    _venderCodeOriginal = printerConfig.vender_code;
                    _softVersionOriginal = printerConfig.soft_version;
                    _hardVersionOriginal = printerConfig.hard_version;

                    // Load MES config
                    var mesConfig = ConfigUtils.LoadConfig<SciiXtConfig>();
                    _httpHostBox.GetTextBox(0).Box.Text = mesConfig.http_host;
                    _procedureCodeBox.GetTextBox(0).Box.Text = mesConfig.procedure_code;
                    _equipmentCodeBox.GetTextBox(0).Box.Text = mesConfig.equipment_code;
                    _batchNoBox.GetTextBox(0).Box.Text = mesConfig.batch_no;
                    _recipeCodeBox.GetTextBox(0).Box.Text = mesConfig.recipe_code;
                    _plcIsReadyAddrBox.GetTextBox(0).Box.Text = mesConfig.plc_is_ready_addr;
                    _plcRegisterAddrBox.GetTextBox(0).Box.Text = mesConfig.plc_register_addr;

                    // Initialize MES original values
                    _httpHostOriginal = mesConfig.http_host;
                    _procedureCodeOriginal = mesConfig.procedure_code;
                    _equipmentCodeOriginal = mesConfig.equipment_code;
                    _batchNoOriginal = mesConfig.batch_no;
                    _recipeCodeOriginal = mesConfig.recipe_code;
                    _plcIsReadyAddrOriginal = mesConfig.plc_is_ready_addr;
                    _plcRegisterAddrOriginal = mesConfig.plc_register_addr;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                base.ResetAllToDefault();

                BeginInvoke(() => {
                    var defaultConfig = ConfigUtils.GetDefault<SciiXtPrinterConfig>();
                    _enablePrinter.Checked = defaultConfig.enabled.ToYesOrNoBool();
                    _enableSecondPrinter.Checked = defaultConfig.enabled_second.ToYesOrNoBool();

                    // Reset combobox enabled state based on toggle
                    _printerName.Enabled = _enablePrinter.Checked;
                    _secondPrinterName.Enabled = _enableSecondPrinter.Checked;
                    _secondBarcodeLength.Enabled = _enableSecondPrinter.Checked;

                    // Reset combobox selection (clear first, then select if valid value exists)
                    _printerName.Reset();
                    _secondPrinterName.Reset();

                    // Select default printer by name if exists in list
                    int printerIndex = _printerName.Names.IndexOf(defaultConfig.printer_name);
                    if (printerIndex >= 0) {
                        _printerName.SetCurrent(printerIndex);
                    }
                    int secondPrinterIndex = _secondPrinterName.Names.IndexOf(defaultConfig.second_printer_name);
                    if (secondPrinterIndex >= 0) {
                        _secondPrinterName.SetCurrent(secondPrinterIndex);
                    }

                    // Reset second barcode length to 0
                    _secondBarcodeLength.GetTextBox(0).Box.Text = "0";

                    // Reset printer info to default
                    _partCodeBox.GetTextBox(0).Box.Text = defaultConfig.part_code;
                    _venderCodeBox.GetTextBox(0).Box.Text = defaultConfig.vender_code;
                    _softVersionBox.GetTextBox(0).Box.Text = defaultConfig.soft_version;
                    _hardVersionBox.GetTextBox(0).Box.Text = defaultConfig.hard_version;

                    // Reset MES config to default
                    var defaultMesConfig = ConfigUtils.GetDefault<SciiXtConfig>();
                    _httpHostBox.GetTextBox(0).Box.Text = defaultMesConfig.http_host;
                    _procedureCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.procedure_code;
                    _equipmentCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.equipment_code;
                    _batchNoBox.GetTextBox(0).Box.Text = defaultMesConfig.batch_no;
                    _recipeCodeBox.GetTextBox(0).Box.Text = defaultMesConfig.recipe_code;
                    _plcIsReadyAddrBox.GetTextBox(0).Box.Text = defaultMesConfig.plc_is_ready_addr;
                    _plcRegisterAddrBox.GetTextBox(0).Box.Text = defaultMesConfig.plc_register_addr;
                });
            });
        }

        protected override string? CheckBeforeSave() {
            // Check printer settings if enabled
            if (_printerSettingsPanel != null) {
                if (_enablePrinter?.Checked == true && string.IsNullOrEmpty(_printerName?.Value)) {
                    return "请选择打印机名称";
                }
                if (_enableSecondPrinter?.Checked == true && string.IsNullOrEmpty(_secondPrinterName?.Value)) {
                    return "请选择第二打印机名称";
                }
                if (_enableSecondPrinter?.Checked == true) {
                    string? lengthText = _secondBarcodeLength?.GetTextBox(0)?.Box?.Text;
                    if (string.IsNullOrEmpty(lengthText) || !int.TryParse(lengthText, out int length) || length <= 0) {
                        return "请输入有效的第二条码长度（大于0的整数）";
                    }
                }
            }
            return base.CheckBeforeSave();
        }

        protected override bool CheckSavedFunc_detail() {
            if (base.CheckSavedFunc_detail()) return true;
            return CheckSvedFuncSeparately(_partCodeBox.GetTextBox(0).Box.Text != _partCodeOriginal, "零部件编码")
                || CheckSvedFuncSeparately(_venderCodeBox.GetTextBox(0).Box.Text != _venderCodeOriginal, "供应商编码")
                || CheckSvedFuncSeparately(_softVersionBox.GetTextBox(0).Box.Text != _softVersionOriginal, "软件版本")
                || CheckSvedFuncSeparately(_hardVersionBox.GetTextBox(0).Box.Text != _hardVersionOriginal, "硬件版本")
                || CheckSvedFuncSeparately(_httpHostBox.GetTextBox(0).Box.Text != _httpHostOriginal, "HTTP地址")
                || CheckSvedFuncSeparately(_procedureCodeBox.GetTextBox(0).Box.Text != _procedureCodeOriginal, "工序编码")
                || CheckSvedFuncSeparately(_equipmentCodeBox.GetTextBox(0).Box.Text != _equipmentCodeOriginal, "设备编码")
                || CheckSvedFuncSeparately(_batchNoBox.GetTextBox(0).Box.Text != _batchNoOriginal, "批次号")
                || CheckSvedFuncSeparately(_recipeCodeBox.GetTextBox(0).Box.Text != _recipeCodeOriginal, "配方编码")
                || CheckSvedFuncSeparately(_plcIsReadyAddrBox.GetTextBox(0).Box.Text != _plcIsReadyAddrOriginal, "PLC就绪地址")
                || CheckSvedFuncSeparately(_plcRegisterAddrBox.GetTextBox(0).Box.Text != _plcRegisterAddrOriginal, "PLC寄存器地址");
        }
    }
}
