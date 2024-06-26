using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
    public class SignButton: CustomImageTextButtonBase {
        private float _closebuttonIconRatio = 0.7F;
        public float ClosebuttonIconRatio { get => _closebuttonIconRatio; set => _closebuttonIconRatio = value; }

        public SignButton() : base() {
            BlockHoverUp = true;
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                int newSide = (int) (Height * _closebuttonIconRatio);
                Size newSize = new(newSide, newSide);
                ImageShowing = WidgetUtils.ResizeImage(Icon, newSize);
                // Recalculate image position
                ImageX = (int) Math.Ceiling((Width - newSize.Width) / 2D);
                ImageY = (Height - newSize.Height) / 2;
            }
        }

        protected override void ResizeTextLabel() {
        }
    }
}
