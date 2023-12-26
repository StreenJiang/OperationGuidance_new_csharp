using CustomLibrary.Panels;

namespace OperationGuidance_new.Views {
    public class DeviceManagementView: CustomContentPanel {
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
