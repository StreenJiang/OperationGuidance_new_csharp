using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class FoldButton: CustomImageTextButtonBase {
        private const float _imageSideRatio = 0.55F;
        private bool _folded;
        private Image? _foldedIcon;
        private Image? _unfoldedIcon;

        public bool Folded {
            get => _folded;
            set {
                _folded = value;
                if (value) {
                    this.Icon = _foldedIcon;
                    this.Label = "打开";
                } else {
                    this.Icon = _unfoldedIcon;
                    this.Label = "收起";
                }
            }
        }
        public Image? FoldedIcon {
            get => _foldedIcon;
            set => _foldedIcon = value;
        }
        public Image? UnfoldedIcon {
            get => _unfoldedIcon;
            set => _unfoldedIcon = value;
        }

        public FoldButton() : base() {
            this.BlockHoverUp = true;
            this.BlockHoverDown = true;
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            CustomMenuPanelBase parent = (CustomMenuPanelBase) this.Parent.Parent;
            parent.OnlyIconMode = !parent.OnlyIconMode;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            if (this.ImageShowing != null) {
                // Gap between image and text
                int gap = (int) (this.ImageShowing.Height * 0.4);
                // Size of text
                using Graphics g = CreateGraphics();
                int textWidth = (int) g.MeasureString(this.Label, this.Font).Width;
                int textHeight = (int) g.MeasureString(this.Label, this.Font).Height;
                // Recalculate image location
                this.ImageX = (this.Width - this.ImageShowing.Width - textWidth - gap) / 2 + 1;
                this.ImageY = (this.Height - this.ImageShowing.Height) / 2 + 1;
                // Recalculate text location
                this.LabelX = this.ImageX + this.ImageShowing.Width + gap;
                this.LabelY = (this.Height - textHeight) / 2 + 1;
            }
        }

        protected override void ResizeIconImage() {
            if (this.Icon != null) {
                int newImageSide = (int) (this.Height * _imageSideRatio);
                this.ImageShowing = WidgetUtils.ResizeImage(this.Icon, newImageSide, newImageSide);
            }
        }

        protected override void ResizeTextLabel() {
            if (this.Label != null) {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, this.Height * .5F, FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }
    }
}
