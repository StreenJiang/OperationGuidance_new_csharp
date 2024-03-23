using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltButton: CommonButtonBase {
        public static Color WAITING = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_WAITING;
        public static Color WORKING = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_WORKING;
        public static Color DONE = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_DONE;
        public static Color ERROR = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_ERROR;

        private readonly int _flikerInterval = 500;
        private int _borderSize;
        private ProductBoltDTO _boltDTO;
        private BoltStatus _boltStatus;
        private System.Windows.Forms.Timer _buttonTimer;
        private bool _mouseLeftDown;
        private bool _moved;
        private bool _showingWhileWorking;
        private int _ngTimes;
        private int? _currentParameterSet;

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
                        BackColor = WORKING;
                        StartFlickering();
                        break;
                    case BoltStatus.DONE:
                        BackColor = DONE;
                        StopFlickering();
                        break;
                    case BoltStatus.ERROR:
                        BackColor = ERROR;
                        StartFlickering();
                        break;
                    case BoltStatus.DEFAULT:
                    default:
                        BackColor = WAITING;
                        StopFlickering();
                        break;
                }
            }
        }
        public bool MouseLeftDown { get => _mouseLeftDown; set => _mouseLeftDown = value; }
        public bool Moved { get => _moved; set => _moved = value; }
        public bool ShowingWhileWorking { get => _showingWhileWorking; set => _showingWhileWorking = value; }
        public int NgTimes { get => _ngTimes; set => _ngTimes=value; }
        public int? CurrentParameterSet { get => _currentParameterSet; set => _currentParameterSet = value; }

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
            BackColor = ColorConfigs.COLOR_WORKPLACE_BOLT_BG_WAITING;
            ForeColor = ColorConfigs.COLOR_WORKPLACE_BOLT_NUMBER;
        }

        public void ResetStatusWithoutChangingVisible() {
            _showingWhileWorking = true;
            _boltStatus = BoltStatus.DEFAULT;
            BackColor = WAITING;
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

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            ConerRadius = Width;
            BorderSize = Width / 10;
            ChangeRegionByConerRadius();
        }

        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            // 绘制边框
            if (ConerRadius > 0) {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = GetGraphicsPath(new(1, 1, Width - .8F, Height - 1))) {
                    if (_borderSize > 1) {
                        e.Graphics.DrawPath(new(ForeColor, _borderSize), path);
                    }
                }
            }
        }

        protected override void ResizeTextLabel() {
            if (Label != null) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, Height / 2.5F + 1.25F, FontStyle.Bold);
                using (Graphics g = CreateGraphics()) {
                    LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                }
                LabelY = (int) ((Height - Font.Height * 1.05) / 2);
            }
        }
    }
}

