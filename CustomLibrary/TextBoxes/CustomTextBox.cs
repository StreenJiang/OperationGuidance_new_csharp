using CustomLibrary.Configs;
using CustomLibrary.Resources;
using CustomLibrary.Utils;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBox: UserControl {
        #region Fields
        private bool _enabled;
        private TextBox _box;
        private string? _defaultText;
        private int _conerRadius;
        private Rectangle _borderRect;
        private Color _originalBackColor;
        private Color _originalForeColor;
        private Color _hoverBackColor;
        private Color _disabledBackColor;
        private Color? _borderColor;
        private Color? _focusBorderColor;
        private Color? _borderColorError;
        private FontStyle? _boxFontStyle;
        private readonly int _borderThickness = 1;

        private bool _numberValidate;
        private bool _numberOnly;
        private bool _intOnly;
        private bool _positiveIntOnly;
        private bool _isError;
        private int _boxOriginalWidth;
        private int _boxErrorWidth;
        private ErrorProvider _errorProvider;
        private Image? _iconShowing;
        private ToolTip _errorTip;
        private Timer _errorBorderTimer;
        private int _errorBorderTimerInterval = 100;
        private int _timerCount;
        private int _timerCountDelay = 2000;
        private bool _timerTicking = false;
        #endregion

        #region Properties
        public new bool Enabled {
            get => this._enabled;
            set {
                this._enabled = value;
                _box.Enabled = value;
                if (!value) {
                    base.BackColor = _disabledBackColor;
                    _box.BackColor = _disabledBackColor;
                } else {
                    base.BackColor = _originalBackColor;
                    _box.BackColor = _originalBackColor;
                }
            }
        }
        public bool ReadOnly { get => _box.ReadOnly; set => _box.ReadOnly = value; }
        public string? DefaultText {
            get => _defaultText;
            set {
                _defaultText = value;
                if (string.IsNullOrEmpty(_box.Text)) {
                    SetToDefault();
                }
            }
        }
        public TextBox Box { get => _box; }
        public override string Text { get => _box.Text; set => _box.Text = value; }
        public bool Multiline { get => _box.Multiline; set => _box.Multiline = value; }
        public override Color BackColor {
            get => base.BackColor;
            set {
                _disabledBackColor = WidgetUtils.DarkenColor(value, .05);
                if (!_enabled) {
                    base.BackColor = _disabledBackColor;
                    _box.BackColor = _disabledBackColor;
                } else {
                    base.BackColor = value;
                    _box.BackColor = value;
                }
                _originalBackColor = value;
            }
        }
        public FontStyle? BoxFontStyle { get => this._boxFontStyle; set => this._boxFontStyle = value; }
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
        public Color DisabledBackColor { get => _disabledBackColor; set => _disabledBackColor = value; }
        public bool NumberValidate { get => _numberValidate; set => _numberValidate = value; }
        public bool NumberOnly { get => _numberOnly; set => _numberOnly = value; }
        public bool IntOnly { get => _intOnly; set => _intOnly = value; }
        public bool PositiveIntOnly { get => _positiveIntOnly; set => _positiveIntOnly = value; }
        public Color? BorderColorError { get => _borderColorError; set => _borderColorError = value; }
        public bool IsError {
            get => _isError;
            set {
                _isError = value;
                Invalidate();
            }
        }
        public bool TimerTicking { get => _timerTicking; set => _timerTicking = value; }

        public new event EventHandler TextChanged {
            add => _box.TextChanged += value;
            remove => _box.TextChanged -= value;
        }
        #endregion

        #region Constructors
        public CustomTextBox(ErrorProvider? errorProvider = null) : base() {
            DoubleBuffered = true;
            Margin = new(0);
            _enabled = true;
            // If current widget is using in a group, probably not just one, all widgets can share one error provider to save sources
            if (errorProvider == null) {
                _errorProvider = new() {
                    DataMember = null,
                    ContainerControl = this,
                };
            } else {
                _errorProvider = errorProvider;
            }
            _originalBackColor = BackColor;
            _disabledBackColor = WidgetUtils.DarkenColor(BackColor, .1);

            _box = new() {
                Parent = this,
                BorderStyle = BorderStyle.None,
                Multiline = false,
            };
            _box.GotFocus += (sender, eventArgs) => ClearDefault();
            _box.LostFocus += (sender, eventArgs) => SetToDefault();
            _errorTip = new();
            _box.TextChanged += (sender, eventArgs) => {
                if (_box.Text == _defaultText) {
                    return;
                }
                if (_numberOnly || _intOnly || _positiveIntOnly) {
                    int errorCount = 0;
                    int index = 0;
                    foreach (char c in _box.Text) {
                        if ((_positiveIntOnly && !char.IsDigit(c)) ||
                            (_intOnly && !char.IsDigit(c) && !(c == '-' && index == 0)) ||
                            (_numberOnly && !char.IsDigit(c) && c != '.' && !(c == '-' && index == 0))) {
                            _box.Text = _box.Text.Replace(c.ToString(), "");
                            errorCount++;
                        }
                        index++;
                    }
                    if (_numberOnly && _box.Text.Where(c => c == '.').Count() > 1) {
                        _box.Text = new(_box.Text.Take(_box.Text.Length - 1).ToArray());
                        errorCount++;
                    }
                    _box.SelectionStart = _box.Text.Length;
                    _box.SelectionLength = 0;
                    if (errorCount == 0) {
                        _isError = false;
                        HideErrorToolTip();
                        Invalidate();
                        _errorBorderTimer?.Stop();
                        _timerTicking = false;
                    } else {
                        _isError = true;
                        ShowErrorToolTip();
                        Invalidate();
                        _timerCount = 0;
                        _errorBorderTimer?.Start();
                        _timerTicking = true;
                    }
                } else if (_numberValidate) {
                    ResetErrorIcon();
                    foreach (char c in _box.Text) {
                        if (!char.IsDigit(c) && c != '.') {
                            _errorProvider.SetError(_box, "请输入数字");
                            _box.Width = _boxErrorWidth;
                            _isError = true;
                            ShowErrorToolTip();
                            Invalidate();
                            return;
                        }
                    }
                    HideErrorToolTip();
                    _isError = false;
                    _errorProvider.SetError(_box, "");
                    _box.Width = _boxOriginalWidth;
                }
            };

            _borderRect = new();
            _numberValidate = false;
            _numberOnly = false;
            _errorBorderTimer = new() {
                Interval = _errorBorderTimerInterval,
            };
            _errorBorderTimer.Tick += (sender, eventArgs) => {
                if (!IsDisposed) {
                    _timerCount += _errorBorderTimerInterval;
                    if (_timerCount >= _timerCountDelay) {
                        _isError = false;
                        HideErrorToolTip();
                        Invalidate();
                        _errorBorderTimer.Stop();
                        _timerTicking = false;
                    }
                }
            };

            _focusBorderColor = ColorConfigs.COLOR_TEXT_BOX_FOCUS_BORDER;
            _hoverBackColor = WidgetUtils.LightColor(ColorConfigs.COLOR_TEXT_BOX_FOCUS_BORDER, .925);

            MouseEnter += (s, e) => OnMouseEnter();
            MouseLeave += (s, e) => OnMouseLeave();
            GotFocus += (s, e) => ResetBack();
            LostFocus += (s, e) => ResetBack();
            _box.MouseEnter += (s, e) => OnMouseEnter();
            _box.MouseLeave += (s, e) => OnMouseLeave();
            _box.GotFocus += (s, e) => ResetBack();
            _box.LostFocus += (s, e) => ResetBack();

            _originalForeColor = ForeColor;
        }
        #endregion

        #region Reusable methods
        private void SetToDefault() {
            if (!string.IsNullOrEmpty(_defaultText)) {
                if (string.IsNullOrEmpty(_box.Text) || _box.Text == string.Empty) {
                    _box.Text = _defaultText;
                    ForeColor = WidgetUtils.LightColor(_originalForeColor, .5);
                }
            }
        }
        private void ClearDefault() {
            if (!string.IsNullOrEmpty(_defaultText)) {
                if (_box.Text == _defaultText) {
                    _box.Text = string.Empty;
                }
            }
            ForeColor = _originalForeColor;
        }
        public bool IsEmpty() => string.IsNullOrEmpty(_box.Text) || _box.Text == _defaultText;
        private void ResetErrorIcon() {
            Size newIconSize = new((int) (Height / 2), (int) (Height / 2));
            if (_iconShowing == null || _iconShowing.Size != newIconSize) {
                using (Image imageTemp = ResxUtils.Load("input_error")) {
                    _iconShowing = WidgetUtils.ResizeImage(imageTemp, newIconSize);
                    _errorProvider.Icon = Icon.FromHandle(new Bitmap(_iconShowing).GetHicon());
                    _errorProvider.SetIconPadding(_box, (int) (_box.Padding.Right * .5));
                }
            }
            int boxErrorNewWidth = _boxOriginalWidth - newIconSize.Width;
            if (_boxErrorWidth != boxErrorNewWidth) {
                _boxErrorWidth = boxErrorNewWidth;
            }
        }
        private void ShowErrorToolTip() {
            if (!IsDisposed) {
                _errorTip.SetToolTip(_box, "请输入数字");
            }
        }
        private void HideErrorToolTip() {
            if (!IsDisposed) {
                _errorTip.SetToolTip(_box, "");
            }
        }
        public void CheckError(bool flag) {
            // Use property to update appearance
            IsError = flag;
            if (flag) {
                _timerCount = 0;
                _errorBorderTimer.Start();
            }
        }
        #endregion

        #region Override methods
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }
        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            _box.Focus();
        }
        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            int boxWidthTemp = Width - Padding.Size.Width;
            if (!Multiline) {
                Font = new(WidgetsConfigs.SystemFontFamily, (Height - _borderThickness * 2) * .4F,
                        _boxFontStyle == null ? FontStyle.Regular : _boxFontStyle.Value, GraphicsUnit.Pixel);
                // Recalculate size and location of box
                _box.Font = Font;
                int hPadding = (int) (Height / 2.6);
                int vPadding = Margin.Top;
                _boxOriginalWidth = boxWidthTemp - hPadding * 2;
                ResetErrorIcon();
                if (_isError) {
                    _box.Width = _boxErrorWidth;
                } else {
                    _box.Width = _boxOriginalWidth;
                }
                _box.Padding = new(hPadding, vPadding, hPadding, vPadding);
                _box.Location = new(hPadding, (int) ((Height - _box.Height) / 2));
            } else {
                int textBoxHeight = WidgetUtils.TextOrComboBoxHeight();
                Font = new(WidgetsConfigs.SystemFontFamily, textBoxHeight * .4F,
                        _boxFontStyle == null ? FontStyle.Regular : _boxFontStyle.Value, GraphicsUnit.Pixel);
                // Recalculate size and location of box
                _box.Font = Font;
                int hPadding = (int) (textBoxHeight / 6);
                int vPadding = (int) (textBoxHeight / 6);
                _box.Size = new(boxWidthTemp - hPadding * 2, Height - vPadding * 2);
                _box.Location = new(hPadding, (int) (vPadding * .85));
            }

            // Recal coner radius
            _conerRadius = WidgetUtils.ControlRadius();
            int maxRadius = (int) (Height * .485);
            if (_conerRadius > maxRadius) {
                _conerRadius = maxRadius;
            }

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                if (_conerRadius > 0) {
                    _borderRect = new(1, 1, Width - 2 - _borderThickness, Height - 2 - _borderThickness);
                } else {
                    _borderRect = new(0, 0, Width - _borderThickness, Height - _borderThickness);
                }
            }

            // Change region
            if (_conerRadius > 0) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(new(0, 0, Width - 1, Height - 1), _conerRadius)) {
                    Region = new(path);
                }
            }
        }
        private void ResetBack() {
            if (_enabled) {
                _box.BackColor = _originalBackColor;
                base.BackColor = _originalBackColor;
                Invalidate();
            }
        }
        private void OnMouseEnter() {
            if (!_box.ReadOnly && !_box.Focused) {
                _box.BackColor = _hoverBackColor;
                base.BackColor = _hoverBackColor;
            }
        }
        private void OnMouseLeave() {
            if (!_box.ReadOnly && !_box.Focused) {
                _box.BackColor = _originalBackColor;
                base.BackColor = _originalBackColor;
            }
        }
        protected override void OnPaint(PaintEventArgs e) {
            // Change back color if disabled
            if (!_enabled) {
                _box.BackColor = _disabledBackColor;
                e.Graphics.Clear(_disabledBackColor);
            } else {
                _box.BackColor = BackColor;
                e.Graphics.Clear(BackColor);
            }
            base.OnPaint(e);

            if (_conerRadius > 0) {
                using (GraphicsPath path = WidgetUtils.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), _conerRadius)) {
                    using Pen penSurface = new Pen(Parent.BackColor, 1);
                    // Draw surface border for HD result
                    e.Graphics.DrawPath(penSurface, path);
                }
            }

            // Draw border if border color is not null
            if (_borderColorError != null && _isError) {
                if (_conerRadius > 0) {
                    using (GraphicsPath path = WidgetUtils.RoundedRect(_borderRect, _conerRadius)) {
                        e.Graphics.DrawPath(new(_borderColorError.Value, _borderThickness), path);
                    }
                } else {
                    e.Graphics.DrawRectangle(new(_borderColorError.Value, _borderThickness), _borderRect);
                }
            } else if (_borderColor != null) {
                if (_conerRadius > 0) {
                    using (GraphicsPath path = WidgetUtils.RoundedRect(_borderRect, _conerRadius)) {
                        if (_focusBorderColor != null && _box.Focused) {
                            e.Graphics.DrawPath(new(_focusBorderColor.Value, _borderThickness), path);
                        } else {
                            e.Graphics.DrawPath(new(_borderColor.Value, _borderThickness), path);
                        }
                    }
                } else {
                    if (_focusBorderColor != null && _box.Focused) {
                        e.Graphics.DrawRectangle(new(_focusBorderColor.Value, _borderThickness), _borderRect);
                    } else {
                        e.Graphics.DrawRectangle(new(_borderColor.Value, _borderThickness), _borderRect);
                    }
                }
            }
        }
        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _box.ForeColor = ForeColor;
        }
        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            _box.BackColor = BackColor;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _iconShowing?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
