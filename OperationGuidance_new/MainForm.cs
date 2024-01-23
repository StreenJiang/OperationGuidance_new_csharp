using log4net;

namespace OperationGuidance_new {
    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public partial class MainForm: Form {
        ILog log = LogManager.GetLogger(typeof(MainForm));

        public MainForm() {
            InitializeComponentManually();
            log.Info("测试一下日志");
            this.FormBorderStyle = FormBorderStyle.None; // 这一句注释掉之后就不会触发下面的 InvokeResizing了，好奇怪
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
            // ResizeChildren(this, EventArgs.Empty); // 上面那句注释掉后暂时用这个触发一下
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (this.WindowState == FormWindowState.Minimized) {
                return;
            }
            foreach (Control control in Controls) {
                if (this.IsHandleCreated) {
                    control.Size = this.ClientSize;
                }
            }
        }
    }
}
