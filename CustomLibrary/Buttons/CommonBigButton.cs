using CustomLibrary.Configs;

namespace CustomLibrary.Buttons {
    public class CommonBigButton: CommonButton {
        protected override void ResizeTextLabel() {
            if (this.Label != null && Height > 0) {
                int size = (int) (Height * .275);
                if (size > 0) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel);
                }
            }
        }

    }
}
