using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Resources;
using CustomLibrary.Utils;

namespace CustomLibrary.Forms {

    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomPopUpForm: Form {
        private Form _popUpFormBackboard;
        private Rectangle? _borderRect;
        private Color? _borderColor;

        private FlowLayoutPanel _titlePanel;
        private Panel _contentPanel;
        private FlowLayoutPanel _buttonsPanel;

        private int _titleHeight;
        private Size _contentSize;
        private CloseButton _closeButton = new();
        private bool _hasTitleBar = true;
        private string? _title;
        private Font? _titleFont;
        private int _virtualHorizontalPadding;
        private int _virtualVerticalPadding;

        private List<FunctionButton> _buttons;
        private HorizontalAlignment _buttonAlignment;
        private int _buttonPanelHeight;

        public Color? BorderColor { get => this._borderColor; set => this._borderColor = value; }
        public Size ContentSize { get => this._contentSize; set => this._contentSize = value; }
        public bool HasTitleBar {
            get => _hasTitleBar;
            set {
                _hasTitleBar = value;
                _closeButton.Visible = value;
            }
        }
        public Form BackForm { get => this._popUpFormBackboard; set => this._popUpFormBackboard = value; }
        public int TitleHeight { get => this._titleHeight; }
        public int ButtonPanelHeight { get => _buttonPanelHeight; }
        public string? Title { get => this._title; set => this._title = value; }
        public int VirtualHorizontalPadding { get => this._virtualHorizontalPadding; set => this._virtualHorizontalPadding = value; }
        public int VirtualVerticalPadding { get => this._virtualVerticalPadding; set => this._virtualVerticalPadding = value; }
        public HorizontalAlignment ButtonAlignment {
            get => this._buttonAlignment;
            set {
                RelocateButtons();
                this._buttonAlignment = value;
            }
        }

        public CustomPopUpForm() : base() {
            _popUpFormBackboard = new() {
                Owner = (Form) WidgetUtils.MainPanel.Parent,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Opacity = .5D,
                BackColor = Color.Black,
                ShowInTaskbar = false
            };
            _popUpFormBackboard.Owner.LocationChanged += (s, e) => {
                ChangeLocationAfterSizeChanged(this, EventArgs.Empty);
            };
            _popUpFormBackboard.Hide();

            this.Owner = _popUpFormBackboard;
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

        public virtual void FakeShowToCreateHandlesForChildren() {
            base.Show();
            Opacity = 0D;
        }

        public virtual void HideForm() {
            this.Hide();
            _popUpFormBackboard.Owner.Focus();
            _popUpFormBackboard.Hide();
        }

        public new void Show() {
            base.Show();
            Opacity = 1D;
        }

        public FunctionButton AddButton(string label) {
            FunctionButton button = new() {
                Label = label,
                Parent = this,
            };
            _buttons.Add(button);
            // ResizeChildren();
            return button;
        }

        public void RemoveButton(FunctionButton button) {
            _buttons.Remove(button);
        }

        public void CalculateDetailProperties() {
            Control mainParent = WidgetUtils.MainPanel.Parent;
            _popUpFormBackboard.Size = mainParent.ClientSize;
            if (this._hasTitleBar) {
                _titleHeight = (int) (mainParent.Height * .0475) + (int) (Math.Abs(mainParent.Width - mainParent.Height) * .0035);
            }
            if (_buttons.Count >= 0) {
               _buttonPanelHeight = (int) (mainParent.Height * .0415) + (int) (Math.Abs(mainParent.Width - mainParent.Height) * .0035);
            }
            this._virtualHorizontalPadding = (int) (mainParent.Width * .01);
            this._virtualVerticalPadding = (int) (mainParent.Height * .01);
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
                        FunctionButton btn = _buttons[i];
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
                        FunctionButton btn = _buttons[i];
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
                    foreach (FunctionButton btn in _buttons) {
                        sumBtnW += btn.Width;
                    }
                    int sumGap = btnGap * _buttons.Count;
                    int centerStart = (Width - sumBtnW - sumGap) / 2 + _virtualHorizontalPadding;
                    for (int i = 0; i < _buttons.Count; i++) {
                        FunctionButton btn = _buttons[i];
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

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            CalculateDetailProperties();
            SizeChanged += ResizeChildren;
            SizeChanged += ChangeLocationAfterSizeChanged;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        public virtual void ResizeChildren(object? sender, EventArgs eventArgs) {
            CalculateDetailProperties();

            if (this._hasTitleBar) {
                this._closeButton.Size = new((int) (this._titleHeight * 1.25), this._titleHeight - 1);
                // 标题字体
                this._titleFont = new Font(WidgetsConfigs.SystemFontFamily, this._titleHeight * .425F, FontStyle.Regular);
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
            // First to check if height of button panel is exist
            if (_buttons.Count > 0) {
                height -= _buttonPanelHeight;
            }
            // _contentSize = new(width, height);
            // Then change the size of buttons after _contentSize has been set, because measure text will interrupt size change, don't know why
            if (_buttons.Count > 0) {
                foreach (FunctionButton button in _buttons) {
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    button.Height = _buttonPanelHeight;
                    int btnLabelWidth = TextRenderer.MeasureText(button.Label, button.Font).Width;
                    button.Width = btnLabelWidth + _buttonPanelHeight;
                }
                RelocateButtons();
                height -= _buttonPanelHeight;
            }
        }

        private void ChangeLocationAfterSizeChanged(object? sender, EventArgs eventArgs) {
            _popUpFormBackboard.Location = WidgetUtils.MainPanel.PointToScreen(Point.Empty);
            int x = _popUpFormBackboard.Location.X;
            int y = _popUpFormBackboard.Location.Y;
            int width = _popUpFormBackboard.Width;
            int height = _popUpFormBackboard.Height;
            Location = new(x + (width - Width) / 2, y + (height - Height) / 2);
            _popUpFormBackboard.Invalidate();
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
                this._popUpFormBackboard.Show();
            } else {
                HideForm();
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

        public class FunctionButton: CommonButton {
            protected override void ResizeTextLabel() {
                if (Label != null) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .55), FontStyle.Bold, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                    }
                    LabelY = (Height - Font.Height) / 2;
                }
            }
        }
    }
}
