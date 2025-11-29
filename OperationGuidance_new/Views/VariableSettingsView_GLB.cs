using CustomLibrary.Buttons;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_GLB: AVariableSettingsView {
        private ToggleButtonGroup _plcBarCodeSelfLoopingToggle;
        private bool _plcBarCodeSelfLoopingOriginal;

        public ToggleButtonGroup PLCBarCodeSelfLoopingToggle { get => _plcBarCodeSelfLoopingToggle; set => _plcBarCodeSelfLoopingToggle = value; }
        public bool PLCBarCodeSelfLoopingModeOriginal { get => _plcBarCodeSelfLoopingOriginal; set => _plcBarCodeSelfLoopingOriginal = value; }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && _plcBarCodeSelfLoopingToggle.Checked == _plcBarCodeSelfLoopingOriginal;

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            _plcBarCodeSelfLoopingToggle = new("PLC条码自循环") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            _plcBarCodeSelfLoopingToggle.CheckedChanged += (s, e) => {
                if (_plcBarCodeSelfLoopingToggle.Checked) {
                    // Uncheck 'SelfLoopingMode'
                    if (MissionSelfLoopingModeToggle.Checked) {
                        MissionSelfLoopingModeToggle.Checked = false;
                    }
                }
            };

            MissionSelfLoopingModeToggle.CheckedChanged += (s, e) => {
                if (MissionSelfLoopingModeToggle.Checked && _plcBarCodeSelfLoopingToggle.Checked) {
                    _plcBarCodeSelfLoopingToggle.Checked = false;
                }
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            bool plcToggle = _plcBarCodeSelfLoopingToggle.Checked;

            MainUtils.SetPLCBarCodeSelfLoopingModeEnabled(plcToggle);

            // 修改初始值
            _plcBarCodeSelfLoopingOriginal = plcToggle;
        }

        protected override string? CheckBeforeSave() {
            string? errorTemp = base.CheckBeforeSave();
            if (!string.IsNullOrEmpty(errorTemp)) {
                return errorTemp;
            }

            return null;
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1;

            // Resize parent settings
            UsbScannerEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);

            _plcBarCodeSelfLoopingToggle.Size = new(boxWidth, BoxNBtnHeight);
            _plcBarCodeSelfLoopingToggle.Margin = new(0, boxVMargin, 0, 0);

            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _plcBarCodeSelfLoopingOriginal = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                    _plcBarCodeSelfLoopingToggle.Checked = _plcBarCodeSelfLoopingOriginal;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _plcBarCodeSelfLoopingToggle.Checked = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                });
            });
        }
    }
}
