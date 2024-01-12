using CustomLibrary.Configs;
using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace CustomLibrary.Buttons {
    public class CustomMainMenuButton: CustomMenuButton {
        private const float _imageSideRatio = 0.33F;
        private const float _imageSideRatioOnlyIcon = 0.45F;
        private int _gapBetweenImageAndText;
        private Color _linerColorUp;
        private Color _linerColorDown;

        public CustomMainMenuButton(Color linerColorUp, Color linerColorDown) {
            this._linerColorUp = linerColorUp;
            this._linerColorDown = linerColorDown;
        }

        public Color LinerColorUp {
            get => this._linerColorUp;
            set => this._linerColorUp = value;
        }
        public Color LinerColorDown {
            get => this._linerColorDown;
            set => this._linerColorDown = value;
        }

        protected override void OnSizeChanged(EventArgs e) {
            _gapBetweenImageAndText = this.Height / 20;
            base.OnSizeChanged(e);
        }

        protected override void PaintAfter(PaintEventArgs e) {
            if (Toggled) {
                Brush b = new LinearGradientBrush(ClientRectangle, _linerColorUp, _linerColorDown, LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(b, this.ClientRectangle);
            }
            base.PaintAfter(e);
        }

        protected override void ResizeIconImage() {
            if (this.Icon != null) {
                int newImageSide = CalcNewImageSide();
                this.ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(this.Icon, newImageSide, newImageSide);
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
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .15F, FontStyle.Bold, GraphicsUnit.Pixel);
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
