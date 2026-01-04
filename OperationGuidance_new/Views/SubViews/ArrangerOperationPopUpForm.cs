using CustomLibrary.Configs;
using CustomLibrary.Forms;
using log4net;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using System.Collections;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerOperationPopUpForm: CustomPopUpForm {
        private ILog log = MainUtils.GetLogger(typeof(ArrangerOperationPopUpForm));

        private IoBoxTask ioBoxTask;
        private AWorkplaceContentPanel _workplace;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private List<SignalButton> _outBtns;
        private List<SignalLabel> _inBtns;

        private System.Windows.Forms.Timer updateTimer;
        private readonly CancellationTokenSource _cts = new();

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }

        public ArrangerOperationPopUpForm(string categoryName,
                                          AWorkplaceContentPanel workplace,
                                          IoBoxTask ioBoxTask) {
            this.ioBoxTask = ioBoxTask;
            _workplace = workplace;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "螺丝机信号点测试 - " + categoryName + "";

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 8,
                Parent = ContentPanel,
            };

            _outBtns = new();
            for (int i = 0; i < 8; i++) {
                SignalButton signalButton = new() {
                    Label = $"输出-{i + 1}",
                    Index = i,
                    Parent = _tablePanel,
                };
                signalButton.Click += OutClick;
                _outBtns.Add(signalButton);
            }

            _inBtns = new();
            for (int i = 0; i < 8; i++) {
                _inBtns.Add(new SignalLabel() {
                    Text = $"输入-{i + 1}",
                    Index = i,
                    Parent = _tablePanel,
                });
            }

            updateTimer = new();
            updateTimer.Interval = 50; // 50ms
            updateTimer.Tick += UpdateTimerTick;
            updateTimer.Start();
        }

        private void UpdateTimerTick(object? sender, EventArgs e) {
            try {
                var (outPos, inPos) = ioBoxTask.ArrangerType!.ReadCurrent();

                BeginInvoke(() => {
                    UpdateButtonStates(_outBtns, outPos);
                    UpdateLabelStates(_inBtns, inPos);
                });
            } catch (OperationCanceledException) {
                log.Info("Cancel looping...");
                updateTimer.Stop();
            } catch (Exception ex) {
                log.Warn("Failed to read IO status", ex);
            }
        }

        private void UpdateButtonStates(List<SignalButton> buttons, int?[] values) {
            for (int i = 0; i < Math.Min(buttons.Count, values.Length); i++) {
                buttons[i].BackColor = values[i] == 1 ? Color.Yellow : Color.Gray;
                buttons[i].ForeColor = values[i] == 1 ? Color.Gray : Color.White;
            }
        }

        private void UpdateLabelStates(List<SignalLabel> labels, int?[] values) {
            for (int i = 0; i < Math.Min(labels.Count, values.Length); i++) {
                labels[i].BackColor = values[i] == 1 ? Color.Yellow : Color.Gray;
                labels[i].ForeColor = values[i] == 1 ? Color.Gray : Color.White;
            }
        }

        private async void OutClick(object? sender, EventArgs eventArgs) {
            SignalButton btn = (SignalButton) sender!;

            // 立即禁用按钮防止重复点击
            btn.Enabled = false;

            try {
                int?[] sendPos = { null, null, null, null, null, null, null, null };
                sendPos[btn.Index] = 1;

                // 直接await，不要再用Task.Run包装
                await ioBoxTask.ArrangerType!.SendPulseAsync(sendPos);

            } catch (Exception ex) {
                log.Warn("Error while sending pulse...", ex);
            } finally {
                // 重新启用按钮
                btn.Enabled = true;
            }
        }

        // 确保窗体关闭时释放资源
        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            _cts.Cancel();
            _cts.Dispose();
            updateTimer.Stop();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, ContentPanel.Height - ContentPanel.Padding.Size.Height);

            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            IList list = _tablePanel.Controls;
            for (int i = 0; i < list.Count; i++) {
                Control? control = (Control?) list[i];
                if (control != null) {
                    control.Margin = new(_boxMargin);
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }
    }
}

