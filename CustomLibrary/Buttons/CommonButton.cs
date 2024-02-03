using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;

namespace CustomLibrary.Buttons {
    public class CommonButton: CommonButtonBase {
        protected override void ResizeTextLabel() {
            if (this.Label != null && IsHandleCreated) {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .55), FontStyle.Bold, GraphicsUnit.Pixel);
                using (Graphics g = CreateGraphics()) {
                    this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                }
                this.LabelY = (this.Height - this.Font.Height) / 2;
            }
        }
    }
}
