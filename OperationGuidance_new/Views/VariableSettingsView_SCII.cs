using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        private ToggleButtonGroup _buzzerEnabledToggle;
        private CommonButtonGroup _buzzerTestButtons;
        private bool _buzzerEnabledOriginal;

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
                BuzzerController.TurnOn();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已启动");
            };
            _buzzerTestButtons.AddButton("测试关").MouseUp += (s, e) => {
                BuzzerController.TurnOff();
                WidgetUtils.ShowNoticePopUp("蜂鸣器已关闭");
            };
            _buzzerEnabledToggle.CheckedChanged += (s, e) => {
                _buzzerTestButtons.Enabled = _buzzerEnabledToggle.Checked;
            };
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

            Padding margin = UsbScannerEnabledToggle.Margin;
            margin.Left = margin.Right;
            margin.Right = 0;
            UsbScannerEnabledToggle.Margin = margin;


            // Resize title
            int boxWidth = (Width - ContentHPadding * 3) / 2;
            int boxVMargin = BoxNBtnHeight / 2;
            // Resize box
            ErrorPromptForArmToggle.Margin = new(0, 0, ContentHGap / 2, 0);
            UsbScannerEnabledToggle.Margin = new(0, 0, 0, 0);

            _buzzerEnabledToggle.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerEnabledToggle.Margin = new(0, boxVMargin, ContentHGap / 2, 0);
            _buzzerTestButtons.Size = new(boxWidth, BoxNBtnHeight);
            _buzzerTestButtons.Margin = new(0, boxVMargin, 0, 0);

            // Resize Content
            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1);
            // Resize outer panel
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height);
        }

        protected override void SaveMissionSettings() {
            base.SaveMissionSettings();

            bool buzzerEnabled = _buzzerEnabledToggle.Checked;

            MainUtils.SetBuzzerEnabled(buzzerEnabled);

            // 修改初始值
            _buzzerEnabledOriginal = buzzerEnabled;
        }

        protected override async void LoadSettings() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.LoadSettings();

                    _buzzerEnabledOriginal = MainUtils.IsBuzzerEnabled();
                    _buzzerEnabledToggle.Checked = _buzzerEnabledOriginal;
                });
            });
        }

        protected override async void ResetAllToDefault() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    base.ResetAllToDefault();

                    _buzzerEnabledToggle.Checked = MainUtils.DefaultIsBuzzerEnabled();
                });
            });
        }

        protected override bool CheckSavedFunc_detail() => base.CheckSavedFunc_detail()
            && _buzzerEnabledToggle.Checked == _buzzerEnabledOriginal;
    }
}
