using CustomLibrary.Buttons;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Utils;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        private ToggleButtonGroup _enableBatchCounter;
        private bool _enableBatchCounterOriginal;

        private ToggleButtonGroup _buzzerEnabledToggle;
        private CommonButtonGroup _buzzerTestButtons;
        private bool _buzzerEnabledOriginal;

        private ToggleButtonGroup _errorPromptForWrongBarcodeToggle;
        private bool _errorPromptForWrongBarcodeOriginal;

        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
            StoreLooseningDataToggle.Hide();   // redundant (base hides it) but harmless
            AutoLockToolToggle.Hide();
            EnableArmLocatingToggle.Hide();
            ArmLocatingAccuracyBox.Hide();
            ErrorPromptForArmToggle.Show();

            // 导出相关 — SCII 可见
            StoragePanel.Show();
            EnableExcelExportToggle.Show();
            StoragePathTextBox.Show();
            StorageFieldsButton.Show();
            ExportTestButton.Show();
        }

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            _enableBatchCounter = new("启用批次计数") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            _errorPromptForWrongBarcodeToggle = new("启用错码验证") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            _buzzerEnabledToggle = new("启用蜂鸣器") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            _buzzerTestButtons = new("蜂鸣器测试") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
                Enabled = false,
            };
            CommonButton testOnButton = _buzzerTestButtons.GetButton(0);
            testOnButton.Label = "测试开";
            testOnButton.MouseUp += (s, e) => {
                _ = BuzzerController.TurnOnAsync();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已启动");
            };
            _buzzerTestButtons.AddButton("测试关").MouseUp += (s, e) => {
                _ = BuzzerController.TurnOffAsync();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已关闭");
            };
            _buzzerEnabledToggle.CheckedChanged += (s, e) => {
                _buzzerTestButtons.Enabled = _buzzerEnabledToggle.Checked;
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            sciiBatchConfig.enabled = _enableBatchCounter.Checked.ToYesOrNoInt();
            ConfigUtils.SaveConfig(sciiBatchConfig);
            _enableBatchCounterOriginal = sciiBatchConfig.enabled.ToYesOrNoBool();

            bool wrongBarcodeEnabled = _errorPromptForWrongBarcodeToggle.Checked;
            MainUtils.SetErrorPromptForWrongBarcodeEnabled(wrongBarcodeEnabled);
            _errorPromptForWrongBarcodeOriginal = wrongBarcodeEnabled;

            bool buzzerEnabled = _buzzerEnabledToggle.Checked;
            MainUtils.SetBuzzerEnabled(buzzerEnabled);
            _buzzerEnabledOriginal = buzzerEnabled;
        }

        protected override void ResizeStoragePanel() {
            base.ResizeStoragePanel();

            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2;
            StorageContentPanel.Size = new(Width, contentHeight);

            StoragePanel.Size = new(Width, StorageTitlePanel.Height + StorageContentPanel.Height);
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            ErrorPromptForArmToggle.Margin = new(0, 0, ContentHGap / 2, 0);
            UsbScannerEnabledToggle.Margin = new(0, 0, 0, 0);

            _buzzerEnabledToggle.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _buzzerTestButtons.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerTestButtons.Margin = new(0, boxVMargin, 0, 0);

            _enableBatchCounter.Size = new(boxWidth, BoxNBtnHeight);
            _enableBatchCounter.Margin = new(0, boxVMargin, ContentHGap / 2, 0);

            _errorPromptForWrongBarcodeToggle.Size = new(boxWidth, BoxNBtnHeight);
            _errorPromptForWrongBarcodeToggle.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 3 + ContentVPadding * 2 + boxVMargin * 2);
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height);
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _buzzerEnabledOriginal = MainUtils.IsBuzzerEnabled();
                    _buzzerEnabledToggle.Checked = _buzzerEnabledOriginal;

                    var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();

                    _errorPromptForWrongBarcodeOriginal = MainUtils.IsErrorPromptForWrongBarcodeEnabled();
                    _errorPromptForWrongBarcodeToggle.Checked = _errorPromptForWrongBarcodeOriginal;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _buzzerEnabledToggle.Checked = MainUtils.DefaultIsBuzzerEnabled();

                    var sciiBatchConfig = ConfigUtils.GetDefault<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();

                    _errorPromptForWrongBarcodeToggle.Checked = MainUtils.DefaultIsErrorPromptForWrongBarcodeEnabled();
                });
            });
        }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && _errorPromptForWrongBarcodeToggle.Checked == _errorPromptForWrongBarcodeOriginal
            && _buzzerEnabledToggle.Checked == _buzzerEnabledOriginal
            && _enableBatchCounter.Checked == _enableBatchCounterOriginal;
    }
}
