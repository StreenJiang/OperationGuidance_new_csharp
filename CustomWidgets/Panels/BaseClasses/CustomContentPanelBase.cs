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
            //DoubleBuffered = true;
            Margin = new Padding(0);
            _newHeight = 0;
        }

        public virtual void VisibleToTrue() {}

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += SizeChangedExtra;
            SizeChangedExtra(this, e);
        }

        private void SizeChangedExtra(object? sender, EventArgs eventArgs) {
            if (!this.Visible && this.IsHandleCreated) {
                // 开始异步调用，提升性能
                new Thread(() => {
                    this.BeginInvoke(new(() => {
                        this.InvokeResizing(this, eventArgs);
                        if (_correspondingMenuButton != null) {
                            CustomContentPanelBase? correspondingPanel = _correspondingMenuButton.CorrespondingContentPanel;
                            if (correspondingPanel != null) {
                                correspondingPanel.InvokeResizing(eventArgs);
                            }
                        }
                    }));
                }).Start();
            } else {
                this.InvokeResizing(this, eventArgs);
                if (_correspondingMenuButton != null) {
                    CustomContentPanelBase? correspondingPanel = _correspondingMenuButton.CorrespondingContentPanel;
                    if (correspondingPanel != null) {
                        correspondingPanel.InvokeResizing(eventArgs);
                    }
                }
            }
        }

        protected sealed override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
        }

        protected virtual void InvokeResizing(object? sender, EventArgs eventArgs) {}
        public void InvokeResizing(EventArgs eventArgs) => InvokeResizing(this, eventArgs);
        public void InvokeResizing() => InvokeResizing(EventArgs.Empty);

        // public void TriggerResizeManually(EventArgs e) {
        //     OnSizeChanged(e);
        // }

        public virtual bool CheckNeedsScrollBar(int parentNewHeight) => throw new NotImplementedException();
    }
}
