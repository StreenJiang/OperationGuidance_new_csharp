using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;

namespace CustomLibrary.Forms {

    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomFloatingForm: Form {
        private Color? _borderColor;
        private Rectangle? _borderRect;
        private readonly int _borderThickness = 1;
        // All containers
        private CustomContentPanel _outerPanel;
        private CustomContentPanel _titlePanel;
        private CustomContentPanel _contentPanel;

        // Outer panel
        // Title panel
        private string _title;
        private Font? _titleFont;
        // Content panel

        // -- Properties --
        // Used in EventFuncs
        public Color? BorderColor {
            get => _borderColor;
            set {
                _borderColor = value;
                if (value != null) {
                    _outerPanel.Location = new(_borderThickness, _borderThickness);
                } else {
                    _outerPanel.Location = new(0, 0);
                }
            }
        }
        // Outer panel
        public CustomContentPanel OuterPanel { get => _outerPanel; set => _outerPanel = value; }
        // Title panel
        public CustomContentPanel TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public string Title { get => _title; set => _title = value; }
        public bool HasTitleBar { get => TitlePanel.Visible; set => TitlePanel.Visible = value; }
        // Content panel
        public CustomContentPanel ContentPanel { get => _contentPanel; set => _contentPanel = value; }

        public CustomFloatingForm() : base() {
            Control mainParent = WidgetUtils.MainForm;
            // Initialize self
            Owner = (Form) mainParent;
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;

            // Title panel
            _titlePanel = new() {
                Height = GetTitlePanelHeight(),
            };
            _title = "(未命名)";
            _titlePanel.SizeChanged += (sender, eventArgs) => {
                _titleFont = new Font(WidgetsConfigs.SystemFontFamily, _titlePanel.Height * .425F, FontStyle.Regular, GraphicsUnit.Pixel);
            };
            _titlePanel.Paint += (sender, eventArgs) => {
                if (_titleFont != null) {
                    Graphics g = eventArgs.Graphics;
                    int titleHeight = _titlePanel.Height;
                    g.DrawString(_title, _titleFont, new SolidBrush(Color.Black), new Point(_titleFont.Height / 4, (int) (titleHeight - _titleFont.Height) / 2));
                    Pen pen = new Pen(_borderColor != null ? _borderColor.Value : Color.Black, _borderThickness);
                    Point point1 = new(0, titleHeight - _borderThickness);
                    Point point2 = new Point(_titlePanel.Width, titleHeight - _borderThickness);
                    g.DrawLine(pen, point1, point2);
                }
            };
            // Content panel
            _contentPanel = new() {
                Margin = new(0),
                Padding = GetContentPadding(),
            };
            // Outer panel
            _outerPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _outerPanel.Controls.Add(_titlePanel);
            _outerPanel.Controls.Add(_contentPanel);
            _outerPanel.SizeChanged += (sender, eventArgs) => {
                _titlePanel.Width = _outerPanel.Width;
                _contentPanel.Width = _outerPanel.Width;
            };
        }

        public virtual void PretendToShowToCreateHandlesForChildren() {
            base.Show();
            Opacity = 0D;
        }

        public new void Show() {
            Opacity = 1D;
            base.Show();
        }

        public void CalculateDetailProperties() {
            _titlePanel.Height = GetTitlePanelHeight();
            _contentPanel.Padding = GetContentPadding();
        }
        private int GetTitlePanelHeight() => WidgetUtils.PopUpOrFloatingFormTitle();
        private Padding GetContentPadding() => WidgetUtils.PopUpOrFloatingFormContentPadding();

        public void SetContentSizeAndSelfSize(Size contentSize) {
            ContentPanel.Height = contentSize.Height;
            int formHeight = ContentPanel.Height;
            if (TitlePanel.Visible) formHeight += TitlePanel.Height;
            if (_borderColor != null) formHeight += _borderThickness * 2;
            Size = new(contentSize.Width, formHeight);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            CalculateDetailProperties();
            SizeChanged += ResizeChildren;
        }

        protected sealed override void OnSizeChanged(EventArgs e) => base.OnSizeChanged(e);
        public void ResizeChildren() => ResizeChildren(EventArgs.Empty);
        public void ResizeChildren(EventArgs eventArgs) => ResizeChildren(this, eventArgs);
        protected virtual void ResizeChildren(object? sender, EventArgs eventArgs) {
            // CalculateDetailProperties();
            // Border
            if (_borderColor != null) {
                _borderRect = new(0, 0, Width - 1, Height - 1);
            }
            // Reset width to outer panel, it will cause width reseting of its children
            Size outerSize = Size;
            if (_borderColor != null) {
                outerSize = new(Width - _borderThickness * 2, Height - _borderThickness * 2);
            }
            _outerPanel.Size = outerSize;
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(BackColor);
            base.OnPaint(e);
            // Draw border
            if (_borderColor != null && _borderRect != null) {
                e.Graphics.DrawRectangle(new(_borderColor.Value, _borderThickness), _borderRect.Value);
            }
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
        }
    }
}
