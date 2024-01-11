using CustomLibrary.Configs;
using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace CustomLibrary.Buttons {
    public class CustomChildMenuFirstButton: CustomMenuButton {
        private const float _imageSideRatio = 0.37F;
        private const float _imageSideRatioOnlyIcon = 0.4F;
        private Color _linerColorLeft;
        private Color _linerColorRight;

        public Color LinerColorLeft {
            get => _linerColorLeft;
            set => _linerColorLeft = value;
        }
        public Color LinerColorRight {
            get => _linerColorRight;
            set => _linerColorRight = value;
        }

        public CustomChildMenuFirstButton(Color linerColorLeft, Color linerColorRight) {
            _linerColorLeft = linerColorLeft;
            _linerColorRight = linerColorRight;
        }

        protected override void PaintAfter(PaintEventArgs e) {
            // Check if is toggled
            if (Toggled) {
                ExtraSize = new(ExtraSize.Width + BarThickness, ExtraSize.Height);
                Rectangle rect = new(ClientRectangle.Location, ClientRectangle.Size);
                if (ToggleBar && ToggleBarRect != null) {
                    Rectangle rectT = ToggleBarRect.Value;
                    switch (ToggleBarDirection) {
                        case ToggleBarDirectionEnum.TOP:
                        case ToggleBarDirectionEnum.BOTTOM:
                            rect.Height -= rectT.Height;
                            if (rectT.Y == 0) {
                                rect.Y -= rectT.Height;
                            }
                            break;
                        case ToggleBarDirectionEnum.LEFT:
                        case ToggleBarDirectionEnum.RIGHT:
                            rect.Width -= rectT.Width;
                            if (rectT.X == 0) {
                                rect.X = rectT.Width;
                            }
                            break;
                    }
                }
                Brush b = new LinearGradientBrush(rect, _linerColorLeft, _linerColorRight, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(b, rect);
            }
            base.PaintAfter(e);
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                int newImageSide = CalcNewImageSide();
                ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(Icon, newImageSide, newImageSide);
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
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height / 5.7F + 1.25F, FontStyle.Bold);
                // Recalculate label location
                LabelX = (int) (Height * 0.45 + Width * .1);
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
