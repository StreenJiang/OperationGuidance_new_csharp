using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Panels {
    public class CustomContentPanel: CustomContentPanelBase {
        private Rectangle? _innerBorderRect;
        private Color? _penBorderColor;
        private bool _autoPadding;
        
        public Color? PenBorderColor {
            get => _penBorderColor;
            set => _penBorderColor = value;
        }
        public bool AutoPadding { get => _autoPadding; set => _autoPadding = value; }

        public CustomContentPanel() {
            _autoPadding = true;
        }

        protected override void DoAfterResizing(object? sender, EventArgs eventArgs) {
            if (_penBorderColor != null) {
                int padding = 0;
                if (_autoPadding) {
                    padding = WidgetUtils.ContentPadding(this.TopLevelControl.Width, this.TopLevelControl.Height);
                }
                // Recalcuate rectangle
                _innerBorderRect = new(padding, padding, this.Width - padding * 2 - 1, this.Height - padding * 2 - 1);
                if (this.Visible) {
                    this.Invalidate();
                }
                if (_autoPadding) {
                    this.Padding = new(padding * 2 + 1);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_innerBorderRect != null && _penBorderColor != null) {
                e.Graphics.Clear(this.BackColor);
                e.Graphics.DrawRectangle(new Pen(_penBorderColor.Value, 1), _innerBorderRect.Value);
            }
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) => false;
    }
}
