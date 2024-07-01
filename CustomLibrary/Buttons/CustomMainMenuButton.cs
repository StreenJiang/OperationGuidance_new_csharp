using CustomLibrary.Configs;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class CustomMainMenuButton: CustomMenuButton {
        private const float _imageSideRatio = 0.335F;
        private const float _imageSideRatioOnlyIcon = 0.455F;
        private int _gapBetweenImageAndText;
        private bool _openFirst = false;

        public bool OpenFirst { get => _openFirst; set => _openFirst = value; }

        protected override void OnSizeChanged(EventArgs e) {
            _gapBetweenImageAndText = this.Height / 15;
            ConerRadius = Height / 9;
            base.OnSizeChanged(e);
        }

        protected override void ResizeIconImage() {
            if (this.Icon != null) {
                int newImageSide = CalcNewImageSide();
                this.ImageShowing = WidgetUtils.ResizeImage(this.Icon, newImageSide, newImageSide);
                // Recalculate image location
                this.ImageX = (this.Width - newImageSide) / 2;
                if (!this.OnlyIcon) {
                    this.ImageY = (this.Height - newImageSide - this.Font.Height - _gapBetweenImageAndText) / 2;
                } else {
                    this.ImageY = (this.Height - newImageSide) / 2;
                }
            }
        }

        protected override void ResizeTextLabel() {
            if (this.Label != null) {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .155F, FontStyle.Bold, GraphicsUnit.Pixel);
                // Recalculate label location
                int newImageSide = CalcNewImageSide();
                using (Graphics g = CreateGraphics()) {
                    this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                }
                this.LabelY = (this.Height - this.Font.Height - newImageSide) / 2 + newImageSide;
            }
        }

        private int CalcNewImageSide() {
            if (!this.OnlyIcon) {
                return (int) (this.Height * _imageSideRatio);
            } else {
                return (int) (this.Height * _imageSideRatioOnlyIcon);
            }
        }
    }
}
