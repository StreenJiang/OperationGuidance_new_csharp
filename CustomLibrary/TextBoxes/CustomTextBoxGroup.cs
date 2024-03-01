using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBoxGroup: UserControl {
        private bool _enabled;
        private bool _readOnly;
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private Point _boxBeginLocation;
        private int _gapNameAndBox;
        private string _separator;
        private Size _separatorSize;
        private List<SeparatorControl> _separators;

        private bool _numberValidate;
        private bool _numberOnly;
        private double? _ratio;
        private FlowLayoutPanel _textBoxesPanel;
        private List<CustomTextBox> _textBoxes;
        private Color _boxBackColor;
        private Color? _disabledBackColor;
        private Color? _borderColor;
        private Color? _borderColorError;

        public new bool Enabled { 
            get => _enabled;
            set {
                _enabled = value;
                SetTextBoxesProperties((textBox) => textBox.Enabled = value);
            }
        }
        public bool ReadOnly {
            get => _readOnly;
            set {
                _readOnly = value;
                SetTextBoxesProperties((textBox) => textBox.ReadOnly = value);
            }
        }
        public string TextName { get => this._textName; set => this._textName = value; }
        protected int NameWidth { get => _nameWidth; set => _nameWidth = value; }
        public Point BoxBeginLocation { get => _boxBeginLocation; set => _boxBeginLocation = value; }
        public string Separator { 
            get => _separator; 
            set {
                _separator = value; 
                SetSeparatorsProperties((separator) => separator.Text = value );
            }
        }
        protected FlowLayoutPanel TextBoxesPanel { get => _textBoxesPanel; set => _textBoxesPanel = value; }
        public List<CustomTextBox> TextBoxes { get => _textBoxes; }
        public Size SeparatorSize { get => _separatorSize; set => _separatorSize = value; }
        protected List<SeparatorControl> Separators { get => _separators; set => _separators = value; }
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
            get => _boxBackColor;
            set {
                _boxBackColor = value;
                SetTextBoxesProperties((textBox) => textBox.BackColor = value);
            }
        }
        public Color? BorderColor { 
            get => _borderColor;
            set {
                _borderColor = value;
                SetTextBoxesProperties((textBox) => textBox.BorderColor = value);
            }
        }
        public Color? BorderColorError { 
            get => _borderColorError; 
            set {
                _borderColorError = value;
                SetTextBoxesProperties((textBox) => textBox.BorderColorError = value);
            }
        }
        public int GapNameAndBox { get => this._gapNameAndBox; set => this._gapNameAndBox = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public bool HasError {
            get {
                foreach (CustomTextBox textBox in _textBoxes) {
                    if (textBox.IsError) {
                        return textBox.IsError;
                    }
                }
                return false;
            }
        }
        public bool NumberValidate { 
            get => _numberValidate; 
            set {
                _numberValidate = value;
                SetTextBoxesProperties((textBox) => textBox.NumberValidate = value);
            }
        }
        public bool NumberOnly { 
            get => _numberOnly; 
            set {
                _numberOnly = value;
                SetTextBoxesProperties((textBox) => textBox.NumberOnly = value);
            }
        }

        private void SetTextBoxesProperties(Action<CustomTextBox> setProperty) {
            foreach (CustomTextBox textBox in _textBoxes) {
                setProperty(textBox);
            }
        }
        private void SetSeparatorsProperties(Action<SeparatorControl> setProperty) {
            foreach (SeparatorControl separator in _separators) {
                setProperty(separator);
            }
        }

        public CustomTextBoxGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _enabled = true;
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize text boxes
            _textBoxesPanel = new() {
                Parent = this,
                Margin = new(0),
            };
            _textBoxes = new();
            _separator = "";
            _separators = new();
            // Add a default box
            AddTextBox();
        }

        public CustomTextBox AddTextBox() {
            if (_textBoxes.Count >= 1) {
                _separators.Add(new() {
                    Parent = _textBoxesPanel,
                    Text = _separator,
                });
            }
            CustomTextBox box = new() {
                Parent = _textBoxesPanel,
                BorderStyle = BorderStyle.None,
                BackColor = _boxBackColor,
                BorderColor = _borderColor,
                BorderColorError = _borderColorError,
                Enabled = _enabled,
                NumberOnly = NumberOnly,
            };
            // box.BackColor = _boxBackColor;
            _textBoxes.Add(box);
            if (IsHandleCreated) {
                ResizeChildren(this, EventArgs.Empty);
            }
            return box;
        }

        public CustomTextBox GetTextBox(int index) {
            return _textBoxes[index];
        }

        public void SetValue(int index, string? value) {
            _textBoxes[index].Text = value != null ? value : "";
        }

        public void CheckError(int index, bool flag) {
            _textBoxes[index].CheckError(flag);
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
            _gapNameAndBox = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (Height / 3.5);
            // Get width of name text
            _separatorSize = new(0, Height - Padding.Size.Height);
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
                if (_separator.Length > 0) {
                    _separatorSize.Width = (int) g.MeasureString(_separator, Font).Width;
                }
            }
            SetSeparatorsProperties((separator) => {
                separator.Size = _separatorSize;
                separator.Font = Font;
            });
            // Calculate width of combo box
            int boxesRange = GetBoxesRange();
            // Find a optimal gap pixels
            int boxesCount = _textBoxes.Count;
            int separatorCount = _separators.Count;
            int pixels = 0;
            int curr = 0;
            int hMarginTemp = 0;
            while (true) {
                if ((boxesRange - (_separatorSize.Width + pixels) * separatorCount) % boxesCount == 0) {
                    int prev = curr;
                    curr = pixels;
                    if (curr > _gapNameAndBox / 2) {
                        if (Math.Abs(curr - _gapNameAndBox / 2) > Math.Abs(prev - _gapNameAndBox / 2)) {
                            hMarginTemp = prev;
                        } else {
                            hMarginTemp = curr;
                        }
                        break;
                    }
                }
                pixels++;
            }
            int vMarginTemp = (Height - _separatorSize.Height) / 2;
            SetSeparatorsProperties((separator) => separator.Margin = new(hMarginTemp, vMarginTemp, hMarginTemp, vMarginTemp));

            // Recalculate size and location of boxes
            int boxWidth = (boxesRange - ((_separatorSize.Width + hMarginTemp * 2) * separatorCount)) / boxesCount;
            SetTextBoxesProperties((textBox) => textBox.Size = new(boxWidth, _textBoxesPanel.Height));

            if (_separators.Count > 0) {
                // If there are any remaining pixels, split them into separate separators
                int remainingWidth = _textBoxesPanel.Width - boxWidth * boxesCount - (_separatorSize.Width + hMarginTemp * 2) * separatorCount;
                int indexTemp = 0;
                while (remainingWidth > 0) {
                    _separators[indexTemp].Width += 1;
                    indexTemp++;
                    remainingWidth--;
                }
            }
        }

        protected virtual int GetBoxesRange() {
            int boxesRangeTemp;
            if (_ratio != null) {
                boxesRangeTemp = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                boxesRangeTemp = Width - _nameWidth - Padding.Size.Width - _gapNameAndBox;
            }
            _textBoxesPanel.Size = new(boxesRangeTemp, Height - Padding.Size.Height);
            _boxBeginLocation = new(Width - Padding.Right - boxesRangeTemp, Padding.Top);
            _textBoxesPanel.Location = _boxBeginLocation;
            return boxesRangeTemp;
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _textBoxesPanel.Location.X - _nameWidth - _gapNameAndBox;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            SetTextBoxesProperties((textBox) => textBox.ForeColor = ForeColor);
            SetSeparatorsProperties((separator) => separator.ForeColor = ForeColor);
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }

        protected class SeparatorControl: UserControl {
            protected override void OnPaint(PaintEventArgs e) {
                e.Graphics.Clear(BackColor);
                base.OnPaint(e);
                int x = Padding.Left;
                e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
            }
        }
    }
}
