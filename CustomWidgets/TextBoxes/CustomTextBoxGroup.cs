using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Resources;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBoxGroup: UserControl {
        private bool _enabled;
        private string? _textName;
        private Font? _textFont;
        private FontStyle? _textFontStyle;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;

        private double? _ratio;
        private Font? _boxFont;
        private FontStyle? _boxFontStyle;
        private List<TextBox> _textBoxes;
        private List<Rectangle> _borderRects;
        private Color? _boxForeColor;
        private Color? _boxBackColor;
        private Color? _boxDisabledBackColor;
        private Color? _borderColor;

        private string? _separator;
        private int _separatorWidth;
        private int _gapBetweenNameNBoxes;
        private int _gapBetweenBoxes;

        private List<bool> _numberValidate;
        private ErrorProvider _errorProvider;
        private int? _currentErrorBoxIndex;
        private Color _borderColorError;

        public string? TextName {
            get => this._textName;
            set => this._textName = value;
        }
        public double? Ratio {
            get => this._ratio;
            set => this._ratio = value;
        }
        public Color? BoxBackColor {
            get => this._boxBackColor;
            set {
                this._boxBackColor = value;
                if (value != null) {
                    _boxDisabledBackColor = WidgetUtils.ChangeColor(value.Value, .975);
                }
                if (value != null && _textBoxes.Count > 0) {
                    foreach (TextBox textBox in _textBoxes) {
                        textBox.BackColor = value.Value;
                    }
                }
            }
        }
        public Color? BorderColor {
            get => this._borderColor;
            set {
                this._borderColor = value;
                if (value != null) {
                    Padding newP = Padding;
                    newP.Left += 1;
                    newP.Right += 1;
                    newP.Top += 1;
                    newP.Bottom += 1;
                    Padding = newP;
                } else {
                    Padding newP = Padding;
                    newP.Left -= 1;
                    newP.Right -= 1;
                    newP.Top -= 1;
                    newP.Bottom -= 1;
                    Padding = newP;

                }
            }
        }
        public int GapBetweenNameNBoxes {
            get => this._gapBetweenNameNBoxes;
            set => this._gapBetweenNameNBoxes = value;
        }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public string? Separator {
            get => this._separator;
            set => this._separator = value;
        }
        public Color? BoxForeColor {
            get => this._boxForeColor;
            set {
                this._boxForeColor = value;
                if (value != null) {
                    foreach (TextBox textBox in _textBoxes) {
                        textBox.ForeColor = value.Value;
                    }
                }
            }
        }

        public new bool Enabled {
            get => this._enabled;
            set {
                this._enabled = value;
                foreach (TextBox box in _textBoxes) {
                    box.Enabled = value;
                }
            }
        }

        public FontStyle? TextFontStyle { get => _textFontStyle; set => _textFontStyle = value; }
        public FontStyle? BoxFontStyle { get => _boxFontStyle; set => _boxFontStyle = value; }
        public int? CurrentErrorBoxIndex { get => _currentErrorBoxIndex; }

        public CustomTextBoxGroup(string? textName, Color borderColorError) : base() {
            Margin = new(0);

            _enabled = true;
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            _separatorWidth = 0;
            _errorProvider = new() {
                DataMember = null,
                ContainerControl = this,
            };
            _borderColorError = borderColorError;

            _textBoxes = new();
            _borderRects = new();
            _numberValidate = new() {
                false,
            };
            // Add a default box
            AddBoxes();
        }

        public void AddBoxes(int num = 1) {
            for (int i = 0; i < num; i++) {
                TextBox box = new() {
                    Parent = this,
                    BorderStyle = BorderStyle.None,
                    Multiline = false,
                    Enabled = _enabled,
                };
                box.GotFocus += (sender, eventArgs) => {
                    EventFuncs.CurrentActiveControl = sender as Control;
                };
                int count = _textBoxes.Count;
                box.TextChanged += (sender, eventArgs) => {
                    if (_numberValidate[count]) {
                        foreach (char c in box.Text) {
                            if (!char.IsDigit(c) && c != '.') {
                                SetError(count, "请输入数字");
                                return;
                            }
                        }
                        SetError(count, "");
                    }
                };
                if (_boxBackColor != null) {
                    box.BackColor = _boxBackColor.Value;
                }
                _textBoxes.Add(box);
                _numberValidate.Add(false);
            }
            // Resize amnually
            InvokeResizing();
        }

        public void SetValue(int index, string? value) {
            _textBoxes[index].Text = value;
        }

        public string GetValue(int index) {
            return _textBoxes[index].Text;
        }

        public TextBox GetBox(int index) {
            return _textBoxes[index];
        }

        public void SetNumberValidate(int index, bool flag) {
            _numberValidate[index] = flag;
        }

        public void SetError(int? index, string errorMessage) {
            _currentErrorBoxIndex = index;
            if (_currentErrorBoxIndex != null) {
                _errorProvider.SetError(_textBoxes[_currentErrorBoxIndex.Value], errorMessage);
                if (errorMessage == null || errorMessage == string.Empty || errorMessage == "") {
                    _currentErrorBoxIndex = null;
                }
            }
            ResetErrorIcon();
            Invalidate();
        }

        private void ResetErrorIcon() {
            if (_currentErrorBoxIndex != null) {
                Size newIconSize = new((int) (Height / 2.5), (int) (Height / 2.5));
                Image image = WidgetUtils.ResizeImageWithoutLosingQuality(CustomResources.input_error, newIconSize);
                _errorProvider.Icon = Icon.FromHandle(new Bitmap(image).GetHicon());
                _errorProvider.SetIconPadding(_textBoxes[_currentErrorBoxIndex.Value], (int) (-Padding.Right * 1.8));
            }
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            // Calculate font
            _textFont = new(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .7F, 
                    _textFontStyle == null ? FontStyle.Regular : _textFontStyle.Value, GraphicsUnit.Pixel);
            this.Font = _textFont;
            _boxFont = new(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .65F, 
                    _boxFontStyle == null ? FontStyle.Regular : _boxFontStyle.Value, GraphicsUnit.Pixel);
            // Get width of name text
            if (_textName != null) {
                using (Graphics g = CreateGraphics()) {
                    _nameWidth = (int) g.MeasureString(_textName, _textFont).Width;
                }
            }
            // Get Width of separator
            if (_separator != null) {
                using (Graphics g = CreateGraphics()) {
                    _separatorWidth = (int) g.MeasureString(_separator, _boxFont).Width;
                }
            }

            // Boexes count
            int boxesCount = _textBoxes.Count;
            // Gap num
            int boxGapNum = boxesCount - 1;

            // Calculate width of text box
            int boxesRange;
            if (_ratio != null) {
                boxesRange = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                boxesRange = Width - _nameWidth - Padding.Size.Width;
            }
            if (_nameWidth > 0) {
                boxesRange -= _gapBetweenNameNBoxes;
            }
            // Find a optimal gap pixels
            int pixels = 0;
            int curr = 0;
            while (true) {
                if ((boxesRange - pixels * boxGapNum - _separatorWidth * boxGapNum) % boxesCount == 0) {
                    int prev = curr;
                    curr = pixels;
                    if (curr > _gapBetweenNameNBoxes) {
                        if (Math.Abs(curr - _gapBetweenNameNBoxes) > Math.Abs(prev - _gapBetweenNameNBoxes)) {
                            _gapBetweenBoxes = prev;
                        } else {
                            _gapBetweenBoxes = curr;
                        }
                        break;
                    }
                }
                pixels++;
            }
            int boxBorderWidth = (boxesRange - _gapBetweenBoxes * boxGapNum - _separatorWidth * boxGapNum) / boxesCount;
            int nameRange = Width - boxesRange - Padding.Right;

            // Recalculate size and location of boxes
            for (int i = 0; i < _textBoxes.Count; i++) {
                TextBox box = _textBoxes[i];
                box.Font = _boxFont;
                int vPadding = (int) (box.Height * .3) + Margin.Top;
                box.Width = boxBorderWidth - vPadding * 2;
                box.Padding = new(vPadding);
                box.Location = new(nameRange + vPadding + i * (_gapBetweenBoxes + box.Width + vPadding * 2) + _separatorWidth * i, (Height - box.Height) / 2);
            }

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                _borderRects.Clear();
                for (int i = 0; i < _textBoxes.Count; i++) {
                    Point borderLocation = new(nameRange + i * (_gapBetweenBoxes + boxBorderWidth) + _separatorWidth * i - 1, Padding.Top);
                    Size borderSize = new(boxBorderWidth + 1, Height - Padding.Top * 2);
                    _borderRects.Add(new(borderLocation, borderSize));
                }
            }
            ResetErrorIcon();
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            if (_textName != null && _textFont != null) {
                int x = Padding.Left;
                if (_nameAlignment == HorizontalAlignment.Right) {
                    x = _textBoxes[0].Location.X - _nameWidth - _gapBetweenNameNBoxes - _textBoxes[0].Padding.Left;
                }
                e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - _textFont.Height) / 2));
            }

            // Draw separator
            if (_separator != null && _textFont != null) {
                for (int i = 1; i < _textBoxes.Count; i++) {
                    int x = _textBoxes[i].Location.X - _separatorWidth - _gapBetweenBoxes / 2 - _textBoxes[i].Padding.Left;
                    e.Graphics.DrawString(_separator, Font, new SolidBrush(ForeColor), new Point(x, (Height - _textFont.Height) / 2));
                }
            }

            // Change color if disabled
            foreach (TextBox box in _textBoxes) {
                if (_enabled) {
                    if (_boxBackColor != null) {
                        box.BackColor = _boxBackColor.Value;
                    }
                } else {
                    if (_boxDisabledBackColor != null) {
                        box.BackColor = _boxDisabledBackColor.Value;
                    }
                }
            }

            // Draw border if border color is not null
            if (_borderColor != null && _borderRects.Count > 0) {
                for (int i = 0; i < _borderRects.Count; i++) {
                    Rectangle rect = _borderRects[i];
                    if (_currentErrorBoxIndex != null && i == _currentErrorBoxIndex) {
                        e.Graphics.DrawRectangle(new(_borderColorError, 1), rect);
                    } else {
                        e.Graphics.DrawRectangle(new(_borderColor.Value, 1), rect);
                    }
                }
            }

            // Draw backColor
            if (_enabled && _boxBackColor != null && _borderRects.Count > 0) {
                foreach (Rectangle rect in _borderRects) {
                    Rectangle rectFill = new(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1);
                    e.Graphics.FillRectangle(new SolidBrush(_boxBackColor.Value), rectFill);
                }
            }
            if (!_enabled && _boxDisabledBackColor != null && _borderRects.Count > 0) {
                foreach (Rectangle rect in _borderRects) {
                    Rectangle rectFill = new(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1);
                    e.Graphics.FillRectangle(new SolidBrush(_boxDisabledBackColor.Value), rectFill);
                }
            }
        }
    }
}
