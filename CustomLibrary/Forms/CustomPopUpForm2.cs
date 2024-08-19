using CustomLibrary.Utils;

namespace CustomLibrary.Forms {
    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomPopUpForm2: CustomPopUpForm {

        public override void CalculateDetailProperties() {
            TitlePanel.Height = GetTitlePanelHeight();
            ButtonsPanel.Padding = GetButtonsPanelPadding();
            ButtonsPanel.Height = GetButtonsPanelHeight();
            ContentOuterPanel.Padding = GetContentPadding();
        }

        protected override void AfterSizeChanged(object? sender, EventArgs eventArgs) {
            if (WidgetUtils.MainForm == null || WidgetUtils.MainForm.IsDisposed || PopUpFormBackboard.IsDisposed) {
                return;
            }
            PopUpFormBackboard.Size = Size;
        }

        protected override void OnLocationChanged(EventArgs e) {
            base.OnLocationChanged(e);
            PopUpFormBackboard.Location = Location;
        }
    }
}
