namespace CustomLibrary.Buttons.AbstractClasses {
    public abstract class AbstractCustomImageTextButton: AbstractCustomButton {
        private Image? _imageShowing;
        private string? _cacheText;
        private Image? _icon;
        private int _imageX;
        private int _imageY;
        private bool _onlyIcon;
        private ToolTip? _toolTip;

        public Image? Icon {
            get => _icon;
            set {
                _icon = value;
                Invalidate();
            }
        }
        protected Image? ImageShowing {
            get => _imageShowing;
            set => _imageShowing = value;
        }
        protected int ImageX {
            get => _imageX;
            set => _imageX = value;
        }
        protected int ImageY {
            get => _imageY;
            set => _imageY = value;
        }
        public bool OnlyIcon {
            get => _onlyIcon;
        }

        public AbstractCustomImageTextButton() : base() {
            this._toolTip = new() {
                InitialDelay = 50
            };
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            this.InvokeResizing();
        }

        private void InvokeResizing() {
            Form? form = TopLevelControl as Form;
            if (form is not null && form.WindowState == FormWindowState.Minimized) {
                return;
            }
            // Rescale image
            ResizeIconImage();
        }

        protected abstract void ResizeIconImage();

        protected override void PaintAfter(PaintEventArgs e) {
            // Draw image
            if (_imageShowing != null) {
                e.Graphics.DrawImage(_imageShowing, new Point(_imageX, _imageY) + ExtraSize);
            }
            // Draw text
            if (Label != null) {
                e.Graphics.DrawString(Label, Font, new SolidBrush(ForeColor), new Point(LabelX, LabelY) + ExtraSize);
            }
        }

        protected override void OnMouseHover(EventArgs e) {
            base.OnMouseHover(e);

            //// 设置提示信息
            //if (_onlyIcon) {
            //    this._toolTip.SetToolTip(this, _cacheText);
            //}
        }

        public void HideLabel() {
            this._toolTip.SetToolTip(this, Label);
            if (_cacheText == null || _cacheText.Equals("")) {
                _cacheText = Label;
            }
            Label = string.Empty;
            _onlyIcon = true;
        }

        public void ShowLabel() {
            this._toolTip.SetToolTip(this, string.Empty);
            Label = _cacheText;
            _onlyIcon = false;
        }
    }
}
