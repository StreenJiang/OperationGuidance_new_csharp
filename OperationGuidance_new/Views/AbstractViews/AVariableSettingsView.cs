using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using Microsoft.Win32;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_service.Constants;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views.AbstractViews {
    public abstract class AVariableSettingsView: CustomContentPanel {
        protected ILog logger;

        #region Fields
        private readonly float _contentHGapRatio = 0.025F;
        private readonly float _contentVGapRatio = 0.05F;
        private readonly float _contentHPaddingRatio = 0.15F;
        private readonly float _contentVPaddingRatio = 0.03F;
        private int _titleHeight;
        private int _boxNBtnHeight;
        private int _contentHGap;
        private int _contentVGap;
        private int _contentHPadding;
        private int _contentVPadding;
        private Panel _buttonsOuterPanel;
        private CustomContentPanel _buttonsPanel;
        private CommonButton _saveBtn;
        private CommonButton _defaultValueBtn;
        // System settings content panel
        private CustomContentPanel _systemSettingsPanel;
        private TitlePanel _systemSettingsTitlePanel;
        private CustomContentPanel _systemSettingsContentPanel;
        private CustomComboBoxButtonGroup<KeyValuePair<Size, SizeRatioNRectColor>> _resolutionOptionsBox;
        private Size _resolutionOriginal;
        private ToggleButtonGroup _autoLaunchToggle;
        private bool _autoLaunchOriginal;
        private ToggleButtonGroup _autoLoginToggle;
        private bool _autoLoginOriginal;
        private CustomTextBoxButtonGroup _logsRetentionDaysBox;
        private int _logsRetentionDaysOriginal;
        // Storage panel
        private CustomContentPanel _storagePanel;
        private TitlePanel _storageTitlePanel;
        private CustomContentPanel _storageContentPanel;
        private CustomTextBoxButtonGroup _storagePathTextBox;
        private string _sotragePathOriginal;
        private CommonButtonGroup _storageFieldsButton;
        private List<int> _sortConfigOriginal;
        private ToggleButtonGroup _storeLooseningDataToggle;
        private bool _sotrageLooseningDataOriginal;
        private ToggleButtonGroup _enableExcelExportToggle;
        private bool _enableExcelExportOriginal;
        private ToggleButtonGroup _enableTxtExportToggle;
        private bool _enableTxtExportOriginal;
        private CommonButtonGroup _exportTestButton;
        // 操作配置
        private CustomContentPanel _workPanel;
        private TitlePanel _workTitlePanel;
        private CustomContentPanel _workContentPanel;
        private ToggleButtonGroup _enableArmLocatingToggle;
        private bool _enableArmLocatingOriginal;
        private CustomTextBoxButtonGroup _armLocatingAccuracyBox;
        private int _armLocatingAccuracyOriginal;
        private ToggleButtonGroup _missionSelfLoopingModeToggle;
        private bool _missionSelfLoopingModeOriginal;
        private ToggleButtonGroup _autoLockToolToggle;
        private bool _autoLockToolOriginal;
        private ToggleButtonGroup _usbScannerEnabledToggle;
        private bool _usbScannerEnabledOriginal;
        private ToggleButtonGroup _errorPromptForArmToggle;
        private bool _errorPromptForArmOriginal;
        #endregion

        private List<OperationDataField> Fields { get; set; } = new();
        private List<int> SortConfig { get; set; } = new();
        public int TitleHeight { get => _titleHeight; set => _titleHeight = value; }
        public int BoxNBtnHeight { get => _boxNBtnHeight; set => _boxNBtnHeight = value; }
        public int ContentHGap { get => _contentHGap; set => _contentHGap = value; }
        public int ContentVGap { get => _contentVGap; set => _contentVGap = value; }
        public int ContentHPadding { get => _contentHPadding; set => _contentHPadding = value; }
        public int ContentVPadding { get => _contentVPadding; set => _contentVPadding = value; }
        public Panel ButtonsOuterPanel { get => _buttonsOuterPanel; set => _buttonsOuterPanel = value; }
        public CustomContentPanel ButtonsPanel { get => _buttonsPanel; set => _buttonsPanel = value; }
        public CommonButton SaveBtn { get => _saveBtn; set => _saveBtn = value; }
        public CommonButton DefaultValueBtn { get => _defaultValueBtn; set => _defaultValueBtn = value; }
        public CustomContentPanel SystemSettingsPanel { get => _systemSettingsPanel; set => _systemSettingsPanel = value; }
        public TitlePanel SystemSettingsTitlePanel { get => _systemSettingsTitlePanel; set => _systemSettingsTitlePanel = value; }
        public CustomContentPanel SystemSettingsContentPanel { get => _systemSettingsContentPanel; set => _systemSettingsContentPanel = value; }
        public CustomComboBoxButtonGroup<KeyValuePair<Size, SizeRatioNRectColor>> ResolutionOptionsBox { get => _resolutionOptionsBox; set => _resolutionOptionsBox = value; }
        public Size ResolutionOriginal { get => _resolutionOriginal; set => _resolutionOriginal = value; }
        public ToggleButtonGroup AutoLaunchToggle { get => _autoLaunchToggle; set => _autoLaunchToggle = value; }
        public bool AutoLaunchOriginal { get => _autoLaunchOriginal; set => _autoLaunchOriginal = value; }
        public ToggleButtonGroup AutoLoginToggle { get => _autoLoginToggle; set => _autoLoginToggle = value; }
        public bool AutoLoginOriginal { get => _autoLoginOriginal; set => _autoLoginOriginal = value; }
        public CustomContentPanel StoragePanel { get => _storagePanel; set => _storagePanel = value; }
        public TitlePanel StorageTitlePanel { get => _storageTitlePanel; set => _storageTitlePanel = value; }
        public CustomContentPanel StorageContentPanel { get => _storageContentPanel; set => _storageContentPanel = value; }
        public CustomTextBoxButtonGroup StoragePathTextBox { get => _storagePathTextBox; set => _storagePathTextBox = value; }
        public string SotragePathOriginal { get => _sotragePathOriginal; set => _sotragePathOriginal = value; }
        public CommonButtonGroup StorageFieldsButton { get => _storageFieldsButton; set => _storageFieldsButton = value; }
        public List<int> SortConfigOriginal { get => _sortConfigOriginal; set => _sortConfigOriginal = value; }
        public ToggleButtonGroup StoreLooseningDataToggle { get => _storeLooseningDataToggle; set => _storeLooseningDataToggle = value; }
        public bool SotrageLooseningDataOriginal { get => _sotrageLooseningDataOriginal; set => _sotrageLooseningDataOriginal = value; }
        public ToggleButtonGroup EnableExcelExportToggle { get => _enableExcelExportToggle; set => _enableExcelExportToggle = value; }
        public bool EnableExcelExportOriginal { get => _enableExcelExportOriginal; set => _enableExcelExportOriginal = value; }
        public ToggleButtonGroup EnableTxtExportToggle { get => _enableTxtExportToggle; set => _enableTxtExportToggle = value; }
        public bool EnableTxtExportOriginal { get => _enableTxtExportOriginal; set => _enableTxtExportOriginal = value; }
        public CommonButtonGroup ExportTestButton { get => _exportTestButton; set => _exportTestButton = value; }
        public CustomContentPanel WorkPanel { get => _workPanel; set => _workPanel = value; }
        public TitlePanel WorkTitlePanel { get => _workTitlePanel; set => _workTitlePanel = value; }
        public CustomContentPanel WorkContentPanel { get => _workContentPanel; set => _workContentPanel = value; }
        public ToggleButtonGroup EnableArmLocatingToggle { get => _enableArmLocatingToggle; set => _enableArmLocatingToggle = value; }
        public bool EnableArmLocatingOriginal { get => _enableArmLocatingOriginal; set => _enableArmLocatingOriginal = value; }
        public CustomTextBoxButtonGroup ArmLocatingAccuracyBox { get => _armLocatingAccuracyBox; set => _armLocatingAccuracyBox = value; }
        public int ArmLocatingAccuracyOriginal { get => _armLocatingAccuracyOriginal; set => _armLocatingAccuracyOriginal = value; }
        public ToggleButtonGroup MissionSelfLoopingModeToggle { get => _missionSelfLoopingModeToggle; set => _missionSelfLoopingModeToggle = value; }
        public bool MissionSelfLoopingModeOriginal { get => _missionSelfLoopingModeOriginal; set => _missionSelfLoopingModeOriginal = value; }
        public ToggleButtonGroup UsbScannerEnabledToggle { get => _usbScannerEnabledToggle; set => _usbScannerEnabledToggle = value; }
        public bool UsbScannerEnabledOriginal { get => _usbScannerEnabledOriginal; set => _usbScannerEnabledOriginal = value; }
        public ToggleButtonGroup AutoLockToolToggle { get => _autoLockToolToggle; set => _autoLockToolToggle = value; }
        public ToggleButtonGroup ErrorPromptForArmToggle { get => _errorPromptForArmToggle; set => _errorPromptForArmToggle = value; }
        public bool ErrorPromptForArmOriginal { get => _errorPromptForArmOriginal; set => _errorPromptForArmOriginal = value; }
        #region Constructors

        public AVariableSettingsView() {
            logger = MainUtils.GetLogger(GetType());

            // Default values
            FlowDirection = FlowDirection.TopDown;

            // Initilizations
            InitializeSystemSettingsPanel();
            InitializeStoragePanel();
            InitializeMissionSettings();

            _buttonsOuterPanel = new() {
                Parent = this,
            };
            _buttonsPanel = new() {
                Parent = _buttonsOuterPanel,
            };
            _saveBtn = new() {
                Parent = _buttonsPanel,
                Label = "保存",
            };
            _defaultValueBtn = new() {
                Parent = _buttonsPanel,
                Label = "默认",
            };
            _saveBtn.MouseUp += SaveBtnMouseUp;
            _defaultValueBtn.MouseUp += DefaultValueBtnMouseUp;

            void DefaultValueBtnMouseUp(object? sender, MouseEventArgs e) {
                if (WidgetUtils.ShowConfirmPopUp("是否将所有配置重置为默认值？")) {
                    ResetAllToDefault();
                    WidgetUtils.ShowNoticePopUp("已将所有配置重置为默认值");
                }
            }
        }
        protected virtual void SaveBtnMouseUp(object? sender, MouseEventArgs e) {
            // Check can save storage settings first
            string? error = CheckBeforeSave();
            if (!string.IsNullOrEmpty(error)) {
                WidgetUtils.ShowErrorPopUp(error);
            } else {
                SaveStorageSettings();
                SaveSystemSettings();
                SaveMissionSettings();
                WidgetUtils.ShowNoticePopUp("保存成功");
            }
        }
        #endregion

        #region Override methods
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            // Load settings
            LoadSettings();

            WidgetUtils.CheckSavedFunc += CheckSavedFunc;
        }
        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
            WidgetUtils.CheckSavedFunc -= CheckSavedFunc;
        }
        private bool CheckSavedFunc() => WidgetUtils.CurrentPanel != this || CheckSavedFunc_detail();
        protected virtual bool CheckSavedFunc_detail() => !(
            CheckSvedFuncSeparately(_enableExcelExportToggle.Checked != _enableExcelExportOriginal, "导出Excel开关")
            || CheckSvedFuncSeparately(_enableTxtExportToggle.Checked != _enableTxtExportOriginal, "导出Txt开关")
            || CheckSvedFuncSeparately(_resolutionOptionsBox.Value.Key != _resolutionOriginal, "分辨率")
            || CheckSvedFuncSeparately(_storagePathTextBox.GetTextBox(0).Box.Text != _sotragePathOriginal, "文件保存路径")
            || CheckSvedFuncSeparately(!SortConfig.SequenceEqual(_sortConfigOriginal), "字段排序")
            || CheckSvedFuncSeparately(_storeLooseningDataToggle.Checked != _sotrageLooseningDataOriginal, "存储字段")
            || CheckSvedFuncSeparately(_enableArmLocatingToggle.Checked != _enableArmLocatingOriginal, "是否开启力臂定位")
            || CheckSvedFuncSeparately(_armLocatingAccuracyBox.GetTextBox(0).Box.Text != _armLocatingAccuracyOriginal + "", "力臂定位精度")
            || CheckSvedFuncSeparately(_missionSelfLoopingModeToggle.Checked != _missionSelfLoopingModeOriginal, "任务自循环")
            || CheckSvedFuncSeparately(_autoLockToolToggle.Checked != _autoLockToolOriginal, "自动锁枪")
            || CheckSvedFuncSeparately(_autoLaunchToggle.Checked != _autoLaunchOriginal, "开机自动启动")
            || CheckSvedFuncSeparately(_autoLoginToggle.Checked != _autoLoginOriginal, "自动登录")
            || CheckSvedFuncSeparately(_logsRetentionDaysBox.GetTextBox(0).Box.Text != _logsRetentionDaysOriginal + "", "日志保留天数")
            || CheckSvedFuncSeparately(_usbScannerEnabledToggle.Checked != _usbScannerEnabledOriginal, "USB扫码枪")
        );
        protected bool CheckSvedFuncSeparately(bool check, string msg) {
            if (check) {
                logger.Info($"[{msg}]修改未保存...");
            }
            return check;
        }
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Control mainForm = WidgetUtils.MainForm;
            _titleHeight = WidgetUtils.ContentTitleHeight();
            _boxNBtnHeight = WidgetUtils.TextOrComboBoxHeight();
            _contentHGap = (int) (mainForm.Height * _contentHGapRatio);
            _contentVGap = (int) (mainForm.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainForm.Width * .015);
            _contentVPadding = (int) (mainForm.Height * .03);

            // Resizes
            ResizeSystemSettingsPanel();
            ResizeStoragePanel();
            ResizeMissionSettings();

            _buttonsOuterPanel.Size = new(Width, _boxNBtnHeight);
            int btnsWidth = 0;
            int count = 0;
            foreach (Control c in _buttonsPanel.Controls) {
                CommonButton btn = (CommonButton) c;
                btn.Height = _boxNBtnHeight;
                btn.Width = WidgetUtils.MeasureString(btn.Label, btn.Font).Width + _boxNBtnHeight * 2;
                if (count > 0) {
                    btn.Margin = new(_contentHGap, 0, 0, 0);
                }
                btnsWidth += btn.Width;

                count++;
            }
            _buttonsPanel.Size = new(btnsWidth + _contentHGap * (_buttonsPanel.Controls.Count - 1), _buttonsOuterPanel.Height);
            _buttonsPanel.Location = new((_buttonsOuterPanel.Width - _buttonsPanel.Width) / 2, 0);
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
            LoadSettings();
        }
        #endregion

        #region Initialization methods
        protected virtual void InitializeSystemSettingsPanel() {
            _systemSettingsPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _systemSettingsTitlePanel = new("系统配置") {
                Parent = _systemSettingsPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _systemSettingsContentPanel = new() {
                Parent = _systemSettingsPanel,
            };
            _resolutionOptionsBox = new("分辨率") {
                Parent = _systemSettingsContentPanel,
                Ratio = 8.5,
            };
            _autoLaunchToggle = new("开机自动启动") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
            };
            _autoLoginToggle = new("自动登录") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
            };
            _logsRetentionDaysBox = new("日志保留天数") {
                Parent = _systemSettingsContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };
        }
        protected virtual void SaveSystemSettings() {
            // Resolution
            Size screenSize = WidgetUtils.GetScreenResolution();
            KeyValuePair<Size, SizeRatioNRectColor> value = _resolutionOptionsBox.Value;
            if (value.Key == new Size(0, 0)) {
                // If user select the defualt item, then set IsError = true
                _resolutionOptionsBox.SetError(true);
            } else {
                // Resize main form according to chosen resolution
                Form mainParent = (Form) WidgetUtils.MainForm;
                Size newSize = value.Key;
                if (_resolutionOptionsBox.IsError) {
                    _resolutionOptionsBox.SetError(false);
                }
                if (newSize != mainParent.Size) {
                    if (newSize == screenSize) {
                        mainParent.WindowState = FormWindowState.Maximized;
                    } else {
                        mainParent.WindowState = FormWindowState.Normal;
                        mainParent.Size = newSize;
                        mainParent.ClientSize = newSize;
                        mainParent.Location = new((screenSize.Width - newSize.Width) / 2, (screenSize.Height - newSize.Height) / 2);
                    }
                    MainUtils.SetSettingResolution(newSize);
                }
                ResizeChildren();
                _resolutionOriginal = newSize;
            }

            // Auto launch
            MainUtils.SetAutoLaunchEnabled(_autoLaunchToggle.Checked);
            _autoLaunchOriginal = _autoLaunchToggle.Checked;
            _addAutoRun();

            // Auto login
            MainUtils.SetAutoLoginEnabled(_autoLoginToggle.Checked);
            _autoLoginOriginal = _autoLoginToggle.Checked;
            if (!_autoLoginOriginal) {
                MainUtils.SetAutoLoginInfo(MainUtils.GetDefaultAutoLoginInfo());
            }

            // Logs retention days
            int logsRetentionDays = int.Parse(_logsRetentionDaysBox.GetTextBox(0).Box.Text);
            MainUtils.SetLogsRetentionDays(logsRetentionDays);
            _logsRetentionDaysOriginal = logsRetentionDays;

            // Use asynchronous method to avoid UI fronzing
            async void _addAutoRun() {
                await Task.Run(() => {
                    try {
                        string keyName = "OperationGuidanceNew_AutoRun";
                        RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        if (registryKey == null) {
                            registryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        }
                        if (_autoLaunchOriginal) {
                            registryKey.SetValue(keyName, Application.ExecutablePath);
                        } else {
                            registryKey.DeleteValue(keyName);
                        }
                    } catch (Exception e) {
                        logger.Warn($"Added auto launch error, e = [{e}]");
                    }
                });
            }
        }
        protected virtual void InitializeStoragePanel() {
            _storagePanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _storageTitlePanel = new("存储参数") {
                Parent = _storagePanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _storageContentPanel = new() {
                Parent = _storagePanel,
            };
            _enableExcelExportToggle = new ToggleButtonGroup("导出至Excel") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
                Checked = false,
            };
            _storagePathTextBox = new("数据存储路径") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };
            _storagePathTextBox.AddButton<CommonButton>("浏览").Click += (sender, eventArgs) => {
                FolderBrowserDialog dialog = new() {
                    ShowNewFolderButton = true,
                };
                string path = _storagePathTextBox.GetTextBox(0).Box.Text;
                if (!Directory.Exists(path)) {
                    dialog.SelectedPath = path;
                }
                if (dialog.ShowDialog() == DialogResult.OK) {
                    _storagePathTextBox.SetValue(0, dialog.SelectedPath + "\\");
                }
            };
            _storageFieldsButton = new("数据存储字段") {
                Parent = _storageContentPanel,
                Ratio = 6.95,
            };
            CommonButton storageFieldsButton = _storageFieldsButton.GetButton(0);
            storageFieldsButton.Label = "配置字段";
            storageFieldsButton.MouseUp += (sender, eventArgs) => {
                PopUpFieldsConfigurationForm(Fields);
            };
            _storageFieldsButton.AddButton("字段预览").MouseUp += (sender, eventArgs) => {
                PopUpFieldsPreviewForm(Fields);
            };
            _exportTestButton = new("导出测试") {
                Parent = _storageContentPanel,
                Ratio = 6.95,
                Enabled = false,
            };
            CommonButton exportExcelBtn = _exportTestButton.GetButton(0);
            exportExcelBtn.Label = "导出至Excel";
            exportExcelBtn.Click += (s, e) => _ = RunExportTest(enableExcel: true, enableTxt: false);
            var exportTxtBtn = _exportTestButton.AddButton("导出至Txt");
            exportTxtBtn.Click += (s, e) => _ = RunExportTest(enableExcel: false, enableTxt: true);
            exportTxtBtn.Hide(); // 默认隐藏，无场景使用
            _storeLooseningDataToggle = new("记录反松数据") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
            };

            _enableTxtExportToggle = new ToggleButtonGroup("导出至Txt") {
                Parent = _storageContentPanel,
                Ratio = 8.5,
                Checked = false,
            };

            // 基类默认全部隐藏 — 子类显式 Show
            _enableExcelExportToggle.Hide();
            _enableTxtExportToggle.Hide();
            _storagePathTextBox.Hide();
            _storageFieldsButton.Hide();
            _storeLooseningDataToggle.Hide();
            _exportTestButton.Hide();
            _storagePanel.Hide();

            _enableExcelExportToggle.CheckedChanged += (s, e) => UpdateExportControlsEnabled();
            _enableTxtExportToggle.CheckedChanged += (s, e) => UpdateExportControlsEnabled();
        }
        protected void SaveStorageSettings() {
            if (!_storagePanel.Visible) return;

            string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
            // Save
            MainUtils.SetStoragePath(newPath);
            MainUtils.SetSortConfig(SortConfig);
            MainUtils.SetSortConfigCurr(null);
            MainUtils.SetStoreLooseningData(_storeLooseningDataToggle.Checked);
            // 修改原始值
            _sortConfigOriginal = SortConfig;
            _sotragePathOriginal = newPath;
            _sotrageLooseningDataOriginal = _storeLooseningDataToggle.Checked;

            ExportConfig.Instance.SetExcelExportEnabled(_enableExcelExportToggle.Checked);
            ExportConfig.Instance.SetTxtExportEnabled(_enableTxtExportToggle.Checked);
            ExportConfig.Instance.Reload();
        }
        private void UpdateExportControlsEnabled() {
            bool anyEnabled = _enableExcelExportToggle.Checked || _enableTxtExportToggle.Checked;
            _storagePathTextBox.Enabled = anyEnabled;
            _storageFieldsButton.Enabled = anyEnabled;
            _exportTestButton.Enabled = anyEnabled;
        }

        private async Task RunExportTest(bool enableExcel, bool enableTxt) {
            try {
                var rng = new Random();
                var fakeData = new List<OperationDataVO>();
                for (int i = 1; i <= 5; i++) {
                    var vo = new OperationDataVO {
                        parameter_set_number = 10 + i,
                        parameter_set_name = $"测试程序{i}",
                        rundown_status = (int)TighteningCommonStatus.OK,
                        rundown_torque_status = (int)TighteningCommonStatus.OK,
                        rundown_angle_status = (int)TighteningCommonStatus.OK,
                        rundown_torque = (float)(rng.NextDouble() * 10 + 20),
                        rundown_torque_max = 35.0f,
                        rundown_torque_target = 28.0f,
                        rundown_torque_min = 20.0f,
                        rundown_angle = rng.Next(30, 90),
                        rundown_angle_max = 90,
                        rundown_angle_target = 60,
                        rundown_angle_min = 30,
                        tightening_status = (int)TighteningStatus.OK,
                        torque_status = (int)TighteningCommonStatus.OK,
                        angle_status = (int)TighteningCommonStatus.OK,
                        torque = (float)(rng.NextDouble() * 10 + 20),
                        torque_max_limit = 35.0f,
                        torque_final_target = 28.0f,
                        torque_min_limit = 20.0f,
                        angle = rng.Next(30, 90),
                        angle_max = 90,
                        angle_final_target = 60,
                        angle_min = 30,
                        batch_counter = i,
                        batch_size = 5,
                        batch_status = (int)BatchStatus.OK,
                        job_id = 1,
                        vin_number = "TEST_VIN_001",
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        tightening_id = 1000 + i,
                        workstation_name = "测试站点",
                        workstation_id = 1,
                        tool_ip = "192.168.1.100",
                        tool_name = "测试工具",
                        tool_type = "PF",
                        gun_num = "G1",
                        tightening_count = i,
                        bolt_serial_num = i,
                        creator = "测试操作员",
                        string_create_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    };
                    fakeData.Add(vo);
                }

                var fields = MainUtils.GetOperationDataFields(ExportConfig.Instance.SortConfig);
                var request = new ExportRequest {
                    Data = fakeData,
                    Fields = fields,
                    BasePath = ExportConfig.Instance.StoragePath,
                    ProductBatch = "TEST_BATCH",
                    ProductBarCode = "TEST_BARCODE",
                    CompletedAt = DateTime.Now,
                    Result = "OK",
                    EnableExcel = enableExcel,
                    EnableTxt = enableTxt,
                };

                await new DataExportService().ExportAsync(request);

                string type = enableExcel ? "Excel" : "Txt";
                WidgetUtils.ShowNoticePopUp($"导出测试完成 — {type} 文件已保存至:\n{ExportConfig.Instance.StoragePath}");
            } catch (Exception ex) {
                logger.Error($"导出测试失败: {ex.Message}", ex);
                WidgetUtils.ShowErrorPopUp($"导出测试失败: {ex.Message}");
            }
        }

        private void PopUpFieldsConfigurationForm(List<OperationDataField> fields) {
            FieldsConfiguration configPanel = new(fields);
            CustomPopUpForm form = new() {
                Title = "配置数据字段",
            };
            CommonButton confirmButton = form.AddButton("确定");
            confirmButton.Click += (s, e) => {
                (SortConfig, Fields) = configPanel.UpdateSortAndFields();
                form.Dispose();
            };
            CommonButton closeButton = form.AddButton("关闭");
            closeButton.Click += (s, e) => {
                form.Dispose();
            };
            form.PretendToShowToCreateHandlesForChildren();
            CustomVScrollingContentPanel scrollPanel = new(ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, configPanel) {
                Parent = form.ContentPanel,
            };
            // Calculate size
            Padding contentPadding = form.ContentPanel.Padding;
            int buttonHeight = WidgetUtils.CommonButtonHeight();
            int buttonMargin = buttonHeight / 5;
            Size contentSize = new((int) (WidgetUtils.MainSize.Width * .4), (int) (WidgetUtils.MainSize.Height * .6));
            configPanel.ToggleButtonHeight = buttonHeight;
            configPanel.ButtonMargin = buttonMargin;
            scrollPanel.Size = new(contentSize.Width - contentPadding.Size.Width, contentSize.Height - contentPadding.Size.Height);
            form.SetContentSizeAndSelfSize(contentSize);
            form.Show();
        }
        protected class FieldsConfiguration: CustomContentPanel {
            private readonly List<OperationDataField> _fields;
            private int _toggleButtonHeight;
            private int _buttonMargin;

            public int ToggleButtonHeight { get => _toggleButtonHeight; set => _toggleButtonHeight = value; }
            public int ButtonMargin { get => _buttonMargin; set => _buttonMargin = value; }

            public FieldsConfiguration(List<OperationDataField> fields) {
                _fields = fields;
                int serial = 1;
                foreach (OperationDataField field in _fields) {
                    MovableButton btn = new(serial++, field) {
                        Parent = this,
                        Checked = field.Visible,
                        Ratio = 5,
                    };
                    btn.CheckedChanged += (sender, eventArgs) => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (!btn.Field.Visible && btn.Checked) {
                            btn.Field.Visible = true;
                            int movementCount = VisibleToTrueMovementCount(currentIndex - 1);
                            btn.SerialNum -= movementCount;
                            Controls.SetChildIndex(btn, currentIndex - movementCount);
                        } else if (btn.Field.Visible && !btn.Checked) {
                            btn.Field.Visible = false;
                            int movementCount = VisibleToFalseMovementCount(currentIndex + 1);
                            btn.SerialNum += movementCount;
                            Controls.SetChildIndex(btn, currentIndex + movementCount);
                        }
                    };
                    btn.PressUp += () => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (!btn.Checked) {
                            btn.Checked = true;
                        } else if (currentIndex > 0) {
                            MovableButton previousBtn = (MovableButton) Controls[currentIndex - 1];
                            previousBtn.SerialNum += 1;
                            btn.SerialNum -= 1;
                            Controls.SetChildIndex(btn, currentIndex - 1);
                        }
                    };
                    btn.PressDown += () => {
                        int currentIndex = Controls.IndexOf(btn);
                        if (btn.Checked && currentIndex < Controls.Count - 1) {
                            MovableButton nextBtn = (MovableButton) Controls[currentIndex + 1];
                            if (nextBtn.Checked) {
                                nextBtn.SerialNum -= 1;
                                btn.SerialNum += 1;
                                Controls.SetChildIndex(btn, currentIndex + 1);
                            }
                        }
                    };
                }
                int VisibleToTrueMovementCount(int previousIndex) {
                    int count = 0;
                    if (previousIndex >= 0) {
                        MovableButton previousBtn = (MovableButton) Controls[previousIndex];
                        if (!previousBtn.Checked) {
                            count++;
                            previousBtn.SerialNum += 1;
                            return count + VisibleToTrueMovementCount(previousIndex - 1);
                        }
                    }
                    return count;
                }
                int VisibleToFalseMovementCount(int nextIndex) {
                    int count = 0;
                    if (nextIndex < Controls.Count) {
                        MovableButton nextBtn = (MovableButton) Controls[nextIndex];
                        if (nextBtn.Checked) {
                            count++;
                            nextBtn.SerialNum -= 1;
                            return count + VisibleToFalseMovementCount(nextIndex + 1);
                        }
                    }
                    return count;
                }
            }

            public Tuple<List<int>, List<OperationDataField>> UpdateSortAndFields() {
                List<int> sortConfig = new();
                List<OperationDataField> fieldsConfig = new();
                List<MovableButton> btns = new();
                foreach (Control ctrl in Controls) {
                    if (ctrl is MovableButton btn) {
                        btns.Add(btn);
                        fieldsConfig.Add(btn.Field);
                    }
                }
                // fieldsConfig = fieldsConfig.OrderBy(f => {
                //     int indexTemp = f.Id;
                //     if (indexTemp == -1) {
                //         indexTemp = _fields.Count;
                //     }
                //     return indexTemp;
                // }).ToList();
                fieldsConfig.ForEach(f => {
                    if (f.Visible) {
                        sortConfig.Add(f.Id);
                    }
                });
                return new(sortConfig, fieldsConfig);
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                base.ResizeChildren(sender, eventArgs);
                int marginTop = _buttonMargin / 2;
                int marginBottom = _buttonMargin - marginTop;
                foreach (Control ctrl in Controls) {
                    ctrl.Size = new(Width - _buttonMargin * 2, _toggleButtonHeight);
                    ctrl.Margin = new(_buttonMargin, marginTop, 0, marginBottom);
                }
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                NewHeight = (_toggleButtonHeight + _buttonMargin) * Controls.Count;
                return NewHeight > parentNewHeight;
            }

            private class MovableButton: ToggleButtonGroup {
                private int _serialNum;
                private OperationDataField _field;
                private Image _upImage = Properties.Resources.direction_up;
                private Image _downImage = Properties.Resources.direction_down;
                private Rectangle? _upImageRect;
                private Rectangle? _downImageRect;
                private Image? _upImageShowing;
                private Image? _downImageShowing;
                private bool _upIsDown = false;
                private bool _downIsDown = false;
                private bool _focusOnUp = false;
                private bool _focusOnDown = false;
                private Action? _onUp;
                private Action? _onDown;
                private bool _isPressing = false;

                public new bool Checked {
                    get => base.Checked;
                    set {
                        base.Checked = value;
                        if (value) {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND;
                        } else {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND_INVISIBLE;
                        }
                    }
                }
                public int SerialNum {
                    get => _serialNum;
                    set {
                        _serialNum = value;
                        TextName = $"{_serialNum}. {_field.FieldName}";
                    }
                }
                public OperationDataField Field { get => _field; set => _field = value; }

                public event Action PressUp { add => _onUp += value; remove => _onUp -= value; }
                public event Action PressDown { add => _onDown += value; remove => _onDown -= value; }

                public MovableButton(int serialNum, OperationDataField field) : base($"{serialNum}. {field.FieldName}") {
                    _serialNum = serialNum;
                    _field = field;
                    ToggleButton.CheckedChanged += (sender, eventArgs) => {
                        if (Checked) {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND;
                        } else {
                            ForeColor = ColorConfigs.COLOR_FIELD_TOGGLE_FOREGROUND_INVISIBLE;
                        }
                    };
                }

                protected override void OnMouseEnter(EventArgs e) {
                    base.OnMouseEnter(e);
                    int btnSide = (int) (Height * .75);
                    int margin = btnSide / 3;
                    Size imageSize = new(btnSide, btnSide);
                    Point imageDownLocation = new(Width - btnSide, (Height - btnSide) / 2);
                    Point imageUpLocation = new(Width - btnSide * 2 - margin, (Height - btnSide) / 2);

                    _upImageRect = new(imageUpLocation, imageSize);
                    _downImageRect = new(imageDownLocation, imageSize);

                    _upImageShowing = WidgetUtils.ResizeImage(_upImage, imageSize);
                    _downImageShowing = WidgetUtils.ResizeImage(_downImage, imageSize);
                }
                private void ClickUpAnimation(bool goDown) {
                    if (_upImageRect != null) {
                        if (goDown) {
                            if (!_upIsDown) {
                                _upImageRect = new(new(_upImageRect.Value.X + 1, _upImageRect.Value.Y + 1), _upImageRect.Value.Size);
                                _upIsDown = true;
                            }
                        } else {
                            if (_upIsDown && !_isPressing) {
                                _upImageRect = new(new(_upImageRect.Value.X - 1, _upImageRect.Value.Y - 1), _upImageRect.Value.Size);
                                _upIsDown = false;
                            }
                        }
                    }
                }
                private void ClickDownAnimation(bool goDown) {
                    if (_downImageRect != null) {
                        if (goDown) {
                            if (!_downIsDown) {
                                _downImageRect = new(new(_downImageRect.Value.X + 1, _downImageRect.Value.Y + 1), _downImageRect.Value.Size);
                                _downIsDown = true;
                            }
                        } else {
                            if (_downIsDown && !_isPressing) {
                                _downImageRect = new(new(_downImageRect.Value.X - 1, _downImageRect.Value.Y - 1), _downImageRect.Value.Size);
                                _downIsDown = false;
                            }
                        }
                    }
                }
                protected override void OnMouseLeave(EventArgs e) {
                    base.OnMouseLeave(e);
                    _upImageRect = null;
                    _downImageRect = null;
                    _upImageShowing = null;
                    _downImageShowing = null;
                    Invalidate();
                }
                protected override void OnMouseMove(MouseEventArgs mevent) {
                    base.OnMouseMove(mevent);
                    if (_upImageRect != null) {
                        if (EventFuncs.MouseInArea(new(PointToScreen(_upImageRect.Value.Location), _upImageRect.Value.Size))) {
                            _focusOnUp = true;
                        } else {
                            ClickUpAnimation(false);
                            _focusOnUp = false;
                        }
                        Invalidate();
                    }
                    if (_downImageRect != null) {
                        if (EventFuncs.MouseInArea(new(PointToScreen(_downImageRect.Value.Location), _downImageRect.Value.Size))) {
                            _focusOnDown = true;
                        } else {
                            ClickDownAnimation(false);
                            _focusOnDown = false;
                        }
                        Invalidate();
                    }
                }
                protected override void OnMouseDown(MouseEventArgs mevent) {
                    base.OnMouseDown(mevent);
                    _isPressing = true;
                    if (_focusOnUp && _upImageRect != null) {
                        ClickUpAnimation(true);
                        Invalidate();
                    }
                    if (_focusOnDown && _downImageRect != null) {
                        ClickDownAnimation(true);
                        Invalidate();
                    }
                }
                protected override void OnMouseUp(MouseEventArgs mevent) {
                    base.OnMouseUp(mevent);
                    _isPressing = false;
                    if (_focusOnUp && _upImageRect != null) {
                        ClickUpAnimation(false);
                        Invalidate();
                        if (_onUp != null) {
                            _onUp();
                        }
                    }
                    if (_focusOnDown && _downImageRect != null) {
                        ClickDownAnimation(false);
                        Invalidate();
                        if (_onDown != null) {
                            _onDown();
                        }
                    }
                }
                protected override void OnPaint(PaintEventArgs e) {
                    base.OnPaint(e);
                    if (_upImageShowing != null && _upImageRect != null) {
                        e.Graphics.DrawImage(_upImageShowing, _upImageRect.Value.Location);
                    }
                    if (_downImageShowing != null && _downImageRect != null) {
                        e.Graphics.DrawImage(_downImageShowing, _downImageRect.Value.Location);
                    }
                }
            }
        }
        private void PopUpFieldsPreviewForm(List<OperationDataField> fields) {
            CustomPopUpForm form = new() {
                Title = "数据字段配置预览",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            CommonButton closeButton = form.AddButton("关闭");
            closeButton.Click += (s, e) => {
                form.Dispose();
            };
            form.PretendToShowToCreateHandlesForChildren();
            WorkplacePiece outer = new() {
                Parent = form.ContentPanel,
                Padding = new(1),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            DataGridView gridView = new() {
                Parent = outer,
                Margin = new(0),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = true,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.None,
            };
            int newHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            int padding = newHeaderHeight / 2;
            if (newHeaderHeight >= 4) {
                gridView.ColumnHeadersHeight = newHeaderHeight;
                gridView.ColumnHeadersDefaultCellStyle.Font = new(WidgetsConfigs.SystemFontFamily, newHeaderHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            gridView.ColumnHeadersDefaultCellStyle.BackColor = WidgetUtils.LightColor(ColorTranslator.FromHtml("#E86C10"), .15);
            gridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FEFEFE");
            gridView.ColumnHeadersDefaultCellStyle.Padding = new(padding, 0, padding, 0);
            gridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = gridView.ColumnHeadersDefaultCellStyle.BackColor;
            DataGridViewColumn[] columnRange = { };
            foreach (OperationDataField field in fields) {
                if (field.Visible) {
                    DataGridViewTextBoxColumn column = new() {
                        HeaderText = field.FieldName,
                        ReadOnly = true,
                    };
                    columnRange = columnRange.Append(column).ToArray();
                }
            }
            gridView.Columns.AddRange(columnRange);
            // Calculate size
            int gridHeaderHeight = WidgetUtils.GridViewHeaderHeight();
            int contentWidth = (int) (WidgetUtils.MainSize.Width * .85);
            outer.Size = new(contentWidth - form.ContentPanel.Padding.Size.Width, gridHeaderHeight + 2);
            outer.Padding = new(1);
            gridView.Size = new(outer.Width - 2, gridHeaderHeight);
            gridView.ColumnHeadersHeight = gridHeaderHeight;
            int columnsWidth = 0;
            foreach (DataGridViewColumn column in gridView.Columns) {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                columnsWidth += column.Width;
            }
            // Check for scroll bar
            if (columnsWidth > gridView.Width) {
                HScrollBar hScrollBar = new() {
                    Parent = gridView,
                    Margin = new(0),
                    Size = new(gridView.Width, WidgetUtils.ScrollBarThickness()),
                    Location = new(0, gridHeaderHeight),
                };
                hScrollBar.ValueChanged += (sender, eventArgs) => {
                    gridView.HorizontalScrollingOffset = hScrollBar.Value;
                };
                gridView.Height += hScrollBar.Height;
                outer.Height += hScrollBar.Height;
                WidgetUtils.CalculateScrollBar(hScrollBar, gridView.Width, columnsWidth);
            }
            form.SetContentSizeAndSelfSize(new(contentWidth, outer.Height + form.ContentPanel.Padding.Size.Height));
            form.Show();
        }
        protected virtual void InitializeMissionSettings() {
            _workPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _workTitlePanel = new("操作配置") {
                Parent = _workPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _workContentPanel = new() {
                Parent = _workPanel,
            };
            _enableArmLocatingToggle = new("开启力臂定位") {
                Parent = _workContentPanel,
                Ratio = 6.95,
            };
            _enableArmLocatingToggle.CheckedChanged += (s, e) => {
                _armLocatingAccuracyBox.Enabled = _enableArmLocatingToggle.Checked;
                if (_armLocatingAccuracyBox.Enabled) {
                    if (_armLocatingAccuracyOriginal == 0) {
                        _armLocatingAccuracyBox.SetValue(0, MainUtils.GetDefaultArmLocatingAccuracy() + "");
                    } else {
                        _armLocatingAccuracyBox.SetValue(0, _armLocatingAccuracyOriginal + "");
                    }
                } else {
                    _armLocatingAccuracyBox.SetValue(0, "0");
                }
            };
            _armLocatingAccuracyBox = new("力臂定位精度") {
                Parent = _workContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };
            _errorPromptForArmToggle = new("力臂定位警告") {
                Parent = _workContentPanel,
                Ratio = 6.95,
                Visible = false,
            };
            _missionSelfLoopingModeToggle = new("任务自循环模式") {
                Parent = _workContentPanel,
                Ratio = 6.95,
            };
            _autoLockToolToggle = new("启动后自动锁枪") {
                Parent = _workContentPanel,
                Ratio = 6.95,
            };
            _usbScannerEnabledToggle = new("USB扫码枪") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
        }
        protected virtual void SaveMissionSettings() {
            MainUtils.SetArmLocatingEnabled(_enableArmLocatingToggle.Checked);
            MainUtils.SetArmLocatingAccuracy(int.Parse(_armLocatingAccuracyBox.GetTextBox(0).Box.Text));
            MainUtils.SetErrorPromptForArmEnabled(_errorPromptForArmToggle.Checked);
            MainUtils.SetMissionSelfLoopingModeEnabled(_missionSelfLoopingModeToggle.Checked);
            MainUtils.SetAutoLockToolEnabled(_autoLockToolToggle.Checked);
            MainUtils.SetUSBScannerEnabled(_usbScannerEnabledToggle.Checked);

            // 修改初始值
            _enableArmLocatingOriginal = _enableArmLocatingToggle.Checked;
            _armLocatingAccuracyOriginal = int.Parse(_armLocatingAccuracyBox.GetTextBox(0).Box.Text);
            _errorPromptForArmOriginal = _errorPromptForArmToggle.Checked;
            _missionSelfLoopingModeOriginal = _missionSelfLoopingModeToggle.Checked;
            _autoLockToolOriginal = _autoLockToolToggle.Checked;
            _usbScannerEnabledOriginal = _usbScannerEnabledToggle.Checked;
        }
        protected virtual string? CheckBeforeSave() {
            if (!_storagePanel.Visible) return null;

            string newPath = _storagePathTextBox.GetTextBox(0).Box.Text;
            if (!Directory.Exists(newPath)) {
                return "当前存储路径格式不正确或不存在";
            }
            return null;
        }
        #endregion

        #region Resize methods
        private void ResizeSystemSettingsPanel() {
            // Resize title
            _systemSettingsTitlePanel.Size = new(Width, _titleHeight);
            int boxVMargin = this._boxNBtnHeight / 2;
            int contentHeight = _boxNBtnHeight * 3 + _contentVPadding * 2 + boxVMargin * 2;
            int boxWidth = (Width - _contentHPadding * 3) / 2;
            // Resize Content
            _systemSettingsContentPanel.Size = new(Width, contentHeight);
            _systemSettingsContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _resolutionOptionsBox.Size = new(Width - _systemSettingsContentPanel.Padding.Size.Width, _boxNBtnHeight);
            _resolutionOptionsBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _autoLaunchToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _autoLaunchToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _autoLoginToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _autoLoginToggle.Margin = new(0, boxVMargin, 0, 0);
            _logsRetentionDaysBox.Size = new(boxWidth, _boxNBtnHeight);
            _logsRetentionDaysBox.Margin = new(0, boxVMargin, 0, 0);
            // Resize outer panel
            _systemSettingsPanel.Size = new(Width, _systemSettingsTitlePanel.Height + _systemSettingsContentPanel.Height);
        }
        protected virtual void ResizeStoragePanel() {
            if (!_storagePanel.Visible) return;

            // Resize title
            _storageTitlePanel.Size = new(Width, _titleHeight);
            int boxWidth = (Width - _contentHPadding * 2);
            int halfBoxWidth = (Width - _contentHPadding * 3) / 2;
            int boxVMargin = this._boxNBtnHeight / 2;
            int contentHeight = this._boxNBtnHeight * 5 + _contentVPadding * 2 + boxVMargin * 4;
            // Resize Content
            _storageContentPanel.Size = new(Width, contentHeight);
            _storageContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box (matches initialization order)
            _enableExcelExportToggle.Size = new(boxWidth, _boxNBtnHeight);
            _enableExcelExportToggle.Margin = new(0, 0, _contentHGap / 2, 0);
            _storagePathTextBox.Size = new(boxWidth, this._boxNBtnHeight);
            _storagePathTextBox.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            // 数据存储字段 + 导出测试 同行（各半宽，参照 MissionSettings 模式）
            _storageFieldsButton.Size = new(halfBoxWidth, _boxNBtnHeight);
            _storageFieldsButton.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _exportTestButton.Size = new(halfBoxWidth, _boxNBtnHeight);
            _exportTestButton.Margin = new(0, boxVMargin, 0, 0);
            _storeLooseningDataToggle.Size = new(boxWidth, _boxNBtnHeight);
            _storeLooseningDataToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _enableTxtExportToggle.Size = new(boxWidth, _boxNBtnHeight);
            _enableTxtExportToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            // Resize outer panel
            _storagePanel.Size = new(Width, _storageTitlePanel.Height + _storageContentPanel.Height);
        }
        protected virtual void ResizeMissionSettings() {
            // Resize title
            _workTitlePanel.Size = new(Width, _titleHeight);
            int boxWidth = (Width - _contentHPadding * 3) / 2;
            int boxVMargin = this._boxNBtnHeight / 2;
            int contentHeight = this._boxNBtnHeight * 3 + _contentVPadding * 2 + boxVMargin * 2;
            // Resize Content
            _workContentPanel.Size = new(Width, contentHeight);
            _workContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box
            _enableArmLocatingToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _enableArmLocatingToggle.Margin = new(0, 0, _contentHGap / 2, 0);
            _armLocatingAccuracyBox.Size = new(boxWidth, _boxNBtnHeight);
            _armLocatingAccuracyBox.Margin = new(0, 0, 0, 0);
            _missionSelfLoopingModeToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _missionSelfLoopingModeToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _autoLockToolToggle.Size = new(boxWidth, this._boxNBtnHeight);
            _autoLockToolToggle.Margin = new(0, boxVMargin, 0, 0);
            _usbScannerEnabledToggle.Size = new(boxWidth, BoxNBtnHeight);
            _usbScannerEnabledToggle.Margin = new(0, boxVMargin, _contentHGap / 2, 0);
            _errorPromptForArmToggle.Size = new(boxWidth, this._boxNBtnHeight);
            // Resize outer panel
            _workPanel.Size = new(Width, _workTitlePanel.Height + _workContentPanel.Height);
        }
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            NewHeight = _buttonsOuterPanel.Height + _buttonsOuterPanel.Margin.Size.Height;
            NewHeight += _systemSettingsPanel.Height + _systemSettingsPanel.Margin.Size.Height;
            if (_storagePanel.Visible) {
                NewHeight += _storagePanel.Height + _storagePanel.Margin.Size.Height;
            }
            NewHeight += _workPanel.Height + _workPanel.Margin.Size.Height;
            return NewHeight > parentNewHeight;
        }
        #endregion

        #region Reusable methods
        protected virtual async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    // System settings
                    Dictionary<Size, SizeRatioNRectColor>.Enumerator enumerator = WidgetsConfigs.Resolutions.GetEnumerator();
                    Size screenSize = WidgetUtils.GetScreenResolution();
                    bool hasFullScreenResolution = false;
                    _resolutionOptionsBox.ClearItem();
                    while (enumerator.MoveNext()) {
                        KeyValuePair<Size, SizeRatioNRectColor> current = enumerator.Current;
                        if (current.Key.Width > screenSize.Width || current.Key.Height > screenSize.Height) {
                            continue;
                        }
                        string itemName = $"{current.Key.Width} x {current.Key.Height}";
                        if (current.Key.Width == screenSize.Width && current.Key.Height == screenSize.Height) {
                            itemName += "（全屏）";
                            hasFullScreenResolution = true;
                        } else {
                            itemName += $"（{current.Value.WidthRatio} x {current.Value.HeightRatio}）";
                        }
                        _resolutionOptionsBox.AddItem(itemName, current);
                    }
                    if (!hasFullScreenResolution) {
                        _resolutionOptionsBox.AddItem($"{screenSize.Width} x {screenSize.Height}（全屏）", new KeyValuePair<Size, SizeRatioNRectColor>(screenSize, new()));
                    }
                    // Change resolution option according to current resolution
                    List<KeyValuePair<Size, SizeRatioNRectColor>> items = _resolutionOptionsBox.Items;
                    for (int i = 0; i < items.Count; i++) {
                        KeyValuePair<Size, SizeRatioNRectColor> item = items[i];
                        if (item.Key == MainUtils.GetSettingResolution()) {
                            _resolutionOptionsBox.SetCurrent(i);
                            _resolutionOriginal = item.Key;
                            break;
                        }
                    }
                    _autoLaunchOriginal = MainUtils.IsAutoLaunchEnabled();
                    _autoLoginOriginal = MainUtils.IsAutoLoginEnabled();
                    _autoLaunchToggle.Checked = _autoLaunchOriginal;
                    _autoLoginToggle.Checked = _autoLoginOriginal;
                    _logsRetentionDaysOriginal = MainUtils.GetLogsRetentionDays();
                    _logsRetentionDaysBox.SetValue(0, _logsRetentionDaysOriginal + "");

                    // Storage settings
                    _sortConfigOriginal = MainUtils.GetSortConfig();
                    _sotragePathOriginal = MainUtils.GetStoragePath();
                    _sotrageLooseningDataOriginal = MainUtils.GetStoreLooseningData();
                    _enableExcelExportOriginal = ExportConfig.Instance.ExcelExportEnabled;
                    _enableTxtExportOriginal = ExportConfig.Instance.TxtExportEnabled;
                    SortConfig.Clear();
                    _sortConfigOriginal.ForEach(id => SortConfig.Add(id));
                    Fields = MainUtils.GetOperationDataFields(SortConfig);
                    _storagePathTextBox.SetValue(0, _sotragePathOriginal);
                    _storeLooseningDataToggle.Checked = _sotrageLooseningDataOriginal;
                    _enableExcelExportToggle.Checked = _enableExcelExportOriginal;
                    _enableTxtExportToggle.Checked = _enableTxtExportOriginal;
                    UpdateExportControlsEnabled();

                    // Operation settings
                    _enableArmLocatingOriginal = MainUtils.IsArmLocatingEnabled();
                    if (_enableArmLocatingOriginal) {
                        _armLocatingAccuracyOriginal = MainUtils.GetArmLocatingAccuracy();
                    } else {
                        _armLocatingAccuracyOriginal = 0;
                    }
                    _errorPromptForArmOriginal = MainUtils.IsErrorPromptForArmEnabled();
                    _missionSelfLoopingModeOriginal = MainUtils.IsMissionSelfLoopingModeEnabled();
                    _autoLockToolOriginal = MainUtils.IsAutoLockToolEnabled();
                    _usbScannerEnabledOriginal = MainUtils.IsUSBScannerEnabled();
                    _enableArmLocatingToggle.Checked = _enableArmLocatingOriginal;
                    _armLocatingAccuracyBox.SetValue(0, _armLocatingAccuracyOriginal + "");
                    _armLocatingAccuracyBox.Enabled = _enableArmLocatingOriginal;
                    _errorPromptForArmToggle.Checked = _errorPromptForArmOriginal;
                    _missionSelfLoopingModeToggle.Checked = _missionSelfLoopingModeOriginal;
                    _autoLockToolToggle.Checked = _autoLockToolOriginal;
                    _usbScannerEnabledToggle.Checked = _usbScannerEnabledOriginal;
                });
            });
        }
        protected virtual async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    // System settings
                    List<KeyValuePair<Size, SizeRatioNRectColor>> items = _resolutionOptionsBox.Items;
                    for (int i = 0; i < items.Count; i++) {
                        KeyValuePair<Size, SizeRatioNRectColor> item = items[i];
                        if (item.Key == MainUtils.GetDefaultSettingResolution()) {
                            _resolutionOptionsBox.SetCurrent(i);
                            break;
                        }
                    }
                    _autoLaunchToggle.Checked = MainUtils.DefaultAutoLaunchEnabled();
                    _autoLoginToggle.Checked = MainUtils.DefaultAutoLoginEnabled();
                    _logsRetentionDaysBox.SetValue(0, MainUtils.DefaultLogsRetentionDays() + "");

                    // Storage settings
                    SortConfig = MainUtils.GetDefaultSortConfig();
                    Fields = MainUtils.GetOperationDataFields(SortConfig);
                    _storagePathTextBox.SetValue(0, MainUtils.GetDefaultStoragePath());
                    _storeLooseningDataToggle.Checked = MainUtils.GetDefaultStoreLooseningData();
                    _enableExcelExportToggle.Checked = MainUtils.GetDefaultExcelExportEnabled();
                    _enableTxtExportToggle.Checked = MainUtils.GetDefaultTxtExportEnabled();
                    UpdateExportControlsEnabled();

                    // Operation settings
                    _enableArmLocatingToggle.Checked = MainUtils.DefaultIsArmLocatingEnabled();
                    _armLocatingAccuracyBox.SetValue(0, MainUtils.GetDefaultArmLocatingAccuracy() + "");
                    _errorPromptForArmToggle.Checked = MainUtils.DefaultIsErrorPromptForArmEnabled();
                    _missionSelfLoopingModeToggle.Checked = MainUtils.DefaultMissionSelfLoopingModeEnabled();
                    _autoLockToolToggle.Checked = MainUtils.DefaultAutoLockToolEnabled();
                    _usbScannerEnabledToggle.Checked = MainUtils.DefaultUSBScannerEnabled();
                });
            });
        }
        #endregion
    }
}
