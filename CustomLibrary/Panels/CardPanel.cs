using System.Drawing.Drawing2D;
using CustomLibrary.Configs;

namespace CustomLibrary.Panels {
    public class CardPanel : Panel {
        private GraphicsPath? _cardPath;
        private GraphicsPath? _shadowPathOuter;
        private GraphicsPath? _shadowPathInner;
        private Font? _titleFont;

        private const int RADIUS = 8;
        private const int SHADOW_OUTER = 3;
        private const int SHADOW_INNER = 1;
        private const int TITLE_X = 20;
        private const int TITLE_Y = 16;

        private static readonly Color SHADOW_COLOR_OUTER = Color.FromArgb(22, 0, 0, 0);
        private static readonly Color SHADOW_COLOR_INNER = Color.FromArgb(38, 0, 0, 0);
        private static readonly Color CARD_BG = Color.White;
        private static readonly Color CARD_BORDER = Color.FromArgb(224, 224, 224);
        private static readonly Color RULE_COLOR = Color.FromArgb(238, 238, 238);

        private string? _title;
        private Color _accentColor = Color.FromArgb(0xE8, 0x6C, 0x10);

        public string? Title {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public Color AccentColor {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        public Padding ContentPadding {
            get {
                int top = 20;
                if (!string.IsNullOrEmpty(_title))
                    top = 68;
                return new Padding(24, top, 24, 20);
            }
        }

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
            _shadowPathOuter?.Dispose();
            _shadowPathInner?.Dispose();

            int cardW = Width - SHADOW_OUTER - 2;
            int cardH = Height - SHADOW_OUTER - 2;
            _cardPath = CreateRoundedPath(1, 1, cardW, cardH, RADIUS);
            _shadowPathOuter = CreateRoundedPath(SHADOW_OUTER + 1, SHADOW_OUTER + 1, cardW, cardH, RADIUS);
            _shadowPathInner = CreateRoundedPath(SHADOW_INNER + 1, SHADOW_INNER + 1, cardW - 2, cardH - 2, RADIUS);
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (_cardPath == null || _shadowPathOuter == null || _shadowPathInner == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Two-layer shadow
            using (var b1 = new SolidBrush(SHADOW_COLOR_OUTER))
                e.Graphics.FillPath(b1, _shadowPathOuter);
            using (var b2 = new SolidBrush(SHADOW_COLOR_INNER))
                e.Graphics.FillPath(b2, _shadowPathInner);

            // Card body
            using (var cardBrush = new SolidBrush(CARD_BG))
                e.Graphics.FillPath(cardBrush, _cardPath);
            using (var borderPen = new Pen(CARD_BORDER, 1))
                e.Graphics.DrawPath(borderPen, _cardPath);

            // Title
            if (!string.IsNullOrEmpty(_title)) {
                _titleFont ??= new Font(WidgetsConfigs.SystemFontFamily, 16F, FontStyle.Bold, GraphicsUnit.Pixel);

                TextRenderer.DrawText(e.Graphics, _title, _titleFont,
                    new Point(TITLE_X, TITLE_Y), _accentColor);

                // Horizontal rule below title
                int ruleY = TITLE_Y + _titleFont.Height + 8;
                using (var rulePen = new Pen(RULE_COLOR, 1)) {
                    e.Graphics.DrawLine(rulePen, TITLE_X, ruleY, Width - 24, ruleY);
                }
            }

            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cardPath?.Dispose();
                _shadowPathOuter?.Dispose();
                _shadowPathInner?.Dispose();
                _titleFont?.Dispose();
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
