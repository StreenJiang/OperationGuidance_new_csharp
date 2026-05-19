using System.Drawing.Drawing2D;

namespace CustomLibrary.Panels {
    public class CardPanel: Panel {
        private GraphicsPath? _cardPath;
        private GraphicsPath? _shadowPath;
        private const int RADIUS = 8;
        private const int SHADOW_OFFSET = 4;
        private static readonly Color SHADOW_COLOR = Color.FromArgb(208, 208, 208);
        private static readonly Color CARD_BG = Color.White;
        private static readonly Color CARD_BORDER = Color.FromArgb(224, 224, 224);

        public CardPanel() {
            DoubleBuffered = true;
            ResizeRedraw = true;
            base.BackColor = Color.Transparent;
        }

        protected override void OnResize(EventArgs eventargs) {
            base.OnResize(eventargs);
            BuildPaths();
        }

        private void BuildPaths() {
            _cardPath?.Dispose();
            _shadowPath?.Dispose();

            int cardW = Width - SHADOW_OFFSET - 2;
            int cardH = Height - SHADOW_OFFSET - 2;
            _cardPath = CreateRoundedPath(1, 1, cardW, cardH, RADIUS);
            _shadowPath = CreateRoundedPath(SHADOW_OFFSET + 1, SHADOW_OFFSET + 1, cardW, cardH, RADIUS);
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (_cardPath == null || _shadowPath == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var shadowBrush = new SolidBrush(SHADOW_COLOR);
            e.Graphics.FillPath(shadowBrush, _shadowPath);

            using var cardBrush = new SolidBrush(CARD_BG);
            e.Graphics.FillPath(cardBrush, _cardPath);

            using var borderPen = new Pen(CARD_BORDER, 1);
            e.Graphics.DrawPath(borderPen, _cardPath);

            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cardPath?.Dispose();
                _shadowPath?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static GraphicsPath CreateRoundedPath(int x, int y, int w, int h, int r) {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
