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
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Extensions;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView: CustomContentPanel {
        private readonly int _tableColumns = 4;
        private readonly float _cellGapRatio = 0.02F;
        private readonly float _cellHightRatio = 0.24F;
        private int _titleHeight;
        private int _cellHorizontalMargin;
        private int _cellVerticalMargin;
        private Size _cellSize;
        private MissionNewButtonPanel _bigButtonPanel;
        private MissionListPanel _missionListPanel;
        private List<ProductMissionDTO> _productMissionDTOs;
        private readonly OperationGuidanceApis apis;

        private CustomTabPanel? _pagePanel;
        private TopBar? _topBar;
        private WorkplaceContentPanel? _workplacePanel;

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
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                System.Console.WriteLine($"_workplacePanel.Activated: {_workplacePanel.Activated}");
                // TODO: 这里或许可以做一个“任务中断”的效果，即不是每次进入都打开一个新的任务
            }
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
                Parent = TopLevelControl,
                Size = TopLevelControl.ClientSize,
            };
            _topBar = new(CommonUtils.CannotBeNull(missionDTO.name)) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND,
                MainMenuLogo = Properties.Resources.logo,
                Margin = new Padding(0),
                PanelDirection = MenuPanelDirection.TOP,
                TitleColor = ColorConfigs.COLOR_WORKPLACE_TITLE,
            };
            _workplacePanel = new(missionDTO) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
                Margin = new Padding(0),
                PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topBar.Workplace = _workplacePanel;
            _pagePanel.ResizeChildren();
            // Hide main panel
            WidgetUtils.MainPanel.Visible = false;
        }

        private void FetchData() {
            QueryProductMissionListReq req = new();
            QueryProductMissionListRsp rsp;

            rsp = apis.QueryProductMissionListRsp(req);
            // 过滤掉某些产品面图片没有配置的不合格的任务
            _productMissionDTOs = rsp.ProductMissionsDTOs
                .Where(dto => dto.ProductSides == null || dto.ProductSides.Where(side => side.image == null || side.image == "").Count() == 0)
                .ToList();
        }
    }
    public class TopBar: CustomMainMenuPanel {
        private WorkplaceContentPanel? _workplace;
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
        public WorkplaceContentPanel? Workplace { get => _workplace; set => _workplace = value; }

        public TopBar(string title) : base() {
            _title = title;
            _backButton = new() {
                Label = "返回",
                Parent = this,
            };
            _backButton.Click += (sender, eventArgs) => {
                if (_workplace != null && _workplace.Activated && !_workplace.Finished) {
                    bool yes = WidgetUtils.ShowConfirmPopUp("当前已激活任务还未完成，返回主界面将终止任务，确认返回？");
                    if (yes) {
                        _workplace.Activated = false;
                        WidgetUtils.MainPanel.Visible = true;
                        Parent.Visible = false;
                        _workplace.Dispose();
                    }
                } else {
                    WidgetUtils.MainPanel.Visible = true;
                    Parent.Visible = false;
                    if (_workplace != null && !_workplace.IsDisposed) {
                        _workplace.Dispose();
                    }
                }
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
            _backButton.Margin = new(0, (Height - newHeight) / 2, 0, 0);
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

    public class WorkplaceContentPanel: CustomContentPanel {
        private OperationGuidanceApis _apis;
        private ProductMissionDTO _mission;
        private bool _activated;
        private bool _finished;
        private Image _defaultImage;

        private List<WorkstationDTO> _workstationsDTOs = new();
        private readonly int _checkDevicesConnectionDelay = 500;
        private List<DeviceArmDTO> _arms;
        private List<DeviceToolDTO> _tools;
        private List<DeviceSerialPortDTO> _serialPorts;
        private Dictionary<int, ArmTask> _armTasks = new();
        private Dictionary<int, ToolTask> _toolTasks = new();
        private Dictionary<int, SerialPortTask> _serialPortTasks = new();
        private string? _barCodeMessage;
        private TighteningData? _tighteningData;
        private List<OperationDataDTO> _dataNeedToBeStored = new();

        private CustomContentPanel _left;
        private CustomContentPanel _right;
        private WorkplacePiece _bottom;

        private WorkplacePiece _leftTop;
        private WorkplacePiece _leftBottom;

        private WorkplacePiece _rightTop;
        private WorkplacePiece _rightMiddle;
        private WorkplacePiece _rightBottom;


        // Left top
        private Image _barCodeImage;
        private PictureBox _barCodePictureBox;
        private CustomTextBox _barCodeTextBox;
        private BarCodeInputPopUpForm? _barCodePopUpForm;

        // Left bottom
        private int _currentSideIndex;
        private List<ProductSideDTO> _sides;
        private ProductImageDisplayPanel _productImageDisplayPanel;
        private List<ProductImageFile> _productImageFiles;
        private List<Image?> _missionImages;
        private List<BoltButton> _allBolts;
        private List<BoltButton> _showingBoltButtons;
        private BoltButton? _currentWorkingBolt;
        private BoltPopUpForm _boltPopUpForm;

        // Right top
        private WorkingProcessPanel _workingProcessPanel;

        // Right middle
        private Label _torqueTitle;
        private Label _torque;
        private Label _angleTitle;
        private Label _angle;

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
        private List<DeviceBlock> _deviceBlocks;

        public bool Activated { get => _activated; set => _activated = value; }
        public bool Finished { get => _finished; set => _finished = value; }

        public WorkplaceContentPanel(ProductMissionDTO mission) : base() {
            _apis = SystemUtils.GetApis();
            _mission = mission;
            _activated = false;
            _finished = false;
            _defaultImage = Properties.Resources.image_choose;
            _currentSideIndex = 0;
            _sides = new();
            if (_mission.ProductSides != null) {
                _sides.AddRange(_mission.ProductSides);
            }

            InitializeContents();
            InitializeLeftTop();
            InitializeLeftBottom();
            InitializeRightTop();
            InitializeRightMiddle();
            InitializeRightBottom();
            InitializeBottom();

            // Load devices asynchronously to avoid delay UI creating
            LoadDevicesAsync();
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
                if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                    _barCodePopUpForm = new(note, _barCodeTextBox) {
                        Title = "录入条码",
                        BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                    };
                    _barCodePopUpForm.AddButton("确定").Click += (sender, eventArgs) => {
                        if (!_barCodePopUpForm.TextBox.IsError) {
                            if (!_activated || _finished) {
                                _barCodeTextBox.Text = _barCodePopUpForm.TextBox.Text;
                                // 激活任务
                                ActivateMission();
                            }
                            _barCodePopUpForm.Dispose();
                        }
                    };
                    _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.Dispose();
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
            _allBolts = new();

            List<ProductSideDTO>? productSides = _mission.ProductSides;
            if (productSides != null) {
                foreach (ProductSideDTO sideDTO in productSides) {
                    ProductImageFile productImageFile = new(_productImageDisplayPanel, sideDTO, 0);
                    _productImageFiles.Add(productImageFile);
                    // Initialize product image info
                    _missionImages.Add(productImageFile.Image);

                    // 配置螺栓点位
                    List<ProductBoltDTO>? bolts = sideDTO.Bolts;
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
                                    if (!_activated || _finished) {
                                        WidgetUtils.ShowErrorPopUp("任务未激活或已完成，无法切换点位！");
                                    } else {
                                        // 切换点位时，只能向后选择没有拧的点位，不能选择前面的。即只能跳过某些点，不能重新打某些点
                                        if (_currentWorkingBolt != null) {
                                            int newIndex = _allBolts.IndexOf(boltBtn);
                                            if (_allBolts.IndexOf(_currentWorkingBolt) > newIndex) {
                                                WidgetUtils.ShowErrorPopUp("无法切换到已完成的螺栓点位！");
                                            } else {
                                                _currentWorkingBolt.ResetStatusWithoutChangingVisible();
                                                _currentWorkingBolt.StopFlickering();
                                                // _currentWorkingBolt.BoltStatus = BoltStatus.DEFAULT;
                                                _currentWorkingBolt = SwitchBolt(newIndex);
                                            }
                                        }
                                    }
                                    _boltPopUpForm.Dispose();
                                };
                                CommonButton closeBtn = _boltPopUpForm.AddButton("关闭");
                                closeBtn.Click += (s, e) => {
                                    _boltPopUpForm.Dispose();
                                };
                                // Show form but make it transparent to create handles for its children
                                _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
                                // Resize all widgets
                                ResizePopUpForm();
                                // Real show
                                _boltPopUpForm.Show();
                            };
                            _allBolts.Add(boltBtn);
                        }
                    }
                }
            }

            // 默认显示第一个产品面和对应的螺栓点位
            _showingBoltButtons = _allBolts.Where(btn => btn.BoltDTO.side_id != null && btn.BoltDTO.side_id == _sides[_currentSideIndex].id).ToList();
            _showingBoltButtons.ForEach(btn => btn.Visible = true);
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
            int currentPage = _currentSideIndex + 1;
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
                _currentSideIndex = 0;
                changeCurrentPageAndInvalidate();
            };
            _backward.Click += (sender, eventArgs) => {
                if (_currentSideIndex <= 0) {
                    _currentSideIndex = 0;
                } else {
                    _currentSideIndex -= 1;
                }
                changeCurrentPageAndInvalidate();
            };
            _forward.Click += (sender, eventArgs) => {
                if (_currentSideIndex >= _missionImages.Count - 1) {
                    _currentSideIndex = _missionImages.Count - 1;
                } else {
                    _currentSideIndex += 1;
                }
                changeCurrentPageAndInvalidate();
            };
            _last.Click += (sender, eventArgs) => {
                _currentSideIndex = _missionImages.Count - 1;
                changeCurrentPageAndInvalidate();
            };
            void changeCurrentPageAndInvalidate() {
                if (_currentWorkingBolt != null) {
                    if (_currentWorkingBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                        _currentWorkingBolt.ShowingWhileWorking = false;
                    } else {
                        _currentWorkingBolt.ShowingWhileWorking = true;
                    }
                }
                int newCurrentPage = _currentSideIndex + 1;
                _first.CurrentPage = newCurrentPage;
                _backward.CurrentPage = newCurrentPage;
                _forward.CurrentPage = newCurrentPage;
                _last.CurrentPage = newCurrentPage;
                // 切换side后也切换点位
                _showingBoltButtons.ForEach(btn => btn.Visible = false);
                _showingBoltButtons = _allBolts.Where(btn => btn.BoltDTO.side_id != null && btn.BoltDTO.side_id == _sides[_currentSideIndex].id).ToList();
                _showingBoltButtons.ForEach(btn => btn.Visible = true);
                // 切换产品图片
                _productImageDisplayPanel.SetImage(_productImageFiles[_currentSideIndex].Image, _productImageFiles[_currentSideIndex].CenterLocation);
                _productImageFiles[_currentSideIndex].RefreshImage();
                ResizeSmallSideImageBox(_smallSideImagesForShowing[_currentSideIndex]);
                _pageInfo.Text = newCurrentPage + "/" + totalPages;
                _productSideTitle.Text = _productImageFiles[_currentSideIndex].SideDTO.name;
                ResetRightBottomTitleFont();
            }
        }

        private void InitializeBottom() {
            _deviceBlocks = new();
            List<DeviceCategory> deviceCategories = DeviceCategories.Elements;
            foreach (DeviceCategory category in deviceCategories) {
                DeviceBlock deviceBlock = new(category) {
                    Parent = _bottom,
                    Margin = new(0),
                    Padding = new(0),
                    ToggledButton = true,
                    BlockHoverUp = true,
                    BlockHoverDown = true,
                    ToggledColor = ColorConfigs.COLOR_DEVICE_BLOCK_TOGGLED,
                };
                deviceBlock.MouseMove += (sender, eventArg) => {
                    if (deviceBlock.FloatingForm == null || deviceBlock.FloatingForm.IsDisposed) {
                        int panelHeight = WidgetUtils.TextOrComboBoxHeight();
                        Size contentSize = new();
                        contentSize.Width = (int) (WidgetUtils.MainPanel.Width * .2);
                        if (deviceBlock.Category == DeviceCategories.TOOL) {
                            if (_toolTasks.Count > 0) {
                                deviceBlock.BlockHoverUp = false;
                                deviceBlock.BlockHoverDown = false;
                                deviceBlock.FloatingForm = new ToolDetailFloatingForm(deviceBlock.CategoryName, _toolTasks, panelHeight);
                                contentSize.Height = panelHeight * _toolTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.ARM) {
                            if (_armTasks.Count > 0) {
                                deviceBlock.BlockHoverUp = false;
                                deviceBlock.BlockHoverDown = false;
                                deviceBlock.FloatingForm = new ArmDetailFloatingForm(deviceBlock.CategoryName, _armTasks, panelHeight);
                                contentSize.Height = panelHeight * _armTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.SERIAL_PORT) {
                            if (_serialPortTasks.Count > 0) {
                                contentSize.Width = (int) (WidgetUtils.MainPanel.Width * .325);
                                deviceBlock.FloatingForm = new SerialPortDetailFloatingForm(deviceBlock.CategoryName, _serialPortTasks, panelHeight);
                                contentSize.Height = panelHeight * _serialPortTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.COMMUNICATION) {
                            // TODO
                        } else {
                            // TODO
                        }
                        if (deviceBlock.FloatingForm != null && !deviceBlock.FloatingForm.IsDisposed) {
                            deviceBlock.FloatingForm.PretendToShowToCreateHandlesForChildren();
                            deviceBlock.FloatingForm.SetContentSizeAndSelfSize(contentSize);
                            Point point = deviceBlock.PointToScreen(Point.Empty);
                            deviceBlock.FloatingForm.Location = new(point.X - deviceBlock.FloatingForm.Width + deviceBlock.Width, point.Y - deviceBlock.FloatingForm.Height);
                            deviceBlock.FloatingForm.Show();
                        }
                    }
                };
                deviceBlock.MouseLeave += (sender, eventArge) => {
                    if (deviceBlock.FloatingForm != null && !deviceBlock.FloatingForm.IsDisposed) {
                        deviceBlock.FloatingForm.Dispose();
                    }
                };
                deviceBlock.Click += (sender, eventArgs) => {
                    try {
                        if (deviceBlock.PopUpForm == null || deviceBlock.PopUpForm.IsDisposed) {
                            int panelHeight = WidgetUtils.TextOrComboBoxHeight();
                            Size contentSize = new();
                            contentSize.Width = (int) (WidgetUtils.MainPanel.Width * .65);
                            if (deviceBlock.Category == DeviceCategories.TOOL) {
                                if (_toolTasks.Count > 0) {
                                    int? currentWorkstationId = null;
                                    int? currentPset = null;
                                    if (_currentWorkingBolt != null) {
                                        currentWorkstationId = _currentWorkingBolt.BoltDTO.workstation_id;
                                        currentPset = _currentWorkingBolt.BoltDTO.parameters_set;
                                    }
                                    deviceBlock.PopUpForm = new ToolOperationPopUpForm(deviceBlock.CategoryName, _workstationsDTOs, _toolTasks, currentWorkstationId, currentPset);
                                    contentSize.Height = panelHeight * _toolTasks.Count + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;

                                    ToolOperationPopUpForm popUpForm = (ToolOperationPopUpForm) deviceBlock.PopUpForm;
                                    Control mainForm = WidgetUtils.MainPanel.Parent;
                                    TableLayoutPanel tablePanel = popUpForm.TablePanel;
                                    Panel contentPanel = deviceBlock.PopUpForm.ContentPanel;
                                    int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                                    int boxMargin = boxHeight / 5;
                                    int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                                    contentSize.Height = tableHeight + contentPanel.Padding.Size.Height;
                                    int tableWidth = contentSize.Width - contentPanel.Padding.Size.Width;
                                    popUpForm.BoxHeight = boxHeight;
                                    popUpForm.BoxMargin = boxMargin;
                                    popUpForm.TablePanel.Size = new(tableWidth, tableHeight);
                                }
                            } else if (deviceBlock.Category == DeviceCategories.ARM) {
                                if (_armTasks.Count > 0) {
                                    List<ArmTask> armTasks = _armTasks.Values.ToList();
                                    armTasks.ForEach(t => t.RetrieveResult = true);
                                    deviceBlock.PopUpForm = new ArmDetailPopUpForm(deviceBlock.CategoryName, _workstationsDTOs, _armTasks, panelHeight);
                                    deviceBlock.PopUpForm.HandleDestroyed += (sender, eventArgs) => armTasks.ForEach(t => t.RetrieveResult = false);
                                    contentSize.Width = (int) (WidgetUtils.MainPanel.Width * .45);
                                    contentSize.Height = panelHeight * _armTasks.Count + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;
                                }
                            } else if (deviceBlock.Category == DeviceCategories.SERIAL_PORT) {
                                // TODO
                            } else if (deviceBlock.Category == DeviceCategories.COMMUNICATION) {
                                // TODO
                            } else {
                                // TODO
                            }
                            if (deviceBlock.PopUpForm != null && !deviceBlock.PopUpForm.IsDisposed) {
                                deviceBlock.PopUpForm.PretendToShowToCreateHandlesForChildren();
                                deviceBlock.PopUpForm.SetContentSizeAndSelfSize(contentSize);
                                deviceBlock.PopUpForm.Show();
                            }
                        }
                    } finally {
                        deviceBlock.SetToggle(false);
                    }
                };

                _deviceBlocks.Add(deviceBlock);
            }
        }

        private async void LoadDevicesAsync() {
            await Task.Run(async () => {
                // 查询所有站点信息
                _workstationsDTOs = _apis.QueryWorkstationList(new()).WorkstationsDTOs;
                // 查询所有设备信息
                _arms = _apis.QueryDeviceArmList(new()).DeviceArmDTOs;
                _tools = _apis.QueryDeviceToolList(new()).DeviceToolDTOs;
                _serialPorts = _apis.QueryDeviceSerialPortList(new()).DeviceSerialPortDTOs;
                // 根据不同的设备类型针对性进行配置
                foreach (DeviceBlock block in _deviceBlocks) {
                    DeviceCategory category = block.Category;
                    if (category == DeviceCategories.TOOL) {
                        _toolTasks = MainUtils.ToolTasks;
                        foreach (KeyValuePair<int, ToolTask> pair in _toolTasks) {
                            ToolTask toolTask = pair.Value;
                            if (_tighteningData == null) {
                                toolTask.ActionAfterAnalysis = DoAfterRecevingTighteningDataAsync;
                            }
                            // 进入工作台先把所有工具都锁住
                            await toolTask.SendLockAsync();
                        }
                    } else if (category == DeviceCategories.ARM) {
                        _armTasks = MainUtils.ArmTasks;
                    } else if (category == DeviceCategories.SERIAL_PORT) {
                        _serialPortTasks = MainUtils.SerialPortTasks;
                        foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                            SerialPortTask serialPortTask = pair.Value;
                            if (_barCodeMessage == null) {
                                serialPortTask.ActionAfterDataReceived = async msg => {
                                    await Task.Run(() => {
                                        BeginInvoke(async () => {
                                            if (!IsDisposed && !_activated || _finished) {
                                                DeviceSerialPortDTO dto = _serialPorts.Single(dto => dto.id == pair.Key);
                                                // 无效字符校验
                                                if (dto.invalid_char != null) {
                                                    msg = String.Concat(msg.Where(c => !dto.invalid_char.Contains(c)));
                                                }
                                                // 数字校验
                                                msg = String.Concat(msg.Where(c => char.IsDigit(c)));
                                                _barCodeMessage = msg;
                                                _barCodeTextBox.Text = _barCodeMessage;
                                                // 如果弹窗已经打开了，则先将条码数据填充至弹窗，然后延时一会再关闭，可以得到比较好看的效果
                                                if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                                                    _barCodePopUpForm.TextBox.Text = _barCodeMessage;
                                                    await Task.Delay(200);
                                                    _barCodePopUpForm.Dispose();
                                                }
                                                // 激活任务
                                                ActivateMission();
                                            }
                                        });
                                    });
                                };
                            }
                        }
                    } else if (category == DeviceCategories.COMMUNICATION) {
                        // TODO
                    } else {
                        // TODO
                    }
                }
            });
            // Keep listenging devices
            CheckDeviceConnections();
        }

        private async void CheckDeviceConnections() {
            await Task.Run(async () => {
                while (!IsDisposed) {
                    if (Visible) {
                        foreach (DeviceBlock block in _deviceBlocks) {
                            DeviceCategory category = block.Category;
                            if (category == DeviceCategories.TOOL) {
                                Check(block, _toolTasks.Values.ToList());
                            } else if (category == DeviceCategories.ARM) {
                                Check(block, _armTasks.Values.ToList());
                            } else if (category == DeviceCategories.SERIAL_PORT) {
                                Check(block, _serialPortTasks.Values.ToList());
                            } else if (category == DeviceCategories.COMMUNICATION) {
                                // TODO
                            } else {
                                // TODO
                            }
                        }
                    }
                    await Task.Delay(_checkDevicesConnectionDelay);
                }
            });
            void Check<T>(DeviceBlock block, List<T> tasks) where T : ATaskBase {
                if (tasks.Count == 0) {
                    block.ResetIconByStatus(DeviceStatus.EMPTY);
                    return;
                } else {
                    foreach (T task in tasks) {
                        if (!task.Connected) {
                            block.ResetIconByStatus(DeviceStatus.ERROR);
                            return;
                        }
                    }
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                }
            }
        }

        // 激活任务
        private void ActivateMission() {
            if (_sides.Count > 0 && _allBolts.Count > 0) {
                // 1. 修改任务激活状态
                _activated = true;
                _finished = false;
                // 2. 将当前任务的所有螺栓点位按顺序排好队，并初始化所有螺栓点位的状态
                _allBolts = _allBolts.OrderBy(btn => btn.BoltDTO.side_id).ThenBy(btn => btn.BoltDTO.serial_num).ToList();
                _allBolts.ForEach(btn => btn.ResetStatusWithoutChangingVisible());
                // 3. 设置当前点位为第一个点位
                _currentWorkingBolt = SwitchBolt(0);
                // 4. 根据当前螺丝点位配置的站点找到对应的力臂并开始读取数据，同时开始监听点位状态
                ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                if (boltDTO.workstation_id != null && boltDTO.parameters_set != null) {
                    int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id.Value).tool_id;
                    if (toolId != null) {
                        ToolTask toolTask = _toolTasks[toolId.Value];
                        // 5. 先将工具锁住防止误操作
                        toolTask.SendLock();
                        // 6. 下发当前螺栓点位的程序号至控制器
                        toolTask.SendPSet(boltDTO.parameters_set.Value);
                    }
                    int? armId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id.Value).arm_id;
                    // 7. 开始读取力臂数据
                    if (armId != null) {
                        ArmTask armTask = _armTasks[armId.Value];
                        armTask.RetrieveResult = true;
                        armTask.OnActionAfterReceiving += ActionAfterArmDataReceived;
                    }
                }
            }
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
            // Store all data left
            StoreTighteningData();
            foreach (KeyValuePair<int, ArmTask> pair in _armTasks) {
                // Clear all delegates once this workplace handle has been destroyed to ensure running performance
                pair.Value.ActionAfterReceiving = new(c => {});
            }
            _serialPortTasks = MainUtils.SerialPortTasks;
            foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                // Clear all delegates once this workplace handle has been destroyed to make sure it won't throw any exception
                pair.Value.ActionAfterDataReceived = new(c => {});
            }
        }

        private async void StoreTighteningData() {
            await Task.Run(() => {
                if (_activated && !_finished && _dataNeedToBeStored.Count > 0) {
                    bool succeed = _dataNeedToBeStored.ExportToExcelFile($"{MainUtils.GetStorageFileName()}.xlsx");
                    // 由于 excel 文件如果被人为开启会导致数据存储出错，因此先判断是否成功再进行后续操作
                    if (succeed) {
                        _apis.BatchAddOperationData(new(_dataNeedToBeStored));
                        _dataNeedToBeStored.ExportToTextFile($"{MainUtils.GetStorageFileName()}.txt");
                        _dataNeedToBeStored.Clear();
                    } else {
                        WidgetUtils.ShowWarningPopUp("Excel文件被占用，无法执行数据存储操作，本次数据已保留，请在下次任务完成以前或关闭工作台前释放被占用的数据文件，以免造成数据丢失！");
                    }
                }
            });
        }

        // 根据index切换点位
        private BoltButton SwitchBolt(int newIndex) {
            // 通过index切换点位
            BoltButton newBolt = _allBolts[newIndex];
            ProductBoltDTO newBoltDTO = newBolt.BoltDTO;
            // 先切换点位，然后检查点位是否还是当前side，不是则跳转
            int? sideId = newBoltDTO.side_id;
            if (_sides[_currentSideIndex].id < sideId) {
                _forward.PerformClick();
            } else if (_sides[_currentSideIndex].id > sideId) {
                _backward.PerformClick();
            }
            // 切换状态的代码放在这里，以保证即使切换了side，也能正确显示动态效果
            newBolt.BoltStatus = BoltStatus.WORKING;
            // 下发程序号（pset）
            if (newBoltDTO.parameters_set != null && newBoltDTO.workstation_id != null) {
                int? toolId = _workstationsDTOs.Single(dto => dto.id == newBoltDTO.workstation_id.Value).tool_id;
                if (toolId != null) {
                    _toolTasks[toolId.Value].SendPSet(newBoltDTO.parameters_set.Value);
                }
            }
            // 将当前螺栓点位的serial_num传给process poanel
            _workingProcessPanel.BoltSerialNum = newBoltDTO.serial_num;
            return newBolt;
        }

        // 读取力臂数据并根据当前螺栓点位配置信息进行解锁、锁枪
        private void ActionAfterArmDataReceived(Coordinates3D armCoordinates) {
            Task.Run(() => {
                BeginInvoke(() => {
                    if (_currentWorkingBolt != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        if (boltDTO.workstation_id != null) {
                            int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id.Value).tool_id;
                            if (toolId != null) {
                                ToolTask toolTask = _toolTasks[toolId.Value];
                                Coordinates3D boltCoordinates = Coordinates3D.FromString(boltDTO.position);
                                int x = armCoordinates.X;
                                int y = armCoordinates.Y;
                                int z = armCoordinates.Z;
                                int limit = 200;
                                if (Math.Abs(x - boltCoordinates.X) < limit && Math.Abs(y - boltCoordinates.Y) < limit
                                        && (boltCoordinates.Z == 0 || Math.Abs(z - boltCoordinates.Z) < limit)) {
                                    toolTask.SendUnlock();
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                } else {
                                    toolTask.SendLock();
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                }
                            }
                        }
                    }
                });
            });
        }

        // 读取到控制器传回的数据后进行处理
        private async void DoAfterRecevingTighteningDataAsync(TighteningData data) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    if (_currentWorkingBolt != null) {
                        _tighteningData = data;
                        _torque.Text = _tighteningData.torque + "";
                        _angle.Text = _tighteningData.angle + "";

                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        OperationDataDTO dataDTO = new();
                        CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);
                        WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id);
                        dataDTO.workstation_id = workstationDTO.id;
                        dataDTO.workstation_name = workstationDTO.name;
                        int? toolId = workstationDTO.tool_id;
                        DeviceToolDTO toolDTO = _tools.Single(t => t.id == toolId);
                        dataDTO.tool_name = toolDTO.name;
                        dataDTO.tool_ip = toolDTO.ip;
                        dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type.Value).Name;
                        dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                        dataDTO.bolt_serial_num = boltDTO.serial_num;
                        _dataNeedToBeStored.Add(dataDTO);

                        bool tighteningCompleted = true;
                        // 检查返回的拧紧状态是否为成功
                        if (_tighteningData.tightening_status != null && _tighteningData.tightening_status != (int) TighteningStatus.OK) {
                            tighteningCompleted = false;
                        }
                        // 检查控制器返回数据与螺栓点位配置的数据是否一致
                        // 程序号（pset）校验
                        if (boltDTO.parameters_set != _tighteningData.parameter_set_number) {
                            tighteningCompleted = false;
                        }
                        // 扭矩校验
                        if (boltDTO.torque_max > 0 && (_tighteningData.torque < boltDTO.torque_min || _tighteningData.torque > boltDTO.torque_max)) {
                            tighteningCompleted = false;
                        }
                        // 角度校验
                        if (boltDTO.angle_max > 0 && (_tighteningData.angle < boltDTO.angle_min || _tighteningData.angle > boltDTO.angle_max)) {
                            tighteningCompleted = false;
                        }
                        // 切换下一个点位
                        if (tighteningCompleted) {
                            // 扭矩角度数据显示panel背景颜色改成绿色
                            _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                            _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                            // 当前点位完成后先把设备的状态都复原
                            _toolTasks[toolId.Value].SendLock();
                            // 力臂
                            int? armId = workstationDTO.arm_id;
                            if (armId != null) {
                                ArmTask armTask = _armTasks[armId.Value];
                                // 修改点位状态并切换点位
                                _currentWorkingBolt.BoltStatus = BoltStatus.DONE;
                                int currentIndex = _allBolts.IndexOf(_currentWorkingBolt);
                                if (currentIndex != _allBolts.Count - 1) {
                                    _currentWorkingBolt = SwitchBolt(currentIndex + 1);
                                } else {
                                    // 已经打完最后一个点位，任务完成
                                    _activated = false;
                                    _finished = true;
                                    _currentWorkingBolt = null;
                                    armTask.RetrieveResult = false;
                                    armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED;
                                    _workingProcessPanel.BoltSerialNum = null;
                                    // 扭矩角度数据显示panel背景颜色改回黑色
                                    _torque.ForeColor = Color.Black;
                                    _angle.ForeColor = Color.Black;
                                    StoreTighteningData();
                                }
                            }
                        } else {
                            // 扭矩角度数据显示panel背景颜色改成红色
                            _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                            _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                            _currentWorkingBolt.BoltStatus = BoltStatus.ERROR;
                        }
                    }
                });
            });
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            ResizeContents();
            ResizeleftTop();
            ResizeLeftBottom();
            ResizeRightTop();
            ResizeRightMiddle();
            ResizeRightBottom();
            ResizeBottom();
            Invalidate();
        }

        private void ResizeContents() {
            int wholeWidth = Width - Padding.Left * 2;
            int wholeHeight = Height - Padding.Top * 2;
            int bottomHeight = (int) (wholeHeight * .07);
            int othersHeight = wholeHeight - bottomHeight - Padding.Top / 2;
            int leftWidth = (int) (wholeWidth * .78);
            int rightWidth = wholeWidth - leftWidth - Padding.Left / 2;

            _left.Size = new(leftWidth, othersHeight);
            _left.Margin = new(0, 0, Padding.Left / 2, Padding.Top / 2);
            _right.Size = new(rightWidth, othersHeight);
            _bottom.Size = new(wholeWidth, bottomHeight);

            _leftTop.Size = new(_left.Width, bottomHeight);
            _leftBottom.Size = new(_left.Width, _left.Height - _leftTop.Height - Padding.Top / 2);
            _leftBottom.Margin = new(0, Padding.Top / 2, 0, 0);

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
            int newH = (int) (_leftTop.Height * .85);
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
            _productImageFiles[_currentSideIndex].RefreshImage();

            // 重新计算螺栓点位按钮的大小和位置
            int btnSide = newPanelSize.Height / 13;
            foreach (BoltButton boltButton in _allBolts) {
                boltButton.Size = new(btnSide, btnSide);
                int newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100);
                int newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100);
                boltButton.Location = new(newX, newY);
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
                for (int i = 0 ; i < _missionImages.Count ; i++) {
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
                    if (i == _currentSideIndex) {
                        ResizeSmallSideImageBox(imageNew);
                    }
                }
            }
            // Resize table panel 
            int tablePanelHeight = _rightBottom.Height - 4 - _productSideTitle.Height - imageWholeHeight;
            int buttonSide = (int) (tablePanelHeight * .725);
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
        private readonly int _loopingInterval = 50;

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
        private BoltStatus _boltStatus;

        public WorkplaceProcessStatus WorkplaceProcessStatus {
            get => _workplaceProcessStatus;
            set {
                _workplaceProcessStatus = value;
                switch (_workplaceProcessStatus) {
                    case WorkplaceProcessStatus.UNACTIVATED:
                        _statusTxt = "未激活";
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.OPERATION_ENABLE:
                        if (_tightenOrLoosen == TightenOrLoosen.TIGHTEN) {
                            _statusDesc = "正在拧紧{0}号螺丝";
                        } else {
                            _statusDesc = "正在反松{0}号螺丝";
                        }
                        _picturePanel.Visible = true;
                        break;
                    case WorkplaceProcessStatus.OPERATION_DISABLE:
                        _statusTxt = "已锁定";
                        _statusDesc = "未在指定坐标位置";
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.NG:
                        _statusTxt = "任务中断";
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.FINISHED:
                        _statusTxt = "完成";
                        _picturePanel.Visible = false;
                        break;
                    default:
                        break;
                }
            }
        }
        public TightenOrLoosen TightenOrLoosen { get => _tightenOrLoosen; set => _tightenOrLoosen = value; }
        public int? BoltSerialNum { get => _boltSerialNum; set => _boltSerialNum = value; }

        public WorkingProcessPanel() : base() {
            _clockwiseIcon = Properties.Resources.processing_clockwise;
            _anticlockwiseIcon = Properties.Resources.processing_anticlockwise;
            _statusTxt = "未激活";
            _statusDesc = "";
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
                                BackColor = ColorConfigs.COLOR_WORKING_PROCESS_THEME;
                                break;
                            case WorkplaceProcessStatus.OPERATION_ENABLE:
                                BackColor = ColorConfigs.COLOR_WORKING_PROCESS_WHITE;
                                // 旋转图标
                                if (_tightenOrLoosen == TightenOrLoosen.TIGHTEN) {
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
                                BackColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                break;
                            case WorkplaceProcessStatus.NG:
                                BackColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                break;
                            case WorkplaceProcessStatus.FINISHED:
                                BackColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                                break;
                            default:
                                break;
                        }
                        await Task.Delay(_loopingInterval);
                    }
                });
            });
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            _borderRect.Size = Size;
            _borderSize = Width / 40 + Height / 80;
            _picturePanelHeight = (int) ((Height - _borderSize * 2) * .75F);
            _picturePanel.Size = new(Width - _borderSize * 2, _picturePanelHeight);
            _picturePanel.Location = new(_borderSize, _borderSize);
            _statusFont = ResetControlFont(_statusTxt, .3f);
            _statusDescFont = ResetControlFont(_statusDesc, .1f);

            int imageSide = (int) (_picturePanel.Height * .85);
            if (_picturePanel.Height > _picturePanel.Width) {
                imageSide = (int) (_picturePanel.Width * .85);
            }
            if (_tightenOrLoosen == TightenOrLoosen.TIGHTEN) {
                _iconShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_clockwiseIcon, new Size(imageSide, imageSide));
            } else {
                _iconShowing = WidgetUtils.ResizeImageWithoutLosingQuality(_anticlockwiseIcon, new Size(imageSide, imageSide));
            }
        }

        private Font ResetControlFont(string text, float fontRatio) {
            using (Graphics graphics = CreateGraphics()) {
                return ResetControlFont(graphics, text, fontRatio -= .005f);
            }
        }

        private Font ResetControlFont(Graphics graphics, string text, float fontRatio) {
            Font font = new Font(WidgetsConfigs.SystemFontFamily, Height * fontRatio, FontStyle.Bold, GraphicsUnit.Pixel);
            if (graphics.MeasureString(text, font).Width >= Width * .9) {
                font = ResetControlFont(graphics, text, fontRatio -= .005f);
            }
            return font;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.Clear(BackColor);
            int statusWidth = (int) (graphics.MeasureString(_statusTxt, _statusFont).Width);
            int statusDescWidth = (int) (graphics.MeasureString(_statusDesc, _statusDescFont).Width);
            Point statusPoint;
            Point statusDescPoint;
            switch (_workplaceProcessStatus) {
                case WorkplaceProcessStatus.UNACTIVATED:
                    // graphics.FillRectangle(new SolidBrush(_warningColor), _borderRect);
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 2);
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    break;
                case WorkplaceProcessStatus.OPERATION_ENABLE:
                    graphics.DrawRectangle(new(_correctColor, _borderSize), _borderRect);
                    string descShowing = _statusDesc;
                    // 设置当前点位信息
                    if (_boltSerialNum != null) {
                        descShowing = string.Format(descShowing, _boltSerialNum);
                    }
                    statusDescWidth = (int) (graphics.MeasureString(descShowing, _statusDescFont).Width);
                    statusDescPoint = new Point((Width - statusDescWidth) / 2, _borderSize + _picturePanelHeight + _statusDescFont.Height / 2);
                    graphics.DrawString(descShowing, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_GREEN), statusDescPoint);
                    break;
                case WorkplaceProcessStatus.OPERATION_DISABLE:
                    // graphics.FillRectangle(new SolidBrush(_correctColor), _borderRect);
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 3);
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    statusDescPoint = new Point((Width - statusDescWidth) / 2, _borderSize + _picturePanelHeight + _statusDescFont.Height / 2);
                    graphics.DrawString(_statusDesc, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusDescPoint);
                    break;
                case WorkplaceProcessStatus.FINISHED:
                    // graphics.FillRectangle(new SolidBrush(_correctColor), _borderRect);
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 2);
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    break;
            }
        }
    }

    public class DeviceBlock: CustomImageTextButtonBase {
        private DeviceCategory _category;

        private readonly float _imageRatio = 0.75F;
        private Rectangle _borderRect;
        private Color _borderColor;
        private string _categoryName;
        private CustomFloatingForm? _floatingForm;
        private CustomPopUpForm? _popUpForm;

        public DeviceCategory Category { get => _category; set => _category = value; }
        public string CategoryName { get => _categoryName; set => _categoryName = value; }
        public CustomFloatingForm? FloatingForm { get => _floatingForm; set => _floatingForm = value; }
        public CustomPopUpForm? PopUpForm { get => _popUpForm; set => _popUpForm = value; }

        public DeviceBlock(DeviceCategory category) : base() {
            _category = category;
            _categoryName = category.Name;
            _borderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER;
            Icon = category.IconEmpty;
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _borderRect = new(0, 0, Width, Height);
        }

        public void ResetIconByStatus(DeviceStatus status) {
            switch (status) {
                case DeviceStatus.NORMAL:
                    Icon = _category.Icon;
                    break;
                case DeviceStatus.ERROR:
                    Icon = _category.IconError;
                    break;
                case DeviceStatus.EMPTY:
                    Icon = _category.IconEmpty;
                    break;
            }
            ResizeIconImage();
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

    public class ArmDetailFloatingForm: CustomFloatingForm {
        private readonly Image _statusIconConnected = Properties.Resources.device_connected;
        private readonly Image _statusIconDisconnected = Properties.Resources.device_disconnected;

        private int _panelHeight;

        public ArmDetailFloatingForm(string categoryName, Dictionary<int, ArmTask> armTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            ContentPanel.FlowDirection = FlowDirection.TopDown;
            _panelHeight = panelHeight;

            DisplayArmDetails(armTasks);
        }

        private void DisplayArmDetails(Dictionary<int, ArmTask> armTasks) {
            Font font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (KeyValuePair<int, ArmTask> armTask in armTasks) {
                CustomContentPanel panel = new() {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                };
                panel.Paint += (sender, eventArgs) => {
                    Graphics g = eventArgs.Graphics;
                    Image icon;
                    ArmTask task = armTask.Value;
                    int imageSide = (int) (_panelHeight * .8);
                    if (task.Connected) {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconDisconnected, imageSide, imageSide);
                    }
                    g.DrawImage(icon, new Point(0, (_panelHeight - imageSide) / 2));
                    g.DrawString($"{task.Ip} : {task.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), 0));
                };
            }
        }
    }

    public class ArmDetailPopUpForm: CustomPopUpForm {
        private List<WorkstationDTO> _workstationDTOs;
        private Dictionary<int, ArmTask> _armTasks;
        private int _panelHeight;

        private List<CoordinatesPanel> armPanels = new();

        public ArmDetailPopUpForm(string categoryName, List<WorkstationDTO> workstationDTOs, Dictionary<int, ArmTask> armTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            _workstationDTOs = workstationDTOs;
            _armTasks = armTasks;
            _panelHeight = panelHeight;

            InitializeDisplay();
        }

        private void InitializeDisplay() {
            foreach (WorkstationDTO dto in _workstationDTOs) {
                if (dto.arm_id != null) {
                    CoordinatesPanel panel = new(CommonUtils.CannotBeNull(dto.name), this._panelHeight) {
                        Parent = ContentPanel,
                    };
                    ContentPanel.SizeChanged += (sender, eventArgs) => {
                        panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, this._panelHeight);
                    };
                    armPanels.Add(panel);
                    // Bind delegate 
                    _armTasks[dto.arm_id.Value].OnActionAfterReceiving += panel.SetCoordinates;
                    // Remove delegate
                    panel.HandleDestroyed += (sender, eventArgs) => {
                        _armTasks[dto.arm_id.Value].OnActionAfterReceiving -= panel.SetCoordinates;
                    };
                }
            }
        }

        private class CoordinatesPanel: CustomContentPanel {
            private string _workstationName;
            private int _panelHeight;

            public string XStr { get; set; }
            public string YStr { get; set; }
            public string ZStr { get; set; }

            public CoordinatesPanel(string workstationName, int panelHeight) {
                _workstationName = workstationName;
                _panelHeight = panelHeight;

                XStr = "0";
                YStr = "0";
                ZStr = "0";
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                base.ResizeChildren(sender, eventArgs);
                Font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                Graphics g = e.Graphics;

                string content = $"站点：{_workstationName}        坐标：  X-{XStr}    Y-{YStr}";
                if (ZStr != "0") {
                    content += $"    Z-{ZStr}";
                }
                g.DrawString(content, Font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point(0, 0));
            }

            public void SetCoordinates(Coordinates3D coordinates) {
                Task.Run(() => {
                    BeginInvoke(() => {
                        XStr = coordinates.X + "";
                        YStr = coordinates.Y + "";
                        ZStr = coordinates.Z + "";
                        Invalidate();
                    });
                });
            }
        }
    }

    public class ToolDetailFloatingForm: CustomFloatingForm {
        private readonly Image _statusIconConnected = Properties.Resources.device_connected;
        private readonly Image _statusIconDisconnected = Properties.Resources.device_disconnected;

        private int _panelHeight;

        public ToolDetailFloatingForm(string categoryName, Dictionary<int, ToolTask> toolTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            ContentPanel.FlowDirection = FlowDirection.TopDown;
            _panelHeight = panelHeight;

            DisplayToolDetails(toolTasks);
        }

        private void DisplayToolDetails(Dictionary<int, ToolTask> toolTasks) {
            Font font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (KeyValuePair<int, ToolTask> toolTask in toolTasks) {
                CustomContentPanel panel = new() {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                };
                panel.Paint += (sender, eventArgs) => {
                    Graphics g = eventArgs.Graphics;
                    Image icon;
                    ToolTask task = toolTask.Value;
                    int imageSide = (int) (_panelHeight * .8);
                    if (task.Connected) {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconDisconnected, imageSide, imageSide);
                    }
                    g.DrawImage(icon, new Point(0, (_panelHeight - imageSide) / 2));
                    g.DrawString($"{task.Ip} : {task.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), 0));
                };
            }
        }
    }

    public class ToolOperationPopUpForm: CustomPopUpForm {
        private List<WorkstationDTO> _workstationDTOs;
        private Dictionary<int, ToolTask> _toolTasks;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private CustomComboBoxGroup<int> _stationComboBox;
        private CustomTextBoxGroup _parameterSetTextBox;

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }

        public ToolOperationPopUpForm(string categoryName, List<WorkstationDTO> workstationDTOs
                , Dictionary<int, ToolTask> toolTasks, int? currentWorkstationId = null, int? currentPset = null) {
            _workstationDTOs = workstationDTOs;
            _toolTasks = toolTasks;
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "手动操作 - " + categoryName + "";

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                Parent = ContentPanel,
            };

            Dictionary<string, int> workstationOptions = _workstationDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _stationComboBox = new("站点") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            foreach (KeyValuePair<string, int> pair in workstationOptions) {
                _stationComboBox.AddItem(pair.Key, pair.Value);
            }
            if (currentWorkstationId != null) {
                _stationComboBox.SetCurrent(_stationComboBox.IndexOf(currentWorkstationId.Value));
            }
            _parameterSetTextBox = new("程序") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberOnly = true,
            };
            if (currentPset != null) {
                _parameterSetTextBox.SetValue(0, currentPset + "");
            } else {
                _parameterSetTextBox.SetValue(0, "1");
            }

            CommonButton btnLock = AddButton("锁枪");
            btnLock.Click += (s, e) => {
                SendCommand(async toolTask => {
                    if (await toolTask.SendLockAsync()) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                    } else {
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 未给当前工具型号配置命令");
                    }
                });
            };
            bool canUnlock = true;
            CommonButton btnUnlock = AddButton("解锁");
            btnUnlock.Click += (s, e) => {
                SendCommand(async toolTask => {
                    if (await toolTask.SendUnlockAsync() && canUnlock) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                    } else {
                        string parameterSet = _parameterSetTextBox.GetTextBox(0).Text;
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 未给当前工具型号配置命令\r\n2. 控制器未配置[程序{parameterSet}], 螺丝枪锁定");
                    }
                });
            };
            CommonButton btnPSet = AddButton("下发");
            btnPSet.Click += (s, e) => {
                SendCommand(async toolTask => {
                    string parameterSet = _parameterSetTextBox.GetTextBox(0).Text;
                    if (await toolTask.SendPSetAsync(int.Parse(parameterSet))) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                        canUnlock = true;
                    } else {
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 未给当前工具型号配置命令\r\n2. 控制器未配置[程序{parameterSet}], 螺丝枪锁定");
                        canUnlock = false;
                    }
                });
            };
            CommonButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => {
                Dispose();
            };
        }

        private void SendCommand(Action<ToolTask> aciont) {
            if (_stationComboBox.IsDefaultValue()) {
                WidgetUtils.ShowErrorPopUp("操作失败！请选择需要操作的工具所在的站点！");
            } else {
                int workstationId = _stationComboBox.Value;
                WorkstationDTO workstationDTO = _workstationDTOs.Single(dto => dto.id == workstationId);
                if (workstationDTO.tool_id == null) {
                    WidgetUtils.ShowErrorPopUp("操作失败！当前选择的站点没有配置工具，请检查配置。");
                } else {
                    if (!_toolTasks.ContainsKey(workstationDTO.tool_id.Value)) {
                        WidgetUtils.ShowErrorPopUp($"操作失败！未找到[{workstationDTO.tool_name} - {workstationDTO.tool_ip} : {workstationDTO.tool_port}]对应的工具，请检查配置。");
                    } else {
                        ToolTask toolTask = _toolTasks[workstationDTO.tool_id.Value];
                        aciont(toolTask);
                    }
                }
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, ContentPanel.Height - ContentPanel.Padding.Size.Height);

            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            IList list = _tablePanel.Controls;
            for (int i = 0 ; i < list.Count ; i++) {
                Control? control = (Control?) list[i];
                if (control != null) {
                    control.Margin = new(_boxMargin);
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }
    }

    public class SerialPortDetailFloatingForm: CustomFloatingForm {
        private readonly Image _statusIconConnected = Properties.Resources.device_connected;
        private readonly Image _statusIconDisconnected = Properties.Resources.device_disconnected;

        private int _panelHeight;

        public SerialPortDetailFloatingForm(string categoryName, Dictionary<int, SerialPortTask> serialPortTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            ContentPanel.FlowDirection = FlowDirection.TopDown;
            _panelHeight = panelHeight;

            DisplaySerialPortDetails(serialPortTasks);
        }

        private void DisplaySerialPortDetails(Dictionary<int, SerialPortTask> serialPortTasks) {
            Font font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (KeyValuePair<int, SerialPortTask> serialPortTask in serialPortTasks) {
                CustomContentPanel panel = new() {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                };
                panel.Paint += (sender, eventArgs) => {
                    Graphics g = eventArgs.Graphics;
                    Image icon;
                    SerialPortTask task = serialPortTask.Value;
                    int imageSide = (int) (_panelHeight * .8);
                    if (task.Connected) {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImageWithoutLosingQuality(_statusIconDisconnected, imageSide, imageSide);
                    }
                    g.DrawImage(icon, new Point(0, (_panelHeight - imageSide) / 2));
                    g.DrawString($"{task.PortName} - {task.FullName}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), 0));
                };
            }
        }
    }
}
