using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Panels {
    public class CustomContentPanel: CustomContentPanelBase {
        private Rectangle? _innerBorderRect;
        private Color? _penBorderColor;
        private bool _autoPadding;
        private bool _paddingWithoutBorder;
        private Func<int, bool>? _onCheckNeedsScrollBar;

        public Color? PenBorderColor { get => _penBorderColor; set => _penBorderColor = value; }
        public bool AutoPadding { get => _autoPadding; set => _autoPadding = value; }
        public bool PaddingWithoutBorder { get => _paddingWithoutBorder; set => _paddingWithoutBorder = value; }

        public event Func<int, bool>? OnCheckNeedsScrollBar { add => _onCheckNeedsScrollBar += value; remove => _onCheckNeedsScrollBar -= value; }

        public CustomContentPanel() {
            _autoPadding = true;
            _paddingWithoutBorder = false;
        }

        protected override void DoAfterResizing(object? sender, EventArgs eventArgs) {
            int padding = WidgetUtils.ContentInnerBorderMargin();
            if (_paddingWithoutBorder) {
                Padding = new(padding * 2);
            } else if (_penBorderColor != null) {
                // Recalcuate rectangle (1 more pixel for border thickness
                _innerBorderRect = new(padding, padding, Width - padding * 2 - 1, Height - padding * 2 - 1);
                if (Visible) {
                    Invalidate();
                }
                if (_autoPadding) {
                    Padding = new(padding * 2 + 1);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_innerBorderRect != null && _penBorderColor != null) {
                e.Graphics.Clear(BackColor);
                e.Graphics.DrawRectangle(new Pen(_penBorderColor.Value, 1), _innerBorderRect.Value);
            }
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) => false || (_onCheckNeedsScrollBar != null && _onCheckNeedsScrollBar(parentNewHeight));
    }
}
