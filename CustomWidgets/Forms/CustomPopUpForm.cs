using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Resources;
using CustomLibrary.Utils;

namespace CustomLibrary.Forms {

    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomPopUpForm: Form {
        private Form _deviceDetailBackground;
        private Rectangle? _borderRect;
        private Color? _borderColor;
        private int _titleHeight;
        private Size _contentSize;
        private CloseButton _closeButton = new();
        private bool _hasTitleBar = true;
        private string? _title;
        private Font? _titleFont;
        private int _virtualHorizontalPadding;
        private int _virtualVerticalPadding;

        private List<CommonButton> _buttons;
        private HorizontalAlignment _buttonAlignment;
        private int _buttonPanelHeight;

        public Color? BorderColor {
            get => this._borderColor;
            set => this._borderColor = value;
        }
        public Size ContentSize {
            get => this._contentSize;
            set {
                this._contentSize = value;
                this.Size = new Size(value.Width, value.Height + HasTitleExtraHeight + HasButtonExtraHeight);
            }
        }

        public bool HasTitleBar {
            get => this._hasTitleBar;
            set {
                this._hasTitleBar = value;
                if (value) {
                    this._closeButton.Show();
                } else {
                    this._closeButton.Hide();
                }
            }
        }

        public Form BackForm {
            get => this._deviceDetailBackground;
            set => this._deviceDetailBackground = value;
        }

        public int TitleHeight {
            get {
                this._titleHeight = this.CalculateTitleHeight();
                return this._titleHeight;
            }
        }
        private int CalculateTitleHeight() {
            if (!this._hasTitleBar) {
                return 0;
            }
            Control mainParent = WidgetUtils.MainPanel.Parent;
            return (int) (mainParent.Height * .0415) + (int) (Math.Abs(mainParent.Width - mainParent.Height) * .0035);
        }
        public int HasTitleExtraHeight {
            get {
                return this.TitleHeight;
            }
        }
        public string? Title {
            get => this._title;
            set => this._title = value;
        }
        public int VirtualHorizontalPadding {
            get => this._virtualHorizontalPadding;
            set => this._virtualHorizontalPadding = value;
        }
        public int VirtualVerticalPadding {
            get => this._virtualVerticalPadding;
            set => this._virtualVerticalPadding = value;
        }
        public HorizontalAlignment ButtonAlignment {
            get => this._buttonAlignment;
            set {
                RelocateButtons();
                this._buttonAlignment = value;
            }
        }
        public int HasButtonExtraHeight {
            get {
                return ButtonPanelHeight;
            }
        }

        public int ButtonPanelHeight {
            get {
                _buttonPanelHeight = CalculateButtonPanelHeight();
                return this._buttonPanelHeight;
            }
        }

        public CustomPopUpForm() : base() {
            _deviceDetailBackground = new() {
                Owner = (Form) WidgetUtils.MainPanel.Parent,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Opacity = .25D,
                BackColor = Color.Black,
                ShowInTaskbar = false
            };
            _deviceDetailBackground.Owner.LocationChanged += (s, e) => {
                this.ChangeLocation();
            };
            _deviceDetailBackground.Hide();

            this.Owner = _deviceDetailBackground;
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;

            _virtualHorizontalPadding = 0;
            _virtualVerticalPadding = 0;

            // 初始化关闭按钮相关属性和事件
            this._closeButton.Parent = this;
            this._closeButton.Click += (s, e) => {
                this.HideForm();
            };

            // 初始化按钮List和按钮对齐方式
            _buttons = new();
            _buttonAlignment = HorizontalAlignment.Right;
        }

        public virtual void HideForm() {
            this.Hide();
            _deviceDetailBackground.Owner.Focus();
            _deviceDetailBackground.Hide();
        }

        public CommonButton AddButton(string label) {
            CommonButton button = new() {
                Label = label,
                Parent = this,
            };
            _buttons.Add(button);
            InvokeResizing();
            return button;
        }

        public void RemoveButton(CommonButton button) {
            _buttons.Remove(button);
        }

        public int CalculateButtonPanelHeight() {
            if (_buttons.Count == 0) {
                return 0;
            }
            Control mainParent = WidgetUtils.MainPanel.Parent;
            return (int) (mainParent.Height * .0415) + (int) (Math.Abs(mainParent.Width - mainParent.Height) * .0035);
        }

        public void RelocateButtons() {
            Control mainParent = WidgetUtils.MainPanel.Parent;
            int btnGap = (int) (mainParent.Width * .01);
            int y = Height - _buttonPanelHeight - _virtualVerticalPadding;
            int preBtnW = 0;
            switch (_buttonAlignment) {
                case HorizontalAlignment.Left:
                    int leftFirst = _virtualHorizontalPadding;
                    for (int i = 0; i < _buttons.Count; i++) {
                        CommonButton btn = _buttons[i];
                        if (i == 0) {
                            btn.Location = new(leftFirst, y);
                        } else {
                            btn.Location = new(leftFirst + preBtnW + btnGap * i, y);
                        }
                        preBtnW += btn.Width;
                    }
                    break;
                case HorizontalAlignment.Right:
                    int counting = 0;
                    int rightLast = Width - _virtualHorizontalPadding;
                    for (int i = _buttons.Count - 1; i >= 0; i--) {
                        CommonButton btn = _buttons[i];
                        if (counting == 0) {
                            btn.Location = new(rightLast - btn.Width, y);
                        } else {
                            btn.Location = new(rightLast - btn.Width - preBtnW - btnGap * counting, y);
                        }
                        preBtnW += btn.Width;
                        counting++;
                    }
                    break;
                case HorizontalAlignment.Center:
                    int sumBtnW = 0;
                    foreach (CommonButton btn in _buttons) {
                        sumBtnW += btn.Width;
                    }
                    int sumGap = btnGap * _buttons.Count;
                    int centerStart = (Width - sumBtnW - sumGap) / 2 + _virtualHorizontalPadding;
                    for (int i = 0; i < _buttons.Count; i++) {
                        CommonButton btn = _buttons[i];
                        if (i == 0) {
                            btn.Location = new(centerStart, y);
                        } else {
                            btn.Location = new(centerStart + preBtnW + btnGap * i, y);
                        }
                        preBtnW += btn.Width;
                    }
                    break;
            }
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
            ChangeLocation();
            Invalidate();
        }

        private void InvokeResizing() {
            Control mainParent = WidgetUtils.MainPanel.Parent;
            _deviceDetailBackground.Size = mainParent.ClientSize;
            this._virtualHorizontalPadding = (int) (mainParent.Width * .006);
            this._virtualVerticalPadding = (int) (mainParent.Height * .00775);

            if (this._hasTitleBar) {
                this._titleHeight = this.CalculateTitleHeight();
                this._closeButton.Size = new((int) (this._titleHeight * 1.25), this._titleHeight - 1);
                // 标题字体
                this._titleFont = new Font(WidgetsConfigs.SystemFontFamily, this._titleHeight * .47F, FontStyle.Regular);
                this._closeButton.Location = new(this.Width - this._closeButton.Width - 1, 1);
            }

            if (this._borderColor != null) {
                this._borderRect = new(0, 0, this.Width - 1, this.Height - 1);
            }
            int width = this.Width;
            int height = this.Height;
            if (this._hasTitleBar) {
                width -= 1;
                height -= _titleHeight;
            }
            if (_buttons.Count > 0) {
                _buttonPanelHeight = CalculateButtonPanelHeight();
                foreach (CommonButton button in _buttons) {
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    button.Height = _buttonPanelHeight;
                    using (Graphics g = CreateGraphics()) {
                        int btnLabelWidth = (int) g.MeasureString(button.Label, button.Font).Width;
                        button.Width = btnLabelWidth + _buttonPanelHeight;
                    }
                }
                RelocateButtons();
                height -= _buttonPanelHeight;
            }
            _contentSize = new(width, height);
        }

        private void ChangeLocation() {
            this._deviceDetailBackground.Location = WidgetUtils.MainPanel.PointToScreen(Point.Empty);
            int x = this._deviceDetailBackground.Location.X;
            int y = this._deviceDetailBackground.Location.Y;
            int width = this._deviceDetailBackground.Width;
            int height = this._deviceDetailBackground.Height;
            this.Location = new(x + (width - this.Width) / 2, y + (height - this.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            if (this._hasTitleBar) {
                e.Graphics.DrawString(_title, _titleFont, new SolidBrush(Color.Black), new Point((int) (Width * .015), (int) (_titleHeight * .125F)));
                Pen pen = new Pen(this._borderColor != null ? this._borderColor.Value : Color.Black, 1);
                Point point1 = new(this._virtualHorizontalPadding, this._titleHeight);
                Point point2 = new Point( this.Width - this._virtualHorizontalPadding, this._titleHeight);
                e.Graphics.DrawLine(pen, point1, point2);
            }

            if (this._borderColor != null && this._borderRect != null) {
                e.Graphics.DrawRectangle(new(this._borderColor.Value, 1), this._borderRect.Value);
            }
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (this.Visible) {
                this._deviceDetailBackground.Show();
            }
        }

        private class CloseButton: CustomImageTextButtonBase {
            private readonly float _closebuttonIconRatio = 0.75F;

            public CloseButton() : base() {
                this.Icon = CustomResources.button_close;
                this.BlockHoverUp = true;
            }

            protected override void ResizeIconImage() {
                if (Icon != null) {
                    int newSide = (int) (this.Height * this._closebuttonIconRatio);
                    Size newSize = new(newSide, newSide);
                    ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(Icon, newSize);
                    // Recalculate image position
                    ImageX = (int) Math.Ceiling((Width - newSize.Width) / 2D);
                    ImageY = (Height - newSize.Height) / 2;
                }
            }

            protected override void ResizeTextLabel() {
            }
        }
    }
}
