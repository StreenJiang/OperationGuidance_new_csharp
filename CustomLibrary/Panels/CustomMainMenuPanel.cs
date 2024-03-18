using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace CustomLibrary.Panels
{
    public class CustomMainMenuPanel: CustomMenuPanelBase {
        private Image? _mainMenuLogo;
        private Image? _mainMenuLogoShowing;
        private Point? _mainMenuLogoLocation;
 
        public Image? MainMenuLogo {
            get => _mainMenuLogo;
            set => _mainMenuLogo = value;
        }

        public CustomMainMenuPanel() {
            WidgetUtils.MakeControlDraggable(this, WidgetUtils.MainForm);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            // Calculate the size of logo image
            if (_mainMenuLogo != null) {
                Size _mainMenuLogoSize = (_mainMenuLogo.Size * this.Height * this.GetLogoZoomingRatio() / _mainMenuLogo.Height).ToSize();
                _mainMenuLogoLocation = this.GetLogoLocation(_mainMenuLogoSize);
                _mainMenuLogoShowing = WidgetUtils.ResizeImageWithoutLosingQuality(
                    _mainMenuLogo, _mainMenuLogoSize.Width, _mainMenuLogoSize.Height
                );
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.Clear(this.BackColor);
            if (_mainMenuLogoShowing != null && _mainMenuLogoLocation != null) {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.DrawImage(_mainMenuLogoShowing, _mainMenuLogoLocation.Value);
            }
        }

        // Ratio of height of main menu panel in main form
        protected override float GetResizeRatio() {
            if (!this.OnlyIconMode) {
                return 0.12F;
            } else {
                return 0.09F;
            }
        }

        protected override void ResizeButtons() {
            Size newButtonSize = new(this.Height, this.Height);
            foreach (Control button in this.Controls) {
                button.Size = newButtonSize;
            }
        }

        protected virtual float GetLogoZoomingRatio() {
            return 0.7F;
        }

        protected virtual Point GetLogoLocation(Size logoSize) {
            return new(
                this.Width - logoSize.Width - (int) Math.Ceiling(this.Width / 300D),
                (int) Math.Ceiling((this.Height - logoSize.Height) / 2D)
            );
        }
    }
}
