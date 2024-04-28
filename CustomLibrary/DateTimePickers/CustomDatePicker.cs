using System.Drawing.Drawing2D;
using CustomLibrary.Configs;
using CustomLibrary.Utils;

namespace CustomLibrary.DateTimePickers {
    public class CustomDatePicker: DateTimePicker {
        #region Fields
        private bool _enabled;
        private int _conerRadius;
        private Rectangle _borderRect;
        private Rectangle _textAreaRect;
        private Rectangle _iconAreaRect;
        private Color _originalBackColor;
        private Color _disabledBackColor;
        private Color? _borderColor;
        private Image _icon = Resources.CustomResources.calendar_fill;
        private Image? _iconShowing;
        private readonly int _borderThickness = 1;
        private Font _font;
        private int _hPadding;
        private int _vPadding;
        private DateTime? _realTimeValue;
        #endregion

        #region Properties
        public new bool Enabled {
            get => this._enabled;
            set {
                this._enabled = value;
                if (!value) {
                    base.BackColor = _disabledBackColor;
                } else {
                    base.BackColor = _originalBackColor;
                }
            }
        }
        public override Color BackColor {
            get => base.BackColor;
            set {
                _disabledBackColor = WidgetUtils.DarkenColor(value, .1);
                if (!_enabled) {
                    base.BackColor = _disabledBackColor;
                } else {
                    base.BackColor = value;
                }
                _originalBackColor = value;
            }
        }
        public Color? BorderColor {
            get => this._borderColor;
            set {
                this._borderColor = value;
                if (value != null) {
                    Padding newP = Padding;
                    newP.Left += _borderThickness;
                    newP.Right += _borderThickness;
                    newP.Top += _borderThickness;
                    newP.Bottom += _borderThickness;
                    Padding = newP;
                } else {
                    Padding newP = Padding;
                    newP.Left -= _borderThickness;
                    newP.Right -= _borderThickness;
                    newP.Top -= _borderThickness;
                    newP.Bottom -= _borderThickness;
                    Padding = newP;

                }
            }
        }
        public new Size Size {
            get => base.Size;
            set {
                base.MinimumSize = value;
                base.Size = value;
            }
        }
        public new DateTime? Value {
            get => _realTimeValue;
            set {
                if (value != null) {
                    base.Value = value.Value;
                }
                _realTimeValue = value;
                Invalidate();
            }
        }
        #endregion

        #region Constructors
        public CustomDatePicker() {
            SetStyle(ControlStyles.UserPaint, true);
            Margin = new(0);
            ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND;

            _enabled = true;
            _borderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER;
            _disabledBackColor = WidgetUtils.DarkenColor(BackColor, .1);
            Font = new(Font.FontFamily, 1);
            _font = new(Font.FontFamily, 1);
        }
        #endregion

        #region Override methods
        protected override void OnValueChanged(EventArgs eventargs) {
            Value = base.Value;
            base.OnValueChanged(eventargs);
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }
        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            _font = new(WidgetsConfigs.SystemFontFamily, Height * .4F, FontStyle.Regular, GraphicsUnit.Pixel);
            // Recalculate size
            _hPadding = (int) (Height / 3);
            _vPadding = (int) (Height / 3.85F);

            // Recal coner radius
            _conerRadius = WidgetUtils.ControlRadius();

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                _borderRect = new(1, 1, Width - 2 - _borderThickness, Height - 2 - _borderThickness);
            }

            // Change region
            using (GraphicsPath path = WidgetUtils.RoundedRect(new(0, 0, Width - 1, Height - 1), _conerRadius)) {
                Region = new(path);
            }
            // // Create border rectangle if border color is not null
            // if (_borderColor != null) {
            //     _borderRect = new(0, 0, Width - _borderThickness, Height - _borderThickness);
            // }

            // Text and icon area rectangle
            int iconSide = Height - _vPadding * 2;
            _textAreaRect = new(_hPadding, _vPadding, Width - _hPadding * 2 - _borderThickness * 2 - iconSide * 2, Height - _vPadding * 2 - _borderThickness * 2);
            _iconAreaRect = new((int) (Width - (Height / 1.25)), 0, Height, Height);
            _iconShowing = WidgetUtils.ResizeImage(_icon, iconSide, iconSide);
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (_iconAreaRect.Contains(e.Location)) {
                Cursor = Cursors.Hand;
            } else {
                Cursor = Cursors.Default;
            }
        }
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            // Change back color if disabled
            if (!_enabled) {
                g.Clear(_disabledBackColor);
            }

            if (_conerRadius > 0) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), _conerRadius)) {
                    using Pen penSurface = new Pen(Parent.BackColor, 1);
                    // Draw surface border for HD result
                    e.Graphics.DrawPath(penSurface, path);
                }
            }

            // Draw border if border color is not null
            if (_borderColor != null) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(_borderRect, _conerRadius)) {
                    e.Graphics.DrawPath(new(_borderColor.Value, 1), path);
                }
            }

            // Draw text
            if (_realTimeValue != null) {
                g.DrawString(_realTimeValue.Value.ToString("yyyy-MM-dd"), _font, new SolidBrush(ForeColor), _textAreaRect.Location);
            }

            // Draw icon
            if (_iconShowing != null) {
                g.DrawImage(_iconShowing, new Point(_iconAreaRect.Location.X, _iconAreaRect.Location.Y + _vPadding));
            }
        }
        #endregion
    }
}
