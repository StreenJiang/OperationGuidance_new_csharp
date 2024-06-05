using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_service.Models.DTOs;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltButton: CommonButtonBase {
        public static Color WAITING = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_WAITING;
        public static Color WORKING = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_WORKING;
        public static Color DONE = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_DONE;
        public static Color ERROR = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_ERROR;
        public static Color TEXT_WHITE = ColorConfigs.COLOR_WORKPLACE_BOLT_NUMBER_WHIE;
        public static Color TEXT_BLACK = ColorConfigs.COLOR_WORKPLACE_BOLT_NUMBER;

        private readonly int _flikerInterval = 500;
        private readonly float _opacity = .75F;
        private readonly int _arranger_pulse_delay = 200;
        private readonly int _arranger_wait_result_delay = 300;
        private readonly int _arranger_time_out = 5000;
        private readonly int _setter_selector_delay = 50;
        private readonly int _setter_selector_time_out = 10000;
        private int _borderSize;
        private ProductBoltDTO _boltDTO;
        private BoltStatus _boltStatus;
        private System.Windows.Forms.Timer _buttonTimer;
        private bool _mouseLeftDown;
        private bool _moved;
        private bool _showingWhileWorking;
        private int _ngTimes;
        private int? _currentParameterSet;
        private string? _label;
        private int? _upperNum;
        private float? _specification;
        private bool _specificationOk;
        private int _arranger_time_count = 0;
        private int _setter_selector_time_count = 0;
        private float? _bitSpecification;
        private bool _bitSpecificationOk;

        public int Arranger_time_out => _arranger_time_out;
        public int Setter_selector_time_out => _setter_selector_time_out;
        public ProductBoltDTO BoltDTO {
            get => _boltDTO;
            set => _boltDTO = value;
        }
        public int BorderSize {
            get => _borderSize;
            set => _borderSize = value;
        }
        public BoltStatus BoltStatus {
            get => _boltStatus;
            set {
                _boltStatus = value;
                switch (_boltStatus) {
                    case BoltStatus.WORKING:
                        BackColor = Color.FromArgb((int) (255 * _opacity), WORKING);
                        ForeColor = TEXT_BLACK;
                        StartFlickering();
                        break;
                    case BoltStatus.DONE:
                        BackColor = Color.FromArgb((int) (255 * _opacity), DONE);
                        ForeColor = TEXT_WHITE;
                        StopFlickering();
                        break;
                    case BoltStatus.ERROR:
                        BackColor = Color.FromArgb((int) (255 * _opacity), ERROR);
                        ForeColor = TEXT_WHITE;
                        StartFlickering();
                        break;
                    case BoltStatus.DEFAULT:
                    default:
                        BackColor = Color.FromArgb((int) (255 * _opacity), WAITING);
                        ForeColor = TEXT_BLACK;
                        Label = _boltDTO.serial_num + "";
                        StopFlickering();
                        break;
                }
            }
        }
        public bool MouseLeftDown { get => _mouseLeftDown; set => _mouseLeftDown = value; }
        public bool Moved { get => _moved; set => _moved = value; }
        public bool ShowingWhileWorking { get => _showingWhileWorking; set => _showingWhileWorking = value; }
        public int NgTimes { get => _ngTimes; set => _ngTimes = value; }
        public int? CurrentParameterSet { get => _currentParameterSet; set => _currentParameterSet = value; }
        public float? Specification { get => _specification; set => _specification = value; }
        public bool SpecificationOk { get => _specificationOk; set => _specificationOk = value; }
        public float? BitSpecification { get => _bitSpecification; set => _bitSpecification = value; }
        public bool BitSpecificationOk { get => _bitSpecificationOk; set => _bitSpecificationOk = value; }
        public new string? Label {
            get => base.Label;
            set {
                _label = value;
                SetLabel();
            }
        }
        public int? UpperNum {
            get => _upperNum;
            set {
                _upperNum = value;
                SetLabel();
            }
        }
        private new Color BackColor {
            get => base.BackColor;
            set => base.BackColor = value;
        }

        public BoltButton(ProductBoltDTO boltDTO) {
            Label = boltDTO.serial_num + "";
            _borderSize = 0;
            _boltDTO = boltDTO;
            _buttonTimer = new();
            _buttonTimer.Interval = _flikerInterval;
            _buttonTimer.Tick += TimerTick;
            _moved = false;
            _showingWhileWorking = true;
            _ngTimes = 0;

            BlockHoverUp = true;
            _boltStatus = BoltStatus.DEFAULT;
            FlatStyle = FlatStyle.Popup;
            BackColor = Color.FromArgb((int) (255 * _opacity), WAITING);
            ForeColor = TEXT_BLACK;
        }

        private void SetLabel() {
            if (_upperNum != null) {
                base.Label = $"{_upperNum}-{_label}";
            } else {
                base.Label = _label;
            }
        }

        public void ResetStatusWithoutChangingVisible() {
            _showingWhileWorking = true;
            _boltStatus = BoltStatus.DEFAULT;
            BackColor = Color.FromArgb((int) (255 * _opacity), WAITING);
            ForeColor = TEXT_BLACK;
            SetLabel();
        }

        public void StartFlickering() {
            _buttonTimer.Start();
        }

        public void StopFlickering() {
            _buttonTimer.Stop();
            if (_showingWhileWorking) {
                Visible = true;
            } else {
                Visible = false;
            }
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            if (_showingWhileWorking) {
                Visible = !Visible;
            } else {
                Visible = false;
            }
        }

        public void SendSignalToArragner(float specification, IoBoxTypeArranger arrangerType, Action<bool, bool> callBack) {
            // Initialize variables
            _specification = specification;
            _specificationOk = false;
            _arranger_time_count = 0;

            // Start task
            BeginInvoke(() => {
                Task.Run(async () => {
                    // Start retrieve result from io box - arranger
                    arrangerType.RetrieveResult = true;
                    arrangerType.ActionAfterIoSignalReceived += DoArrangerActionAfterAnalysis;

                    // Reset if disposed
                    HandleDestroyed += (s, e) => {
                        // Set to true to break from loop
                        _specificationOk = true;

                        arrangerType.RetrieveResult = false;
                        if (arrangerType.ActionAfterIoSignalReceived != null && arrangerType.ActionAfterIoSignalReceived.GetInvocationList().Contains(DoArrangerActionAfterAnalysis)) {
                            arrangerType.ActionAfterIoSignalReceived -= DoArrangerActionAfterAnalysis;
                        }

                        // Reset again to ensure status of arranger is right
                        arrangerType.Reset();
                    };

                    // Waiting for finish signal from arranger
                    while (_arranger_time_count < _arranger_time_out) {
                        if (_specificationOk) {
                            break;
                        }
                        // Start sending signal
                        arrangerType.WritePosition((int) specification);
                        await WaitAndCountAsync(_arranger_pulse_delay);

                        // Reset signal
                        arrangerType.Reset();
                        await WaitAndCountAsync(_arranger_wait_result_delay);
                    }

                    // Reset again to ensure status of arranger is right
                    arrangerType.Reset();

                    callBack(_specificationOk, _arranger_time_count >= _arranger_time_out);

                    // Stop retrieve result
                    arrangerType.RetrieveResult = false;
                    arrangerType.ActionAfterIoSignalReceived -= DoArrangerActionAfterAnalysis;

                    // Reset variables
                    _specification = null;
                    _specificationOk = false;
                    _arranger_time_count = 0;
                });
            });

            async Task WaitAndCountAsync(int delay) {
                // Delay for a little bit
                await Task.Delay(delay);
                // Counting time
                _arranger_time_count += delay;
            }
        }
        private void DoArrangerActionAfterAnalysis(int position) {
            BeginInvoke(() => {
                if (position > 0) {
                    if (!_specificationOk && _specification == position) {
                        _specificationOk = true;
                    }
                }
            });
        }

        public void SendSignalToSetterSelector(float bitSpecification, IoBoxTypeSetterSelector setterSelectorType, Action<bool, bool> callBack) {
            _bitSpecification = bitSpecification;
            _bitSpecificationOk = false;
            _setter_selector_time_count = 0;

            // Start task
            BeginInvoke(() => {
                Task.Run(async () => {
                    // Start retrieve result from io box - arranger
                    setterSelectorType.RetrieveResult = true;
                    setterSelectorType.ActionAfterIoSignalReceived += DoSetterSelectorActionAfterAnalysis;

                    // Reset if disposed
                    HandleDestroyed += (s, e) => {
                        // Set to true to break from loop
                        _bitSpecificationOk = true;

                        setterSelectorType.RetrieveResult = false;
                        if (setterSelectorType.ActionAfterIoSignalReceived != null && setterSelectorType.ActionAfterIoSignalReceived.GetInvocationList().Contains(DoSetterSelectorActionAfterAnalysis)) {
                            setterSelectorType.ActionAfterIoSignalReceived -= DoSetterSelectorActionAfterAnalysis;
                        }

                        // Send reset command
                        setterSelectorType.Reset();
                    };

                    // Start sending signal
                    while (!_bitSpecificationOk && _setter_selector_time_count < _setter_selector_time_out) {
                        setterSelectorType.WritePosition((int) bitSpecification);

                        // Delay for a little bit
                        await Task.Delay(_setter_selector_delay);
                        // Counting time
                        _setter_selector_time_count += _setter_selector_delay;
                    }

                    // Send reset command
                    setterSelectorType.Reset();

                    callBack(_bitSpecificationOk, _setter_selector_time_count >= _setter_selector_time_out);

                    // Stop retrieve result
                    setterSelectorType.RetrieveResult = false;
                    setterSelectorType.ActionAfterIoSignalReceived -= DoSetterSelectorActionAfterAnalysis;

                    // Reset variables
                    _bitSpecification = null;
                    _bitSpecificationOk = false;
                    _setter_selector_time_count = 0;
                });
            });
        }
        private void DoSetterSelectorActionAfterAnalysis(int position) {
            BeginInvoke(() => {
                if (position > 0) {
                    if (!_bitSpecificationOk && _bitSpecification == position) {
                        _bitSpecificationOk = true;
                    }
                }
            });
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            ConerRadius = Width / 2;
            BorderSize = Width / 12;
            ChangeRegionByConerRadius();
        }

        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            // 绘制边框
            if (ConerRadius > 0) {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                if (_borderSize > 1) {
                    using (GraphicsPath path = GetGraphicsPath(new(0, 0, Width - 1, Height - 1))) {
                        e.Graphics.DrawPath(new(TEXT_BLACK, _borderSize), path);
                    }
                }
            }
        }

        protected override void ResizeTextLabel() {
            if (Label != null) {
                // Font = new Font(WidgetsConfigs.SystemFontFamily, Height / 2.5F + 1.25F, FontStyle.Bold);
                Font = WidgetUtils.GetProperFont(Size, Label, .6F, .85F);
                // using (Graphics g = CreateGraphics()) {
                //     LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                // }
                // LabelY = (int) ((Height - Font.Height * 1.05) / 2);
            }
        }
    }
}

