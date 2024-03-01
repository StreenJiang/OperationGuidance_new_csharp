using CustomLibrary.Buttons;
using CustomLibrary.Panels.AbstractClasses;

namespace CustomLibrary.Panels.BaseClasses {
    public class CustomContentPanelBase: AbstractCustomPanel {
        private CustomMenuButton? _correspondingMenuButton;
        private int _newHeight;

        public int NewHeight {
            get => this._newHeight;
            set => this._newHeight = value;
        }
        public CustomMenuButton? CorrespondingMenuButton { get => _correspondingMenuButton; set => _correspondingMenuButton = value; }
        public CustomVScrollingContentPanel? OuterVScrollPanel {
            get {
                if (base.Parent is Panel && base.Parent.Parent is CustomContentPanel && base.Parent.Parent.Parent is CustomVScrollingContentPanel) {
                    return (CustomVScrollingContentPanel) base.Parent.Parent.Parent;
                } else {
                    return null;
                }
            }
        }
        public new Control Parent {
            get {
                if (base.Parent is Panel && base.Parent.Parent is CustomContentPanel && base.Parent.Parent.Parent is CustomVScrollingContentPanel) {
                    return base.Parent.Parent.Parent.Parent;
                } else {
                    return base.Parent;
                }
            }
            set {
                base.Parent = value;
            }
        }

        public CustomContentPanelBase() {
            Margin = new Padding(0);
            _newHeight = 0;
        }

        public override void VisibleToTrue() {
            if (IsHandleCreated) {
                ResizeChildren();
            }
        }
        public override void VisibleToFalse() {}
        public virtual bool CheckNeedsScrollBar(int parentNewHeight) => throw new NotImplementedException();
    }
}
