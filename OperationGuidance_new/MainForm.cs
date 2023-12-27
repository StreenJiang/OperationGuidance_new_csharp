using log4net;

namespace OperationGuidance_new {
    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public partial class MainForm: Form {
        ILog log = LogManager.GetLogger(typeof(MainForm));

        public MainForm() {
            this.FormBorderStyle = FormBorderStyle.None;
            InitializeComponentManually();
            log.Info("测试一下日志");
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += InvokeResizing;
            InvokeResizing(this, e);
        }

        private void InvokeResizing(object? sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized) {
                return;
            }
            foreach (Control control in Controls) {
                if (!control.Visible && this.IsHandleCreated) {
                    // 开始异步调用，提升性能
                    //IAsyncResult asyncResult = this.BeginInvoke(new(() => {
                    //    control.Size = this.ClientSize;
                    //}));
                    new Thread(() => {
                        this.BeginInvoke(new(() => {
                            if (control is not Form) {
                                control.Size = this.ClientSize;
                            }
                        }));
                    }).Start();

                    //// 结束异步调用
                    //this.EndInvoke(asyncResult);
                } else {
                    control.Size = this.ClientSize;
                }
            }
        }
    }
}
