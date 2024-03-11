using CustomLibrary.Buttons.AbstractClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons.BaseClasses {
    public class AvatarButton: AbstractCustomImageTextButton {
        #region Fields
        private Image _defaultAvatar = Resources.CustomResources.avatar_default;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public AvatarButton() {
            BlockHoverUp = true;
            BlockHoverDown = true;
        }
        #endregion

        #region Override methods
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            ConerRadius = Width;
            ChangeRegionByConerRadius();
        }
        protected override void ResizeIconImage() {
            if (Icon != null) {
                ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(Icon, new(Width, Height));
            } else {
                ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_defaultAvatar, new(Width, Height));
            }
            ImageX = 0;
            ImageY = 0;
        }
        protected override void ResizeTextLabel() {}
        protected override void OnMouseHover(EventArgs e) {}
        protected override void OnMouseEnter(EventArgs e) {}
        protected override void OnMouseLeave(EventArgs e) {}
        protected override void OnMouseClick(MouseEventArgs e) {}
        protected override void OnMouseDown(MouseEventArgs mevent) {}
        protected override void OnMouseUp(MouseEventArgs mevent) {}
        #endregion
    }
}
