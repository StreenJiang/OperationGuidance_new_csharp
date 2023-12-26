using CustomLibrary.ComboBoxs;
using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomComboBoxGroup<T>: UserControl {
        private bool _enabled;
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private int _gapNameAndBox;

        private double? _ratio;
        private CustomComboBox<T> _comboBox;
        private Color _boxBackColor;
        private Color? _disabledBackColor;
        private Color? _borderColor;

        public new bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled = value;
                _comboBox.Enabled = value;
            }
        }
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
        public Color BoxBackColor { 
            get => _comboBox.BackColor; 
            set => _comboBox.BackColor = value; 
        }
        public Color? BorderColor { get => _comboBox.BorderColor; set => _comboBox.BorderColor = value; }
        public Color? BorderColorError { get => _comboBox.BorderColorError; set => _comboBox.BorderColorError = value; }
        public int GapBetweenNameNBoxes { get => this._gapNameAndBox; set => this._gapNameAndBox = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public bool ShowRealValue { get => _comboBox.ShowRealValue; set => _comboBox.ShowRealValue = value; }

        public CustomComboBoxGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize combo box
            _comboBox = new();
            _comboBox.Parent = this;
        }

        public int AddItem(string itemName, T? obj) {
            return _comboBox.AddItem(itemName, obj);
        }

        public void RemoveItem(int index) {
            _comboBox.RemoveItem(index);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += InvokeResizing;
            InvokeResizing(this, e);
        }

        private void InvokeResizing(object? sender, EventArgs eventArgs) {
            int hPadding = (int) (Height / 3.5);
            int vPadding = 0;
            Padding = new(hPadding, vPadding, hPadding, vPadding);
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .5f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and box
            _gapNameAndBox = hPadding;
            // Get width of name text
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
            }
            // Calculate width of combo box
            int boxWidth;
            if (_ratio != null) {
                boxWidth = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                boxWidth = Width - _nameWidth - Padding.Size.Width - _gapNameAndBox;
            }
            // Resize combo box first to get font of it
            _comboBox.Size = new(boxWidth, Height - Padding.Size.Height);
            // Calculate remain width of name
            int nameRange = Width - boxWidth - Padding.Size.Width - _gapNameAndBox;
            // Relocate combo box
            _comboBox.Location = new(Width - boxWidth - Padding.Right, (Height - _comboBox.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _comboBox.Location.X - _nameWidth - _gapNameAndBox - _comboBox.Padding.Left;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _comboBox.ForeColor = ForeColor;
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }
    }
}
