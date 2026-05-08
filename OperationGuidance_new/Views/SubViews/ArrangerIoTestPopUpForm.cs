using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerIoTestPopUpForm: CustomPopUpForm {
        private ILog log = MainUtils.GetLogger(typeof(ArrangerIoTestPopUpForm));

        private IoBoxTask ioBoxTask;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private List<SignalButton> _outBtns;
        private List<SignalLabel> _inBtns;

        private System.Windows.Forms.Timer updateTimer;
        private int?[] _lastOutPos;
        private int?[] _lastInPos;
        private bool _isUpdating;
        private bool _isSending;

        public ArrangerIoTestPopUpForm(string categoryName, IoBoxTask ioBoxTask) {
            this.ioBoxTask = ioBoxTask;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "螺丝机信号点测试 - " + categoryName;

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
            updateTimer.Interval = 50;
            updateTimer.Tick += UpdateTimerTick;
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            updateTimer.Start();
        }

        private bool IsEqual(int?[]? arr1, int?[]? arr2) {
            if (arr1 == null && arr2 == null)
                return true;
            if (arr1 == null || arr2 == null)
                return false;
            if (arr1.Length != arr2.Length)
                return false;

            for (int i = 0; i < arr1.Length; i++) {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }

        private void UpdateTimerTick(object? sender, EventArgs e) {
            if (_isUpdating)
                return;

            if (ioBoxTask.ArrangerType == null)
                return;

            _isUpdating = true;
            try {
                var result = ioBoxTask.ArrangerType.ReadCurrent();
                var outPos = result.Item1;
                var inPos = result.Item2;

                bool outChanged = !IsEqual(outPos, _lastOutPos);
                bool inChanged = !IsEqual(inPos, _lastInPos);

                if (outChanged || inChanged) {
                    var capturedOutPos = outPos;
                    var capturedInPos = inPos;

                    BeginInvoke(() => {
                        UpdateButtonStates(_outBtns, capturedOutPos);
                        UpdateLabelStates(_inBtns, capturedInPos);
                        _lastOutPos = capturedOutPos;
                        _lastInPos = capturedInPos;
                    });
                }
            } catch (OperationCanceledException) {
                log.Info("Cancel looping...");
                updateTimer.Stop();
            } catch (Exception ex) {
                log.Warn("Failed to read IO status", ex);
            } finally {
                _isUpdating = false;
            }
        }

        private void UpdateButtonStates(List<SignalButton> buttons, int?[] values) {
            if (values == null)
                return;
            for (int i = 0; i < Math.Min(buttons.Count, values.Length); i++) {
                buttons[i].BackColor = values[i] == 1 ? Color.Yellow : Color.Gray;
                buttons[i].ForeColor = values[i] == 1 ? Color.Gray : Color.White;
            }
        }

        private void UpdateLabelStates(List<SignalLabel> labels, int?[] values) {
            if (values == null)
                return;
            for (int i = 0; i < Math.Min(labels.Count, values.Length); i++) {
                labels[i].BackColor = values[i] == 1 ? Color.Yellow : Color.Gray;
                labels[i].ForeColor = values[i] == 1 ? Color.Gray : Color.White;
            }
        }

        private async void OutClick(object? sender, EventArgs eventArgs) {
            SignalButton btn = (SignalButton)sender!;

            if (_isSending)
                return;

            if (ioBoxTask.ArrangerType == null)
                return;

            _isSending = true;

            try {
                int?[] sendPos = { null, null, null, null, null, null, null, null };
                sendPos[btn.Index] = 1;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await ioBoxTask.ArrangerType.SendPulseAsync(sendPos, 200, cts.Token);

            } catch (OperationCanceledException) {
                log.Info("Send pulse operation timed out.");
            } catch (Exception ex) {
                log.Warn("Error while sending pulse...", ex);
            } finally {
                _isSending = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            updateTimer.Stop();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, ContentPanel.Height - ContentPanel.Padding.Size.Height);

            _boxHeight = WidgetUtils.TextOrComboBoxHeight();
            _boxMargin = _boxHeight / 5;
            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            foreach (Control? control in _tablePanel.Controls) {
                if (control != null) {
                    control.Margin = new(_boxMargin);
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }
    }
}
