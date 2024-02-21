using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.Buttons {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CommonButtonGroup: UserControl {
        private bool _enabled;
        private bool _readOnly;
        private string _textName;
        private Color _textColor;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private Point _buttonBeginLocation;
        private int _gapNameAndButton;
        private int _gapButtons;

        private double? _ratio;
        private FlowLayoutPanel _buttonsPanel;
        private List<CommonButton> _buttons;

        public new bool Enabled { 
            get => _enabled;
            set {
                _enabled = value;
                SetButtonsProperties((button) => button.Enabled = value);
            }
        }
        public string TextName { get => this._textName; set => this._textName = value; }
        public Color TextColor { get => _textColor; set => _textColor = value; }
        public Point ButtonBeginLocation { get => _buttonBeginLocation; set => _buttonBeginLocation = value; }
        public List<CommonButton> Buttons { get => _buttons; }
        public double? Ratio { get => this._ratio; set => this._ratio = value; }
        public new Color BackColor { get; private set; }
        public new Color ForeColor { get => _textColor; set => _textColor = value; }
        public new Control Parent { 
            get => base.Parent; 
            set {
                base.Parent = value;
                BackColor = value.BackColor;
            } 
        }
        public int GapNameAndButton { get => this._gapNameAndButton; set => this._gapNameAndButton = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }

        private void SetButtonsProperties(Action<CommonButton> setProperty) {
            foreach (CommonButton button in _buttons) {
                setProperty(button);
            }
        }

        public CommonButtonGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _enabled = true;
            _textName = textName;
            _textColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize text buttons
            _buttonsPanel = new() {
                Parent = this,
                Margin = new(0),
            };
            _buttons = new();
            // Add a default button
            AddButton("button1");
        }

        public CommonButton AddButton(string label) {
            CommonButton button = new() {
                Parent = _buttonsPanel,
                Enabled = _enabled,
                Label = label,
            };
            _buttons.Add(button);
            if (IsHandleCreated) {
                ResizeChildren(this, EventArgs.Empty);
            }
            return button;
        }

        public CommonButton GetButton(int index) {
            return _buttons[index];
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .55f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and button
            _gapNameAndButton = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (Height / 3.5);
            // Get width of name text
            _gapButtons = _gapNameAndButton;
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
            }
            // Calculate width of combo button
            int buttonsRange;
            if (_ratio != null) {
                buttonsRange = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                buttonsRange = Width - _nameWidth - Padding.Size.Width - _gapNameAndButton;
            }
            _buttonsPanel.Size = new(buttonsRange, Height - Padding.Size.Height);
            _buttonBeginLocation = new(Width - Padding.Right - buttonsRange, Padding.Top);
            _buttonsPanel.Location = _buttonBeginLocation;
            // Find a optimal gap pixels
            int buttonsCount = _buttons.Count;
            int pixels = 0;
            int curr = 0;
            int hMarginTemp = 0;
            while (true) {
                if ((buttonsRange - _gapButtons * _buttons.Count) % buttonsCount == 0) {
                    int prev = curr;
                    curr = pixels;
                    if (curr > _gapNameAndButton / 2) {
                        if (Math.Abs(curr - _gapNameAndButton / 2) > Math.Abs(prev - _gapNameAndButton / 2)) {
                            hMarginTemp = prev;
                        } else {
                            hMarginTemp = curr;
                        }
                        break;
                    }
                }
                pixels++;
            }

            // Recalculate size and location of buttons
            int buttonWidth = (buttonsRange - ((_gapButtons + hMarginTemp * 2) * _buttons.Count)) / buttonsCount;
            SetButtonsProperties((button) => {
                button.Size = new((int) (TextRenderer.MeasureText(button.Label, button.Font).Width + _buttonsPanel.Height * 1.2), _buttonsPanel.Height);
                if (_buttons.IndexOf(button) != _buttons.Count - 1) {
                    button.Margin = new(0, 0, _gapButtons, 0);
                }
            });

            if (_buttons.Count > 1) {
                // If there are any remaining pixels, split them into margin right
                int remainingWidth = _buttonsPanel.Width - buttonWidth * buttonsCount - (_gapButtons + hMarginTemp * 2) * _buttons.Count;
                int indexTemp = 0;
                while (remainingWidth > 0) {
                    CommonButton button = _buttons[indexTemp];
                    if (_buttons.IndexOf(button) == _buttons.Count - 1) {
                        indexTemp = 0;
                        continue;
                    }
                    Padding margin = button.Margin;
                    margin.Right += 1;
                    button.Margin = margin;
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
                x = _buttonsPanel.Location.X - _nameWidth - _gapNameAndButton;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            SetButtonsProperties((button) => button.ForeColor = ForeColor);
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }
    }
}
