using CustomLibrary.Configs;
using CustomLibrary.Utils;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.Buttons {
    public class ToggleButton: CheckBox {
        #region Fields
        private string _onText;
        private string _offText;
<<<<<<< HEAD
        private bool _showText;
=======
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
        private Point _textLocation;
        private Color _onBackColor;
        private Color _onToggleColor;
        private Color _offBackColor;
        private Color _offToggleColor;
        private bool _isSolid;
        private Size _toggleRectSize;
        private Point _toggleRectLocation;
        private int _toggleBorderThickness;
        private Timer _slideTimer;
        private int _slideStep;
        private readonly int _slideSpend = 30;
        #endregion

        #region Properteis
<<<<<<< HEAD
        public bool ShowText { get => _showText; set => _showText = value; }
=======
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
        public Color OnBackColor { get => _onBackColor; set => _onBackColor = value; }
        public Color OnToggleColor { get => _onToggleColor; set => _onToggleColor = value; }
        public Color OffBackColor { get => _offBackColor; set => _offBackColor = value; }
        public Color OffToggleColor { get => _offToggleColor; set => _offToggleColor = value; }
        public bool IsSolid { get => _isSolid; set => _isSolid = value; }
        #endregion

        #region Constructors
        public ToggleButton() {
            // Default values
            DoubleBuffered = true;
<<<<<<< HEAD
            _showText = true;
=======
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
            _onText = "ON";
            _offText = "OFF";
            _onBackColor = ColorTranslator.FromHtml("#E86C10");
            _onToggleColor = ColorTranslator.FromHtml("#FEFEFE");
            _offBackColor = ColorTranslator.FromHtml("#999999");
            _offToggleColor = ColorTranslator.FromHtml("#FEFEFE");
            _isSolid = true;
            _slideStep = 5;
            Margin = new(0);
            Size = new(60, 20);
<<<<<<< HEAD
            // Invoke initialization methods
=======
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
            CalcToggleSize();
            CalcToggleLocation();
            CalcFontAndTextLocation();
            // Timer
            _slideTimer = new();
            _slideTimer.Tick += TimerTick;
            CalcInterval();
        }
        #endregion

        #region Reusable methods
        private void CalcToggleSize() {
            _toggleBorderThickness = (int) (Math.Ceiling(Height / 15F));
            _toggleRectSize = new((int) ((Width - _toggleBorderThickness * 2) / 2.15), Height - _toggleBorderThickness * 2);
            _slideStep = _toggleRectSize.Width / 5;
            if (_slideStep <= 0) {
                _slideStep = 5;
            }
        }
        private void CalcToggleLocation() {
            if (Checked) {
                _toggleRectLocation = new(Width - _toggleBorderThickness - _toggleRectSize.Width, _toggleBorderThickness);
            } else {
                _toggleRectLocation = new(_toggleBorderThickness, _toggleBorderThickness);
            }
        }
        private void CalcFontAndTextLocation() {
<<<<<<< HEAD
            if (_showText) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .5F, FontStyle.Regular, GraphicsUnit.Pixel);
                using (Graphics g = CreateGraphics()) {
                    int textRangeWidth = Width - _toggleRectSize.Width - _toggleBorderThickness;
                    int x;
                    if (Checked) {
                        x = (int) ((textRangeWidth - g.MeasureString(_onText, Font).Width) / 2 + _toggleBorderThickness);
                    } else {
                        x = (int) ((textRangeWidth - g.MeasureString(_onText, Font).Width) / 2 + _toggleBorderThickness + _toggleRectSize.Width);
                    }
                    _textLocation = new(x, 0);
                }
                _textLocation.Y = (int) ((Height - Font.Height - _toggleBorderThickness) / 1.5);
            }
=======
            Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .5F, FontStyle.Regular, GraphicsUnit.Pixel);
            using (Graphics g = CreateGraphics()) {
                int textRangeWidth = Width - _toggleRectSize.Width - _toggleBorderThickness;
                int x;
                if (Checked) {
                    x = (int) ((textRangeWidth - g.MeasureString(_onText, Font).Width) / 2 + _toggleBorderThickness);
                } else {
                    x = (int) ((textRangeWidth - g.MeasureString(_onText, Font).Width) / 2 + _toggleBorderThickness + _toggleRectSize.Width);
                }
                _textLocation = new(x, 0);
            }
            _textLocation.Y = (int) ((Height - Font.Height - _toggleBorderThickness) / 1.5);
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
        }
        private void CalcInterval() {
            int interval = (int) (_slideSpend / ((decimal) (Width - _toggleBorderThickness) / _slideStep));
            if (interval > 0) {
                _slideTimer.Interval = interval;
            } else {
                _slideTimer.Interval = 1;
            }
        }
        #endregion

        #region Events
        private void TimerTick(object? sender, EventArgs eventArgs) {
            if (Checked) {
                if (_toggleRectLocation.X + _slideStep >= Width - _toggleBorderThickness - _toggleRectSize.Width) {
                    _toggleRectLocation.X = Width - _toggleBorderThickness - _toggleRectSize.Width;
                    _slideTimer.Stop();
                } else {
                    _toggleRectLocation.X += _slideStep;
                }
            } else {
                if (_toggleRectLocation.X - _slideStep <= _toggleBorderThickness) {
                    _toggleRectLocation.X = _toggleBorderThickness;
                    _slideTimer.Stop();
                } else {
                    _toggleRectLocation.X -= _slideStep;
                }
            }
            Invalidate();
        }
        #endregion

        #region Overrdie method
        protected override void OnCheckedChanged(EventArgs e) {
            base.OnCheckedChanged(e);
            CalcFontAndTextLocation();
            _slideTimer.Start();
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            CalcToggleSize();
            CalcToggleLocation();
            CalcFontAndTextLocation();
            CalcInterval();
        }
        protected override void OnPaint(PaintEventArgs pevent) {
            Graphics g = pevent.Graphics;
            if (Checked) {
                if (_isSolid) {
                    g.Clear(_onBackColor);
<<<<<<< HEAD
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(_onToggleColor), _textLocation);
                    }
=======
                    g.DrawString(_onText, Font, new SolidBrush(_onToggleColor), _textLocation);
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
                } else {
                    g.Clear(Parent.BackColor);
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    g.DrawRectangle(new(_onBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
<<<<<<< HEAD
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(_onBackColor), _textLocation);
                    }
=======
                    g.DrawString(_onText, Font, new SolidBrush(_onBackColor), _textLocation);
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
                }
                g.FillRectangle(new SolidBrush(_onToggleColor), new(_toggleRectLocation, _toggleRectSize));
            } else {
                if (_isSolid) {
                    g.Clear(_offBackColor);
<<<<<<< HEAD
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(_offToggleColor), _textLocation);
                    }
=======
                    g.DrawString(_offText, Font, new SolidBrush(_offToggleColor), _textLocation);
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
                } else {
                    g.Clear(Parent.BackColor);
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    g.DrawRectangle(new(_offBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
<<<<<<< HEAD
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(_offBackColor), _textLocation);
                    }
=======
                    g.DrawString(_offText, Font, new SolidBrush(_offBackColor), _textLocation);
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
                }
                g.FillRectangle(new SolidBrush(_offToggleColor), new(_toggleRectLocation, _toggleRectSize));
            }
        }
        #endregion

    }
}
