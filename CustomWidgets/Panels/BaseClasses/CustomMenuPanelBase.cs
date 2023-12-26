using CustomLibrary.Buttons;
using CustomLibrary.Panels.AbstractClasses;

namespace CustomLibrary.Panels.BaseClasses {
    public class CustomMenuPanelBase: AbstractCustomMenuPanel<CustomMenuButton> {
        protected override float GetResizeRatio() => throw new NotImplementedException();
        protected override void ResizeButtons() => throw new NotImplementedException();
    }
}
