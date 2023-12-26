using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Resources;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBox: UserControl {
        private bool _enabled;
        private TextBox _box;
        private Rectangle _borderRect;
        private Color _originalBackColor;
        private Color _disabledBackColor;
        private Color? _borderColor;
        private FontStyle? _boxFontStyle;

        private bool _numberValidate;
        private bool _isError;
        private int _boxOriginalWidth;
        private int _boxErrorWidth;
        private ErrorProvider _errorProvider;
        private Color? _borderColorError;

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
        public override Color BackColor {
            get => base.BackColor;
            set {
                _disabledBackColor = WidgetUtils.ChangeColor(value, .975);
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
        public Color DisabledBackColor { get => _disabledBackColor; set => _disabledBackColor = value; }
        public bool NumberValidate { get => _numberValidate; set => _numberValidate = value; }
        public Color? BorderColorError { get => _borderColorError; set => _borderColorError = value; }

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
            _disabledBackColor = WidgetUtils.ChangeColor(BackColor, .975);

            _box = new() {
                Parent = this,
                BorderStyle = BorderStyle.None,
                Multiline = false,
            };
            _box.GotFocus += (sender, eventArgs) => {
                EventFuncs.CurrentActiveControl = sender as Control;
            };
            _box.TextChanged += (sender, eventArgs) => {
                if (_numberValidate) {
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
        }

        private void ResetErrorIcon() {
            Size newIconSize = new((int) (Height / 2), (int) (Height / 2));
            Image image = WidgetUtils.ResizeImageWithoutLosingQuality(CustomResources.input_error, newIconSize);
            _errorProvider.Icon = Icon.FromHandle(new Bitmap(image).GetHicon());
            _errorProvider.SetIconPadding(_box, (int) (_box.Padding.Right * .5));
            _boxErrorWidth = _boxOriginalWidth - newIconSize.Width;
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += InvokeResizing;
            InvokeResizing(this, e);
        }


        private void InvokeResizing(object? sender, EventArgs eventArgs) {
            Font = new(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .5F, 
                    _boxFontStyle == null ? FontStyle.Regular : _boxFontStyle.Value, GraphicsUnit.Pixel);
            int boxWidth = Width - Padding.Size.Width;
            // Recalculate size and location of box
            _box.Font = Font;
            int padding = (int) (_box.Height * .3) + Margin.Top;
            _boxOriginalWidth = boxWidth - padding * 2;
            ResetErrorIcon();
            if (_isError) {
                _box.Width = _boxErrorWidth;
            } else {
                _box.Width = _boxOriginalWidth;
            }
            _box.Padding = new(padding);
            _box.Location = new(padding, (Height - _box.Height) / 2);

            // Create border rectangle if border color is not null
            if (_borderColor != null) {
                _borderRect = new(0, 0, Width - 1, Height - 1);
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
                e.Graphics.DrawRectangle(new(_borderColorError.Value, 1), _borderRect);
            } else if (_borderColor != null) {
                e.Graphics.DrawRectangle(new(_borderColor.Value, 1), _borderRect);
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
    }
}
