using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            WorkContentPanel.Height -= BoxNBtnHeight + BoxNBtnHeight / 2;
            WorkPanel.Height = WorkTitlePanel.Height + WorkContentPanel.Height;
        }
    }
}
