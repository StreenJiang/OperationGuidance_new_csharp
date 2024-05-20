using System.Runtime.InteropServices;
using CustomLibrary.Configs;

namespace OperationGuidance_new {
    [System.ComponentModel.DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public partial class MainForm: Form {

        public MainForm() {
            InitializeComponentManually();
            this.FormBorderStyle = FormBorderStyle.None; // 这一句注释掉之后就不会触发下面的 InvokeResizing了，好奇怪
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            // SizeChanged += ResizeChildren;
            // ResizeChildren(this, EventArgs.Empty); // 上面那句注释掉后暂时用这个触发一下

            // AllocConsole();
        }

        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (this.WindowState == FormWindowState.Minimized) {
                return;
            }
            foreach (Control control in Controls) {
                if (IsHandleCreated && !IsDisposed && !control.IsDisposed) {
                    control.Size = this.ClientSize;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, ColorConfigs.COLOR_MAIN_MENU_BACKGROUND, ButtonBorderStyle.Solid);
        }

        private void Form_Load(object sender, EventArgs e) {
            AllocConsole();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            DialogResult result = MessageBox.Show(null, "确定要退出吗？", "退出程序", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) {
                base.OnFormClosing(e);
            } else {
                e.Cancel = true;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        const int WS_MINIMIZEBOX = 0x20000;
        const int CS_DBLCLKS = 0x8;
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }
    }
}
