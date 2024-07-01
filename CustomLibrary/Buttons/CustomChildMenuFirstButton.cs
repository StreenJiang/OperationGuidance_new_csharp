using CustomLibrary.Configs;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class CustomChildMenuFirstButton: CustomMenuButton {
        private const float _imageSideRatio = 0.42F;
        private const float _imageSideRatioOnlyIcon = 0.45F;

        protected override void OnSizeChanged(EventArgs e) {
            ConerRadius = Height / 7;
            base.OnSizeChanged(e);
        }

        protected override void PaintAfter(PaintEventArgs e) {
            // Check if is toggled
            if (Toggled) {
                // Recalculate extra size for image(icon) and label
                if (!OnlyIcon) {
                    ExtraSize = new(ExtraSize.Width + BarThickness, ExtraSize.Height);
                } else {
                    ExtraSize = new(ExtraSize.Width + BarThickness / 2, ExtraSize.Height);
                }
            }
            base.PaintAfter(e);
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                int newImageSide = CalcNewImageSide();
                ImageShowing = WidgetUtils.ResizeImage(Icon, newImageSide, newImageSide);
                // Recalculate image location
                if (!OnlyIcon) {
                    ImageX = (int) (Height * 0.04 + Width * .1);
                } else {
                    ImageX = (Width - newImageSide) / 2;
                }
                ImageY = (Height - newImageSide) / 2;
            }
        }

        protected override void ResizeTextLabel() {
            if (Label != null) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height * .315F, FontStyle.Bold, GraphicsUnit.Pixel);
                // Recalculate label location
                LabelX = (int) (Height * 0.75 + Width * .1);
                LabelY = (Height - Font.Height) / 2;
            }
        }

        private int CalcNewImageSide() {
            if (!OnlyIcon) {
                return (int) (Height * _imageSideRatio);
            } else {
                return (int) (Height * _imageSideRatioOnlyIcon);
            }
        }
    }
}
