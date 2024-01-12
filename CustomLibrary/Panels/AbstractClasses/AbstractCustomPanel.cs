namespace CustomLibrary.Panels.AbstractClasses {
    public abstract class AbstractCustomPanel : FlowLayoutPanel {
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // 用双缓冲绘制窗口的所有子控件
                return cp;
            }
        }

        public abstract void VisibleToTrue();
        public abstract void VisibleToFalse();

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += DoAfterResizing;
            SizeChanged += ResizeChildren;
        }
        protected virtual void DoAfterResizing(object? sender, EventArgs eventArgs) {}
        protected virtual void ResizeChildren(object? sender, EventArgs eventArgs) {}
        public void ResizeChildren(EventArgs eventArgs) => ResizeChildren(this, eventArgs);
        public void ResizeChildren() => ResizeChildren(EventArgs.Empty);

        protected sealed override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
        }

        protected sealed override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (Visible) {
                VisibleToTrue();
            } else {
                VisibleToFalse();
            }
        }
    }
}
