using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Resources;
using CustomLibrary.Utils;
using System.ComponentModel;
using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBox: UserControl {
        #region Fields
        private bool _enabled;
        private TextBox _box;
        private Rectangle _borderRect;
        private Color _originalBackColor;
        private Color _disabledBackColor;
        private Color? _borderColor;
        private FontStyle? _boxFontStyle;
        private readonly int _borderThickness = 1;

        private bool _numberValidate;
        private bool _numberOnly;
        private bool _isError;
        private int _boxOriginalWidth;
        private int _boxErrorWidth;
        private ErrorProvider _errorProvider;
        private Image? _iconShowing;
        private Color? _borderColorError;
        private ToolTip _errorTip;
        private Timer _errorBorderTimer;
        private int _timerCount;
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
        public TextBox Box { get => _box; }
        public override string Text { get => _box.Text; set => _box.Text = value; }
        public bool Multiline { get => _box.Multiline; set => _box.Multiline = value; }
        public override Color BackColor {
            get => base.BackColor;
            set {
                _disabledBackColor = WidgetUtils.DarkerColor(value, .1);
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
        public Color? BorderColorError { get => _borderColorError; set => _borderColorError = value; }
        public bool IsError { get => _isError; set => _isError = value; }
        #endregion

        #region Constructors
        public CustomTextBox(ErrorProvider? errorProvider = null) : base() {
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
            _disabledBackColor = WidgetUtils.DarkerColor(BackColor, .1);

            _box = new() {
                Parent = this,
                BorderStyle = BorderStyle.None,
                Multiline = false,
            };
            _box.GotFocus += (sender, eventArgs) => {
                EventFuncs.CurrentActiveControl = sender as Control;
            };
            _errorTip = new();
            _box.TextChanged += (sender, eventArgs) => {
                if (_numberOnly) {
                    int errorCount = 0;
                    foreach (char c in _box.Text) {
                        if (!char.IsDigit(c) && c != '.') {
                            _box.Text = _box.Text.Replace(c.ToString(), "");
                            errorCount++;
                        }
                    }
                    _box.SelectionStart = _box.Text.Length;
                    _box.SelectionLength = 0;
                    if (errorCount == 0) {
                        _isError = false;
                        HideErrorToolTip();
                        Invalidate();
                    } else {
                        _isError = true;
                        ShowErrorToolTip();
                        Invalidate();
                        _timerCount = 0;
                        _errorBorderTimer?.Start();
                    }
                } else if (_numberValidate) {
                    ResetErrorIcon();
                    foreach (char c in _box.Text) {
                        if (!char.IsDigit(c) && c != '.') {
                            _errorProvider.SetError(_box, "请输入数字");
                            _box.Width = _boxErrorWidth;
                            _isError = true;
                            Invalidate();
                            return;
                        }
                    }
                    _isError = false;
                    _errorProvider.SetError(_box, "");
                    _box.Width = _boxOriginalWidth;
                }
            };

            _borderRect = new();
            _numberValidate = false;
            _numberOnly = false;
            _errorBorderTimer = new() {
                Interval = 1000,
            };
            _errorBorderTimer.Tick += (sender, eventArgs) => {
                _timerCount += 1000;
                if (_timerCount >= 2000) {
                    _isError = false;
                    HideErrorToolTip();
                    Invalidate();
                    _timerCount = 0;
                    _errorBorderTimer.Stop();
                }
            };
        }
        #endregion

        #region Reusable methods
        private void ResetErrorIcon() {
            Size newIconSize = new((int) (Height / 2), (int) (Height / 2));
            if (_iconShowing == null || _iconShowing.Size != newIconSize) {
                _iconShowing = WidgetUtils.ResizeImageWithoutLosingQuality(CustomResources.input_error, newIconSize);
                _errorProvider.Icon = Icon.FromHandle(new Bitmap(_iconShowing).GetHicon());
                _errorProvider.SetIconPadding(_box, (int) (_box.Padding.Right * .5));
            }
            int boxErrorNewWidth = _boxOriginalWidth - newIconSize.Width;
            if (_boxErrorWidth != boxErrorNewWidth) {
                _boxErrorWidth = boxErrorNewWidth;
            }
        }
        private void ShowErrorToolTip() {
            _errorTip.Show("请输入数字", _box);
        }
        private void HideErrorToolTip() {
            _errorTip.Hide(_box);
        }
        #endregion

        #region Override methods
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }
        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            Font = new(WidgetsConfigs.SystemFontFamily, (Height - _borderThickness * 2) * .53F, 
                    _boxFontStyle == null ? FontStyle.Regular : _boxFontStyle.Value, GraphicsUnit.Pixel);
            int boxWidthTemp = Width - Padding.Size.Width;
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
            _box.Location = new(hPadding, (int) ((Height - _box.Height) / 1.8));

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                _borderRect = new(0, 0, Width - _borderThickness, Height - _borderThickness);
            }
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Change back color if disabled
            if (!_enabled) {
                _box.BackColor = _disabledBackColor;
            } else {
                _box.BackColor = BackColor;
            }

            // Draw border if border color is not null
            if (_borderColorError != null && _isError) {
                e.Graphics.DrawRectangle(new(_borderColorError.Value, _borderThickness), _borderRect);
            } else if (_borderColor != null) {
                e.Graphics.DrawRectangle(new(_borderColor.Value, _borderThickness), _borderRect);
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
        #endregion
    }
}
