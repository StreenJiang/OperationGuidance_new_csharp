using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.DateTimePickers {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomDatePickerGroup: UserControl {
        private bool _enabled;
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private Point _pickerBeginLocation;
        private int _gapNameAndPicker;
        private string _separator;
        private List<SeparatorControl> _separators;

        private double? _ratio;
        private FlowLayoutPanel _pickersPanel;
        private List<CustomDatePicker> _pickers;
        private Color _pickerBackColor;
        private Color? _borderColor;

        public new bool Enabled {
            get => _enabled;
            set {
                _enabled = value;
                SetPickersProperties((picker) => picker.Enabled = value);
            }
        }
        public string TextName { get => this._textName; set => this._textName = value; }
        public Point PickerBeginLocation { get => _pickerBeginLocation; set => _pickerBeginLocation = value; }
        public string Separator {
            get => _separator;
            set {
                _separator = value;
                SetSeparatorsProperties((separator) => separator.Text = value);
            }
        }
        public List<CustomDatePicker> Pickers { get => _pickers; }
        public double? Ratio { get => this._ratio; set => this._ratio = value; }
        public new Color BackColor { get; private set; }
        public new Control Parent {
            get => base.Parent;
            set {
                base.Parent = value;
                BackColor = value.BackColor;
            }
        }
        public Color PickerBackColor {
            get => _pickerBackColor;
            set {
                _pickerBackColor = value;
                SetPickersProperties((picker) => picker.BackColor = value);
            }
        }
        public Color? BorderColor {
            get => _borderColor;
            set {
                _borderColor = value;
                SetPickersProperties((picker) => picker.BorderColor = value);
            }
        }
        public int GapNameAndPicker { get => this._gapNameAndPicker; set => this._gapNameAndPicker = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }

        private void SetPickersProperties(Action<CustomDatePicker> setProperty) {
            foreach (CustomDatePicker picker in _pickers) {
                setProperty(picker);
            }
        }
        private void SetSeparatorsProperties(Action<SeparatorControl> setProperty) {
            foreach (SeparatorControl separator in _separators) {
                setProperty(separator);
            }
        }

        public CustomDatePickerGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _enabled = true;
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize text pickers
            _pickersPanel = new() {
                Parent = this,
                Margin = new(0),
            };
            _pickers = new();
            _separator = "";
            _separators = new();
            _borderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER;
            // Add a default picker
            AddPicker();
        }

        public CustomDatePicker AddPicker() {
            if (_pickers.Count >= 1) {
                _separators.Add(new() {
                    Parent = _pickersPanel,
                    Text = _separator,
                });
            }
            CustomDatePicker picker = new() {
                Parent = _pickersPanel,
                BackColor = _pickerBackColor,
                BorderColor = _borderColor,
                Enabled = _enabled,
            };
            _pickers.Add(picker);
            if (IsHandleCreated) {
                ResizeChildren(this, EventArgs.Empty);
            }
            return picker;
        }

        public CustomDatePicker GetPicker(int index) {
            return _pickers[index];
        }

        public void SetValue(int index, string? value) {
            _pickers[index].Text = value != null ? value : "";
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .425f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and picker
            _gapNameAndPicker = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (Height / 3.5);
            // Get width of name text
            Size separatorSize = new(0, Height - Padding.Size.Height);
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
                if (_separator.Length > 0) {
                    separatorSize.Width = (int) g.MeasureString(_separator, Font).Width;
                }
            }
            SetSeparatorsProperties((separator) => {
                separator.Size = separatorSize;
                separator.Font = Font;
            });
            // Calculate width of combo picker
            int pickersRange;
            if (_ratio != null) {
                pickersRange = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                pickersRange = Width - _nameWidth - Padding.Size.Width - _gapNameAndPicker;
            }
            _pickersPanel.Size = new(pickersRange, Height - Padding.Size.Height);
            _pickerBeginLocation = new(Width - Padding.Right - pickersRange, Padding.Top);
            _pickersPanel.Location = _pickerBeginLocation;
            // Find a optimal gap pixels
            int pickersCount = _pickers.Count;
            int separatorCount = _separators.Count;
            int pixels = 0;
            int curr = 0;
            int hMarginTemp = 0;
            while (true) {
                if ((pickersRange - (separatorSize.Width + pixels) * separatorCount) % pickersCount == 0) {
                    int prev = curr;
                    curr = pixels;
                    if (curr > _gapNameAndPicker / 2) {
                        if (Math.Abs(curr - _gapNameAndPicker / 2) > Math.Abs(prev - _gapNameAndPicker / 2)) {
                            hMarginTemp = prev;
                        } else {
                            hMarginTemp = curr;
                        }
                        break;
                    }
                }
                pixels++;
            }
            int vMarginTemp = (Height - separatorSize.Height) / 2;
            SetSeparatorsProperties((separator) => separator.Margin = new(hMarginTemp, vMarginTemp, hMarginTemp, vMarginTemp));

            // Recalculate size and location of pickers
            int pickerWidth = (pickersRange - ((separatorSize.Width + hMarginTemp * 2) * separatorCount)) / pickersCount;
            SetPickersProperties((picker) => picker.Size = new(pickerWidth, _pickersPanel.Height));

            if (_separators.Count > 0) {
                // If there are any remaining pixels, split them into separate separators
                int remainingWidth = _pickersPanel.Width - pickerWidth * pickersCount - (separatorSize.Width + hMarginTemp * 2) * separatorCount;
                int indexTemp = 0;
                while (remainingWidth > 0) {
                    _separators[indexTemp].Width += 1;
                    indexTemp++;
                    remainingWidth--;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _pickersPanel.Location.X - _nameWidth - _gapNameAndPicker;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            SetPickersProperties((picker) => picker.ForeColor = ForeColor);
            SetSeparatorsProperties((separator) => separator.ForeColor = ForeColor);
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }

        private class SeparatorControl: UserControl {
            protected override void OnPaint(PaintEventArgs e) {
                e.Graphics.Clear(BackColor);
                base.OnPaint(e);
                int x = Padding.Left;
                e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
            }
        }
    }
}
