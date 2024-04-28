using CustomLibrary.Buttons.AbstractClasses;

namespace CustomLibrary.Buttons.BaseClasses {
    public class CommonButtonBase: AbstractCustomButton {

        public CommonButtonBase() : base() {
            BackColor = ColorTranslator.FromHtml("#E86C10");
            ForeColor = ColorTranslator.FromHtml("#FEFEFE");
        }

        protected override void ResizeTextLabel() { }

        protected override void PaintAfter(PaintEventArgs e) {
            // Draw text
            if (Label != null) {
                if (LabelX != null && LabelY != null) {
                    e.Graphics.DrawString(Label, Font, new SolidBrush(ForeColor), new Point(LabelX.Value, LabelY.Value) + ExtraSize);
                } else {
                    StringFormat format = new() {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                    };
                    e.Graphics.DrawString(Label, Font, new SolidBrush(ForeColor), new Point((int) Math.Round(Width / 2.01F), (int) Math.Round(Height / 1.925F)) + ExtraSize, format);
                }
            }
        }
    }
}
