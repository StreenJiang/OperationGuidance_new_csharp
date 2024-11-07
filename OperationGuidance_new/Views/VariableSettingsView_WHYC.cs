using CustomLibrary.Buttons;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_WHYC: AVariableSettingsView {
        private CustomTextBoxButtonGroup _getMatCodeApiBox;
        private string _getMatCodeApiOriginal;
        private CustomTextBoxButtonGroup _uploadDataApiBox;
        private string _uploadDataApiOriginal;

        public CustomTextBoxButtonGroup GetMatCodeApiBox { get => _getMatCodeApiBox; set => _getMatCodeApiBox = value; }
        public string GetMatCodeApiOriginal { get => _getMatCodeApiOriginal; set => _getMatCodeApiOriginal = value; }
        public CustomTextBoxButtonGroup UploadDataApiBox { get => _uploadDataApiBox; set => _uploadDataApiBox = value; }
        public string UploadDataApiOriginal { get => _uploadDataApiOriginal; set => _uploadDataApiOriginal = value; }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && !(
                CheckSvedFuncSeparately(GetMatCodeApiBox.GetTextBox(0).Box.Text != _getMatCodeApiOriginal + "", "获取MatCode接口RUL")
                || CheckSvedFuncSeparately(UploadDataApiBox.GetTextBox(0).Box.Text != _uploadDataApiOriginal + "", "上传数据接口URL")
            );

        protected override void InitializeMissionSettings() {
            base.InitializeMissionSettings();

            GetMatCodeApiBox = new("MatCode接口") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
            UploadDataApiBox = new("上传数据接口") {
                Parent = WorkContentPanel,
                Ratio = 6.95,
            };
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            string matCodeApi = GetMatCodeApiBox.GetTextBox(0).Box.Text;
            string uploadDataApi = UploadDataApiBox.GetTextBox(0).Box.Text;

            MainUtils.SetMatCodeApi(matCodeApi);
            MainUtils.SetUploadDataApi(uploadDataApi);

            // 修改初始值
            _getMatCodeApiOriginal = matCodeApi;
            UploadDataApiOriginal = uploadDataApi;
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1;

            // // Resize parent settings
            // UsbScannerEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);

            GetMatCodeApiBox.Size = new(boxWidth, BoxNBtnHeight);
            GetMatCodeApiBox.Margin = new(0, boxVMargin, 0, 0);
            UploadDataApiBox.Size = new(boxWidth, BoxNBtnHeight);
            UploadDataApiBox.Margin = new(0, boxVMargin, 0, 0);

            WorkContentPanel.Height += BoxNBtnHeight + boxVMargin;
            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _getMatCodeApiOriginal = MainUtils.GetMatCodeApi();
                    UploadDataApiOriginal = MainUtils.GetUploadDataApi();
                    GetMatCodeApiBox.SetValue(0, _getMatCodeApiOriginal + "");
                    UploadDataApiBox.SetValue(0, UploadDataApiOriginal);
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    GetMatCodeApiBox.SetValue(0, MainUtils.GetDefaultMatCodeApi() + "");
                    UploadDataApiBox.SetValue(0, MainUtils.GetDefaultUploadDataApi());
                });
            });
        }
    }
}
