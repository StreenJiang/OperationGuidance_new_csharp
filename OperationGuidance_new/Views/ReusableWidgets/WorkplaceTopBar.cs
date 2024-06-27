using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class WorkplaceTopBar: CustomMainMenuPanel {
        private AWorkplaceContentPanel? _workplace;
        private BackCommonButton _backButton;
        private string _title;
        private Color _titleColor;
        private int _titleX;
        private int _titleY;
        private bool _operatorOpenning = false;

        public BackCommonButton BackButton {
            get => _backButton;
            set => _backButton = value;
        }
        public string Title {
            get => _title;
            set {
                _title = value;
                Invalidate();
            }
        }
        public Color TitleColor {
            get => _titleColor;
            set => _titleColor = value;
        }
        public AWorkplaceContentPanel? Workplace { get => _workplace; set => _workplace = value; }
        public bool OperatorOpenning {
            get => _operatorOpenning;
            set {
                _operatorOpenning = value;
                if (value) {
                    _backButton.Label = "退出登录";
                } else {
                    _backButton.Label = "返回";
                }
            }
        }

        public WorkplaceTopBar() : base() {
            _title = "";
            _backButton = new() {
                Parent = this,
                Label = "返回",
            };
            _backButton.Click += (sender, eventArgs) => {
                if (_workplace != null && _workplace.Activated) {
                    bool yes = WidgetUtils.ShowConfirmPopUp("当前已激活任务，返回主界面将终止任务，确认返回？");
                    if (yes) {
                        ExitConfirm();
                    }
                } else {
                    ExitConfirm();
                }
            };

        }

        protected virtual void ExitConfirm() {
            if (_operatorOpenning) {
                if (WidgetUtils.BackToLoginView != null) {
                    MainUtils.ActionAfterLogout = CloseWorkplace;
                    WidgetUtils.BackToLoginView(true);
                }
            } else {
                CloseWorkplace();
            }
        }

        protected void CloseWorkplace() {
            if (_workplace != null) {
                _workplace.Activated = false;
                if (WidgetUtils.MainPanel != null) {
                    WidgetUtils.MainPanel.Visible = true;
                }
                Parent.Visible = false;
                _workplace.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_title != null) {
                // Recalculate the font of title
                Font = new(WidgetsConfigs.SystemFontFamily, Height * .425f, FontStyle.Bold, GraphicsUnit.Pixel);
                Size titleSize = WidgetUtils.MeasureString(_title, Font);
                _titleX = (Width - titleSize.Width) / 2;
                _titleY = (Height - titleSize.Height) / 2;
                e.Graphics.DrawString(_title, Font, new SolidBrush(_titleColor), new Point(_titleX, _titleY));
            }
        }

        protected override void ResizeButtons() {
            int newHeight = (int) (Height * .5);
            // 先设定高度，则font就会重设
            _backButton.Height = newHeight;
            int newWidth = WidgetUtils.MeasureString(_backButton.Label, _backButton.Font).Width + newHeight * 2;
            _backButton.Width = newWidth;
            _backButton.Margin = new(0, (Height - newHeight) / 2, 0, 0);
        }

        protected override float GetResizeRatio() => WidgetUtils.WorkplaceTopBarHeightRatio();

        protected override float GetLogoZoomingRatio() => .7F;

        protected override Point GetLogoLocation(Size logoSize) {
            return new(
                Width - logoSize.Width - (int) Math.Ceiling(Width / 400D),
                (int) Math.Ceiling((Height - logoSize.Height) / 2D)
            );
        }

        public class BackCommonButton: CommonButton {
            protected override void ResizeTextLabel() {
                if (Label != null) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .425), FontStyle.Bold, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                    }
                    LabelY = (Height - Font.Height) / 2;
                }
            }
        }
    }

}

