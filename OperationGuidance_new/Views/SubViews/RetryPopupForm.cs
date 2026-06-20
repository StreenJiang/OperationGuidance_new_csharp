using System.Drawing;
using System.Windows.Forms;
using CustomLibrary.Forms;

namespace OperationGuidance_new.Views.SubViews {
    public class RetryPopupForm : CustomPopUpForm {
        public bool ShouldRetry { get; private set; }

        public RetryPopupForm(int retryCount, int totalWaitSeconds) {
            Text = "数据存储失败";
            Size = new Size(520, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            var msg = new Label {
                Text = $"拧紧数据写入数据库失败，已自动重试 {retryCount} 次（等待共 {totalWaitSeconds} 秒）仍未能成功。\n\n"
                     + "可能原因：网络中断、数据库服务未启动、磁盘空间不足。\n\n"
                     + "若重试后问题仍存在，请联系系统管理员检查网络和数据库状态。",
                Location = new Point(20, 20),
                Size = new Size(460, 120),
                AutoSize = false,
            };

            var retryBtn = new Button {
                Text = "重试 — 再尝试一次。如网络刚刚恢复，点击后数据将继续正常存储，不影响当前任务。",
                Location = new Point(20, 160),
                Size = new Size(460, 45),
            };
            retryBtn.Click += (s, e) => { ShouldRetry = true; Close(); };

            var terminateBtn = new Button {
                Text = "终止任务 — 停止当前任务。已完成的拧紧数据不会丢失，任务结束后可手动导出 Excel/TXT 文件。",
                Location = new Point(20, 215),
                Size = new Size(460, 45),
            };
            terminateBtn.Click += (s, e) => { ShouldRetry = false; Close(); };

            Controls.Add(msg);
            Controls.Add(retryBtn);
            Controls.Add(terminateBtn);
        }
    }
}
