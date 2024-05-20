using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Extensions;
using System.Reflection;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView: CustomContentPanel {
        private MissionListPanel? _missionListPanel;
        private List<ProductMissionDTO>? _productMissionDTOs;
        private OperationGuidanceApis? apis;
        private bool _operatorOpenning = false;

        private CustomTabPanel? _pagePanel;
        private TopBar? _topBar;
        private WorkplaceContentPanel? _workplacePanel;

        public WorkplaceMissionView() : base() => OpenMissionListView();
        public WorkplaceMissionView(bool operatorOpenning) : base() {
            _operatorOpenning = operatorOpenning;
            // 如果是操作员登录，则直接打开工作台
            if (operatorOpenning) {
                OpenWorkplaceViewDirectly();
            } else {
                OpenMissionListView();
            }
        }

        private void OpenMissionListView() {
            // Get apis
            apis = SystemUtils.GetApis();
            // Initialize
            _missionListPanel = new("选择任务", "直接进入工作台", (s, e) => OpenWorkplaceViewDirectly()) {
                Margin = new Padding(0),
                Parent = this,
            };
        }
        private void OpenWorkplaceViewDirectly() => OpenWorkplaceView(null);

        private void CheckAndDisplay() {
            if (_missionListPanel != null) {
                // Fetch data
                FetchData();
                // If there is no any mission, so show the big button
                if (_productMissionDTOs != null) {
                    _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenWorkplaceView);
                }
            }
        }

        public override void VisibleToTrue() {
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                System.Console.WriteLine($"_workplacePanel.Activated: {_workplacePanel.Activated}");
                // TODO: 这里或许可以做一个“任务中断”的效果，即不是每次进入都打开一个新的任务
            }
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            base.VisibleToTrue();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize mission list panel
            if (_missionListPanel != null) {
                _missionListPanel.Size = new(Width, Height);
                _missionListPanel.ResizeChildren(eventArgs);
                if (_missionListPanel.Visible) {
                    _missionListPanel.Invalidate();
                }
            }
        }

        private void OpenWorkplaceView(int? missionId) {
            if (_pagePanel != null && !_pagePanel.IsDisposed) {
                _pagePanel.Dispose();
            }
            if (_topBar != null && !_topBar.IsDisposed) {
                _topBar.Dispose();
            }
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                _workplacePanel.Dispose();
            }
            // Create a new view
            _pagePanel = new() {
                Parent = WidgetUtils.MainForm,
                Size = WidgetUtils.MainForm.ClientSize,
            };
            _topBar = new(_operatorOpenning) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND,
                MainMenuLogo = Properties.Resources.logo,
                Margin = new Padding(0),
                PanelDirection = MenuPanelDirection.TOP,
                TitleColor = ColorConfigs.COLOR_WORKPLACE_TITLE,
            };
            _workplacePanel = new(missionId, missionName => {
                _topBar.Title = missionName;
            }) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
            _topBar.Workplace = _workplacePanel;
            _pagePanel.ResizeChildren();
            // Hide main panel
            if (WidgetUtils.MainPanel != null) {
                WidgetUtils.MainPanel.Visible = false;
            }
            if (_operatorOpenning) {
                WidgetUtils.MainForm.SizeChanged += (s, e) => {
                    _pagePanel.Size = WidgetUtils.MainSize;
                };
            }
            _pagePanel.Size = new(WidgetUtils.MainSize.Width - 2, WidgetUtils.MainSize.Height - 2);
            _pagePanel.Location = new(1, 1);
        }

        private void FetchData() {
            if (apis != null) {
                _productMissionDTOs = apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs;
            }
        }
    }

    public class TopBar: CustomMainMenuPanel {
        private AWorkplaceContentPanel? _workplace;
        private BackCommonButton _backButton;
        private string _title;
        private Color _titleColor;
        private int _titleX;
        private int _titleY;
        private bool _operatorOpenning;

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

        public TopBar(bool operatorOpenning) : base() {
            _title = "";
            _operatorOpenning = operatorOpenning;
            _backButton = new() {
                Parent = this,
            };
            if (!_operatorOpenning) {
                _backButton.Label = "返回";
            } else {
                _backButton.Label = "退出登录";
            }
            _backButton.Click += (sender, eventArgs) => {
                if (_workplace != null && _workplace.Activated && !_workplace.Finished) {
                    bool yes = WidgetUtils.ShowConfirmPopUp("当前已激活任务还未完成，返回主界面将终止任务，确认返回？");
                    if (yes) {
                        if (_operatorOpenning) {
                            if (WidgetUtils.BackToLoginView != null) {
                                MainUtils.ActionAfterLogout = CloseWorkplace;
                                WidgetUtils.BackToLoginView(true);
                            }
                        } else {
                            CloseWorkplace();
                        }
                    }
                } else {
                    if (_operatorOpenning) {
                        if (WidgetUtils.BackToLoginView != null) {
                            MainUtils.ActionAfterLogout = CloseWorkplace;
                            WidgetUtils.BackToLoginView(true);
                        }
                    } else {
                        CloseWorkplace();
                    }
                }
            };

            void CloseWorkplace() {
                if (_workplace != null) {
                    _workplace.Activated = false;
                    if (WidgetUtils.MainPanel != null) {
                        WidgetUtils.MainPanel.Visible = true;
                    }
                    Parent.Visible = false;
                    _workplace.Dispose();
                }
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

    public class WorkplaceContentPanel: AWorkplaceContentPanel {
        // 上方
        private CustomContentPanel _top;
        // 上方左边
        private CustomContentPanel _topLeft;
        // 上方左边上面
        private WorkplacePiece _barCodeOuter;
        // 上方左边下面
        private WorkplacePiece _imageDisplayOuter;
        // 上方右边
        private CustomContentPanel _topRight;
        // 上方右边的上面
        private WorkplacePiece _topRightTop;
        // 上方右边的中间
        private CustomContentPanel _topRightMiddle;
        // 上方右边的中间的左边
        private WorkplacePiece _topRightMiddleTop;
        // 上方右边的中间的右边
        private WorkplacePiece _topRightMiddleBottom;
        // 上方右边的下面
        private WorkplacePiece _topRightBottom;

        // 下方
        private WorkplacePiece _bottom;


        // private Label _productSideTitle;
        // private List<Image?> _smallSideImagesForShowing;
        // private PictureBox _smallSideImage;
        // private TableLayoutPanel _buttonPanel;
        // private PageSwitchButton _first;
        // private PageSwitchButton _backward;
        // private PageSwitchButton _forward;
        // private PageSwitchButton _last;
        // private Label _pageInfo;

        public WorkplaceContentPanel(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            // 初始化所有组件
            // 上方
            _top = new() {
                Parent = this,
                Padding = new(0),
            };

            // 上方左边
            _topLeft = new() {
                Parent = _top,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };

            // 上方左边上面
            _barCodeOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _barCodeOuter.Controls.Add(_barCodePictureBox);
            _barCodeOuter.Controls.Add(_barCodeTextBox);
            _barCodeOuter.Click += barCodePopUp;

            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
            };
            _imageDisplayOuter.Controls.Add(_productImageDisplayPanel);

            // 上方右边
            _topRight = new() {
                Parent = _top,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };
            // 上方右边的上面
            _topRightTop = new() {
                Parent = _topRight,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _topRightTop.Controls.Add(_operatorInfoTitle);
            _topRightTop.Controls.Add(_operatorName);
            _topRightTop.Controls.Add(_operatorId);

            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的上面
            _topRightMiddleTop = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topRightMiddleTop.Controls.Add(_workingProcessPanel);

            // 上方右边的中间的下面
            _topRightMiddleBottom = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topRightMiddleBottom.Controls.Add(_torquePanel);
            _topRightMiddleBottom.Controls.Add(_anglePanel);

            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _missionSelectedName.Ratio = 7.5;
            _pset.Ratio = 7.5;
            _currentSideName.Ratio = 7.5;
            _topRightBottom.Controls.Add(_missionDetailTitle);
            _topRightBottom.Controls.Add(_missionSelectedName);
            _topRightBottom.Controls.Add(_currentSideName);
            _topRightBottom.Controls.Add(_pset);

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_BOTTOM_BAR_BACKGROUND,
            };
            foreach (DeviceBlock block in _deviceBlocks) {
                block.ConerRadius = WidgetUtils.ControlRadius();
                _bottom.Controls.Add(block);
            }
            _bottom.Controls.Add(_timeDisplayerOuter);
        }

        protected override void RefreshImageDisplayPanel() => ResizeTopLeftBottom();

        protected override void SetMissionDetails() {
            _missionSelectedName.SetValue(0, _mission.name);
            if (_sides.Count > 0 && _currentSideIndex >= 0) {
                _currentSideName.SetValue(0, _sides[_currentSideIndex].name);
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            if (IsHandleCreated && !IsDisposed) {
                Padding contentPadding = WidgetUtils.ContentPadding();
                int panelPadding = WidgetUtils.ContentInnerBorderMargin() * 2;

                int boxHeight = (int) (WidgetUtils.MainSize.Height * .038);
                int titleHeight = (int) (boxHeight * 1.2);
                int contentVPadding = (int) (boxHeight * .4);
                int contentHPadding = contentVPadding;
                Font titleFont = new Font(WidgetsConfigs.SystemFontFamily, titleHeight * .45f, FontStyle.Bold, GraphicsUnit.Pixel);

                ResizeOuters(panelPadding, boxHeight, titleHeight, contentVPadding);
                ResizeTopLeftTop();
                ResizeTopLeftBottom();
                ResizeTopRightTop(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeTopRightMiddleLeft();
                ResizeTopRightMiddleRight(panelPadding);
                ResizeTopRightBottom(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeBottom();
                Invalidate();
            }
        }

        // 计算尺寸： 外框
        private void ResizeOuters(int panelPadding, int boxHeight, int titleHeight, int contentVPadding) {
            int workplaceWidth = Width - panelPadding * 2;
            int workplaceHeight = Height - panelPadding; // Bottom panel has to dock at bottom
            int bottomHeight = (int) (workplaceHeight * .055);
            int topHeight = workplaceHeight - bottomHeight - panelPadding;
            int barCodeHeight = (int) (workplaceHeight * .06);
            int imagePanelHeight = topHeight - barCodeHeight - panelPadding;
            int topLeftWidth = (int) (workplaceWidth * .65) + (int) (Math.Abs(workplaceWidth - workplaceHeight) * .25);
            int topRightWidth = workplaceWidth - topLeftWidth - panelPadding;

            int topRightTopHeight = titleHeight + boxHeight * (_topRightTop.Controls.Count - 1) + contentVPadding * (_topRightTop.Controls.Count + 1);
            int topRightBottomHeight = titleHeight + boxHeight * (_topRightBottom.Controls.Count - 1) + contentVPadding * (_topRightBottom.Controls.Count + 1);
            int topRightMiddleHeight = topHeight - topRightTopHeight - topRightBottomHeight - panelPadding * 2;
            int topRightMiddleTopHeight = (int) (topRightMiddleHeight * .7);
            int topRightMiddleBottomHeight = topRightMiddleHeight - topRightMiddleTopHeight - panelPadding;

            // 上方
            _top.Size = new(workplaceWidth, topHeight);
            _top.Margin = new(panelPadding);
            // 上方左边
            _topLeft.Size = new(topLeftWidth, topHeight);
            // 上方左边上面
            _barCodeOuter.Size = new(topLeftWidth, barCodeHeight);
            _barCodeOuter.Margin = new(0, 0, 0, panelPadding);
            // 上方左边下面
            _imageDisplayOuter.Size = new(topLeftWidth, imagePanelHeight);
            // 上方右边
            _topRight.Size = new(topRightWidth, topHeight);
            _topRight.Margin = new(panelPadding, 0, 0, 0);
            // 上方右边的上面
            _topRightTop.Size = new(topRightWidth, topRightTopHeight);
            _topRightTop.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间
            _topRightMiddle.Size = new(topRightWidth, topRightMiddleHeight);
            _topRightMiddle.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间的上面
            _topRightMiddleTop.Size = new(topRightWidth, topRightMiddleTopHeight);
            _topRightMiddleTop.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间的下面
            _topRightMiddleBottom.Size = new(topRightWidth, topRightMiddleBottomHeight);
            _topRightMiddleBottom.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的下面
            _topRightBottom.Size = new(topRightWidth, topRightBottomHeight);

            // 下方
            _bottom.Size = new(Width, bottomHeight);
        }

        // 计算尺寸： 条码框
        private void ResizeTopLeftTop() {
            // icon的边长
            int side = (int) (_barCodePictureBox.Parent.Height * .5);
            Padding iconMargin = new(side, (_barCodePictureBox.Parent.Height - side) / 2, 0, 0);
            // Size of text box
            int newH = (int) (_barCodePictureBox.Parent.Height * .875);
            Size textBoxSize = new(_barCodePictureBox.Parent.Width - side * 2 - iconMargin.Left, newH);
            Padding textBoxMargin = new(0, (_barCodePictureBox.Parent.Height - newH) / 2, 0, 0);

            if (_barCodePictureBox.Parent is CustomContentPanel parent) {
                if (parent.ConerRadius > 0) {
                    iconMargin.Left += 1;

                    textBoxSize.Width -= 1;
                    textBoxSize.Height -= 1;
                }
            }

            // 重设icon
            _barCodePictureBox.Image = WidgetUtils.ResizeImage(_barCodeImage, side, side);
            _barCodePictureBox.Margin = iconMargin;
            _barCodePictureBox.Size = new(side, side);

            // 重设输入框
            _barCodeTextBox.Size = textBoxSize;
            _barCodeTextBox.Margin = textBoxMargin;

            // 重新计算弹框的大小
            ResizeBarCodePopUpForm();
        }
        private void ResizeBarCodePopUpForm() {
            if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                _barCodePopUpForm.CalculateDetailProperties();

                Control mainForm = WidgetUtils.MainForm;
                Padding contentPadding = _barCodePopUpForm.ContentPanel.Padding;
                int boxHeight = (int) (mainForm.Height * .05);
                Size contentSize = new((int) (mainForm.Width * .75), boxHeight + contentPadding.Size.Height);
                int boxWidth = contentSize.Width - contentPadding.Size.Width;
                // _barCodePopUpForm.TextBox.Size = new(boxWidth, boxHeight);
                _barCodePopUpForm.ResizeSelf();

                _barCodePopUpForm.SetContentSizeAndSelfSize(contentSize);
            }
        }

        // 计算尺寸： 产品图片展示区域
        private void ResizeTopLeftBottom() {
            // Image panel 要比 _leftMiddle 小2，是为了显示出后者的边框
            Size newPanelSize = new(_productImageDisplayPanel.Parent.Width - 2, _productImageDisplayPanel.Parent.Height - 2);
            _productImageDisplayPanel.Size = newPanelSize;

            foreach (ProductImageFile productImageFile in _productImageFiles) {
                productImageFile.RecalculateZoomingRatio();
            }
            _productImageFiles[_currentSideIndex].RefreshImage();
            Rectangle? imageRange = _productImageFiles[_currentSideIndex].ImageRange;

            // 重新计算螺栓点位按钮的大小和位置
            int btnSide = (int) (newPanelSize.Height * .085) + (int) (Math.Abs(newPanelSize.Width - newPanelSize.Height) * .02);
            foreach (KeyValuePair<int, List<BoltButton>> pair in _allBolts) {
                foreach (BoltButton boltButton in pair.Value) {
                    boltButton.Size = new(btnSide, btnSide);
                    int newX;
                    int newY;
                    if (imageRange != null) {
                        newX = imageRange.Value.Location.X + (int) (imageRange.Value.Width * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                        newY = imageRange.Value.Y + (int) (imageRange.Value.Height * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                    } else {
                        newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                        newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                    }
                    boltButton.Location = new(newX, newY);
                }
            }

            // 重新计算弹框的大小和位置
            ResizeBoltPopUpForm();
        }

        // 计算尺寸： 员工信息框
        private void ResizeTopRightTop(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _operatorInfoTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _operatorInfoTitle.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _operatorInfoTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = _operatorInfoTitle.Width - contentHPadding * 2;
            _operatorName.Size = new(boxWidth, boxHeight);
            _operatorName.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _operatorId.Size = new(boxWidth, boxHeight);
            _operatorId.Margin = new(contentHPadding, contentVPadding, 0, 0);
        }

        // 计算尺寸： 实时状态框
        private void ResizeTopRightMiddleLeft() {
            _workingProcessPanel.Size = _workingProcessPanel.Parent.Size;
        }

        // 计算尺寸： 实时扭矩、角度框
        private void ResizeTopRightMiddleRight(int panelPadding) {
            Size panelSize = new((_topRightMiddleBottom.Width - panelPadding) / 2, _topRightMiddleBottom.Height);
            _torquePanel.Size = panelSize;
            _torquePanel.Margin = new(0, 0, panelPadding, 0);
            _anglePanel.Size = panelSize;
        }

        // 计算尺寸： 任务信息框
        private void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _missionDetailTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _missionDetailTitle.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _missionDetailTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = _operatorInfoTitle.Parent.Width - contentHPadding * 2;
            foreach (Control ctrl in _topRightBottom.Controls) {
                if (ctrl is CustomTextBoxGroup box) {
                    box.Size = new(boxWidth, boxHeight);
                    box.Margin = new(contentHPadding, contentVPadding, 0, 0);
                }
            }
        }

        // 计算尺寸： 底部横框
        private void ResizeBottom() {
            int blocksWidth = 0;
            int blockSide = (int) (_bottom.Height * .85);
            int padding = (_bottom.Height - blockSide) / 2;
            int blockCount = 0;
            foreach (Control control in _bottom.Controls) {
                if (control is DeviceBlock) {
                    control.Size = new(blockSide, blockSide);
                    control.Margin = new(0, padding, padding, 0);
                    blocksWidth += blockSide;
                    blockCount++;
                }
            }
            blocksWidth += padding * blockCount;
            int timeDisplayerWidth = _bottom.Width - blocksWidth;
            _timeDisplayerOuter.Size = new(timeDisplayerWidth - 2, _bottom.Height - 2);
            _timeDisplayer.Font = new Font(WidgetsConfigs.SystemFontFamily, _bottom.Height * .325f, FontStyle.Regular, GraphicsUnit.Pixel);
            _timeDisplayer.Margin = new(_timeDisplayer.Height / 3, (_timeDisplayerOuter.Height - _timeDisplayer.Height) / 2, 0, 0);
        }


        // private void InitializeMiddleBottom() {
        //     _productSideTitle = new() {
        //         Parent = _middleBottom,
        //         Margin = new(1),
        //         Padding = new(0),
        //         TextAlign = ContentAlignment.MiddleCenter,
        //         ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_TEXT,
        //         BackColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_BACK,
        //     };
        //     _smallSideImage = new() {
        //         Parent = _middleBottom,
        //         Margin = new(0),
        //         Padding = new(0),
        //     };
        //     int totalPages = 0;
        //     List<ProductSideDTO>? productSides = _mission.ProductSides;
        //     if (productSides != null) {
        //         _productSideTitle.Text = productSides[0].name;
        //         totalPages = productSides.Count;
        //     }
        //     if (_missionImages.Count > 0) {
        //         _smallSideImagesForShowing = new();
        //         foreach (Image? image in _missionImages) {
        //             if (image == null) {
        //                 _smallSideImagesForShowing.Add(_defaultImage);
        //             } else {
        //                 _smallSideImagesForShowing.Add(image);
        //             }
        //         }
        //     }
        //     int currentPage = _currentSideIndex + 1;
        //     _first = new() {
        //         Icon = Properties.Resources.page_btn_backward_fast,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _backward = new() {
        //         Icon = Properties.Resources.page_btn_backward,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _forward = new() {
        //         Icon = Properties.Resources.page_btn_forward,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _last = new() {
        //         Icon = Properties.Resources.page_btn_forward_fast,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _pageInfo = new() {
        //         Margin = new(0),
        //         Padding = new(0),
        //         TextAlign = ContentAlignment.MiddleCenter,
        //         ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_PAGE_TEXT,
        //     };
        //     _pageInfo.Text = currentPage + "/" + totalPages;
        //     _buttonPanel = new() {
        //         Parent = _middleBottom,
        //         Margin = new(1),
        //         Padding = new(0),
        //         ColumnCount = 5,
        //     };
        //     _buttonPanel.Controls.Add(_first);
        //     _buttonPanel.Controls.Add(_backward);
        //     _buttonPanel.Controls.Add(_pageInfo);
        //     _buttonPanel.Controls.Add(_forward);
        //     _buttonPanel.Controls.Add(_last);
        //
        //     _first.Click += (sender, eventArgs) => {
        //         _currentSideIndex = 0;
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _backward.Click += (sender, eventArgs) => {
        //         if (_currentSideIndex <= 0) {
        //             _currentSideIndex = 0;
        //         } else {
        //             _currentSideIndex -= 1;
        //         }
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _forward.Click += (sender, eventArgs) => {
        //         if (_currentSideIndex >= _missionImages.Count - 1) {
        //             _currentSideIndex = _missionImages.Count - 1;
        //         } else {
        //             _currentSideIndex += 1;
        //         }
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _last.Click += (sender, eventArgs) => {
        //         _currentSideIndex = _missionImages.Count - 1;
        //         changeCurrentPageAndInvalidate();
        //     };
        //     void changeCurrentPageAndInvalidate() {
        //         if (_currentWorkingBolt != null) {
        //             if (_currentWorkingBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
        //                 _currentWorkingBolt.ShowingWhileWorking = false;
        //             } else {
        //                 _currentWorkingBolt.ShowingWhileWorking = true;
        //             }
        //         }
        //         int newCurrentPage = _currentSideIndex + 1;
        //         _first.CurrentPage = newCurrentPage;
        //         _backward.CurrentPage = newCurrentPage;
        //         _forward.CurrentPage = newCurrentPage;
        //         _last.CurrentPage = newCurrentPage;
        //         // 切换side后也切换点位
        //         _showingBoltButtons.ForEach(btn => btn.Visible = false);
        //         _showingBoltButtons = _allBolts.Where(btn => btn.BoltDTO.side_id == _sides[_currentSideIndex].id).ToList();
        //         _showingBoltButtons.ForEach(btn => btn.Visible = true);
        //         // 切换产品图片
        //         _productImageDisplayPanel.SetImage(_productImageFiles[_currentSideIndex].Image, _productImageFiles[_currentSideIndex].CenterLocation);
        //         _productImageFiles[_currentSideIndex].RefreshImage();
        //         ResizeSmallSideImageBox(_smallSideImagesForShowing[_currentSideIndex]);
        //         _pageInfo.Text = newCurrentPage + "/" + totalPages;
        //         _productSideTitle.Text = _productImageFiles[_currentSideIndex].SideDTO.name;
        //         ResetRightBottomTitleFont();
        //     }
        // }


        private async void StoreTighteningData(OperationDataDTO operationDataDTO) {
            await Task.Run(() => {
                lock (DataStorageLockObj) {
                    List<OperationDataDTO> data = new() { operationDataDTO };
                    List<string>? headers = null;
                    string textFileName = $"{MainUtils.GetStorageFormattedName()}.txt";
                    string excelFileName = $"{MainUtils.GetStorageFormattedName()}.xlsx";
                    string textFilePath = MainUtils.GetStoragePath() + textFileName;
                    string excelFilePath = MainUtils.GetStoragePath() + excelFileName;
                    // 检查当前文件是否存在
                    bool textFileExists = File.Exists(textFilePath);
                    bool excelFileExists = File.Exists(excelFilePath);
                    // 从配置文件读取配置
                    List<int> sortConfig = MainUtils.GetSortConfig();
                    List<int>? sortConfigCurr = MainUtils.GetSortConfigCurr();
                    List<OperationDataField> fieldsConfig = MainUtils.GetOperationDataFields(sortConfigCurr);
                    List<string> propertyNames = fieldsConfig.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
                    // 检查当前是否存在正在使用的字段配置
                    if (sortConfigCurr == null || !sortConfig.SequenceEqual(sortConfigCurr) || !textFileExists || !excelFileExists) {
                        sortConfigCurr = sortConfig;
                        MainUtils.SetSortConfigCurr(sortConfigCurr);
                        headers = fieldsConfig.Where(f => f.Visible).Select(f => f.FieldName).ToList();
                    }
                    // 组装数据
                    List<Dictionary<int, object?>> dataWithConfigFields = new();
                    List<OperationDataVO> dataFormatted = new();
                    CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(data, dataFormatted);
                    // 先根据每个字段的排序，将排序值和数据值作为一个dictionary存入一个集合
                    dataFormatted.ForEach(dto => {
                        Dictionary<int, object?> record = new();
                        for (int i = 0; i < propertyNames.Count; i++) {
                            string pName = propertyNames[i];
                            PropertyInfo? propertyInfo = dto.GetType().GetProperty(pName);
                            if (propertyInfo != null) {
                                record.Add(i, propertyInfo.GetValue(CommonUtils.CannotBeNull(dto)));
                            }
                        }
                        dataWithConfigFields.Add(record);
                    });
                    // 组装最终数据
                    List<List<object?>> finalData = new();
                    dataWithConfigFields.ForEach(dict => {
                        IOrderedEnumerable<KeyValuePair<int, object?>> orderedEnumerable = from pair in dict orderby pair.Key select pair;
                        finalData.Add(orderedEnumerable.Select(pair => pair.Value).ToList());
                    });
                    // 写入数据
                    // bool succeed = finalData.ExportToExcelFile(headers, excelFilePath, excelFileExists);
                    // // 由于 excel 文件如果打开后没有关闭会导致数据存储出错，因此先判断是否成功再进行后续操作
                    // if (succeed) {
                    //     _apis.BatchAddOperationData(new(data));
                    //     finalData.ExportToTextFile(headers, textFilePath, textFileExists);
                    // } else {
                    //     WidgetUtils.ShowWarningPopUp("Excel文件被占用，无法执行数据存储操作，本次数据已保留，请在下次任务完成以前或关闭工作台前释放被占用的数据文件，以免造成数据丢失！");
                    // }

                    // 先将组装好的VOs加入到实时显示数据列表中
                    _tighteningDataVOs.AddRange(dataFormatted);
                    RefreshTighteningDataPanel();
                    // 显示完后立马存入数据库
                    _apis.BatchAddOperationData(new(data));
                    // 最后再存进本地文件
                    finalData.ExportToTextFile(headers, textFilePath, textFileExists);
                    finalData.ExportToExcelFile(headers, excelFilePath, excelFileExists);
                }
            });
        }
        private void RefreshTighteningDataPanel() {
            _tighteningDataPanel.DataSource = _tighteningDataVOs;
        }

        // 读取到控制器传回的数据后进行处理
        protected override async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            await Task.Run(() => {
                BeginInvoke(async () => {
                    try {
                        ToolTask toolTask = _toolTasks[deviceId];
                        if (toolTask.WorkstationId != null) {
                            int workstationId = toolTask.WorkstationId.Value;

                            List<WorkstationDTO> workstationDTOs;
                            if (CheckIfIsMultiDeviceIndependenceMode()) {
                                workstationDTOs = _workstationsDTOs.Where(dto => _currentWorkingBoltIndependence.Keys.Contains(dto.id)).ToList();
                            } else {
                                List<int> workstationIds = new();
                                foreach (List<BoltButton> bolts in _allBolts.Values) {
                                    workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                                }
                                workstationIds = workstationIds.Distinct().ToList();
                                workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                            }
                            List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                            // Main display
                            _torquePanel.Data = data.torque + "";
                            _anglePanel.Data = data.angle + "";

                            // Get current bolt
                            BoltButton currentBolt;
                            if (CheckIfIsMultiDeviceIndependenceMode()) {
                                currentBolt = _currentWorkingBoltIndependence[workstationId];
                            } else {
                                currentBolt = CommonUtils.CannotBeNull(_currentWorkingBolt);
                            }

                            // Check if current showing side is equal to side of working bolt, if no then switch to the right side
                            if (currentBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                                ProductSideDTO? sideTemp = _sides.Find(s => s.id == currentBolt.BoltDTO.side_id);
                                if (sideTemp != null) {
                                    _currentSideIndex = _sides.IndexOf(sideTemp);
                                    ChangeSideAndInvalidate();
                                }
                            }

                            ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                            OperationDataDTO dataDTO = new();
                            CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);

                            WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == workstationId);
                            dataDTO.workstation_id = workstationDTO.id;
                            dataDTO.workstation_name = workstationDTO.name;

                            DeviceToolDTO toolDTO = _tools.Single(t => t.id == deviceId);
                            dataDTO.tool_name = toolDTO.name;
                            dataDTO.tool_ip = $"{toolDTO.ip}:{toolDTO.port}";
                            dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type).Name;
                            dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                            dataDTO.bolt_serial_num = boltDTO.serial_num;
                            MissionRecordDTO missionRecord = CommonUtils.CannotBeNull(_missionRecord);
                            dataDTO.mission_record_id = missionRecord.id;
                            dataDTO.vin_number = missionRecord.product_bar_code;
                            if (_realTimeArmCoordinates != null) {
                                dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                            }

                            // If result type is tightening
                            if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                                bool tighteningOK = true;
                                string errorMsg = "";

                                // Check tightening status
                                if (data.tightening_status != (int) TighteningStatus.OK) {
                                    tighteningOK = false;
                                    if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                        _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                        if (!string.IsNullOrEmpty(errorMsg)) {
                                            errorMsg += "\r\n";
                                        }
                                        errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                    }
                                    if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                        _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                        if (!string.IsNullOrEmpty(errorMsg)) {
                                            errorMsg += "\r\n";
                                        }
                                        errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                    }
                                }

                                // Check torque
                                if (boltDTO.torque_max > 0 && (data.torque < boltDTO.torque_min || data.torque > boltDTO.torque_max)) {
                                    tighteningOK = false;
                                    _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += "扭矩与配置范围不符";
                                }

                                // Check angle
                                if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                    tighteningOK = false;
                                    _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += "角度与配置范围不符";
                                }

                                // Switch to next bolt
                                if (tighteningOK) {
                                    // Reset tightening type to tightening in case somewhere did some changes
                                    _needLoosening = false;
                                    _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.CustomError);
                                    _workingProcessPanel.CustomError = null;

                                    // Tightening ok, data color change to green
                                    _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                                    _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

                                    // Lock the device
                                    if (_locating_enabled) {
                                        toolTask.SendLock();
                                    }

                                    currentBolt.BoltStatus = BoltStatus.DONE;

                                    List<BoltButton> currentSideBolts;
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        currentSideBolts = _allBoltsIndependence[_sides[_currentSideIndex].id][workstationId];
                                    } else {
                                        currentSideBolts = _allBolts[_sides[_currentSideIndex].id];
                                    }
                                    // Check next index
                                    int nextIndex = currentSideBolts.IndexOf(currentBolt) + 1;
                                    // 检查是否存在跳点的情况
                                    while (nextIndex < currentSideBolts.Count && currentSideBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                        nextIndex++;
                                    }

                                    if (nextIndex < currentSideBolts.Count) {
                                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                                            _currentWorkingBoltIndependence[workstationId] = SwitchBolt(workstationId, nextIndex);
                                            ChangeBoltStatusToWorking(_currentWorkingBoltIndependence[workstationId]);
                                        } else {
                                            _currentWorkingBolt = SwitchBolt(nextIndex);
                                            ChangeBoltStatusToWorking(_currentWorkingBolt);
                                        }
                                    } else {
                                        bool allDone = true;
                                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                                            foreach (int id in _allBoltsIndependence[_sides[_currentSideIndex].id].Keys) {
                                                if (id != workstationId) {
                                                    BoltButton? boltButton = _allBoltsIndependence[_sides[_currentSideIndex].id][id].Find(b => b.BoltStatus != BoltStatus.DONE);
                                                    if (boltButton != null) {
                                                        allDone = false;
                                                        break;
                                                    }
                                                }
                                            }
                                        } else {
                                            if (_currentSideIndex < _sides.Count - 1) {
                                                _currentSideIndex++;
                                                _currentWorkingBolt = SwitchBolt(0);
                                                ChangeBoltStatusToWorking(_currentWorkingBolt);
                                                ChangeSideAndInvalidate();
                                                allDone = false;
                                            }
                                        }

                                        if (allDone) {
                                            // All ok
                                            _activated = false;
                                            _finished = true;

                                            // Delay a bit to make sure [WorkplaceProcessStatus] won't be changed by arm device incorrectly
                                            await Task.Delay(300);

                                            _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_OK;
                                            _workingProcessPanel.CustomError = null;
                                            _workingProcessPanel.BoltSerialNum = null;

                                            // Change color back to black
                                            _torque.ForeColor = Color.Black;
                                            _angle.ForeColor = Color.Black;

                                            // Stop retrieve coordinates data
                                            if (_locating_enabled) {
                                                // Lock again in case _locating disabled
                                                _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                                // Stop listening coordinates
                                                workstationDTOs.ForEach(dto => {
                                                    int? armId = dto.arm_id;
                                                    if (armId != null) {
                                                        ArmTask armTask = _armTasks[armId.Value];
                                                        armTask.RetrieveResult = false;
                                                        armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                                    }
                                                });
                                            }

                                            // Update mission result to ok
                                            _missionRecord.mission_result = (int) TighteningStatus.OK;
                                            _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                            // Clear all cached bar codes
                                            _barCodeObj.Reset();

                                            // // 重置任务信息
                                            // ResetMissionDetails();
                                        }
                                    }

                                    // Store data
                                    dataDTO.tightening_status = (int) TighteningStatus.OK;
                                    StoreTighteningData(dataDTO);
                                } else {
                                    // Lock first
                                    if (_locating_enabled) {
                                        // Lock all tools here
                                        _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());
                                    }

                                    // Change bolt status
                                    currentBolt.BoltStatus = BoltStatus.ERROR;

                                    // Count ng times
                                    currentBolt.NgTimes++;

                                    // Mission failed
                                    if (_mission.max_ng_num != 0 && currentBolt.NgTimes >= _mission.max_ng_num) {
                                        // Lock again in case _locating disabled
                                        _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                        // Change mission status
                                        _activated = false;
                                        _finished = true;

                                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                                            foreach (BoltButton bolt in _currentWorkingBoltIndependence.Values) {
                                                bolt.StopFlickering();
                                            }
                                            _currentWorkingBoltIndependence.Clear();
                                        } else {
                                            currentBolt.StopFlickering();
                                            _currentWorkingBolt = null;
                                        }

                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_NG;
                                        _workingProcessPanel.CustomError = errorMsg;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                        _workingProcessPanel.BoltSerialNum = null;

                                        // Change color back to black
                                        _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                                        _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                                        // Stop retrieve coordinates data
                                        if (_locating_enabled) {
                                            // Lock again in case _locating disabled
                                            _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                            // Stop listening coordinates
                                            workstationDTOs.ForEach(dto => {
                                                int? armId = dto.arm_id;
                                                if (armId != null) {
                                                    ArmTask armTask = _armTasks[armId.Value];
                                                    armTask.RetrieveResult = false;
                                                    armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                                }
                                            });
                                        }

                                        // Clear all cached bar codes
                                        _barCodeObj.Reset();

                                        // 记录数据
                                        StoreTighteningData(dataDTO);

                                        // 先记录数据再弹出提示
                                        WidgetUtils.ShowErrorPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败");
                                    } else {
                                        // 扭矩角度数据颜色改成红色
                                        _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                        _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                        // _needLoosening = true;
                                        // _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                        // 记录数据
                                        StoreTighteningData(dataDTO);
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                        _workingProcessPanel.CustomError = errorMsg;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                    }
                                    dataDTO.tightening_status = (int) TighteningStatus.NG;
                                }
                            } else {
                                // 反松时把扭矩角度改回黑色
                                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                                _needLoosening = false;
                                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                _workingProcessPanel.CustomError = null;
                                if (MainUtils.GetStoreLooseningData()) {
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                }
                            }
                        }
                    } catch (Exception e) {
                        logger.Error($"Error occurred while handling tightening data, e: {e}");
                    }
                });
            });
        }

        // private void ResizeMiddleBottom() {
        //     // Resize title
        //     _productSideTitle.Size = new(_middleBottom.Width - 2, (int) (_middleBottom.Height * .2));
        //     // Reset font size
        //     ResetRightBottomTitleFont();
        //     // Resize product side image
        //     int imageWholeHeight = (int) ((_middleBottom.Height - 2 - _productSideTitle.Height) * .815);
        //     int vPadding = (int) (imageWholeHeight * .1);
        //     int imageHeight = imageWholeHeight - vPadding * 2;
        //     if (_missionImages.Count > 0) {
        //         for (int i = 0 ; i < _missionImages.Count ; i++) {
        //             Image? image = _missionImages[i];
        //             Size newISize;
        //             if (image == null) {
        //                 image = _defaultImage;
        //                 newISize = new((int) (imageHeight / (decimal) _defaultImage.Height * _defaultImage.Width), imageHeight);
        //                 _smallSideImagesForShowing[i] = WidgetUtils.ResizeImageWithoutLosingQuality(_defaultImage, newISize);
        //             }
        //             newISize = new((int) (imageHeight / (decimal) image.Height * image.Width), imageHeight);
        //             Image imageNew = WidgetUtils.ResizeImageWithoutLosingQuality(image, newISize);
        //             _smallSideImagesForShowing[i] = imageNew;
        //             if (i == _currentSideIndex) {
        //                 ResizeSmallSideImageBox(imageNew);
        //             }
        //         }
        //     }
        //     // Resize table panel 
        //     int tablePanelHeight = _middleBottom.Height - 4 - _productSideTitle.Height - imageWholeHeight;
        //     int buttonSide = (int) (tablePanelHeight * .725);
        //     int buttonVPadding = (tablePanelHeight - buttonSide) / 2;
        //     int buttonHPdding = (int) (buttonSide * .45);
        //     _buttonPanel.Size = new(_middleBottom.Width - 2 - buttonHPdding * 2, tablePanelHeight);
        //     _buttonPanel.Margin = new(buttonHPdding, 0, buttonHPdding, 0);
        //     // Resize icon button
        //     _first.Size = new(buttonSide, buttonSide);
        //     _first.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _backward.Size = new(buttonSide, buttonSide);
        //     _backward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _forward.Size = new(buttonSide, buttonSide);
        //     _forward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _last.Size = new(buttonSide, buttonSide);
        //     _last.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     // Resize page info label
        //     _pageInfo.Size = new(_buttonPanel.Width - 4 * buttonSide - buttonHPdding * 8, tablePanelHeight);
        //     _pageInfo.Margin = new(0, 0, 0, 0);
        //     _pageInfo.Font = new(WidgetsConfigs.SystemFontFamily, _pageInfo.Height * .675F, FontStyle.Bold, GraphicsUnit.Pixel);
        // }
        //
        // private void ResizeSmallSideImageBox(Image? newImage) {
        //     if (newImage != null) {
        //         int imageWholeHeight = (int) ((_middleBottom.Height - 2 - _productSideTitle.Height) * .8);
        //         int vPadding = (int) (imageWholeHeight * .1);
        //         int hPadding = (_middleBottom.Width - 2 - newImage.Width) / 2;
        //         _smallSideImage.Size = newImage.Size;
        //         _smallSideImage.Image = newImage;
        //         _smallSideImage.Margin = new(hPadding, vPadding, hPadding, vPadding);
        //     }
        // }
        //
        // private void ResetRightBottomTitleFont(float fontRatio = .55f) {
        //     Font font = new Font(WidgetsConfigs.SystemFontFamily, _productSideTitle.Height * fontRatio, FontStyle.Bold, GraphicsUnit.Pixel);
        //     using (Graphics g = CreateGraphics()) {
        //         if (g.MeasureString(_productSideTitle.Text, font).Width >= _productSideTitle.Width * .9) {
        //             ResetRightBottomTitleFont(fontRatio -= .025f);
        //         } else {
        //             _productSideTitle.Font = font;
        //         }
        //     }
        // }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }

        public override void VisibleToTrue() {
            SetOperatorInfo();
        }
    }

}
