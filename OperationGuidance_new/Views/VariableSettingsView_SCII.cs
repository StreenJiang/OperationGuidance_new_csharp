using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
        }

        protected override void ResizeMissionSettings() {
            base.ResizeMissionSettings();

            Padding margin = UsbScannerEnabledToggle.Margin;
            margin.Left = margin.Right;
            margin.Right = 0;
            UsbScannerEnabledToggle.Margin = margin;


            // Resize title
            int boxVMargin = BoxNBtnHeight / 2;
            // Resize Content
            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 2 + ContentVPadding * 2 + boxVMargin * 1);
            // Resize outer panel
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height);
        }
    }
}
