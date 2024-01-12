using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;

namespace CustomLibrary.Buttons {
    public class CommonBigButton: CommonButtonBase {
        protected override void ResizeTextLabel() {
            if (this.Label != null) {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, this.Width / 30 + this.Height / 9 + 1.25F, FontStyle.Bold);
                using (Graphics g = CreateGraphics()) {
                    this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                }
                this.LabelY = (this.Height - this.Font.Height) / 2;
            }
        }

    }
}
