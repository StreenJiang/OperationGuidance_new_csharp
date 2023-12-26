using CustomLibrary.Panels;

namespace OperationGuidance_new.Views {
    public class EventLogView: CustomContentPanel {
        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
