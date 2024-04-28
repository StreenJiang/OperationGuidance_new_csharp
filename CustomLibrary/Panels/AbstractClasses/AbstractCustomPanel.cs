using System.Drawing.Drawing2D;
using CustomLibrary.Utils;

namespace CustomLibrary.Panels.AbstractClasses {
    public abstract class AbstractCustomPanel: FlowLayoutPanel {
        private int _conerRadius = 0;
        private GraphicsPath? _pathSurface;

        public int ConerRadius { get => _conerRadius; set => _conerRadius = value; }
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
        protected virtual void DoAfterResizing(object? sender, EventArgs eventArgs) { }
        protected virtual void ResizeChildren(object? sender, EventArgs eventArgs) { }
        public void ResizeChildren(EventArgs eventArgs) => ResizeChildren(this, eventArgs);
        public void ResizeChildren() => ResizeChildren(EventArgs.Empty);

        protected sealed override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            ChangeRegionByConerRadius();
        }

        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (Visible) {
                VisibleToTrue();
            } else {
                VisibleToFalse();
            }
        }

        protected void ChangeRegionByConerRadius() {
            if (_conerRadius > 0) {
                _pathSurface = GetGraphicsPath(new Rectangle(0, 0, Width - 1, Height - 1));
                Region = new Region(_pathSurface);
            }
        }

        protected GraphicsPath GetGraphicsPath(Rectangle rect) => WidgetUtils.RoundedRect(rect, _conerRadius);
    }
}
