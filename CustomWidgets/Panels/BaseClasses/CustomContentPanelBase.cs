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
            //DoubleBuffered = true;
            Margin = new Padding(0);
            _newHeight = 0;
        }

        public virtual void VisibleToTrue() {}

        protected sealed override void OnSizeChanged(EventArgs e) {
            if (!this.Visible && this.IsHandleCreated) {
                // 开始异步调用，提升性能
                new Thread(() => {
                    this.BeginInvoke(new(() => {
                        base.OnSizeChanged(e);
                        this.InvokeResizing();
                    }));
                }).Start();
            } else {
                base.OnSizeChanged(e);
                this.InvokeResizing();
            }
        }

        public virtual void InvokeResizing() {}

        // public void TriggerResizeManually(EventArgs e) {
        //     OnSizeChanged(e);
        // }

        public virtual bool CheckNeedsScrollBar(int parentNewHeight) => throw new NotImplementedException();
    }
}
