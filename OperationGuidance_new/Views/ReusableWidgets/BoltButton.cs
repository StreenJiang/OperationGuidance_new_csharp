using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltButton: CommonButtonBase {
        public static Color WAITING = ConfigsVariables.COLOR_WORKPLACE_BOLT_BG_WAITING;
        public static Color WORKING = ConfigsVariables.COLOR_WORKPLACE_BOLT_BG_WORKING;
        public static Color DONE = ConfigsVariables.COLOR_WORKPLACE_BOLT_BG_DONE;
        public static Color ERROR = ConfigsVariables.COLOR_WORKPLACE_BOLT_BG_ERROR;

        private int _borderSize;
        private ProductBoltDTO _boltDTO;
        private WorkingProcessPanel? _workProcessPanel;
        private BoltStatus _boltStatus;
        private System.Windows.Forms.Timer _buttonTimer;
        private bool _mouseLeftDown;
        private bool _moved;

        public ProductBoltDTO BoltDTO {
            get => _boltDTO;
            set => _boltDTO = value;
        }
        public int BorderSize {
            get => _borderSize;
            set => _borderSize = value;
        }
        public WorkingProcessPanel? WorkProcessPanel {
            get => _workProcessPanel;
            set => _workProcessPanel = value;
        }
        public BoltStatus BoltStatus {
            get => _boltStatus;
            set => _boltStatus = value;
        }
        public bool MouseLeftDown { get => _mouseLeftDown; set => _mouseLeftDown = value; }
        public bool Moved { get => _moved; set => _moved = value; }

        public BoltButton(ProductBoltDTO boltDTO) {
            Label = boltDTO.serial_num + "";
            _borderSize = 0;
            _boltDTO = boltDTO;
            _buttonTimer = new();
            _buttonTimer.Interval = 500;
            _buttonTimer.Tick += TimerTick;
            _moved = false;

            BlockHoverUp = true;
            BackColor = ConfigsVariables.COLOR_WORKPLACE_BOLT_BG_WAITING;
            ForeColor = ConfigsVariables.COLOR_WORKPLACE_BOLT_NUMBER;
        }

        public void StartFlicker() {
            _buttonTimer.Start();

            if (_workProcessPanel == null) {
                throw new NullReferenceException("WorkProcessPanel should be set, it's null now.");
            }
            _workProcessPanel.BoltDTO = _boltDTO;
            _workProcessPanel.Status = ProductMissionStatus.WORKING;
            _workProcessPanel.BoltStatus = _boltStatus;
        }

        public void StopFlicker() {
            _buttonTimer.Stop();
            Visible = true;
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            Visible = !Visible;
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

