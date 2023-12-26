using CustomLibrary.Buttons.AbstractClasses;

namespace CustomLibrary.Buttons.BaseClasses {
    public class CommonButtonBase: AbstractCustomButton {

        public CommonButtonBase() : base() {
            BackColor = ColorTranslator.FromHtml("#E86C10");
            ForeColor = ColorTranslator.FromHtml("#FEFEFE");
        }

        protected override void ResizeTextLabel() {}

        protected override void PaintAfter(PaintEventArgs e) {
            // Draw text
            if (Label != null) {
                e.Graphics.DrawString(Label, Font, new SolidBrush(ForeColor), new Point(LabelX, LabelY) + ExtraSize);
            }
        }
    }
}
