using CustomLibrary.Configs;
using CustomLibrary.Utils;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.Buttons {
    public class ToggleButton: CheckBox {
        #region Fields
        private string _onText;
        private string _offText;
        private bool _showText;
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
        private readonly double _disabledDilutionRatio = .2;
        private ToolTip _toolTip;
        private bool _enabled;
        #endregion

        #region Properteis
        public bool ShowText { get => _showText; set => _showText = value; }
        public Color OnBackColor { get => _onBackColor; set => _onBackColor = value; }
        public Color OnToggleColor { get => _onToggleColor; set => _onToggleColor = value; }
        public Color OffBackColor { get => _offBackColor; set => _offBackColor = value; }
        public Color OffToggleColor { get => _offToggleColor; set => _offToggleColor = value; }
        public bool IsSolid { get => _isSolid; set => _isSolid = value; }
        public new bool Enabled {
            get => _enabled;
            set {
                _enabled = value;
                if (value) {
                    _toolTip.SetToolTip(this, string.Empty);
                } else {
                    _toolTip.SetToolTip(this, "已禁用");
                }
            }
        }
        #endregion

        #region Constructors
        public ToggleButton() {
            // Default values
            DoubleBuffered = true;
            _showText = true;
            _onText = "ON";
            _offText = "OFF";
            _onBackColor = ColorTranslator.FromHtml("#E86C10");
            _onToggleColor = ColorTranslator.FromHtml("#FEFEFE");
            _offBackColor = ColorTranslator.FromHtml("#999999");
            _offToggleColor = ColorTranslator.FromHtml("#FEFEFE");
            _isSolid = true;
            _slideStep = 5;
            _enabled = true;
            Margin = new(0);
            Size = new(60, 20);
            // Invoke initialization methods
            CalcToggleSize();
            CalcToggleLocation();
            CalcFontAndTextLocation();
            // Timer
            _slideTimer = new();
            _slideTimer.Tick += TimerTick;
            CalcInterval();
            // Tooltip
            _toolTip = new() {
                InitialDelay = 50,
            };
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
                    g.DrawString(_onText, Font, new SolidBrush(_onBackColor), _textLocation);
                    _textLocation = new(x, 0);
                }
                _textLocation.Y = (int) ((Height - Font.Height - _toggleBorderThickness) / 1.5);
            }
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
        protected override void OnClick(EventArgs e) {
            if (_enabled) {
                base.OnClick(e);
            }
        }
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
            Color onBackColor = _onBackColor;
            Color onToggleColor = _onToggleColor;
            Color offBackColor = _offBackColor;
            Color offToggleColor = _offToggleColor;
            if (!Enabled) {
                onBackColor = WidgetUtils.LightColor(_onBackColor, _disabledDilutionRatio);
                onToggleColor = WidgetUtils.LightColor(_onToggleColor, _disabledDilutionRatio);
                offBackColor = WidgetUtils.LightColor(_offBackColor, _disabledDilutionRatio);
                offToggleColor = WidgetUtils.LightColor(_offToggleColor, _disabledDilutionRatio);
            }
            if (Checked) {
                if (_isSolid) {
                    g.Clear(onBackColor);
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(onToggleColor), _textLocation);
                    }
                } else {
                    g.Clear(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio));
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    g.DrawRectangle(new(onBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(onBackColor), _textLocation);
                    }
                }
                g.FillRectangle(new SolidBrush(onToggleColor), new(_toggleRectLocation, _toggleRectSize));
            } else {
                if (_isSolid) {
                    g.Clear(offBackColor);
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(offToggleColor), _textLocation);
                    }
                } else {
                    g.Clear(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio));
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    g.DrawRectangle(new(offBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(offBackColor), _textLocation);
                    }
                }
                g.FillRectangle(new SolidBrush(offToggleColor), new(_toggleRectLocation, _toggleRectSize));
            }
        }
        #endregion

    }
}
