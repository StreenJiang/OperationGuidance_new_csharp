using CustomLibrary.TextBoxes;

namespace CustomLibrary.Forms {
    public class WaitDialog: CustomPopUpForm {
        private readonly TaskCompletionSource<bool> _tcs = new();
        protected bool IsClosingAllowed { get; set; } = false;
        private CustomTextBoxGroup _textBox;

        public CustomTextBoxGroup TextBox { get => _textBox; set => _textBox = value; }

        public WaitDialog(string textBoxName) {
            _textBox = new CustomTextBoxGroup(textBoxName) {
                Parent = this.ContentPanel
            };

            SetupUI();
            SetupCloseProtection();
        }

        private void SetupUI() {
            this.ControlBox = false;          // 1. 禁用标题栏控制框（含X按钮）
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;       // 2. 不显示在任务栏
            // this.TopMost = true;              // 3. 保持置顶
        }

        private void SetupCloseProtection() {
            // 4. 拦截所有关闭请求（包括 Alt+F4、任务管理器关闭等）
            this.FormClosing += (s, e) => {
                if (!IsClosingAllowed) {
                    e.Cancel = true;  // 强制取消关闭
                }
            };
        }

        /// <summary>
        /// 外部调用：设置完成信号，弹窗将自动关闭
        /// </summary>
        public void SignalComplete() {
            // 线程安全：确保在 UI 线程执行关闭
            if (this.InvokeRequired) {
                this.Invoke(new Action(CloseInternal));
            } else {
                CloseInternal();
            }
        }

        private void CloseInternal() {
            IsClosingAllowed = true;  // 5. 允许关闭
            _tcs.TrySetResult(true);   // 通知等待者
            this.Close();              // 执行关闭
        }

        /// <summary>
        /// 异步等待弹窗关闭（非阻塞调用方线程）
        /// </summary>
        public Task WaitAsync() => _tcs.Task;

        /// <summary>
        /// 同步等待（会阻塞当前线程，慎用）
        /// </summary>
        public void Wait() => _tcs.Task.Wait();
    }
}
