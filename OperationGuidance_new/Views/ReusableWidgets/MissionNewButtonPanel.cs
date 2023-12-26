using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MissionNewButtonPanel: Panel {
        private CommonBigButton _addNewButton;

        public MissionNewButtonPanel() {
            _addNewButton = new() {
                Label = "点击添加任务",
                Parent = this,
            };
            _addNewButton.Click += (sender, eventArgs) => {
                // 点击按钮后跳转至任务编辑界面并新增一个任务
                MissionEditionView editionView = WidgetUtils.GetView<MissionEditionView>();
                CustomMainMenuButton missionManagementButton = WidgetUtils.GetMainMenu(100);
                editionView.OpenEditionPage(new() {
                    name = "任务名称",
                    ProductSides = new(),
                });
                CommonUtils.CannotBeNull(editionView.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
                missionManagementButton.TriggerClick(EventArgs.Empty);
            };
        }

        protected override void OnSizeChanged(EventArgs e) {
            _addNewButton.Size = new((int) (Width * 0.2), (int) (Height * 0.13));
            _addNewButton.Location = new((Width - _addNewButton.Width) / 2, (Height - _addNewButton.Height) / 2);
        }
    }
}
