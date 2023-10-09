using CustomLibrary.Panels;

namespace OperationGuidance_new.Views {
    public class UserInfoView: CustomContentPanel {
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
