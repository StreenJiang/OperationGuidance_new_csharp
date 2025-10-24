using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MissionNewButtonPanel: Panel {
        private CommonBigButton _addNewButton;

        public MissionNewButtonPanel() {
            _addNewButton = new() {
                Label = "点击添加任务",
                Parent = this,
                Size = new(100, 50),
            };
            _addNewButton.Click += (sender, eventArgs) => {
                if (MainUtils.License.MenuIds.ContainsKey(100)) {
                    // 点击按钮后跳转至任务编辑界面并新增一个任务
                    AppVersion appVersion = (AppVersion) Enum.Parse(typeof(AppVersion), MainUtils.License.AppVersion);
                    switch (appVersion) {
                        default:
                        case AppVersion.STANDARD:
                            MissionEditionView editionView = WidgetUtils.GetView<MissionEditionView>();
                            CustomMainMenuButton missionManagementButton = WidgetUtils.GetMainMenu(100);
                            if (editionView.EditionPage == null || !editionView.EditionPage.Modified || WidgetUtils.ShowConfirmPopUp("编辑界面存在未保存内容，是否打开新的界面？")) {
                                editionView.OpenEditionPage(null);
                                CommonUtils.CannotBeNull(editionView.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
                                missionManagementButton.TriggerClick(EventArgs.Empty);
                            }
                            break;
                        case AppVersion.SCII:
                            MissionEditionView_SCII editionView_scii = WidgetUtils.GetView<MissionEditionView_SCII>();
                            CustomMainMenuButton missionManagementButton_scii = WidgetUtils.GetMainMenu(100);
                            if (editionView_scii.EditionPage == null || !editionView_scii.EditionPage.Modified || WidgetUtils.ShowConfirmPopUp("编辑界面存在未保存内容，是否打开新的界面？")) {
                                editionView_scii.OpenEditionPage(null);
                                CommonUtils.CannotBeNull(editionView_scii.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
                                missionManagementButton_scii.TriggerClick(EventArgs.Empty);
                            }
                            break;
                        case AppVersion.SCII_XT:
                            MissionEditionView_SCII_XT editionView_scii_xt = WidgetUtils.GetView<MissionEditionView_SCII_XT>();
                            CustomMainMenuButton missionManagementButton_scii_xt = WidgetUtils.GetMainMenu(100);
                            if (editionView_scii_xt.EditionPage == null || !editionView_scii_xt.EditionPage.Modified || WidgetUtils.ShowConfirmPopUp("编辑界面存在未保存内容，是否打开新的界面？")) {
                                editionView_scii_xt.OpenEditionPage(null);
                                CommonUtils.CannotBeNull(editionView_scii_xt.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
                                missionManagementButton_scii_xt.TriggerClick(EventArgs.Empty);
                            }
                            break;
                    }
                } else {
                    WidgetUtils.ShowWarningPopUp("没有找到任务编辑界面，请确认许可证信息无误");
                }
            };
        }

        protected override void OnSizeChanged(EventArgs e) {
            _addNewButton.Height = (int) (Height * 0.14);
            int newWidth = WidgetUtils.MeasureString(_addNewButton.Label, _addNewButton.Font).Width + _addNewButton.Height * 2;
            _addNewButton.Width = newWidth;
            _addNewButton.Location = new((Width - _addNewButton.Width) / 2, (Height - _addNewButton.Height) / 2);
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
        }
    }
}
