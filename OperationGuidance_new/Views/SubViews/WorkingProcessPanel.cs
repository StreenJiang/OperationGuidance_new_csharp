using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.SubViews {
    public class WorkingProcessPanel: CustomContentPanel {
        private readonly int _loopingInterval = 50;
        public static readonly string UnlockedManually = "已手动解锁";
        public static readonly string TighteningDesc = "正在拧紧{0}号螺丝";
        public static readonly string LooseningDesc = "正在反松{0}号螺丝";
        public static readonly string LockedArmPosition = "力臂未在指定位置";
        public static readonly string AdminConfirmation = "需要管理员确认";
        public static readonly string LockedArmDisconnected = "力臂连接异常";
        public static readonly string LockedManually = "已手动锁止";
        public static readonly string LockedPsetSending = "正在下发程序号";
        public static readonly string LockedPsetNull = "{0}号螺丝未配置程序号";
        public static readonly string LockedPsetFailed = "{0}号螺丝程序号下发失败";
        public static readonly string LockedPsetNotMatched = "{0}号螺丝程序号与控制器不匹配";
        public static readonly string LockedArrangerTimedOut = "{0}号螺丝送钉超时";
        public static readonly string LockedArrangerNotDone = "{0}号螺丝送钉未完成";
        public static readonly string LockedSetterSelectorTimedOut = "{0}号螺丝套筒选择超时";
        public static readonly string LockedSetterSelectorNotMatched = "{0}号螺丝套筒选择错误";
        public static readonly string LockedBoltBarCode = "{0}号螺丝需要录入绑定的物料码";

        private Image _clockwiseIcon;
        private Image _anticlockwiseIcon;
        private Image _iconShowing;
        private Color _correctColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
        private Color _warningColor = ColorConfigs.COLOR_WORKING_PROCESS_THEME;
        private Color _errorColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
        private int _borderSize;
        private Rectangle _borderRect;

        private Panel _picturePanel;
        private PictureBox _pictureBox;
        private int _picturePanelHeight;
        private float _rotateAngle;

        private string _statusTxt;
        private string _statusDesc;
        private Font _statusFont;
        private Font _statusDescFont;

        private WorkplaceProcessStatus _workplaceProcessStatus;
        private int? _boltSerialNum;
        private TightenOrLoosen _tightenOrLoosen;

        public int? BoltSerialNum { get => _boltSerialNum; set => _boltSerialNum = value; }
        public string? NGReasons { get; set; }
        public TightenOrLoosen TightenOrLoosen {
            get => _tightenOrLoosen;
            set {
                _tightenOrLoosen = value;
                InvokeResizing();
            }
        }
        public string StatusDesc { get => _statusDesc; set => _statusDesc = value; }
        public WorkplaceProcessStatus WorkplaceProcessStatus {
            get => _workplaceProcessStatus;
            set {
                if (_workplaceProcessStatus != value) {
                    _workplaceProcessStatus = value;
                    BeginInvoke(() => {
                        switch (_workplaceProcessStatus) {
                            case WorkplaceProcessStatus.UNACTIVATED:
                                _statusTxt = "未激活";
                                _picturePanel.Visible = false;
                                break;
                            case WorkplaceProcessStatus.ACTIVATED:
                                _statusTxt = "已激活";
                                _picturePanel.Visible = false;
                                break;
                            case WorkplaceProcessStatus.OPERATION_ENABLE:
                                _picturePanel.Visible = true;
                                break;
                            case WorkplaceProcessStatus.OPERATION_DISABLE:
                                _statusTxt = "已锁定";
                                _picturePanel.Visible = false;
                                break;
                            case WorkplaceProcessStatus.FINISHED_NG:
                                _statusTxt = "NG";
                                _statusDesc = "任务失败";
                                _picturePanel.Visible = false;
                                break;
                            case WorkplaceProcessStatus.FINISHED_OK:
                                _statusTxt = "OK";
                                _statusDesc = "任务完成";
                                _picturePanel.Visible = false;
                                break;
                            default:
                                break;
                        }
                    });
                }
            }
        }

        public WorkingProcessPanel() : base() {
            _clockwiseIcon = Properties.Resources.processing_clockwise;
            _anticlockwiseIcon = Properties.Resources.processing_anticlockwise;
            _statusTxt = "未激活";
            _statusDesc = string.Empty;
            _workplaceProcessStatus = WorkplaceProcessStatus.UNACTIVATED;

            BackColor = ColorConfigs.COLOR_WORKING_PROCESS_THEME;
            _borderRect = new();
            _picturePanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                Visible = false,
            };
            _pictureBox = new() {
                Parent = _picturePanel,
                Margin = new(0),
                Padding = new(0),
            };
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            // Run loop to check/dsiplay continually
            RunLoop();
        }

        private void RunLoop() {
            Task.Run(() => {
                BeginInvoke(async () => {
                    while (!IsDisposed) {
                        switch (_workplaceProcessStatus) {
                            case WorkplaceProcessStatus.UNACTIVATED:
                                if (BackColor.ToArgb() != ColorConfigs.COLOR_WORKING_PROCESS_THEME.ToArgb()) {
                                    BackColor = ColorConfigs.COLOR_WORKING_PROCESS_THEME;
                                }
                                break;
                            case WorkplaceProcessStatus.ACTIVATED:
                                if (BackColor.ToArgb() != ColorConfigs.COLOR_WORKING_PROCESS_BLUE.ToArgb()) {
                                    BackColor = ColorConfigs.COLOR_WORKING_PROCESS_BLUE;
                                }
                                break;
                            case WorkplaceProcessStatus.OPERATION_ENABLE:
                                if (BackColor.ToArgb() != ColorConfigs.COLOR_WORKING_PROCESS_WHITE.ToArgb()) {
                                    BackColor = ColorConfigs.COLOR_WORKING_PROCESS_WHITE;
                                }
                                // 旋转图标
                                if (_tightenOrLoosen == TightenOrLoosen.TIGHTENING) {
                                    _rotateAngle += 15;
                                } else {
                                    _rotateAngle -= 15;
                                }
                                Image image = WidgetUtils.RotateImage(_iconShowing, _rotateAngle);
                                _pictureBox.Size = image.Size;
                                _pictureBox.Image = image;
                                _pictureBox.Location = new((_picturePanel.Width - _pictureBox.Width) / 2, (_picturePanel.Height - _pictureBox.Height) / 2);
                                break;
                            case WorkplaceProcessStatus.OPERATION_DISABLE:
                            case WorkplaceProcessStatus.FINISHED_NG:
                                if (BackColor.ToArgb() != ColorConfigs.COLOR_WORKING_PROCESS_RED.ToArgb()) {
                                    BackColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                }
                                break;
                            case WorkplaceProcessStatus.FINISHED_OK:
                                if (BackColor.ToArgb() != ColorConfigs.COLOR_WORKING_PROCESS_GREEN.ToArgb()) {
                                    BackColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                                }
                                break;
                            default:
                                break;
                        }
                        Invalidate();
                        Update();
                        await Task.Delay(_loopingInterval);
                    }
                });
            });
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (IsHandleCreated && !IsDisposed) {
                InvokeResizing();
            }
        }

        private void InvokeResizing() {
            _borderRect.Size = Size;
            _borderSize = Width / 40 + Height / 80;
            if (ConerRadius == 0) {
                _picturePanelHeight = (int) ((Height - _borderSize * 2) * .6F);
                _picturePanel.Size = new(Width - _borderSize * 2, _picturePanelHeight);
                _picturePanel.Margin = new(_borderSize);
            } else {
                _picturePanelHeight = (int) ((Height - _borderSize * 2) * .6F) - ConerRadius * 2;
                _picturePanel.Size = new(Width - _borderSize * 2 - ConerRadius * 2, _picturePanelHeight);
                _picturePanel.Margin = new(_borderSize + ConerRadius);
            }

            int imageSide = (int) (_picturePanel.Height * .85);
            if (_picturePanel.Height > _picturePanel.Width) {
                imageSide = (int) (_picturePanel.Width * .85);
            }
            if (_tightenOrLoosen == TightenOrLoosen.TIGHTENING) {
                _iconShowing = WidgetUtils.ResizeImage(_clockwiseIcon, new Size(imageSide, imageSide));
            } else {
                _iconShowing = WidgetUtils.ResizeImage(_anticlockwiseIcon, new Size(imageSide, imageSide));
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);
            base.OnPaint(e);

            _statusFont = WidgetUtils.GetProperFont(Size, _statusTxt, .375f, .9F);
            int lines = _statusDesc.Split("\r\n").Count();
            _statusDescFont = WidgetUtils.GetProperFont(Size, _statusDesc, .1f - lines * .005F, .9F);
            int statusWidth = (int) graphics.MeasureString(_statusTxt, _statusFont).Width;
            int statusDescWidth = (int) graphics.MeasureString(_statusDesc, _statusDescFont).Width;
            Point statusPoint;
            Point statusDescPoint;
            int otherHeihgt = _borderSize + _picturePanelHeight;
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            switch (_workplaceProcessStatus) {
                case WorkplaceProcessStatus.UNACTIVATED:
                case WorkplaceProcessStatus.ACTIVATED:
                    // graphics.FillRectangle(new SolidBrush(_warningColor), _borderRect);
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 2);
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    break;
                case WorkplaceProcessStatus.OPERATION_ENABLE:
                    if (ConerRadius == 0) {
                        graphics.DrawRectangle(new(_correctColor, _borderSize), _borderRect);
                    } else {
                        int temp = _borderSize / 2;
                        using (GraphicsPath path = WidgetUtils.RoundedRect(new(new Point(temp, temp), _borderRect.Size - new Size(1 + _borderSize, 1 + _borderSize)), ConerRadius)) {
                            graphics.DrawPath(new(_correctColor, _borderSize), path);
                        }
                    }
                    string descShowing = _statusDesc;
                    // 使用 StringFormat 进行居中时，是以坐标点位为中心，因此 x，y 都要设置为中心点
                    statusDescPoint = new Point(Width / 2, otherHeihgt + (Height - otherHeihgt) / 2);
                    graphics.DrawString(descShowing, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_GREEN), statusDescPoint, stringFormat);
                    break;
                case WorkplaceProcessStatus.OPERATION_DISABLE:
                case WorkplaceProcessStatus.FINISHED_NG:
                case WorkplaceProcessStatus.FINISHED_OK:
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 3 - lines * (int) (_statusFont.Height * 0.05));
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    // 使用 StringFormat 进行居中时，是以坐标点位为中心，因此 x，y 都要设置为中心点
                    statusDescPoint = new Point(Width / 2, otherHeihgt + (Height - otherHeihgt) / 2);
                    if (!string.IsNullOrEmpty(NGReasons) && !_statusDesc.Contains(NGReasons)) {
                        _statusDesc += "\r\n" + NGReasons;
                    }
                    graphics.DrawString(_statusDesc, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusDescPoint, stringFormat);
                    break;
            }
        }
    }
}

