using CustomLibrary.Buttons;
using CustomLibrary.Panels.AbstractClasses;

namespace CustomLibrary.Panels.BaseClasses {
    public class CustomMenuPanelBase: AbstractCustomMenuPanel<CustomMenuButton> {
        public override void VisibleToTrue() {}
        public override void VisibleToFalse() {}
        protected override float GetResizeRatio() => throw new NotImplementedException();
        protected override void ResizeButtons() => throw new NotImplementedException();
    }
}
