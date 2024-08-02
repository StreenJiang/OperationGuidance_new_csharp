using CustomLibrary.Buttons;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_GLB: AVariableSettingsView {
        private ToggleButtonGroup _plcBarCodeSelfLoopingToggle;
        private bool _plcBarCodeSelfLoopingOriginal;
        private CustomTextBoxButtonGroup _plcBarCodeStartAddressBox;
        private int _plcBarCodeStartAddressOriginal;
        private CustomTextBoxButtonGroup _plcBarCodeLengthBox;
        private int _plcBarCodeLengthOriginal;

        public ToggleButtonGroup PLCBarCodeSelfLoopingToggle { get => _plcBarCodeSelfLoopingToggle; set => _plcBarCodeSelfLoopingToggle = value; }
        public bool PLCBarCodeSelfLoopingModeOriginal { get => _plcBarCodeSelfLoopingOriginal; set => _plcBarCodeSelfLoopingOriginal = value; }
        public CustomTextBoxButtonGroup PLCBarCodeStartAddressBox { get => _plcBarCodeStartAddressBox; set => _plcBarCodeStartAddressBox = value; }
        public int PLCBarCodeStartAddressOriginal { get => _plcBarCodeStartAddressOriginal; set => _plcBarCodeStartAddressOriginal = value; }
        public CustomTextBoxButtonGroup PLCBarCodeLengthBox { get => _plcBarCodeLengthBox; set => _plcBarCodeLengthBox = value; }
        public int PLCBarCodeLengthOriginal { get => _plcBarCodeLengthOriginal; set => _plcBarCodeLengthOriginal = value; }

        protected override bool CheckSavedFunc() => base.CheckSavedFunc()
            || _plcBarCodeSelfLoopingToggle.Checked != _plcBarCodeSelfLoopingOriginal
            || _plcBarCodeStartAddressBox.GetTextBox(0).Box.Text != _plcBarCodeStartAddressOriginal + ""
            || _plcBarCodeLengthBox.GetTextBox(0).Box.Text != _plcBarCodeLengthOriginal + "";

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            _plcBarCodeSelfLoopingToggle = new("PLC条码自循环") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            _plcBarCodeStartAddressBox = new("PLC条码起始地址") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };
            _plcBarCodeLengthBox = new("PLC条码长度") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };

            _plcBarCodeSelfLoopingToggle.CheckedChanged += (s, e) => {
                _plcBarCodeStartAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                _plcBarCodeLengthBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;

                if (_plcBarCodeSelfLoopingToggle.Checked) {
                    _plcBarCodeStartAddressBox.SetValue(0, _plcBarCodeStartAddressOriginal + "");
                    _plcBarCodeLengthBox.SetValue(0, _plcBarCodeLengthOriginal + "");

                    // Uncheck 'SelfLoopingMode'
                    if (MissionSelfLoopingModeToggle.Checked) {
                        MissionSelfLoopingModeToggle.Checked = false;
                    }
                } else {
                    _plcBarCodeStartAddressBox.SetValue(0, "0");
                    _plcBarCodeLengthBox.SetValue(0, "0");
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
            int plcAddr = int.Parse(_plcBarCodeStartAddressBox.GetTextBox(0).Box.Text);
            int plcLen = int.Parse(_plcBarCodeLengthBox.GetTextBox(0).Box.Text);

            MainUtils.SetPLCBarCodeSelfLoopingModeEnabled(plcToggle);
            MainUtils.SetPLCBarCodeStartAddress(plcAddr);
            MainUtils.SetPLCBarCodeLength(plcLen);

            // 修改初始值
            _plcBarCodeSelfLoopingOriginal = plcToggle;
            if (_plcBarCodeSelfLoopingOriginal) {
                _plcBarCodeStartAddressOriginal = plcAddr;
                _plcBarCodeLengthOriginal = plcLen;
            } else {
                _plcBarCodeStartAddressOriginal = 0;
                _plcBarCodeLengthOriginal = 0;
            }
        }

        protected override string? CheckBeforeSave() {
            string? errorTemp = base.CheckBeforeSave();
            if (!string.IsNullOrEmpty(errorTemp)) {
                return errorTemp;
            }

            bool plcToggle = _plcBarCodeSelfLoopingToggle.Checked;
            if (plcToggle) {
                int plcAddr = int.Parse(_plcBarCodeStartAddressBox.GetTextBox(0).Box.Text);
                if (plcAddr <= 0) {
                    return "PLC条码起始地址不能等于0";
                }

                int plcLen = int.Parse(_plcBarCodeLengthBox.GetTextBox(0).Box.Text);
                if (plcLen <= 0) {
                    return "PLC条码长度不能等于0";
                }
            }

            return null;
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1;

            _plcBarCodeSelfLoopingToggle.Size = new(boxWidth, BoxNBtnHeight);
            _plcBarCodeSelfLoopingToggle.Margin = new(0, boxVMargin, 0, 0);
            _plcBarCodeStartAddressBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcBarCodeStartAddressBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _plcBarCodeLengthBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcBarCodeLengthBox.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Height += BoxNBtnHeight + BoxNBtnHeight / 2;
            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _plcBarCodeSelfLoopingOriginal = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                    _plcBarCodeStartAddressOriginal = MainUtils.GetPLCBarCodeStartAddress();
                    _plcBarCodeLengthOriginal = MainUtils.GetPLCBarCodeLength();
                    _plcBarCodeSelfLoopingToggle.Checked = _plcBarCodeSelfLoopingOriginal;
                    if (_plcBarCodeSelfLoopingToggle.Checked) {
                        _plcBarCodeStartAddressBox.SetValue(0, _plcBarCodeStartAddressOriginal + "");
                        _plcBarCodeLengthBox.SetValue(0, _plcBarCodeLengthOriginal + "");
                    } else {
                        _plcBarCodeStartAddressBox.SetValue(0, "0");
                        _plcBarCodeLengthBox.SetValue(0, "0");
                    }
                    _plcBarCodeStartAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                    _plcBarCodeLengthBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _plcBarCodeSelfLoopingToggle.Checked = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                    _plcBarCodeStartAddressBox.SetValue(0, MainUtils.GetPLCBarCodeStartAddress() + "");
                    _plcBarCodeLengthBox.SetValue(0, MainUtils.GetPLCBarCodeLength() + "");
                });
            });
        }
    }
}
