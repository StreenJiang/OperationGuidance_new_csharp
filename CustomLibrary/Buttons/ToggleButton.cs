using System.Drawing.Drawing2D;
using CustomLibrary.Configs;
using CustomLibrary.Utils;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.Buttons {
    public class ToggleButton: CheckBox {
        #region Fields
        private string _onText;
        private string _offText;
        private bool _showText;
        private int _conerRadius;
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
        public int ConerRadius { get => _conerRadius; set => _conerRadius = value; }
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
            if (_conerRadius > 0) {
                _toggleBorderThickness = (int) (Math.Ceiling((Height - 2) / 7.5F));
                _toggleRectSize = new((int) ((Width - 2 - _toggleBorderThickness * 2) / 2.25), Height - 2 - _toggleBorderThickness * 2);
            } else {
                _toggleBorderThickness = (int) (Math.Ceiling(Height / 7.5F));
                _toggleRectSize = new((int) ((Width - _toggleBorderThickness * 2) / 2.25), Height - _toggleBorderThickness * 2);
            }

            _slideStep = _toggleRectSize.Width / 5;
            if (_slideStep <= 0) {
                _slideStep = 5;
            }
        }
        private void CalcToggleLocation() {
            if (Checked) {
                if (_conerRadius > 0) {
                    _toggleRectLocation = new(Width - 2 - _toggleBorderThickness - _toggleRectSize.Width, _toggleBorderThickness + 1);
                } else {
                    _toggleRectLocation = new(Width - 1 - _toggleBorderThickness - _toggleRectSize.Width, _toggleBorderThickness);
                }
            } else {
                if (_conerRadius > 0) {
                    _toggleRectLocation = new(_toggleBorderThickness + 1, _toggleBorderThickness + 1);
                } else {
                    _toggleRectLocation = new(_toggleBorderThickness, _toggleBorderThickness);
                }
            }

        }
        private void CalcFontAndTextLocation() {
            if (_showText) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .4F, FontStyle.Regular, GraphicsUnit.Pixel);
                int textRangeWidth = Width - _toggleRectSize.Width - _toggleBorderThickness;
                Size textSize = WidgetUtils.MeasureString(_onText, Font);
                int x;
                if (Checked) {
                    x = (int) ((textRangeWidth - textSize.Width) / 2);
                } else {
                    x = (int) ((textRangeWidth - textSize.Width) / 2 + _toggleRectSize.Width);
                }
                _textLocation = new(x, (Height - textSize.Height) / 2);
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
            // Recal coner radius
            _conerRadius = WidgetUtils.ControlRadius();
            // Change region
            if (_conerRadius > 0) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(new(0, 0, Width - 1, Height - 1), _conerRadius)) {
                    Region = new(path);
                }
            }

            CalcToggleSize();
            CalcToggleLocation();
            CalcFontAndTextLocation();
            CalcInterval();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs pevent) {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            g.Clear(Parent.BackColor);
            if (_conerRadius > 0) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), _conerRadius)) {
                    using Pen penSurface = new Pen(Parent.BackColor, 1);
                    // Draw surface border for HD result
                    g.DrawPath(penSurface, path);
                }
            }

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
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), _conerRadius)) {
                            g.FillPath(new SolidBrush(onBackColor), path);
                        }
                    } else {
                        g.Clear(onBackColor);
                    }
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(onToggleColor), _textLocation);
                    }
                } else {
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), _conerRadius)) {
                            g.FillPath(new SolidBrush(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio)), path);
                        }
                    } else {
                        g.Clear(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio));
                    }
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new(borderLocation, borderSize), _conerRadius)) {
                            g.DrawPath(new(onBackColor, _toggleBorderThickness), path);
                        }
                    } else {
                        g.DrawRectangle(new(onBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
                    }
                    if (_showText) {
                        g.DrawString(_onText, Font, new SolidBrush(onBackColor), _textLocation);
                    }
                }
                if (_conerRadius > 0) {
                    using (GraphicsPath path = WidgetUtils.RoundedRect(new(_toggleRectLocation, _toggleRectSize - new Size(1, 1)), _conerRadius)) {
                        g.FillPath(new SolidBrush(onToggleColor), path);
                    }
                } else {
                    g.FillRectangle(new SolidBrush(onToggleColor), new(_toggleRectLocation, _toggleRectSize));
                }
            } else {
                if (_isSolid) {
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), _conerRadius)) {
                            g.FillPath(new SolidBrush(offBackColor), path);
                        }
                    } else {
                        g.Clear(offBackColor);
                    }
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(offToggleColor), _textLocation);
                    }
                } else {
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), _conerRadius)) {
                            g.FillPath(new SolidBrush(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio)), path);
                        }
                    } else {
                        g.Clear(Enabled ? Parent.BackColor : WidgetUtils.LightColor(Parent.BackColor, _disabledDilutionRatio));
                    }
                    Size borderSize = new(Width - _toggleBorderThickness, Height - _toggleBorderThickness);
                    Point borderLocation = new(_toggleBorderThickness - 1, _toggleBorderThickness - 1);
                    if (_conerRadius > 0) {
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new(borderLocation, borderSize), _conerRadius)) {
                            g.DrawPath(new(offBackColor, _toggleBorderThickness), path);
                        }
                    } else {
                        g.DrawRectangle(new(offBackColor, _toggleBorderThickness), new(borderLocation, borderSize));
                    }
                    if (_showText) {
                        g.DrawString(_offText, Font, new SolidBrush(offBackColor), _textLocation);
                    }
                }
                if (_conerRadius > 0) {
                    using (GraphicsPath path = WidgetUtils.RoundedRect(new(_toggleRectLocation, _toggleRectSize - new Size(1, 1)), _conerRadius)) {
                        g.FillPath(new SolidBrush(offToggleColor), path);
                    }
                } else {
                    g.FillRectangle(new SolidBrush(offToggleColor), new(_toggleRectLocation, _toggleRectSize));
                }
            }
        }
        #endregion

    }
}
