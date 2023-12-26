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
            get => this._linerColorLeft;
            set => this._linerColorLeft = value;
        }
        public Color LinerColorRight {
            get => this._linerColorRight;
            set => this._linerColorRight = value;
        }

        public CustomChildMenuFirstButton(Color linerColorLeft, Color linerColorRight) {
            this._linerColorLeft = linerColorLeft;
            this._linerColorRight = linerColorRight;
        }

        protected override void PaintAfter(PaintEventArgs e) {
            // Check if is toggled
            if (this.Toggled) {
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
            if (this.Icon != null) {
                int newImageSide = CalcNewImageSide();
                this.ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(this.Icon, newImageSide, newImageSide);
                // Recalculate image location
                if (!this.OnlyIcon) {
                    this.ImageX = (int) (this.Height * 0.45);
                } else {
                    this.ImageX = (this.Width - newImageSide) / 2;
                }
                this.ImageY = (this.Height - newImageSide) / 2;
            }
        }

        protected override void ResizeTextLabel() {
            if (this.Label != null) {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, this.Height / 5.7F + 1.25F, FontStyle.Bold);
                // Recalculate label location
                this.LabelX = (int) (this.Height * 0.95);
                this.LabelY = (this.Height - this.Font.Height) / 2;
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
