using CustomLibrary.Buttons;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_WHYC: AVariableSettingsView {
        private ToggleButtonGroup _mesEnabledToggle;
        private bool _mesEnabledOriginal;
        private CustomTextBoxButtonGroup _getMatCodeApiBox;
        private string _getMatCodeApiOriginal;
        private CustomTextBoxButtonGroup _uploadDataApiBox;
        private string _uploadDataApiOriginal;

        public ToggleButtonGroup MesEnabledToggle { get => _mesEnabledToggle; set => _mesEnabledToggle = value; }
        public bool MesEnabledOriginal { get => _mesEnabledOriginal; set => _mesEnabledOriginal = value; }
        public CustomTextBoxButtonGroup GetMatCodeApiBox { get => _getMatCodeApiBox; set => _getMatCodeApiBox = value; }
        public string GetMatCodeApiOriginal { get => _getMatCodeApiOriginal; set => _getMatCodeApiOriginal = value; }
        public CustomTextBoxButtonGroup UploadDataApiBox { get => _uploadDataApiBox; set => _uploadDataApiBox = value; }
        public string UploadDataApiOriginal { get => _uploadDataApiOriginal; set => _uploadDataApiOriginal = value; }

        protected override bool CheckSavedFunc() => base.CheckSavedFunc()
            || MesEnabledToggle.Checked != _mesEnabledOriginal
            || GetMatCodeApiBox.GetTextBox(0).Box.Text != _getMatCodeApiOriginal + ""
            || UploadDataApiBox.GetTextBox(0).Box.Text != _uploadDataApiOriginal + "";

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            MesEnabledToggle = new("是否对接MES") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            GetMatCodeApiBox = new("MatCode接口") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            UploadDataApiBox = new("上传数据接口") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };

            MesEnabledToggle.CheckedChanged += (s, e) => {
                GetMatCodeApiBox.Enabled = MesEnabledToggle.Checked;
                UploadDataApiBox.Enabled = MesEnabledToggle.Checked;

                if (MesEnabledToggle.Checked) {
                    GetMatCodeApiBox.SetValue(0, _getMatCodeApiOriginal + "");
                    UploadDataApiBox.SetValue(0, UploadDataApiOriginal);

                    // Uncheck 'SelfLoopingMode'
                    if (MissionSelfLoopingModeToggle.Checked) {
                        MissionSelfLoopingModeToggle.Checked = false;
                    }
                } else {
                    GetMatCodeApiBox.SetValue(0, "");
                    UploadDataApiBox.SetValue(0, "");
                }
            };

            MissionSelfLoopingModeToggle.CheckedChanged += (s, e) => {
                if (MissionSelfLoopingModeToggle.Checked && MesEnabledToggle.Checked) {
                    MesEnabledToggle.Checked = false;
                }
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            bool mesEnabled = MesEnabledToggle.Checked;
            string matCodeApi;
            string uploadDataApi;
            if (mesEnabled) {
                matCodeApi = GetMatCodeApiBox.GetTextBox(0).Box.Text;
                uploadDataApi = UploadDataApiBox.GetTextBox(0).Box.Text;
            } else {
                matCodeApi = "";
                uploadDataApi = "";
            }

            MainUtils.SetMESModeEnabled(mesEnabled);
            MainUtils.SetMatCodeApi(matCodeApi);
            MainUtils.SetUploadDataApi(uploadDataApi);

            // 修改初始值
            _mesEnabledOriginal = mesEnabled;
            if (_mesEnabledOriginal) {
                _getMatCodeApiOriginal = matCodeApi;
                UploadDataApiOriginal = uploadDataApi;
            } else {
                _getMatCodeApiOriginal = "";
                UploadDataApiOriginal = "";
            }
        }

        protected override string? CheckBeforeSave() {
            string? errorTemp = base.CheckBeforeSave();
            if (!string.IsNullOrEmpty(errorTemp)) {
                return errorTemp;
            }

            bool mesEnabled = MesEnabledToggle.Checked;
            if (mesEnabled) {
                string matCodeApi = GetMatCodeApiBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(matCodeApi)) {
                    GetMatCodeApiBox.CheckError(0, true);
                    return "获取MatCode的接口不能为空";
                }

                string uploadDataApi = UploadDataApiBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(uploadDataApi)) {
                    UploadDataApiBox.CheckError(0, true);
                    return "上传数据至MES的接口不能为空";
                }
            }

            return null;
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1;

            MesEnabledToggle.Size = new(boxWidth, BoxNBtnHeight);
            MesEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            GetMatCodeApiBox.Size = new(boxWidth, BoxNBtnHeight);
            GetMatCodeApiBox.Margin = new(0, boxVMargin, 0, 0);
            UploadDataApiBox.Size = new(boxWidth, BoxNBtnHeight);
            UploadDataApiBox.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Height += (BoxNBtnHeight + boxVMargin) * 2;
            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _mesEnabledOriginal = MainUtils.IsMESEnabled();
                    _getMatCodeApiOriginal = MainUtils.GetMatCodeApi();
                    UploadDataApiOriginal = MainUtils.GetUploadDataApi();
                    MesEnabledToggle.Checked = _mesEnabledOriginal;
                    if (MesEnabledToggle.Checked) {
                        GetMatCodeApiBox.SetValue(0, _getMatCodeApiOriginal + "");
                        UploadDataApiBox.SetValue(0, UploadDataApiOriginal);
                    } else {
                        GetMatCodeApiBox.SetValue(0, "");
                        UploadDataApiBox.SetValue(0, "");
                    }
                    GetMatCodeApiBox.Enabled = MesEnabledToggle.Checked;
                    UploadDataApiBox.Enabled = MesEnabledToggle.Checked;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    MesEnabledToggle.Checked = MainUtils.IsMESEnabled();
                    GetMatCodeApiBox.SetValue(0, MainUtils.GetMatCodeApi() + "");
                    UploadDataApiBox.SetValue(0, MainUtils.GetUploadDataApi());
                });
            });
        }
    }
}
