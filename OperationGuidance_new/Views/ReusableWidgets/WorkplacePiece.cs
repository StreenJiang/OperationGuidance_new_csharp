using CustomLibrary.Panels;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class WorkplacePiece: CustomContentPanel {
        private Rectangle? _borderRect;
        private Color? _outerPenBorderColor;

        public Color? OuterPenBorderColor {
            get => _outerPenBorderColor;
            set => _outerPenBorderColor = value;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (OuterPenBorderColor != null) {
                // Recalcuate rectangle
                _borderRect = new(0, 0, Width - 1, Height - 1);
                if (Visible) {
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_borderRect != null && OuterPenBorderColor != null) {
                e.Graphics.Clear(BackColor);
                e.Graphics.DrawRectangle(new Pen(OuterPenBorderColor.Value, 1), _borderRect.Value);
            }
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}

