using CustomLibrary.Configs;
using CustomLibrary.Panels.BaseClasses;

namespace OperationGuidance_new.Views {
    public class OperatorView: Panel {
        #region Constructors
        public OperatorView() {
            WorkplaceMissionView workplaceView = new(true);
            CustomVScrollingContentPanel outerScrollingPanel = new(ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, workplaceView);
        }
        #endregion
    }
}
