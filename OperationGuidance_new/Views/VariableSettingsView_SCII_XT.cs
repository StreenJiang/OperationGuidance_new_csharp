using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII_XT : VariableSettingsView_SCII {
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

            // Bind toggle events to control combobox enable state
            _enablePrinter.CheckedChanged += (s, e) => {
                _printerName.Enabled = _enablePrinter.Checked;
            };
            _enableSecondPrinter.CheckedChanged += (s, e) => {
                _secondPrinterName.Enabled = _enableSecondPrinter.Checked;
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
            ConfigUtils.SaveConfig(printerConfig);

            _enablePrinterOriginal = printerConfig.enabled.ToYesOrNoBool();
            _enableSecondPrinterOriginal = printerConfig.enabled_second.ToYesOrNoBool();
            _printerNameOriginal = printerConfig.printer_name;
            _secondPrinterNameOriginal = printerConfig.second_printer_name;

            // Reset unsaved changes check state
            WidgetUtils.CheckSaved = true;
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
            // Resize Content with padding
            _printerSettingsContentPanel.Size = new(Width, BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin);
            _printerSettingsContentPanel.Padding = new(ContentHPadding, ContentVPadding, ContentHPadding, ContentVPadding);
            // Resize outer panel
            _printerSettingsPanel.Size = new(Width, _printerSettingsTitlePanel.Height + _printerSettingsContentPanel.Height);

            // Resize WorkPanel to include printer settings
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height + _printerSettingsPanel.Height + ContentVPadding);
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
                    foreach (var printer in printerList) {
                        _printerName.AddItem(printer, printer);
                        _secondPrinterName.AddItem(printer, printer);
                    }

                    _enablePrinter.Checked = printerConfig.enabled.ToYesOrNoBool();
                    _enableSecondPrinter.Checked = printerConfig.enabled_second.ToYesOrNoBool();

                    // Set combobox enabled state based on toggle
                    _printerName.Enabled = _enablePrinter.Checked;
                    _secondPrinterName.Enabled = _enableSecondPrinter.Checked;

                    // Select saved printer by name
                    int printerIndex = _printerName.Names.IndexOf(printerConfig.printer_name);
                    if (printerIndex >= 0) {
                        _printerName.SetCurrent(printerIndex);
                    }
                    int secondPrinterIndex = _secondPrinterName.Names.IndexOf(printerConfig.second_printer_name);
                    if (secondPrinterIndex >= 0) {
                        _secondPrinterName.SetCurrent(secondPrinterIndex);
                    }

                    // Initialize Original values for unsaved change detection
                    _enablePrinterOriginal = printerConfig.enabled.ToYesOrNoBool();
                    _enableSecondPrinterOriginal = printerConfig.enabled_second.ToYesOrNoBool();
                    _printerNameOriginal = printerConfig.printer_name;
                    _secondPrinterNameOriginal = printerConfig.second_printer_name;
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
            }
            return base.CheckBeforeSave();
        }
    }
}
