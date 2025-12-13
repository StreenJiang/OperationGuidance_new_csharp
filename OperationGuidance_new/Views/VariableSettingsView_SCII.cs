using CustomLibrary.Buttons;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        private ToggleButtonGroup _enableBatchCounter;
        private bool _enableBatchCounterOriginal;

        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
            StoreLooseningDataToggle.Hide();
            AutoLockToolToggle.Hide();
            EnableArmLocatingToggle.Hide();
            ArmLocatingAccuracyBox.Hide();
            ErrorPromptForArmToggle.Show();
        }

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            _enableBatchCounter = new("启用批次计数") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            sciiBatchConfig.enabled = _enableBatchCounter.Checked.ToYesOrNoInt();
            ConfigUtils.SaveConfig(sciiBatchConfig);

            _enableBatchCounterOriginal = sciiBatchConfig.enabled.ToYesOrNoBool();
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

            // Resize title
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            // Resize box
            ErrorPromptForArmToggle.Margin = new(0, 0, ContentHGap / 2, 0);
            UsbScannerEnabledToggle.Margin = new(0, 0, 0, 0);
            _enableBatchCounter.Size = new(boxWidth, this.BoxNBtnHeight);
            _enableBatchCounter.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            // Resize Content
            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 2 + ContentVPadding * 3 + boxVMargin * 0);
            // Resize outer panel
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height);
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                base.LoadSettings();

                BeginInvoke(() => {
                    var sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                base.ResetAllToDefault();

                BeginInvoke(() => {
                    var sciiBatchConfig = ConfigUtils.GetDefault<SciiBatchConfig>();
                    _enableBatchCounter.Checked = sciiBatchConfig.enabled.ToYesOrNoBool();
                });
            });
        }
    }
}
