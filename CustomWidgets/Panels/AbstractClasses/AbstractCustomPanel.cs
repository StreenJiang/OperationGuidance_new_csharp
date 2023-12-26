using Timer = System.Windows.Forms.Timer;

namespace CustomLibrary.Panels.AbstractClasses {
    public abstract class AbstractCustomPanel : FlowLayoutPanel {
        // Check if is double click
        public bool EnableClick { get; set; }
        public int ClickTimes { get; set; }
        public int Milliseconds { get; set; }
        public Timer ClickTimer { get; set; }
        public bool DoubleClickIndependent { get; set; }

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // 用双缓冲绘制窗口的所有子控件
                return cp;
            }
        }

        public AbstractCustomPanel(): base() {
            DoubleClickIndependent = false;
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
            EnableClick = false;
            ClickTimes = 0;
            Milliseconds = 0;
            ClickTimer = new();
            ClickTimer.Interval = 50;
            ClickTimer.Tick += (sender, eventArgs) => {
                Milliseconds += ClickTimer.Interval;
                if (Milliseconds >= 1000) {
                    Milliseconds = 0;
                    ClickTimer.Stop();
                    ClickTimes = 0;
                    EnableClick = false;
                } else if (Milliseconds >= 200 && !EnableClick) {
                    EnableClick = true;
                    switch (ClickTimes) {
                        case 1:
                            OnClick(EventArgs.Empty);
                            break;
                        case 2:
                            OnDoubleClick(EventArgs.Empty);
                            break;
                    }
                }
            };
        }

        protected override void OnMouseUp(MouseEventArgs mevent) {
            if (DoubleClickIndependent && mevent.Button == MouseButtons.Left) {
                if (ClickTimes == 0) {
                    ClickTimer.Start();
                }
                ClickTimes++;
            }
            base.OnMouseUp(mevent);
        }

        protected override void OnClick(EventArgs e) {
            if (!DoubleClickIndependent) {
                base.OnClick(e);
            } else if (EnableClick) {
                base.OnClick(e);
            }
        }

        protected override void OnDoubleClick(EventArgs e) {
            if (!DoubleClickIndependent) {
                base.OnDoubleClick(e);
            } else if (EnableClick) {
                base.OnDoubleClick(e);
            }
        }
    }
}
