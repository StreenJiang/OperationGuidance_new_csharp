using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_SCII: AVariableSettingsView {
        public VariableSettingsView_SCII() {
            MissionSelfLoopingModeToggle.Hide();
            StoreLooseningDataToggle.Hide();
            AutoLockToolToggle.Hide();
            EnableArmLocatingToggle.Hide();
            ArmLocatingAccuracyBox.Hide();
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
            int boxVMargin = BoxNBtnHeight / 2;
            // Resize box
            UsbScannerEnabledToggle.Margin = new(0, 0, 0, 0);
            // Resize Content
            WorkContentPanel.Size = new(Width, BoxNBtnHeight * 1 + ContentVPadding * 2 + boxVMargin * 0);
            // Resize outer panel
            WorkPanel.Size = new(Width, WorkTitlePanel.Height + WorkContentPanel.Height);
        }
    }
}
