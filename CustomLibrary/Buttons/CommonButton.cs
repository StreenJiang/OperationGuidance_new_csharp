using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class CommonButton: CommonButtonBase {
        public CommonButton() {
            BlockHoverUp = true;
            BlockHoverDown = true;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            ConerRadius = WidgetUtils.ControlRadius();
        }

        protected override void ResizeTextLabel() {
            if (Label != null && Height > 0) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .425), FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }
    }
}
