using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_TZYX: AVariableSettingsView {
        public VariableSettingsView_TZYX() { }

        protected override void SaveBtnMouseUp(object? sender, MouseEventArgs e) {
            // Check can save storage settings first
            string? error = CheckBeforeSave();
            if (!string.IsNullOrEmpty(error)) {
                WidgetUtils.ShowErrorPopUp(error);
            } else {
                SaveStorageSettings();
                SaveSystemSettings();
                SaveMissionSettings();
                WidgetUtils.ShowNoticePopUp("保存成功");

                if (AutoLockToolToggle.Checked) {
                    MainUtils.ToolTasks.Values.ToList().ForEach(tool => tool.ForceSendLock());
                } else {
                    MainUtils.ToolTasks.Values.ToList().ForEach(tool => tool.ForceSendUnlock());
                }
            }
        }
    }
}
