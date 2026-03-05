namespace OperationGuidance_new.Extensions {
    public static class ControlExtensions {
        /// <summary>
        /// 安全调用 BeginInvoke，自动处理窗体已销毁的情况
        /// </summary>
        public static void SafeInvoke(this Control control, Action action) {
            if (control == null || control.IsDisposed || !control.IsHandleCreated)
                return;

            try {
                control.BeginInvoke(action);
            } catch (InvalidOperationException) {
                // 窗体已销毁，安全忽略
            }
        }
    }
}
