using System.Collections;
using System.Drawing.Drawing2D;
using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView: CustomContentPanel {
        private readonly int _tableColumns = 4;
        private readonly float _cellGapRatio = 0.02F;
        private readonly float _cellHightRatio = 0.25F;
        private int _titleHeight;
        private int _cellHorizontalMargin;
        private int _cellVerticalMargin;
        private Size _cellSize;
        private MissionNewButtonPanel _bigButtonPanel;
        private MissionListPanel _missionListPanel;
        private List<ProductMissionDTO> _productMissionDTOs;
        private readonly OperationGuidanceApis apis;

        public WorkplaceMissionView() : base() {
            // Get apis
            apis = SystemUtils.GetApis();
            // Initialize
            _bigButtonPanel = new() {
                Margin = new Padding(0),
                Parent = this,
                Visible = false,
            };
            _missionListPanel = new(
                "选择任务", 
                _tableColumns, 
                "直接进入工作台", 
                (sender, eventArgs) => {
                    OpenWorkplaceView(new ProductMissionDTO() {
                        name = "工作台 - 未选择任务",
                        ProductSides = new() {
                            new() {
                                name = "-",
                            },
                        },
                    });
                }
            ) {
                Margin = new Padding(0),
                Parent = this,
                Visible = false,
            };

            // Check and display view
            CheckAndDisplay();
        }

        private void CheckAndDisplay() {
            // Fetch data
            FetchData();
            // If there is no any mission, so show the big button
            if (_productMissionDTOs.Count == 0) {
                _missionListPanel.Visible = false;
                _bigButtonPanel.Visible = true;
            } else {
                _bigButtonPanel.Visible = false;
                _missionListPanel.Visible = true;
                _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenWorkplaceView);
            }
        }

        public override void VisibleToTrue() {
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            base.VisibleToTrue();
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            _titleHeight = WidgetUtils.ContentTitle();
            // Calculate height of cells: use height of top level control is because self height will automatically change because of scroll bar
            _cellSize = new(0, (int) (TopLevelControl.Height * _cellHightRatio));
            _cellVerticalMargin = _cellSize.Height / 15;
            // If there is no any mission, then don't need scroll bar
            if (_productMissionDTOs.Count == 0) {
                NewHeight = 0;
                return false;
            }
            // Calculate table's size, depends on all cells
            int rowsCount = (int) Math.Ceiling(_productMissionDTOs.Count / (double) _tableColumns);
            NewHeight = _titleHeight + (rowsCount + 1) * _cellVerticalMargin + rowsCount * _cellSize.Height;
            return NewHeight > parentNewHeight;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize big button panel
            _bigButtonPanel.Size = new(Parent.Width, Parent.Height);
            if (_bigButtonPanel.Visible) {
                _bigButtonPanel.Invalidate();
            }
            // Calculate width of cells
            _cellHorizontalMargin = (int) (Width * _cellGapRatio);
            int gapNum = _tableColumns + 1; // Including outer margin
            _cellSize.Width = (Width - _cellHorizontalMargin * gapNum) / _tableColumns;
            // Set properties before resize mission list panel
            _missionListPanel.TitleHeight = _titleHeight;
            _missionListPanel.CellSize = _cellSize;
            _missionListPanel.CellHorizontalMargin = _cellHorizontalMargin;
            _missionListPanel.CellVerticalMargin = _cellVerticalMargin;
            // Resize mission list panel
            _missionListPanel.Size = new(Width, Height);
            _missionListPanel.ResizeChildren(eventArgs);
            if (_missionListPanel.Visible) {
                _missionListPanel.Invalidate();
            }
        }

        private void OpenWorkplaceView(ProductMissionDTO missionDTO) {
            // Create a new view
            CustomTabPanel page = new() {
                Parent = TopLevelControl,
                Size = TopLevelControl.ClientSize,
            };
            TopBar topBar = new(missionDTO.name) {
                Parent = page,
                BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND,
                MainMenuLogo = Properties.Resources.logo,
                Margin = new Padding(0),
                PanelDirection = MenuPanelDirection.TOP,
                TitleColor = ColorConfigs.COLOR_WORKPLACE_TITLE,
            };
            WorkplaceContent workplaceContent = new(missionDTO) {
                Parent = page,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
                Margin = new Padding(0),
                PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            page.ResizeChildren();
            // Hide main panel
            WidgetUtils.MainPanel.Visible = false;
        }

        private void FetchData() {
            QueryProductMissionListReq req = new();
            QueryProductMissionListRsp rsp;

            rsp = apis.QueryProductMissionListRsp(req);
            _productMissionDTOs = rsp.ProductMissionsDTOs;
        }
    }
    public class TopBar: CustomMainMenuPanel {
        private BackCommonButton _backButton;
        private string _title;
        private Color _titleColor;
        private int _titleX;
        private int _titleY;

        public BackCommonButton BackButton {
            get => _backButton;
            set => _backButton = value;
        }
        public string Title {
            get => _title;
            set => _title = value;
        }
        public Color TitleColor {
            get => _titleColor;
            set => _titleColor = value;
        }

        public TopBar(string title) : base() {
            _title = title;
            _backButton = new() {
                Label = "返回",
                Parent = this,
            };
            _backButton.Click += (sender, eventArgs) => {
                WidgetUtils.MainPanel.Visible = true;
                Parent.Visible = false;
            };
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_title != null) {
                e.Graphics.DrawString(_title, Font, new SolidBrush(_titleColor), new Point(_titleX, _titleY));
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            // Recalculate the font of title
            Font = new(WidgetsConfigs.SystemFontFamily, Height * .625f, FontStyle.Bold, GraphicsUnit.Pixel);
            // Recalculate label location
            using (Graphics g = CreateGraphics()) {
                _titleX = (int) ((Width - g.MeasureString(_title, Font).Width) / 2);
            }
            _titleY = (int) ((Height - Font.Height * 1.1) / 2);
        }

        protected override void ResizeButtons() {
            int newHeight = (int) (Height * .7);
            int newWidth = (int) (newHeight * 2.25);
            _backButton.Size = new(newWidth, newHeight);
            _backButton.Margin = new((Height - newHeight) / 2);
        }

        protected override float GetResizeRatio() => .05F;

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
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .6), FontStyle.Bold, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                    }
                    LabelY = (Height - Font.Height) / 2;
                }
            }
        }
    }

    public class WorkplaceContent: CustomContentPanel {
        private OperationGuidanceApis apis;
        private ProductMissionDTO _mission;
        private Image _defaultImage;

        private CustomContentPanel _left;
        private CustomContentPanel _right;
        private WorkplacePiece _bottom;

        private WorkplacePiece _leftTop;
        private WorkplacePiece _leftBottom;

        private WorkplacePiece _rightTop;
        private WorkplacePiece _rightMiddle;
        private WorkplacePiece _rightBottom;

        private int _productSideIndex;

        // Left top
        private Image _barCodeImage;
        private PictureBox _barCodePictureBox;
        private CustomTextBox _barCodeTextBox;
        private BarCodeInputPopUpForm _barCodePopUpForm;

        // Left bottom
        private ProductImageDisplayPanel _productImageDisplayPanel;
        private List<ProductImageFile> _productImageFiles;
        private List<Image?> _missionImages;
        private List<List<BoltButton>> _boltButtons;
        private List<BoltButton> _currentBoltButtons;
        private BoltButton _currentChosenButton;
        private BoltPopUpForm _boltPopUpForm;

        // Right top
        private WorkingProcessPanel _workingProcessPanel;

        // Right middle
        private Label _torqueTitle;
        private Label _torque;
        private Label _angleTitle;
        private Label _angle;
        private System.Windows.Forms.Timer _dataTimer;

        // Right bottom
        private Label _productSideTitle;
        private List<Image?> _smallSideImagesForShowing;
        private PictureBox _smallSideImage;
        private TableLayoutPanel _buttonPanel;
        private PageSwitchButton _first;
        private PageSwitchButton _backward;
        private PageSwitchButton _forward;
        private PageSwitchButton _last;
        private Label _pageInfo;

        // Bottom
        private List<DeviceDTO> _deviceDTOs;
        private Dictionary<int, DeviceBlock> _deviceBlocks;

        public BoltButton CurrentWorkingButton {
            get => _currentChosenButton;
            set => _currentChosenButton = value;
        }

        public WorkplaceContent(ProductMissionDTO mission) : base() {
            apis = SystemUtils.GetApis();
            _mission = mission;
            _defaultImage = Properties.Resources.image_choose;
            _productSideIndex = 0;

            InitializeContents();
            InitializeLeftTop();
            InitializeLeftBottom();
            InitializeRightTop();
            InitializeRightMiddle();
            InitializeRightBottom();
            InitializeBottom();
        }

        private void InitializeContents() {
            _left = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };
            _right = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };

            _leftTop = new() {
                Parent = _left,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _leftBottom = new() {
                Parent = _left,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };

            _rightTop = new() {
                Parent = _right,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _rightMiddle = new() {
                Parent = _right,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _rightBottom = new() {
                Parent = _right,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
        }

        private void InitializeLeftTop() {
            _leftTop.BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND;
            _barCodeImage = Properties.Resources.bar_code_icon;
            _barCodePictureBox = new() {
                Parent = _leftTop,
                Margin = new(0),
                Padding = new(0),
            };
            _barCodeTextBox = new() {
                Parent = _leftTop,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                DisabledBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            string note = ConfigsVariables.BAR_CODE_NOTE;
            EventFuncs.CurrentActiveControl = _leftTop;
            _barCodeTextBox.Text = note;
            _barCodeTextBox.Enabled = false;
            _leftTop.Click += barCodePopUp;
            _barCodePictureBox.Click += barCodePopUp;
            _barCodeTextBox.Click += barCodePopUp;

            void barCodePopUp(object? s, EventArgs e) {
                if (_barCodePopUpForm == null) {
                    _barCodePopUpForm = new(note, _barCodeTextBox) {
                        Title = "录入条码",
                        BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                    };
                    _barCodePopUpForm.AddButton("确定").Click += (sender, eventArgs) => {
                        if (!_barCodePopUpForm.TextBox.IsError) {
                            _barCodeTextBox.Text = _barCodePopUpForm.TextBox.Text;
                            _barCodePopUpForm.HideForm();
                        }
                    };
                    _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.HideForm();
                    _barCodePopUpForm.PretendToShowToCreateHandlesForChildren();
                    ResizeBarCodePopUpForm();
                }
                if (_barCodeTextBox.Text != note) {
                    _barCodePopUpForm.TextBox.Text = _barCodeTextBox.Text;
                }
                _barCodePopUpForm.Show();
            }
        }

        private void InitializeLeftBottom() {
            _productImageDisplayPanel = new(_defaultImage) {
                Parent = _leftBottom,
                Margin = new(1, 1, 0, 0),
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
            };
            _missionImages = new();
            _productImageFiles = new();
            _boltButtons = new();

            List<ProductSideDTO>? productSides = _mission.ProductSides;
            if (productSides != null) {
                foreach (ProductSideDTO sideDTO in productSides) {
                    ProductImageFile productImageFile = new(_productImageDisplayPanel, sideDTO, 0);
                    _productImageFiles.Add(productImageFile);
                    // Initialize product image info
                    _missionImages.Add(productImageFile.Image);

                    // 配置螺栓点位
                    List<ProductBoltDTO>? bolts = sideDTO.Bolts;
                    List<BoltButton> boltButtons = new();
                    if (bolts != null) {
                        foreach (ProductBoltDTO boltDTO in bolts) {
                            BoltButton boltBtn = new(boltDTO) {
                                Parent = _productImageDisplayPanel,
                                Label = boltDTO.serial_num + "",
                                Visible = false,
                            };
                            boltBtn.Click += (s, e) => {
                                _boltPopUpForm = new(boltDTO) {
                                    Title = boltDTO.serial_num + " - " + boltDTO.name,
                                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                                };
                                // 添加按钮
                                CommonButton switchBtn = _boltPopUpForm.AddButton("切换到此点位");
                                switchBtn.Click += (s, e) => {
                                    if (CurrentWorkingButton != null) {
                                        CurrentWorkingButton.BackColor = BoltButton.WAITING;
                                        CurrentWorkingButton.BoltStatus = BoltStatus.DEFAULT;
                                        CurrentWorkingButton.StopFlicker();
                                    }
                                    boltBtn.BackColor = BoltButton.WORKING;
                                    boltBtn.BoltStatus = BoltStatus.TIGHTENING;
                                    boltBtn.StartFlicker();
                                    CurrentWorkingButton = boltBtn;
                                    _boltPopUpForm.HideForm();
                                };
                                CommonButton closeBtn = _boltPopUpForm.AddButton("关闭");
                                closeBtn.Click += (s, e) => {
                                    _boltPopUpForm.HideForm();
                                };
                                // Show form but make it transparent to create handles for its children
                                _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
                                // Resize all widgets
                                ResizePopUpForm();
                                // Real show
                                _boltPopUpForm.Show();
                            };
                            boltButtons.Add(boltBtn);
                        }
                    }
                    _boltButtons.Add(boltButtons);
                }
            }

            // 默认显示第一个产品面和对应的螺栓点位
            foreach (BoltButton boltBtn in _boltButtons[_productSideIndex]) {
                boltBtn.Visible = true;
            }
            _currentBoltButtons = _boltButtons[_productSideIndex];
        }

        private void ResizePopUpForm() {
            if (_boltPopUpForm != null) {
                _boltPopUpForm.CalculateDetailProperties();

                Control mainForm = WidgetUtils.MainPanel.Parent;
                TableLayoutPanel tablePanel = _boltPopUpForm.TablePanel;
                Padding contentPadding = _boltPopUpForm.ContentPanel.Padding;
                int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxMargin = boxHeight / 5;
                int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                Size contentSize = new((int) (mainForm.Width * .75), tableHeight + contentPadding.Size.Height);
                int tableWidth = contentSize.Width - contentPadding.Size.Width;
                _boltPopUpForm.BoxHeight = boxHeight;
                _boltPopUpForm.BoxMargin = boxMargin;
                _boltPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);

                _boltPopUpForm.SetContentSizeAndSelfSize(contentSize);
                if (_boltPopUpForm.Visible) {
                    _boltPopUpForm.Invalidate();
                }
            }
        }

        private void InitializeRightTop() {
            _workingProcessPanel = new() {
                Parent = _rightTop,
                Margin = new(0),
                Padding = new(0),
            };
            foreach (List<BoltButton> btns in _boltButtons) {
                foreach (BoltButton btn in btns) {
                    btn.WorkProcessPanel = _workingProcessPanel;
                }
            }
        }

        private void InitializeRightMiddle() {
            _torqueTitle = new() {
                Parent = _rightMiddle,
                Margin = new(1),
                Padding = new(0),
                Text = "扭矩（N*m）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_DATA_TITLE,
            };
            _torque = new() {
                Parent = _rightMiddle,
                Margin = new(1),
                Padding = new(0),
                Text = "0.0",
                TextAlign = ContentAlignment.MiddleRight,
            };
            _angleTitle = new() {
                Parent = _rightMiddle,
                Margin = new(1),
                Padding = new(0),
                Text = "角度（°）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_DATA_TITLE,
            };
            _angle = new() {
                Parent = _rightMiddle,
                Margin = new(1),
                Padding = new(0),
                Text = "0",
                TextAlign = ContentAlignment.MiddleRight,
            };

            _dataTimer = new();
            _dataTimer.Interval = 10;
            _dataTimer.Tick += TimerTick;
        }

        private void InitializeRightBottom() {
            _productSideTitle = new() {
                Parent = _rightBottom,
                Margin = new(1),
                Padding = new(0),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_TEXT,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_BACK,
            };
            _smallSideImage = new() {
                Parent = _rightBottom,
                Margin = new(0),
                Padding = new(0),
            };
            int totalPages = 0;
            List<ProductSideDTO>? productSides = _mission.ProductSides;
            if (productSides != null) {
                _productSideTitle.Text = productSides[0].name;
                totalPages = productSides.Count;
            }
            if (_missionImages.Count > 0) {
                _smallSideImagesForShowing = new();
                foreach (Image? image in _missionImages) {
                    if (image == null) {
                        _smallSideImagesForShowing.Add(_defaultImage);
                    } else {
                        _smallSideImagesForShowing.Add(image);
                    }
                }
            }
            int currentPage = _productSideIndex + 1;
            _first = new() {
                Icon = Properties.Resources.page_btn_backward_fast,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            _backward = new() {
                Icon = Properties.Resources.page_btn_backward,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            _forward = new() {
                Icon = Properties.Resources.page_btn_forward,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            _last = new() {
                Icon = Properties.Resources.page_btn_forward_fast,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            _pageInfo = new() {
                Margin = new(0),
                Padding = new(0),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_PAGE_TEXT,
            };
            _pageInfo.Text = currentPage + "/" + totalPages;
            _buttonPanel = new() {
                Parent = _rightBottom,
                Margin = new(1),
                Padding = new(0),
                ColumnCount = 5,
            };
            _buttonPanel.Controls.Add(_first);
            _buttonPanel.Controls.Add(_backward);
            _buttonPanel.Controls.Add(_pageInfo);
            _buttonPanel.Controls.Add(_forward);
            _buttonPanel.Controls.Add(_last);

            _first.Click += (sender, eventArgs) => {
                _productSideIndex = 0;
                changeCurrentPageAndInvalidate();
            };
            _backward.Click += (sender, eventArgs) => {
                if (_productSideIndex <= 0) {
                    _productSideIndex = 0;
                } else {
                    _productSideIndex -= 1;
                }
                changeCurrentPageAndInvalidate();
            };
            _forward.Click += (sender, eventArgs) => {
                if (_productSideIndex >= _missionImages.Count - 1) {
                    _productSideIndex = _missionImages.Count - 1;
                } else {
                    _productSideIndex += 1;
                }
                changeCurrentPageAndInvalidate();
            };
            _last.Click += (sender, eventArgs) => {
                _productSideIndex = _missionImages.Count - 1;
                changeCurrentPageAndInvalidate();
            };
            void changeCurrentPageAndInvalidate() {
                if (CurrentWorkingButton != null) {
                    CurrentWorkingButton.BackColor = BoltButton.WAITING;
                    CurrentWorkingButton.BoltStatus = BoltStatus.DEFAULT;
                    CurrentWorkingButton.StopFlicker();
                }
                int newCurrentPage = _productSideIndex + 1;
                _first.CurrentPage = newCurrentPage;
                _backward.CurrentPage = newCurrentPage;
                _forward.CurrentPage = newCurrentPage;
                _last.CurrentPage = newCurrentPage;

                foreach (BoltButton btn in _currentBoltButtons) {
                    btn.Visible = false;
                }
                _currentBoltButtons = _boltButtons[_productSideIndex];
                foreach (BoltButton btn in _currentBoltButtons) {
                    btn.Visible = true;
                }
                _productImageDisplayPanel.SetImage(_productImageFiles[_productSideIndex].Image, _productImageFiles[_productSideIndex].CenterLocation);
                _productImageFiles[_productSideIndex].RefreshImage();
                ResizeSmallSideImageBox(_smallSideImagesForShowing[_productSideIndex]);
                _pageInfo.Text = newCurrentPage + "/" + totalPages;
                _productSideTitle.Text = _productImageFiles[_productSideIndex].SideDTO.name;
                ResetRightBottomTitleFont();
            }
        }

        private void InitializeBottom() {
            _deviceBlocks = new();
            // 查询所有设备信息
            QueryDeviceListRsp rsp = apis.QueryDeviceList(new() {
                UserId = SystemUtils.LoggedUserId()
            });
            _deviceDTOs = rsp.DeviceDTOs;
            // Create device blocks
            foreach (DeviceDTO deviceDTO in _deviceDTOs) {
                DeviceBlock deviceBlock;
                if (deviceDTO.category_id != null) {
                    if (_deviceBlocks.ContainsKey(deviceDTO.category_id.Value)) {
                        deviceBlock = _deviceBlocks[deviceDTO.category_id.Value];
                    } else {
                        deviceBlock = new DeviceBlock(
                            ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, deviceDTO.category_name
                        ) {
                            Icon = CommonUtils.ImageBase64ToImage(deviceDTO.icon_normal),
                            Parent = _bottom,
                            Margin = new(0),
                            Padding = new(0),
                            ToggledButton = true,
                            ToggledColor = ColorConfigs.COLOR_DEVICE_BLOCK_TOGGLED,
                        };
                        deviceBlock.Click += (sender, eventArgs) => {
                            try {
                                EventFuncs.CurrentPopUpForm = deviceBlock.PopUpForm;
                                deviceBlock.PopUpForm.Show();
                            } finally {
                                deviceBlock.SetToggle(false);
                            }
                        };
                        _deviceBlocks.Add(deviceDTO.category_id.Value, deviceBlock);
                    }
                    deviceBlock.DeviceDTOs.Add(deviceDTO.id, deviceDTO);
                    deviceBlock.PopUpForm.DeviceDTOs.Add(deviceDTO);
                }
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            ReSizeContents();
            ResizeleftTop();
            ResizeLeftBottom();
            ResizeRightTop();
            ResizeRightMiddle();
            ResizeRightBottom();
            ResizeBottom();
            Invalidate();
        }

        private void ReSizeContents() {
            int wholeWidth = Width - Padding.Left * 2;
            int wholeHeight = Height - Padding.Top * 2;
            int bottomHeight = (int) (wholeHeight * .09);
            int othersHeight = wholeHeight - bottomHeight - Padding.Top / 2;
            int leftWidth = (int) (wholeWidth * .78);
            int rightWidth = wholeWidth - leftWidth - Padding.Left / 2;

            _left.Size = new(leftWidth, othersHeight);
            _left.Margin = new(0, 0, Padding.Left / 2, Padding.Top / 2);
            _right.Size = new(rightWidth, othersHeight);
            _bottom.Size = new(wholeWidth, bottomHeight);

            Size mainSize = WidgetUtils.MainPanel.Parent.Size;
            double ratio = mainSize.Width / (double) mainSize.Height;
            _leftBottom.Size = new(_left.Width, (int) (_left.Width / ratio));
            _leftBottom.Margin = new(0, Padding.Top / 2, 0, 0);
            _leftTop.Size = new(_left.Width, _left.Height - _leftBottom.Height - Padding.Top / 2);

            _rightTop.Size = new(_right.Width, (int) (_right.Height * .4));
            _rightMiddle.Size = new(_right.Width, (int) (_right.Height * .3));
            _rightBottom.Size = new(_right.Width, _right.Height - _rightTop.Height - _rightMiddle.Height - Padding.Top / 2 * 2);
            _rightMiddle.Margin = new(0, Padding.Top / 2, 0, Padding.Top / 2);
        }

        private void ResizeleftTop() {
            // icon的边长
            int side = (int) (_leftTop.Height * .65);
            // 重设icon
            _barCodePictureBox.Image = WidgetUtils.ResizeImageWithoutLosingQuality(_barCodeImage, side, side);
            _barCodePictureBox.Margin = new((_leftTop.Height - side) / 2);
            _barCodePictureBox.Size = new(side, side);

            // 重设输入框
            int newH = (int) (_leftTop.Height * .75);
            _barCodeTextBox.Size = new(_leftTop.Width - side * 2, newH);
            _barCodeTextBox.Margin = new(0, (_leftTop.Height - newH) / 2, 0, 0);

            // 重新计算弹框的大小
            if (_barCodePopUpForm != null) {
                ResizeBarCodePopUpForm();
            }
        }

        private void ResizeBarCodePopUpForm() {
            _barCodePopUpForm.CalculateDetailProperties();

            Control mainForm = WidgetUtils.MainPanel.Parent;
            Padding contentPadding = _barCodePopUpForm.ContentPanel.Padding;
            int boxHeight = (int) (mainForm.Height * .05);
            Size contentSize = new((int) (mainForm.Width * .75), boxHeight + contentPadding.Size.Height);
            int boxWidth = contentSize.Width - contentPadding.Size.Width;
            _barCodePopUpForm.TextBox.Size = new(boxWidth, boxHeight);

            _barCodePopUpForm.SetContentSizeAndSelfSize(contentSize);
        }

        private void ResizeLeftBottom() {
            // Image panel 要比 _middleBottom 小2，是为了显示出后者的边框
            Size newPanelSize = new(_leftBottom.Width - 2, _leftBottom.Height - 2);
            _productImageDisplayPanel.Size = newPanelSize;

            foreach (ProductImageFile productImageFile in _productImageFiles) {
                productImageFile.RecalculateZoomingRatio();
            }
            _productImageFiles[_productSideIndex].RefreshImage();

            // 重新计算螺栓点位按钮的大小和位置
            if (_boltButtons.Count > 0) {
                int btnSide = newPanelSize.Height / 13;
                foreach (List<BoltButton> boltList in _boltButtons) {
                    if (boltList.Count > 0) {
                        foreach (BoltButton boltButton in boltList) {
                            boltButton.Size = new(btnSide, btnSide);
                            int newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100);
                            int newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100);
                            boltButton.Location = new(newX, newY);
                        }
                    }
                }
            }

            // 重新计算弹框的大小和位置
            ResizePopUpForm();
        }

        private void ResizeRightTop() {
            _workingProcessPanel.Size = _rightTop.Size;
        }

        private void ResizeRightMiddle() {
            // Resize titles
            _torqueTitle.Size = new(_rightMiddle.Width - 2, (int) (_rightMiddle.Height * .2));
            _angleTitle.Size = _torqueTitle.Size;
            // Reset font size
            _torqueTitle.Font = new Font(WidgetsConfigs.SystemFontFamily, _torqueTitle.Height * .55f, FontStyle.Bold, GraphicsUnit.Pixel);
            _angleTitle.Font = _torqueTitle.Font;
            // Resize data text
            int heightRemain = _rightMiddle.Height - _torqueTitle.Height - _angleTitle.Height - 6; // 2 vertical border, 2 vertical margin of each title
            _torque.Size = new(_rightMiddle.Width - 2, (int) (heightRemain * .6) - 2);
            _angle.Size = new(_rightMiddle.Width - 2, heightRemain - _torque.Height - 2);
            // Reset font size depends on theirs height
            _torque.Font = new(WidgetsConfigs.SystemFontFamily, _torque.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
            _angle.Font = new(WidgetsConfigs.SystemFontFamily, _angle.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private void ResizeRightBottom() {
            // Resize title
            _productSideTitle.Size = new(_rightBottom.Width - 2, (int) (_rightBottom.Height * .2));
            // Reset font size
            ResetRightBottomTitleFont();
            // Resize product side image
            int imageWholeHeight = (int) ((_rightBottom.Height - 2 - _productSideTitle.Height) * .8);
            int vPadding = (int) (imageWholeHeight * .1);
            int imageHeight = imageWholeHeight - vPadding * 2;
            if (_missionImages.Count > 0) {
                for (int i = 0; i < _missionImages.Count; i++) {
                    Image? image = _missionImages[i];
                    Size newISize;
                    if (image == null) {
                        image = _defaultImage;
                        newISize = new((int) (imageHeight / (decimal) _defaultImage.Height * _defaultImage.Width), imageHeight);
                        _smallSideImagesForShowing[i] = WidgetUtils.ResizeImageWithoutLosingQuality(_defaultImage, newISize);
                    }
                    newISize = new((int) (imageHeight / (decimal) image.Height * image.Width), imageHeight);
                    Image imageNew = WidgetUtils.ResizeImageWithoutLosingQuality(image, newISize);
                    _smallSideImagesForShowing[i] = imageNew;
                    if (i == _productSideIndex) {
                        ResizeSmallSideImageBox(imageNew);
                    }
                }
            }
            // Resize table panel 
            int tablePanelHeight = _rightBottom.Height - 4 - _productSideTitle.Height - imageWholeHeight;
            int buttonSide = (int) (tablePanelHeight * .725);
            int labelHeight = (int) (tablePanelHeight * .825);
            int labelVPadding = (tablePanelHeight - labelHeight) / 5;
            int buttonVPadding = (tablePanelHeight - buttonSide) / 2;
            int buttonHPdding = (int) (buttonSide * .45);
            _buttonPanel.Size = new(_rightBottom.Width - 2 - buttonHPdding * 2, tablePanelHeight);
            _buttonPanel.Margin = new(buttonHPdding, 0, buttonHPdding, 0);
            // Resize icon button
            _first.Size = new(buttonSide, buttonSide);
            _first.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
            _backward.Size = new(buttonSide, buttonSide);
            _backward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
            _forward.Size = new(buttonSide, buttonSide);
            _forward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
            _last.Size = new(buttonSide, buttonSide);
            _last.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
            // Resize page info label
            _pageInfo.Size = new(_buttonPanel.Width - 4 * buttonSide - buttonHPdding * 8, tablePanelHeight);
            _pageInfo.Margin = new(0, 0, 0, 0);
            _pageInfo.Font = new(WidgetsConfigs.SystemFontFamily, _pageInfo.Height * .675F, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private void ResizeSmallSideImageBox(Image? newImage) {
            if (newImage != null) {
                int imageWholeHeight = (int) ((_rightBottom.Height - 2 - _productSideTitle.Height) * .8);
                int vPadding = (int) (imageWholeHeight * .1);
                int hPadding = (_rightBottom.Width - 2 - newImage.Width) / 2;
                _smallSideImage.Size = newImage.Size;
                _smallSideImage.Image = newImage;
                _smallSideImage.Margin = new(hPadding, vPadding, hPadding, vPadding);
            }
        }

        private void ResetRightBottomTitleFont(float fontRatio = .55f) {
            Font font = new Font(WidgetsConfigs.SystemFontFamily, _productSideTitle.Height * fontRatio, FontStyle.Bold, GraphicsUnit.Pixel);
            using (Graphics g = CreateGraphics()) {
                if (g.MeasureString(_productSideTitle.Text, font).Width >= _productSideTitle.Width * .9) {
                    ResetRightBottomTitleFont(fontRatio -= .025f);
                } else {
                    _productSideTitle.Font = font;
                }
            }
        }

        private void ResizeBottom() {
            foreach (Control control in _bottom.Controls) {
                control.Size = new(_bottom.Height, _bottom.Height);
            }
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            // Read realtime data
            // TODO: read data here
            // Set data and display
            _torque.Text = "";
            _angle.Text = "";
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }

    public class ProductImageDisplayPanel: AProductImageDisplayPanel {
        public ProductImageDisplayPanel(Image productDefaultImage) : base() {
            ProductDefaultImage = productDefaultImage;
        }

        protected override void InvokeResizing() {
            // Make maximum width equals to 85% of parent width to ensure all retangles can be seen
            MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByHeight(Height);
            MaxRectWidth = MaxRectSize.Width;
            MaxRectHeight = MaxRectSize.Height;
            // Calculate location of max rectangle depends on size
            MaxRectLocation = new((Width - MaxRectWidth) / 2, (Height - MaxRectHeight) / 2);
            MaxRect = new(MaxRectLocation, MaxRectSize);
        }

        protected override void InvokePaint(Graphics g) {
            g.SmoothingMode = SmoothingMode.HighSpeed;
            if (ProductImage == null || ImageLocation == null) {
                int newImageSide = Height / 2;
                ProductDefaultImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(ProductDefaultImage, newImageSide, newImageSide);
                g.DrawImage(ProductDefaultImageShowing, new Point((Width - ProductDefaultImageShowing.Width) / 2, (Height - newImageSide) / 2));
            } else {
                g.DrawImage(ProductImage, ImageLocation.Value);
            }
        }
    }

    public class BarCodeInputPopUpForm: CustomPopUpForm {
        private CustomTextBox _textBox;
        private string _initStr;

        public CustomTextBox TextBox { get => _textBox; set => _textBox = value; }

        public BarCodeInputPopUpForm(string initStr, CustomTextBox upperBox) : base() {
            _initStr = initStr;
            _textBox = new() {
                Parent = ContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberValidate = true,
            };
            _textBox.GotFocus += (s, e) => {
                EventFuncs.CurrentActiveControl = _textBox;
                if (_textBox.Text == _initStr) {
                    _textBox.Text = "";
                }
            };
            _textBox.LostFocus += (s, e) => {
                if (_textBox.Text == "") {
                    _textBox.Text = _initStr;
                }
            };
            _textBox.Focus();
        }
    }

    public class WorkingProcessPanel: Panel {
        private Image _clockwiseIcon;
        private Image _anticlockwiseIcon;
        private Image _iconShowing;
        private Color _correctColor;
        private Color _errorColor;
        private int _borderSize;
        private Rectangle _borderRect;

        private Panel _picturePanel;
        private PictureBox _pictureBox;
        private float _rotateAngle;

        private Label _statusTxt;
        private Label _statusDesc;

        private ProductMissionStatus _missionStatus;
        private ProductBoltDTO? _boltDTO;
        private BoltStatus _boltStatus;

        private System.Windows.Forms.Timer _processingTimer;

        public ProductMissionStatus Status {
            get => _missionStatus;
            set {
                _missionStatus = value;
                InvokePainting();
                switch (value) {
                    case ProductMissionStatus.READY:
                        _statusTxt.Text = "就绪";
                        _statusTxt.BackColor = _correctColor;
                        _statusTxt.Visible = true;
                        break;
                    case ProductMissionStatus.ERROR:
                        _statusTxt.Text = "错误";
                        _statusTxt.BackColor = _errorColor;
                        _statusTxt.Visible = true;
                        break;
                    case ProductMissionStatus.FINISHED:
                        _statusTxt.Text = "完成";
                        _statusTxt.BackColor = _correctColor;
                        _statusTxt.Visible = true;
                        break;
                    default:
                        _statusTxt.Visible = false;
                        break;
                }
            }
        }

        public BoltStatus BoltStatus {
            get => _boltStatus;
            set {
                _boltStatus = value;
                switch (value) {
                    case BoltStatus.TIGHTENING:
                        StartTimer();
                        _statusDesc.Text = "正在拧紧[%]号螺丝";
                        if (_boltDTO != null) {
                            _statusDesc.Text = _statusDesc.Text.Replace("%", _boltDTO.serial_num + "");
                        }
                        _statusDesc.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                        _statusDesc.BackColor = BackColor;
                        _statusDesc.Visible = true;
                        break;
                    case BoltStatus.LOOSENING:
                        StartTimer();
                        _statusDesc.Text = "正在反松[%]号螺丝";
                        if (_boltDTO != null) {
                            _statusDesc.Text = _statusDesc.Text.Replace("%", _boltDTO.serial_num + "");
                        }
                        _statusDesc.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                        _statusDesc.BackColor = BackColor;
                        _statusDesc.Visible = true;
                        break;
                    default:
                        StopTimer();
                        _statusDesc.Visible = false;
                        break;
                }
                InvokeResizing();
                InvokePainting();
            }
        }

        public ProductBoltDTO? BoltDTO {
            get => _boltDTO;
            set => _boltDTO = value;
        }

        public WorkingProcessPanel() : base() {
            _clockwiseIcon = Properties.Resources.processing_clockwise;
            _anticlockwiseIcon = Properties.Resources.processing_anticlockwise;
            _correctColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
            _errorColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
            _statusTxt = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_WHITE,
                BackColor = _correctColor,
                Text = "就绪",
            };
            _statusDesc = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                Visible = false,
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _missionStatus = ProductMissionStatus.READY;
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
            _processingTimer = new();
            _processingTimer.Interval = 40;
            _processingTimer.Tick += TimerTick;
        }

        public void StartTimer() {
            _processingTimer.Start();
            _picturePanel.Visible = true;
            _rotateAngle = 0F;
        }

        public void StopTimer() {
            _processingTimer.Stop();
            _picturePanel.Visible = false;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            _borderRect.Size = Size;
            _borderSize = Width / 40 + Height / 80;
            _picturePanel.Size = new(Width - _borderSize * 2, (int) ((Height - _borderSize * 2) * .75F));
            _picturePanel.Location = new(_borderSize, _borderSize);

            int imageSide = (int) (_picturePanel.Height * .85);
            if (_picturePanel.Height > _picturePanel.Width) {
                imageSide = (int) (_picturePanel.Width * .85);
            }
            if (_missionStatus == ProductMissionStatus.WORKING) {
                if (_boltStatus == BoltStatus.TIGHTENING) {
                    _iconShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_clockwiseIcon, new Size(imageSide, imageSide));
                } else if (_boltStatus == BoltStatus.LOOSENING) {
                    _iconShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_anticlockwiseIcon, new Size(imageSide, imageSide));
                }
            }

            int labelWdith = Width - _borderSize * 2;
            // Resize and relocated status desc
            _statusDesc.Font = new(WidgetsConfigs.SystemFontFamily, (Height - _picturePanel.Height) * .28F, FontStyle.Bold, GraphicsUnit.Pixel);
            _statusDesc.Width = labelWdith;
            ResetControlFont(_statusDesc, .1f);
            _statusDesc.Height = _statusDesc.Font.Height;
            int hTemp = _picturePanel.Location.Y + _picturePanel.Height;
            _statusDesc.Location = new(_borderSize, (int) (hTemp + (Height - hTemp - _borderSize - _statusDesc.Height) / 1.5));
            // Resize and relocate status text
            _statusTxt.Width = labelWdith;
            ResetControlFont(_statusTxt, .3f);
            _statusTxt.Height = _statusTxt.Font.Height;
            _statusTxt.Location = new(_borderSize, (Height - _statusTxt.Height - (_statusDesc.Visible ? _statusDesc.Height : 0)) / 2);
        }

        private void ResetControlFont(Control control, float fontRatio) {
            Font font = new Font(WidgetsConfigs.SystemFontFamily, Height * fontRatio, FontStyle.Bold, GraphicsUnit.Pixel);
            using (Graphics g = CreateGraphics()) {
                if (g.MeasureString(control.Text, font).Width >= control.Width * .9) {
                    ResetControlFont(control, fontRatio -= .005f);
                } else {
                    control.Font = font;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            InvokePainting();
        }

        private void InvokePainting() {
            using (Graphics g = CreateGraphics()) {
                if (_missionStatus == ProductMissionStatus.WORKING) {
                    g.DrawRectangle(new(_correctColor, _borderSize), _borderRect);
                } else if (_missionStatus == ProductMissionStatus.FINISHED) {
                    g.FillRectangle(new SolidBrush(_correctColor), _borderRect);
                } else if (_missionStatus == ProductMissionStatus.ERROR) {
                    g.FillRectangle(new SolidBrush(_errorColor), _borderRect);
                } else if (_missionStatus == ProductMissionStatus.READY) {
                    g.FillRectangle(new SolidBrush(_correctColor), _borderRect);
                }
            }
        }

        private void TimerTick(object? sender, EventArgs eventArgs) {
            _rotateAngle += 15;
            Image image = WidgetUtils.RotateImage(_iconShowing, _rotateAngle);
            _pictureBox.Size = image.Size;
            _pictureBox.Image = image;
            _pictureBox.Location = new((_picturePanel.Width - _pictureBox.Width) / 2, (_picturePanel.Height - _pictureBox.Height) / 2);
        }
    }

    public class DeviceBlock: CustomImageTextButtonBase {
        private readonly float _imageRatio = 0.75F;
        private Rectangle _borderRect;
        private Color _borderColor;
        private Dictionary<int, DeviceDTO> _deviceDTOs;
        private DeviceDetailPopUpForm _popUpForm;

        public Dictionary<int, DeviceDTO> DeviceDTOs {
            get => _deviceDTOs;
        }
        public DeviceDetailPopUpForm PopUpForm {
            get => _popUpForm;
        }

        public DeviceBlock(Color borderColor, string categoryName) : base() {
            _borderColor = borderColor;
            _deviceDTOs = new();
            _popUpForm = new(categoryName);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _borderRect = new(0, 0, Width, Height);

            // 计算以及弹出窗口的size
            if (_popUpForm != null) {
                int count = _deviceDTOs.Count;
                int mainW = WidgetUtils.MainPanel.Parent.Width;
                int mainH = WidgetUtils.MainPanel.Parent.Height;
                int extraH = _popUpForm.TitlePanel.Height + _popUpForm.ButtonsPanel.Height;
                int popUpW = (int) (mainW * .4);
                int hGap = (int) (mainH * .02);
                int popUpContentH = (int) (mainH * .05 + mainW * .0165 - extraH) * count + hGap * (count + 1);
                _popUpForm.HeightGap = hGap;
                _popUpForm.ContentPanel.Size = new(popUpW, popUpContentH);
            }
        }

        protected override void PaintAfter(PaintEventArgs e) {
            base.PaintAfter(e);
            e.Graphics.DrawRectangle(new Pen(_borderColor, 1), _borderRect);
        }

        protected override void ResizeIconImage() {
            if (Icon != null) {
                Size newSize = (Size * _imageRatio).ToSize();
                ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(Icon, newSize);
                // Recalculate image position
                ImageX = (Width - newSize.Width) / 2;
                ImageY = (Height - newSize.Height) / 2;
            }
        }

        protected override void ResizeTextLabel() {
        }
    }

    public class DeviceDetailPopUpForm: CustomPopUpForm {
        private readonly Image _statusIconConnected;
        private readonly Image _statusIconDisconnected;
        private int _iconHeight;
        private Image? _connectedShowing;
        private Image? _disconnectedShowing;
        private int _firstImageX;
        private int _firstImageY;
        private List<DeviceDTO> _deviceDTOs;
        private Dictionary<int, CommonButton> _operateButtons;
        private int _heightGap;
        private DeviceOperationPopUpForm? _currentPopUpForm;

        public List<DeviceDTO> DeviceDTOs {
            get => _deviceDTOs;
        }
        public int HeightGap {
            get => _heightGap;
            set => _heightGap = value;
        }

        public DeviceDetailPopUpForm(string categoryName) : base() {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;

            _statusIconConnected = Properties.Resources.device_connected;
            _statusIconDisconnected = Properties.Resources.device_connected;
            _deviceDTOs = new();
            _operateButtons = new();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            ResizeIconImage();
            ResizeText();

            for (int i = 0; i < DeviceDTOs.Count; i++) {
                DeviceDTO deviceDTO = _deviceDTOs[i];
                CommonButton button;
                if (_operateButtons.ContainsKey(deviceDTO.id)) {
                    button = _operateButtons[deviceDTO.id];
                } else {
                    button = new();
                    button.Label = "操作";
                    button.Parent = this;
                    button.Enabled = deviceDTO.can_manipulate == (int) (YesOrNo.YES);
                    _operateButtons.Add(deviceDTO.id, button);
                    button.Click += (s, e) => {
                        HideForm();
                        _currentPopUpForm = new(this, deviceDTO.ip + "-" + deviceDTO.port);
                        ResizeSecondPopUpForm();
                        _currentPopUpForm.Show();
                        EventFuncs.CurrentPopUpForm = _currentPopUpForm;
                    };
                }
                if (deviceDTO.ip != null && deviceDTO.port != null) {
                    if (ConnectionUtils.CheckConnection(deviceDTO.ip, deviceDTO.port.Value) == ConnectionStatus.DISCONNECTED) {
                        button.Enabled = false;
                    }
                }

                int btnH = (int) (_iconHeight * 1.15);
                button.Size = new((int) (btnH * 2.15), btnH);
                button.Location = new((int) ((Width - button.Width) * .95), HeightGap * (i + 1) + _iconHeight * i + TitlePanel.Height - (btnH - _iconHeight) / 2);
            }

            ResizeSecondPopUpForm();
        }

        private void ResizeSecondPopUpForm() {
            if (_currentPopUpForm != null) {
                _currentPopUpForm.CalculateDetailProperties();

                Control mainForm = WidgetUtils.MainPanel.Parent;
                TableLayoutPanel tablePanel = _currentPopUpForm.TablePanel;
                Panel contentPanel = _currentPopUpForm.ContentPanel;
                int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxMargin = boxHeight / 5;
                int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                Size contentSize = new((int) (mainForm.Width * .75), tableHeight + contentPanel.Padding.Size.Height);
                int tableWidth = contentSize.Width - contentPanel.Padding.Size.Width;
                _currentPopUpForm.BoxHeight = boxHeight;
                _currentPopUpForm.BoxMargin = boxMargin;
                _currentPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);

                _currentPopUpForm.SetContentSizeAndSelfSize(contentSize);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            if (_connectedShowing != null && _disconnectedShowing != null) {
                for (int i = 0; i < _deviceDTOs.Count; i++) {
                    DeviceDTO deviceDTO = _deviceDTOs[i];

                    Point imagePoint;
                    Point textPoint;
                    if (i == 0) {
                        imagePoint = new(_firstImageX, _firstImageY);
                    } else {
                        imagePoint = new(_firstImageX, HeightGap * (i + 1) + _iconHeight * i + TitlePanel.Height);
                    }
                    textPoint = new((int) (imagePoint.X * 1.2) + _connectedShowing.Width, imagePoint.Y + (_connectedShowing.Height - Font.Height) / 2);

                    if (deviceDTO.ip != null && deviceDTO.port != null) {
                        ConnectionStatus status = ConnectionUtils.CheckConnection(deviceDTO.ip, deviceDTO.port.Value);
                        if (status == ConnectionStatus.CONNECTED) {
                            e.Graphics.DrawImage(_connectedShowing, imagePoint);
                        } else {
                            e.Graphics.DrawImage(_disconnectedShowing, imagePoint);
                        }
                    }
                    e.Graphics.DrawString(deviceDTO.ip + "-" + deviceDTO.port, Font, new SolidBrush(Color.Black), textPoint);
                }
            }
        }

        protected void ResizeIconImage() {
            int count = DeviceDTOs.Count;
            _iconHeight = (Height - TitlePanel.Height - HeightGap * (count + 1)) / count;
            _connectedShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconConnected, _iconHeight, _iconHeight);
            _disconnectedShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconDisconnected, _iconHeight, _iconHeight);
            // Recalculate image location
            _firstImageX = (int) (Width * .075);
            _firstImageY = HeightGap + TitlePanel.Height;
        }

        protected void ResizeText() {
            Font = new Font(WidgetsConfigs.SystemFontFamily, _iconHeight * .55F, FontStyle.Regular);
        }

    }

    public class DeviceOperationPopUpForm: CustomPopUpForm {
        private CustomPopUpForm _upperForm;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private CustomTextBoxGroup _stationTextBox;
        private CustomTextBoxGroup _procedureTextBox;

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }

        public DeviceOperationPopUpForm(CustomPopUpForm upperForm, string titleInfo) : base() {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "手动操作工具（" + titleInfo + "）";

            _upperForm = upperForm;
            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                Parent = this,
            };

            _stationTextBox = new("站点") {
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _procedureTextBox = new("程序") {
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _stationTextBox.SetValue(0, "1");
            _procedureTextBox.SetValue(0, "1");

            _tablePanel.Controls.Add(_stationTextBox);
            _tablePanel.Controls.Add(_procedureTextBox);

            CommonButton btnLock = AddButton("禁用");
            btnLock.Click += (s, e) => {
                //_stationTextBox.SetValue(0, "test");
            };
            CommonButton btnUnlock = AddButton("使能");
            btnUnlock.Click += (s, e) => {
                //_procedureTextBox.SetValue(0, "test22");
            };
            CommonButton btnPSet = AddButton("下发");
            btnPSet.Click += (s, e) => {
                string station = _stationTextBox.GetTextBox(0).Text;
                string procedure = _procedureTextBox.GetTextBox(0).Text;
            };
            CommonButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => {
                HideForm();
            };
        }

        public override void HideForm() {
            base.HideForm();
            _upperForm.Show();
            EventFuncs.CurrentPopUpForm = _upperForm;
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
