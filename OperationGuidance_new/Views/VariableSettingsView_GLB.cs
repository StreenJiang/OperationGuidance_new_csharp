using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Utils;
using S7.Net;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_GLB: AVariableSettingsView {
        private ToggleButtonGroup _plcBarCodeSelfLoopingToggle;
        private bool _plcBarCodeSelfLoopingOriginal;
        private CustomComboBoxButtonGroup<string> _plcModelComboBox;
        private string _plcModelOriginal;
        private CustomTextBoxButtonGroup _plcDBAddressBox;
        private int _plcDBAddressOriginal;
        private CustomTextBoxButtonGroup _plcDBRegisterNoBox;
        private string _plcDBRegisterNoOriginal;
        private CustomTextBoxButtonGroup _plcDBBitAddressBox;
        private int _plcDBBitAddressOriginal;
        private CustomTextBoxButtonGroup _plcBarCodeLengthBox;
        private int _plcBarCodeLengthOriginal;

        public ToggleButtonGroup PLCBarCodeSelfLoopingToggle { get => _plcBarCodeSelfLoopingToggle; set => _plcBarCodeSelfLoopingToggle = value; }
        public bool PLCBarCodeSelfLoopingModeOriginal { get => _plcBarCodeSelfLoopingOriginal; set => _plcBarCodeSelfLoopingOriginal = value; }
        public CustomComboBoxButtonGroup<string> PlcModelComboBox { get => _plcModelComboBox; set => _plcModelComboBox = value; }
        public string PlcModelOriginal { get => _plcModelOriginal; set => _plcModelOriginal = value; }
        public CustomTextBoxButtonGroup PLCDBAddressBox { get => _plcDBAddressBox; set => _plcDBAddressBox = value; }
        public int PLCDBAddressOriginal { get => _plcDBAddressOriginal; set => _plcDBAddressOriginal = value; }
        public CustomTextBoxButtonGroup PLCDBRegisterNoBox { get => _plcDBRegisterNoBox; set => _plcDBRegisterNoBox = value; }
        public string PLCDBRegisterNoOriginal { get => _plcDBRegisterNoOriginal; set => _plcDBRegisterNoOriginal = value; }
        public CustomTextBoxButtonGroup PLCDBBitAddressBox { get => _plcDBBitAddressBox; set => _plcDBBitAddressBox = value; }
        public int PLCDBBitAddressOriginal { get => _plcDBBitAddressOriginal; set => _plcDBBitAddressOriginal = value; }
        public CustomTextBoxButtonGroup PLCBarCodeLengthBox { get => _plcBarCodeLengthBox; set => _plcBarCodeLengthBox = value; }
        public int PLCBarCodeLengthOriginal { get => _plcBarCodeLengthOriginal; set => _plcBarCodeLengthOriginal = value; }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && !(_plcBarCodeSelfLoopingToggle.Checked != _plcBarCodeSelfLoopingOriginal
                || _plcDBAddressBox.GetTextBox(0).Box.Text != _plcDBAddressOriginal + ""
                || _plcBarCodeLengthBox.GetTextBox(0).Box.Text != _plcBarCodeLengthOriginal + ""
                );

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            _plcBarCodeSelfLoopingToggle = new("PLC条码自循环") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            _plcModelComboBox = new("PLC型号") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            foreach (string model in Enum.GetNames<CpuType>()) {
                _plcModelComboBox.AddItem(model, model);
            }
            _plcDBAddressBox = new("PLC_DB地址") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
                PositiveIntOnly = true,
            };
            _plcDBRegisterNoBox = new("PLC寄存器号") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            _plcDBBitAddressBox = new("PLC位") {
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
                _plcDBAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                _plcModelComboBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                _plcDBRegisterNoBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                _plcDBBitAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                _plcBarCodeLengthBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;

                if (_plcBarCodeSelfLoopingToggle.Checked) {
                    _plcDBAddressBox.SetValue(0, _plcDBAddressOriginal + "");
                    _plcModelComboBox.SetCurrent(_plcModelComboBox.IndexOf(_plcModelOriginal));
                    _plcDBRegisterNoBox.SetValue(0, _plcDBRegisterNoOriginal);
                    _plcDBBitAddressBox.SetValue(0, _plcDBBitAddressOriginal + "");
                    _plcBarCodeLengthBox.SetValue(0, _plcBarCodeLengthOriginal + "");

                    // Uncheck 'SelfLoopingMode'
                    if (MissionSelfLoopingModeToggle.Checked) {
                        MissionSelfLoopingModeToggle.Checked = false;
                    }
                } else {
                    _plcDBAddressBox.SetValue(0, "0");
                    _plcModelComboBox.Reset();
                    _plcDBRegisterNoBox.SetValue(0, "");
                    _plcDBBitAddressBox.SetValue(0, "0");
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
            int plcAddr;
            string plcModel;
            string plcRgiNo;
            int plcBitAddr;
            int plcLen;
            if (plcToggle) {
                plcAddr = int.Parse(_plcDBAddressBox.GetTextBox(0).Box.Text);
                plcModel = CommonUtils.CannotBeNull(_plcModelComboBox.Value);
                plcRgiNo = _plcDBRegisterNoBox.GetTextBox(0).Box.Text;
                plcBitAddr = int.Parse(_plcDBBitAddressBox.GetTextBox(0).Box.Text);
                plcLen = int.Parse(_plcBarCodeLengthBox.GetTextBox(0).Box.Text);
            } else {
                plcAddr = 0;
                plcModel = "";
                plcRgiNo = "";
                plcBitAddr = 0;
                plcLen = 0;
            }

            MainUtils.SetPLCBarCodeSelfLoopingModeEnabled(plcToggle);
            MainUtils.SetPLCDBAddress(plcAddr);
            MainUtils.SetPLCModel(plcModel);
            MainUtils.SetPLCDBRegisterNo(plcRgiNo);
            MainUtils.SetPLCDBBitAddress(plcBitAddr);
            MainUtils.SetPLCBarCodeLength(plcLen);

            // 修改初始值
            _plcBarCodeSelfLoopingOriginal = plcToggle;
            if (_plcBarCodeSelfLoopingOriginal) {
                _plcDBAddressOriginal = plcAddr;
                _plcModelOriginal = plcModel;
                _plcDBRegisterNoOriginal = plcRgiNo;
                _plcDBBitAddressOriginal = plcBitAddr;
                _plcBarCodeLengthOriginal = plcLen;
            } else {
                _plcDBAddressOriginal = 0;
                _plcModelOriginal = "";
                _plcDBRegisterNoOriginal = "";
                _plcDBBitAddressOriginal = 0;
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
                int plcAddr = int.Parse(_plcDBAddressBox.GetTextBox(0).Box.Text);
                if (plcAddr <= 0) {
                    _plcDBAddressBox.CheckError(0, true);
                    return "PLC_DB地址不能等于0";
                }

                int plcLen = int.Parse(_plcBarCodeLengthBox.GetTextBox(0).Box.Text);
                if (plcLen <= 0) {
                    _plcBarCodeLengthBox.CheckError(0, true);
                    return "PLC条码长度不能等于0";
                }

                string? plcModel = _plcModelComboBox.Value;
                if (_plcModelComboBox.IsDefaultValue() || string.IsNullOrEmpty(plcModel)) {
                    _plcModelComboBox.CheckError(true);
                    return "PLC型号不能为空";
                }

                string plcRgiNo = _plcDBRegisterNoBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(plcRgiNo)) {
                    _plcDBRegisterNoBox.CheckError(0, true);
                    return "PLC型号不能为空";
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
            _plcBarCodeSelfLoopingToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _plcModelComboBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcModelComboBox.Margin = new(0, boxVMargin, 0, 0);
            _plcDBAddressBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcDBAddressBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _plcDBRegisterNoBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcDBRegisterNoBox.Margin = new(0, boxVMargin, 0, 0);
            _plcDBBitAddressBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcDBBitAddressBox.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _plcBarCodeLengthBox.Size = new(boxWidth, BoxNBtnHeight);
            _plcBarCodeLengthBox.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Height += (BoxNBtnHeight + boxVMargin) * 3;
            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _plcBarCodeSelfLoopingOriginal = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                    _plcDBAddressOriginal = MainUtils.GetPLCDBAddress();
                    _plcModelOriginal = MainUtils.GetPLCModel();
                    _plcDBRegisterNoOriginal = MainUtils.GetPLCDBRegisterNo();
                    _plcDBBitAddressOriginal = MainUtils.GetPLCDBBitAddress();
                    _plcBarCodeLengthOriginal = MainUtils.GetPLCBarCodeLength();
                    _plcBarCodeSelfLoopingToggle.Checked = _plcBarCodeSelfLoopingOriginal;
                    if (_plcBarCodeSelfLoopingToggle.Checked) {
                        _plcDBAddressBox.SetValue(0, _plcDBAddressOriginal + "");
                        _plcModelComboBox.SetCurrent(_plcModelComboBox.IndexOf(_plcModelOriginal));
                        _plcDBRegisterNoBox.SetValue(0, _plcDBRegisterNoOriginal);
                        _plcDBBitAddressBox.SetValue(0, _plcDBBitAddressOriginal + "");
                        _plcBarCodeLengthBox.SetValue(0, _plcBarCodeLengthOriginal + "");
                    } else {
                        _plcDBAddressBox.SetValue(0, "0");
                        _plcModelComboBox.Reset();
                        _plcDBRegisterNoBox.SetValue(0, "");
                        _plcDBBitAddressBox.SetValue(0, "0");
                        _plcBarCodeLengthBox.SetValue(0, "0");
                    }
                    _plcDBAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                    _plcModelComboBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                    _plcDBRegisterNoBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                    _plcDBBitAddressBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                    _plcBarCodeLengthBox.Enabled = _plcBarCodeSelfLoopingToggle.Checked;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _plcBarCodeSelfLoopingToggle.Checked = MainUtils.IsPLCBarCodeSelfLoopingEnabled();
                    _plcModelComboBox.SetCurrent(_plcModelComboBox.IndexOf(MainUtils.GetPLCModel()));
                    _plcDBAddressBox.SetValue(0, MainUtils.GetPLCDBAddress() + "");
                    _plcDBRegisterNoBox.SetValue(0, MainUtils.GetPLCDBRegisterNo());
                    _plcDBBitAddressBox.SetValue(0, MainUtils.GetPLCDBBitAddress() + "");
                    _plcBarCodeLengthBox.SetValue(0, MainUtils.GetPLCBarCodeLength() + "");
                });
            });
        }
    }
}
