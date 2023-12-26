using CustomLibrary.Buttons.AbstractClasses;

namespace CustomLibrary.Buttons.BaseClasses {
    public class CustomImageTextButtonBase: AbstractCustomImageTextButton {
        protected override void ResizeIconImage() => throw new NotImplementedException();
        protected override void ResizeTextLabel() => throw new NotImplementedException();
    }
}
