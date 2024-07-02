using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace CustomLibrary.Buttons.AbstractClasses {
    public abstract class AbstractCustomButton: Button {
        // Enum: toggle bar direction
        public enum ToggleBarDirectionEnum {
            TOP, BOTTOM, LEFT, RIGHT
        }

        // Fields
        private int _conerRadius;
        private bool _toggleButton;
        private bool _up;
        private bool _groupMode;
        private bool _toggleBar;
        private Color? _toggleBarColor;
        private ToggleBarDirectionEnum? _toggleBarDirection;
        private Rectangle? toggleBarRect;
        private int _barThickness;
        private const float _toggleBarHorizontalHeightRatio = 0.125F;
        private const float _toggleBarVerticalHeightRatio = 0.175F;
        private Color? _toggledColor;
        private Color? _saveBackColor;
        private bool _isMouseIn;
        private bool _loaded;
        private Size _extraSize;
        private bool _blockHoverUp;
        private bool _blockHoverDown;
        private Color? _disableColor;
        private Color? _enabledSaveBackColor;
        private double _clickDelay = 300;
        private double _lastClickTimeStamp = 0;

        // Label
        private string? _label;
        private int? _labelX;
        private int? _labelY;
        private bool _isPressing;

        public string? Label {
            get => this._label;
            set {
                this._label = value;
                ResizeTextLabel();
            }
        }
        public int? LabelX {
            get => this._labelX;
            set => this._labelX = value;
        }
        public int? LabelY {
            get => this._labelY;
            set => this._labelY = value;
        }
        protected Size ExtraSize {
            get => _extraSize;
            set => _extraSize = value;
        }
        public int ConerRadius {
            get => _conerRadius;
            set {
                _conerRadius = value;
                int maxRadius = (int) (Height * .485);
                if (_conerRadius > maxRadius) {
                    _conerRadius = maxRadius;
                }
                Invalidate();
            }
        }
        public bool ToggledButton {
            get => _toggleButton;
            set {
                _toggleButton = value;
                Invalidate();
            }
        }
        public bool GroupMode {
            get => _groupMode;
            set => _groupMode = value;
        }
        public bool ToggleBar {
            get => _toggleBar;
            set {
                _toggleBar = value;
                Invalidate();
            }
        }
        public ToggleBarDirectionEnum? ToggleBarDirection {
            get => _toggleBarDirection;
            set {
                _toggleBarDirection = value;
                Invalidate();
            }
        }
        public Color? ToggledColor {
            get => _toggledColor;
            set {
                _toggledColor = value;
                Invalidate();
            }
        }
        public bool Toggled {
            get => !_up;
        }
        protected int BarThickness {
            get => _barThickness;
        }
        protected bool IsMouseIn {
            get => _isMouseIn;
        }
        public bool Loaded {
            get => this._loaded;
            set => this._loaded = value;
        }
        public bool IsPressing {
            get => this._isPressing;
            set => this._isPressing = value;
        }
        public bool BlockHoverUp {
            get => this._blockHoverUp;
            set => this._blockHoverUp = value;
        }
        public bool BlockHoverDown {
            get => this._blockHoverDown;
            set => this._blockHoverDown = value;
        }
        public Rectangle? ToggleBarRect {
            get => this.toggleBarRect;
        }
        public Color? ToggleBarColor { get => _toggleBarColor; set => _toggleBarColor = value; }

        //private const int CS_DropShadow = 0x00020000;
        //protected override CreateParams CreateParams {
        //    get {
        //        CreateParams cp = base.CreateParams;
        //        cp.ClassStyle = CS_DropShadow;
        //        return cp;
        //    }
        //}

        // Constructor
        public AbstractCustomButton() {
            // 去掉黑框
            this.SetStyle(ControlStyles.Selectable, false);

            // Initialize fields with default value
            _conerRadius = 0;
            _toggleButton = false;
            _up = true;
            _isMouseIn = false;
            _groupMode = false;
            _loaded = false;
            _blockHoverUp = false;
            _blockHoverDown = false;

            // Some default value of other fields of Button
            //DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Size = new Size(150, 40);
            Margin = new Padding(0);

            this.DoubleBuffered = true;
        }

        // Methods
        protected override void OnCreateControl() {
            base.OnCreateControl();
            _loaded = true;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
            ChangeRegionByConerRadius();
        }

        private void InvokeResizing() {
            Form? form = TopLevelControl as Form;
            if (form is not null && form.WindowState == FormWindowState.Minimized) {
                return;
            }
            if (_toggleButton && _toggleBar) {
                if (_toggleBar) {
                    // Draw toggle bar if needed
                    int barHeight = (int) (Height * .6);
                    int coordinate = _conerRadius > 0 ? 1 : 0;
                    if (_toggleBarDirection == null) {
                        _toggleBarDirection = ToggleBarDirectionEnum.LEFT;
                    }
                    switch (_toggleBarDirection) {
                        case ToggleBarDirectionEnum.LEFT:
                            coordinate += (Height - barHeight) / 2 - 1;
                            _barThickness = (int) (Height * _toggleBarVerticalHeightRatio);
                            toggleBarRect = new Rectangle(coordinate, coordinate, _barThickness, barHeight);
                            break;
                        case ToggleBarDirectionEnum.RIGHT:
                            coordinate += (Height - barHeight) / 2 - 1;
                            _barThickness = (int) (Height * _toggleBarVerticalHeightRatio);
                            toggleBarRect = new Rectangle(Width - _barThickness - coordinate, coordinate, _barThickness, barHeight);
                            break;
                        case ToggleBarDirectionEnum.BOTTOM:
                            _barThickness = (int) (Height * _toggleBarHorizontalHeightRatio);
                            toggleBarRect = new Rectangle(coordinate, Height - _barThickness - coordinate, Width, _barThickness);
                            break;
                        case ToggleBarDirectionEnum.TOP:
                        default:
                            _barThickness = (int) (Height * _toggleBarHorizontalHeightRatio);
                            toggleBarRect = new Rectangle(coordinate, coordinate, Width, _barThickness);
                            break;
                    }
                }
            }
            // Resize font
            ResizeTextLabel();
        }

        protected void ChangeRegionByConerRadius() {
            if (_conerRadius > 0) {
                using (GraphicsPath path = GetGraphicsPath(new Rectangle(0, 0, Width - 1, Height - 1))) {
                    Region = new Region(path);
                }
            }
        }

        protected abstract void ResizeTextLabel();

        protected GraphicsPath GetGraphicsPath(Rectangle rect) {
            // GraphicsPath graphicsPath = new GraphicsPath();
            // graphicsPath.StartFigure();
            // // 方向->顺时针：3点钟方向是0，6点钟方向是90，9点钟方向是180，12点钟方向是270
            // graphicsPath.AddArc(rect.X, rect.Y, this._conerRadius, _conerRadius, 180, 90);
            // graphicsPath.AddArc(rect.Width - _conerRadius - 1, rect.Y, _conerRadius, _conerRadius, 270, 90);
            // graphicsPath.AddArc(rect.Width - _conerRadius - 1, rect.Height - _conerRadius - 1, _conerRadius, _conerRadius, 0, 90);
            // graphicsPath.AddArc(rect.X, rect.Height - _conerRadius - 1, _conerRadius, _conerRadius, 90, 90);
            // graphicsPath.CloseFigure();

            return WidgetUtils.RoundedRect(rect, _conerRadius);
        }

        protected sealed override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            // e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (_conerRadius > 0) { // Rrounded button
                using (GraphicsPath path = GetGraphicsPath(new Rectangle(0, 0, Width - 1, Height - 1))) {
                    using Pen penSurface = new Pen(Parent.BackColor, 1);
                    // Draw surface border for HD result
                    e.Graphics.DrawPath(penSurface, path);
                }
            }

            Size toggleBarExtraSize = new();
            if (_isPressing && !_blockHoverDown) {
                _extraSize.Width += 1;
                _extraSize.Height += 1;
                toggleBarExtraSize = new(1, 1);
            }
            if (_isMouseIn && !_blockHoverUp) {
                _extraSize.Width -= 1;
                _extraSize.Height -= 1;
                toggleBarExtraSize = new(-1, -1);
            }

            // Check if is toggle button
            if (_toggleButton) {
                if (Toggled) {
                    // Change background color to toggled color if exits
                    if (_toggledColor != null) {
                        if (_saveBackColor == null) {
                            _saveBackColor = BackColor;
                        }
                        BackColor = _toggledColor.Value;
                    }
                    if (_toggleBar && toggleBarRect != null) {
                        Rectangle rect = toggleBarRect.Value;
                        float radius;
                        if (rect.Width < rect.Height) {
                            radius = rect.Width / 2;
                        } else {
                            radius = rect.Height / 2;
                        }
                        rect.Location = new(rect.Location.X + toggleBarExtraSize.Width, rect.Location.Y + toggleBarExtraSize.Height);
                        GraphicsPath graphicsPath = WidgetUtils.RoundedRect(rect, (int) radius);
                        e.Graphics.FillPath(new SolidBrush(_toggleBarColor == null ? ForeColor : _toggleBarColor.Value), graphicsPath);
                    }
                } else {
                    if (_saveBackColor != null) {
                        this.BackColor = _saveBackColor.Value;
                    }
                }
            }

            // If disabled, then color should be lighter
            if (!this.Enabled) {
                if (this._disableColor == null) {
                    this._disableColor = WidgetUtils.LightColor(this.BackColor, .2);
                    this._enabledSaveBackColor = this.BackColor;
                }
                this.BackColor = this._disableColor.Value;
            } else {
                if (this._enabledSaveBackColor != null) {
                    this.BackColor = this._enabledSaveBackColor.Value;
                }
            }

            PaintAfter(e);
            _extraSize = new();
        }

        protected abstract void PaintAfter(PaintEventArgs e);

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
        }

        protected override void OnClick(EventArgs e) {
            // Add this to prevent user from multiple click incorrectly
            double clickTimeStamp = WidgetUtils.GetTimeMillisec(DateTime.Now);
            if (_lastClickTimeStamp == 0 || clickTimeStamp - _lastClickTimeStamp >= _clickDelay) {
                _lastClickTimeStamp = clickTimeStamp;
                base.OnClick(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs mevent) {
            base.OnMouseDown(mevent);
            if (_toggleButton && Enabled && !_groupMode) {
                _up = !_up;
            }
            _isPressing = true;
        }

        protected override void OnMouseUp(MouseEventArgs mevent) {
            base.OnMouseUp(mevent);
            _isPressing = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            _isMouseIn = true;
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _isMouseIn = false;
        }

        public void SetToggle(bool flag) {
            _up = !flag;
            Refresh();
        }

        // Set 'selectable' to false mess 'SetStyle' up and block base.PerformClick, so NEW a new one
        public new void PerformClick() {
            OnClick(EventArgs.Empty);
        }
    }
}
