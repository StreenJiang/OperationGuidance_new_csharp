using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.Buttons {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class ToggleButtonGroup: UserControl {
        private bool _enabled;
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private int _gapNameAndButton;

        private double? _ratio;
        private ToggleButton _toggleButton;

        public new bool Enabled { get => _toggleButton.Enabled; set => _toggleButton.Enabled = value; }
        public string TextName { get => this._textName; set => this._textName = value; }
        public double? Ratio { get => this._ratio; set => this._ratio = value; }
        public new Color BackColor { get; private set; }
        public new Control Parent { 
            get => base.Parent; 
            set {
                base.Parent = value;
                BackColor = value.BackColor;
            } 
        }
        public Color ButtonBackColor { 
            get => _toggleButton.BackColor; 
            set => _toggleButton.BackColor = value; 
        }
        public int GapBetweenNameNBoxes { get => this._gapNameAndButton; set => this._gapNameAndButton = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public bool Checked { get => _toggleButton.Checked; set => _toggleButton.Checked= value; }
        public event EventHandler CheckedChanged { add => _toggleButton.CheckedChanged += value; remove => _toggleButton.CheckedChanged -= value; }

        public ToggleButtonGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize combo box
            _toggleButton = new();
            _toggleButton.Parent = this;
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .55f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and box
            _gapNameAndButton = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (Height / 3.5);
            // Get width of name text
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
            }
            // Calculate width of combo box
            int buttonX;
            if (_ratio != null) {
                buttonX = Width - (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                buttonX = _nameWidth + Padding.Size.Width + _gapNameAndButton;
            }
            // Resize combo box first to get font of it
            int buttonH = (int) ((Height - Padding.Size.Height) * .65);
            _toggleButton.Size = new(buttonH * 3, buttonH);
            // Relocate combo box (height plus extra 1 makes better display)
            _toggleButton.Location = new(buttonX, (Height - _toggleButton.Height) / 2 + 1);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _toggleButton.Location.X - _nameWidth - _gapNameAndButton;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _toggleButton.ForeColor = ForeColor;
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }
    }
}
