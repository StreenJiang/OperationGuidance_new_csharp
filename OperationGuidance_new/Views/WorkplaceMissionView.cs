using System.Collections;
using System.Drawing.Drawing2D;
using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
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
using CustomLibrary.TextBoxes;
using System.Reflection;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_service.Constants;
using Timer = System.Windows.Forms.Timer;
using log4net;
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView: CustomContentPanel {
        private MissionListPanel? _missionListPanel;
        private List<ProductMissionDTO>? _productMissionDTOs;
        private OperationGuidanceApis? apis;
        private bool _operatorOpenning = false;

        private CustomTabPanel? _pagePanel;
        private TopBar? _topBar;
        private WorkplaceContentPanel? _workplacePanel;

        public WorkplaceMissionView() : base() {
            OpenMissionListView();
        }
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
        private void OpenWorkplaceViewDirectly() {
            OpenWorkplaceView(null);
        }

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
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
                Margin = new Padding(0),
                PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
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
        private WorkplaceContentPanel? _workplace;
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
        public WorkplaceContentPanel? Workplace { get => _workplace; set => _workplace = value; }

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
                            if (!_operatorOpenning) {
                                _workplace.Activated = false;
                                if (WidgetUtils.MainPanel != null) {
                                    WidgetUtils.MainPanel.Visible = true;
                                }
                                Parent.Visible = false;
                                _workplace.Dispose();
                            } else {
                                if (WidgetUtils.BackToLoginView != null) {
                                    WidgetUtils.BackToLoginView(true);
                                }
                            }
                        }
                    } else {
                        if (!_operatorOpenning) {
                            if (WidgetUtils.MainPanel != null) {
                                WidgetUtils.MainPanel.Visible = true;
                            }
                            Parent.Visible = false;
                            if (_workplace != null && !_workplace.IsDisposed) {
                                _workplace.Dispose();
                            }
                        } else {
                            if (WidgetUtils.BackToLoginView != null) {
                                WidgetUtils.BackToLoginView(true);
                            }
                        }
                    }
            };
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_title != null) {
                _titleX = (int) ((Width - TextRenderer.MeasureText(_title, Font).Width) / 2);
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
            // 先设定高度，则font就会重设
            _backButton.Height = newHeight;
            int newWidth = (int) (TextRenderer.MeasureText(_backButton.Label, _backButton.Font).Width * 1.5);
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
        private ILog logger = MainUtils.GetLogger(typeof(WorkplaceContentPanel));

        private OperationGuidanceApis _apis;
        private ProductMissionDTO _mission;
        private Action<string> _resetMissionName;
        private bool _activated;
        private bool _finished;
        private Image _defaultImage;
        private readonly int _checkDevicesConnectionDelay = 2500;
        private readonly int _resendPsetMaxTimes = 5;

        private List<WorkstationDTO> _workstationsDTOs = new();
        private List<DeviceArmDTO> _arms;
        private List<DeviceToolDTO> _tools;
        private List<DeviceSerialPortDTO> _serialPorts;
        private Dictionary<int, ArmTask> _armTasks = new();
        private Dictionary<int, ToolTask> _toolTasks = new();
        private Dictionary<int, SerialPortTask> _serialPortTasks = new();
        private Dictionary<int, CommunicationTask> _communicationTask = new();
        private MissionRecordDTO? _missionRecord;
        private TighteningData? _tighteningData;
        private bool _needLoosening = false;
        private bool? _adminConfirmed = null;
        private CustomPopUpForm? _adminPasswordPopUpForm;
        private int _isRedo = (int) YesOrNo.NO;
        private Coordinates3D? _realTimeArmCoordinates;
        private readonly object DataStorageLockObj = new();

        // 上方
        private CustomContentPanel _top;
        // 上方左边
        private CustomContentPanel _topLeft;
        // 上方左边上面
        private WorkplacePiece _barCodeOuter;
        private Image _barCodeImage;
        private PictureBox _barCodePictureBox;
        private CustomTextBox _barCodeTextBox;
        private List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
        private BarCodeInputPopUpForm? _barCodePopUpForm;
        private Dictionary<int, List<BarCodeMatchingRuleDTO>> _productBarCodeMatchingRules;
        private Dictionary<int, List<BarCodeMatchingRuleDTO>> _partsBarCodeMatchingRules;
        private readonly BarCodeObj _barCodeObj = new();
        // 上方左边下面
        private WorkplacePiece _imageDisplayOuter;
        private int _currentSideIndex;
        private List<ProductSideDTO> _sides;
        private ProductImageDisplayPanel _productImageDisplayPanel;
        private List<ProductImageFile> _productImageFiles;
        private List<Image?> _missionImages;
        private List<BoltButton> _allBolts;
        private List<BoltButton> _showingBoltButtons;
        private BoltButton? _currentWorkingBolt;
        private BoltPopUpForm _boltPopUpForm; // 如果以后要支持软件尺寸可拖拽改变，则需要在打开时动态改变
        private bool _locating_enabled = MainUtils.IsArmLocatingEnabled();
        private int _armLocatingAccuracy;
        // 上方右边
        private CustomContentPanel _topRight;
        // 上方右边的上面
        private WorkplacePiece _topRightTop;
        private Label _operatorInfoTitle;
        private CustomTextBoxGroup _operatorName;
        private CustomTextBoxGroup _operatorId;
        // 上方右边的中间
        private CustomContentPanel _topRightMiddle;
        // 上方右边的中间的左边
        private WorkplacePiece _topRightMiddleLeft;
        private WorkingProcessPanel _workingProcessPanel;
        // 上方右边的中间的右边
        private WorkplacePiece _topRightMiddleRight;
        private Label _torqueTitle;
        private Label _torque;
        private Label _angleTitle;
        private Label _angle;
        // 上方右边的下面
        private WorkplacePiece _topRightBottom;
        private Label _missionDetailTitle;
        private CustomTextBoxGroup _productBatch;
        private CustomTextBoxButtonGroup _missionSelectedName;
        private CustomTextBoxGroup _productSumPerDay;
        private CustomTextBoxGroup _okSumPerDay;
        private CustomTextBoxGroup _ngRatePerDay;
        private CustomTextBoxGroup _pset;

        // 中间
        private WorkplacePiece _middle;
        private DataGridViewPanel<OperationDataVO> _tighteningDataPanel;
        private List<OperationDataVO> _tighteningDataVOs = new();

        // 下方
        private WorkplacePiece _bottom;
        private List<DeviceBlock> _deviceBlocks;
        private CustomContentPanel _timeDisplayerOuter;
        private Label _timeDisplayer;
        private Timer _timeDisplayerTimer;


        // private Label _productSideTitle;
        // private List<Image?> _smallSideImagesForShowing;
        // private PictureBox _smallSideImage;
        // private TableLayoutPanel _buttonPanel;
        // private PageSwitchButton _first;
        // private PageSwitchButton _backward;
        // private PageSwitchButton _forward;
        // private PageSwitchButton _last;
        // private Label _pageInfo;


        public OperationGuidanceApis Apis { get => _apis; set => _apis = value; }
        public bool Activated { get => _activated; set => _activated = value; }
        public bool Finished { get => _finished; set => _finished = value; }
        public bool? AdminConfirmed { get => _adminConfirmed; set => _adminConfirmed = value; }
        public int IsRedo { get => _isRedo; set => _isRedo = value; }
        public BarCodeObj BarCodeObj => _barCodeObj;
        public CustomTextBox BarCodeTextBox { get => _barCodeTextBox; set => _barCodeTextBox = value; }

        public WorkplaceContentPanel(int? missionId, Action<string> resetMissionName) : base() {
            logger.Info($"Open workplace with mission_id = {missionId}");

            _apis = SystemUtils.GetApis();
            if (missionId == null) {
                _mission = new ProductMissionDTO() {
                    name = "未选择任务",
                    ProductSides = new() {
                        new() {
                            name = "-",
                        },
                    }
                };
            } else {
                ProductMissionDTO? mission = _apis.QueryProductMissionDetail(new(missionId.Value)).ProductMissionDTO;
                if (mission == null) {
                    logger.Debug($"Can not find mission from database by mission_id = {missionId}");
                    _mission = new ProductMissionDTO() {
                        name = "未选择任务",
                        ProductSides = new() {
                            new() {
                                name = "-",
                            },
                        }
                    };
                } else {
                    _mission = mission;
                }
            }
            _resetMissionName = resetMissionName;
            _resetMissionName(_mission.name);
            _activated = false;
            _finished = false;
            _defaultImage = Properties.Resources.image_choose;
            _currentSideIndex = 0;
            _sides = new();

            // 初始化所有组件
            InitializeOuters();
            InitializeTopLeftTop();
            InitializeTopLeftBottom();
            InitializeTopRightTop();
            InitializeTopRightMiddleLeft();
            InitializeTopRightMiddleRight();
            InitializeTopRightBottom();
            InitializeMiddle();
            InitializeBottom();

            // Load devices asynchronously to avoid delay UI creating
            LoadDevicesAsync();
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(GetBarCodeMatchingRules));
        }
        // 获取条码匹配规则
        private async void GetBarCodeMatchingRules() {
            await Task.Run(() => {
                _barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id)).BarCodeMatchingRuleDTOs;
                _productBarCodeMatchingRules = new();
                _partsBarCodeMatchingRules = new();
                foreach (BarCodeMatchingRuleDTO dto in _barCodeMatchingRuleDTOs) {
                    if (dto.type == BarCodeTypes.PRODUCT.Id) {
                        if (!_productBarCodeMatchingRules.ContainsKey(dto.mission_id)) {
                            _productBarCodeMatchingRules.Add(dto.mission_id, new() { dto });
                        } else {
                            _productBarCodeMatchingRules[dto.mission_id].Add(dto);
                        }
                    } else if (dto.type == BarCodeTypes.PARTS.Id) {
                        if (!_partsBarCodeMatchingRules.ContainsKey(dto.mission_id)) {
                            _partsBarCodeMatchingRules.Add(dto.mission_id, new() { dto });
                        } else {
                            _partsBarCodeMatchingRules[dto.mission_id].Add(dto);
                        }
                    }
                }
                // 检查匹配规则中所对应的任务是否还存在
                List<int> missionIds = _apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs.Select(m => m.id).ToList();
                Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator productCodes = _productBarCodeMatchingRules.GetEnumerator();
                while (productCodes.MoveNext()) {
                    int currId = productCodes.Current.Key;
                    if (!missionIds.Contains(currId)) {
                        _productBarCodeMatchingRules.Remove(currId);
                    }
                }
                Dictionary<int, List<BarCodeMatchingRuleDTO>>.Enumerator partsCodes = _partsBarCodeMatchingRules.GetEnumerator();
                while (partsCodes.MoveNext()) {
                    int currId = partsCodes.Current.Key;
                    if (!missionIds.Contains(currId)) {
                        _partsBarCodeMatchingRules.Remove(currId);
                    }
                }
            });
        }

        // 初始化所有外框
        private void InitializeOuters() {
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
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
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
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的左边
            _topRightMiddleLeft = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的中间的右边
            _topRightMiddleRight = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };

            // 中间
            _middle = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
        }

        // 初始化顶部左侧顶部
        private void InitializeTopLeftTop() {
            _barCodeImage = Properties.Resources.bar_code_icon;
            _barCodePictureBox = new() {
                Parent = _barCodeOuter,
                Margin = new(0),
                Padding = new(0),
            };
            _barCodeTextBox = new() {
                Parent = _barCodeOuter,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                DisabledBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            EventFuncs.CurrentActiveControl = _barCodeOuter;
            _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
            _barCodeTextBox.Enabled = false;
            _barCodeOuter.Click += barCodePopUp;
            _barCodePictureBox.Click += barCodePopUp;
            _barCodeTextBox.Click += barCodePopUp;

            void barCodePopUp(object? s, EventArgs e) {
                OpenBarCodePopUpForm();
            }
        }
        private void OpenBarCodePopUpForm(string? barCode = null) {
            if (!_activated) {
                string batchNum = _productBatch.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(batchNum)) {
                    WidgetUtils.ShowErrorPopUp("产品批次还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _productBatch.GetTextBox(0).IsError = true;
                    _productBatch.GetTextBox(0).Box.Focus();
                    return;
                }
            }

            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                _barCodePopUpForm = new(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated, 
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                if (!_activated) {
                    _barCodePopUpForm.AddButton("激活任务").Click += (sender, eventArgs) => {
                        if (!_activated) {
                            if (!_barCodePopUpForm.CheckCanActivateMission()) {
                                CustomTextBox customTextBox = _barCodePopUpForm.ProductBarCodeBox.GetTextBox(0);
                                if (string.IsNullOrEmpty(_barCodeObj.ProductBarCode)) {
                                    customTextBox.IsError = true;
                                }
                                for (int i = 0; i < _barCodePopUpForm.PartsBarCodeContentPanel.Controls.Count; i++) {
                                    if (i >= _barCodeObj.PartsBarCodes.Count) {
                                        ((CustomTextBoxButtonGroup) _barCodePopUpForm.PartsBarCodeContentPanel.Controls[i]).GetTextBox(0).IsError = true;
                                    }
                                }
                                WidgetUtils.ShowWarningPopUp("条码录入完成后才可激活任务");
                            } else {
                                ActivateMission();
                                _barCodePopUpForm.Dispose();
                            }
                        } else {
                            _barCodePopUpForm.Dispose();
                        }
                    };
                }
                _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.Dispose();
                _barCodePopUpForm.PretendToShowToCreateHandlesForChildren();
                _barCodePopUpForm.ResizeSelf();
            }
            _barCodePopUpForm.Show();
        }

        // 初始化顶部左侧底部
        private void InitializeTopLeftBottom() {
            _productImageDisplayPanel = new(_defaultImage) {
                Parent = _imageDisplayOuter,
                Margin = new(1, 1, 0, 0),
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
            };
            _missionImages = new();
            _productImageFiles = new();
            _allBolts = new();
            _armLocatingAccuracy = MainUtils.GetArmLocatingAccuracy();

            SetProductImagePanel();
        }
        private void SetProductImagePanel() {
            _sides.Clear();
            _allBolts.Clear();
            _productImageDisplayPanel.Controls.Clear();
            _productImageFiles.Clear();
            _missionImages.Clear();
            if (_mission.ProductSides != null) {
                _sides.AddRange(_mission.ProductSides);
                foreach (ProductSideDTO sideDTO in _sides) {
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
                                    ClickOutsideToClose = true,
                                };
                                // 添加按钮
                                if (_currentWorkingBolt == null || _currentWorkingBolt.BoltDTO.serial_num != boltDTO.serial_num) {
                                    CommonButton switchBtn = _boltPopUpForm.AddButton("切换到此点位");
                                    switchBtn.Click += (s, e) => {
                                        if (!_activated || _finished) {
                                            WidgetUtils.ShowErrorPopUp("任务未激活或已完成，无法切换点位！");
                                            _boltPopUpForm.Dispose();
                                        } else {
                                            if (_currentWorkingBolt != null) {
                                                _adminConfirmed = false;
                                                OpenAdminPasswordPopUpForm("点位切换。请输入管理员密码解锁。");
                                                if (_adminConfirmed.Value) {
                                                    int newIndex = _allBolts.IndexOf(boltBtn);
                                                    _currentWorkingBolt.ResetStatusWithoutChangingVisible();
                                                    _currentWorkingBolt.StopFlickering();
                                                    _currentWorkingBolt = SwitchBoltAndChangeStatus(newIndex);
                                                    _boltPopUpForm.Dispose();
                                                }
                                                _adminConfirmed = null;

                                                // 切换点位时，只能向后选择没有拧的点位，不能选择前面的。即只能跳过某些点，不能重新打某些点
                                                // if (_allBolts.IndexOf(_currentWorkingBolt) > newIndex) {
                                                //     WidgetUtils.ShowErrorPopUp("无法切换到已完成的螺栓点位！");
                                                // } else {
                                                //     _currentWorkingBolt.ResetStatusWithoutChangingVisible();
                                                //     _currentWorkingBolt.StopFlickering();
                                                //     _currentWorkingBolt = SwitchBoltAndChangeStatus(newIndex);
                                                // }
                                            }
                                        }
                                    };
                                }
                                CommonButton closeBtn = _boltPopUpForm.AddButton("关闭");
                                closeBtn.Click += (s, e) => {
                                    _boltPopUpForm.Dispose();
                                };
                                // Show form but make it transparent to create handles for its children
                                _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
                                // Resize all widgets
                                ResizeBoltPopUpForm();
                                // Real show
                                _boltPopUpForm.Show();
                            };
                            _allBolts.Add(boltBtn);
                        }
                    }
                }
            }

            // 默认显示第一个产品面和对应的螺栓点位
            _showingBoltButtons = _allBolts.Where(btn => btn.BoltDTO.side_id == _sides[_currentSideIndex].id).ToList();
            _showingBoltButtons.ForEach(btn => btn.Visible = true);
        }

        // 初始化顶部右侧的顶部
        private void InitializeTopRightTop() {
            _operatorInfoTitle = new() {
                Parent = _topRightTop,
                Margin = new(1),
                Padding = new(0),
                Text = "操作员信息",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _operatorName = new("操作员") {
                Parent = _topRightTop,
                ReadOnly = true,
                Enabled = false,
            };
            _operatorId = new("员工号") {
                Parent = _topRightTop,
                ReadOnly = true,
                Enabled = false,
            };
            SetOperatorInfo();
        }
        private void SetOperatorInfo() {
            _operatorName.SetValue(0, SystemUtils.LoggedUserName);
            _operatorId.SetValue(0, SystemUtils.LoggedUserId + "");
        }

        // 初始化顶部中间的左侧
        private void InitializeTopRightMiddleLeft() {
            // 初始化实时状态显示框
            _workingProcessPanel = new() {
                Parent = _topRightMiddleLeft,
                Margin = new(0),
                Padding = new(0),
            };
        }

        // 初始化顶部中间的右侧
        private void InitializeTopRightMiddleRight() {
            // 初始化实时螺钉拧紧数据框
            _torqueTitle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "扭矩（N*m）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _torque = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "0.0",
                TextAlign = ContentAlignment.MiddleRight,
            };
            _angleTitle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "角度（°）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _angle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "0",
                TextAlign = ContentAlignment.MiddleRight,
            };
        }

        // 初始化顶部右侧的底部
        private void InitializeTopRightBottom() {
            _missionDetailTitle = new() {
                Parent = _topRightBottom,
                Margin = new(1),
                Padding = new(0),
                Text = "任务信息",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _productBatch = new("产品批次") {
                Parent = _topRightBottom,
            };
            _productBatch.GetTextBox(0).Box.TextChanged += (s, e) => {
                _productBatch.GetTextBox(0).IsError = false;
                if (_activated && _missionRecord != null && _productBatch.GetTextBox(0).Box.Text != _missionRecord.product_batch) {
                    WidgetUtils.ShowErrorPopUp("任务已激活，无法修改产品批次");
                    _productBatch.GetTextBox(0).Box.Text = _missionRecord.product_batch;
                }
            };
            _missionSelectedName = new("任务名称") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            CommonButton missionSelectBtn = _missionSelectedName.AddButton("切换");
            missionSelectBtn.Enabled = true;
            missionSelectBtn.Click += (s, e) => {
                if (_activated) {
                    WidgetUtils.ShowWarningPopUp("任务已激活，无法切换任务");
                } else {
                    List<ProductMissionDTO> missions = _apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs;
                    if (missions.Count > 0) {
                        PopUpMissionListForm(missions);
                    } else {
                        WidgetUtils.ShowWarningPopUp("没有可选任务，请前往任务管理添加");
                    }
                }
            };
            _productSumPerDay = new("今日生产") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _okSumPerDay = new("合格数") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _ngRatePerDay = new("不良品率") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _pset = new("程序号") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };

            SetMissionDetails();
        }
        private async void SetMissionDetails() {
            _missionSelectedName.SetValue(0, _mission.name);
            ResetMissionDetails();

            await Task.Run(async () => {
                while (!IsHandleCreated) {
                    await Task.Delay(200);
                }
                BeginInvoke(() => {
                    MissionRecordDTO? missionRecordDTO = _apis.QueryLatestMissionRecord(new(SystemUtils.LoggedUserId)).MissionRecordDTO;
                    // 存在可以回填的数据
                    if (missionRecordDTO != null) {
                        // 刚登录
                        if (MainUtils.LoginFlag) {
                            // 需要回填确认
                            if (MainUtils.IsProductBatchNoticeEnabled()) {
                                // 弹出提示确认是否回填
                                if (WidgetUtils.ShowConfirmPopUp($"是否继续批次【{missionRecordDTO.product_batch}】？")) {
                                    MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                                } else {
                                    MainUtils.LastProductBatch = null;
                                }
                            } 
                            // 不需要提示则直接回填
                            else {
                                MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                            }
                        }
                        // 最新查到的批次信息与缓存的不一致，则换掉
                        else if (MainUtils.LastProductBatch != missionRecordDTO.product_batch) {
                            MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                        }
                        // 不管是否回填，登录标识都要改
                        MainUtils.LoginFlag = false;
                        // 不为空就回填
                        if (!string.IsNullOrEmpty(MainUtils.LastProductBatch)) {
                            _productBatch.SetValue(0, MainUtils.LastProductBatch);
                        }
                    }
                });
            });
        }
        private void ResetMissionDetails() {
            SetTodayData();
            SetPset();
        }
        private void SetTodayData() {
            List<MissionRecordDTO> missionRecordDTOs = _apis.QueryMissionRecordList(new() { UserId = SystemUtils.LoggedUserId, Date = DateTime.Now }).MissionRecordDTOs;
            int sum = missionRecordDTOs.Count;
            int okSum = missionRecordDTOs.Where(dto => dto.mission_result == (int) TighteningStatus.OK).Count();
            double ngRate;
            if (sum == 0) {
                ngRate = 0;
            } else {
                ngRate = Math.Round((sum - okSum) / (double) sum, 4) * 100;
            }

            _productSumPerDay.SetValue(0, sum + "");
            _okSumPerDay.SetValue(0, okSum + "");
            _ngRatePerDay.SetValue(0, ngRate + "%");
        }
        private void SetPset() => SetPset(null);
        private void SetPset(string? customMsg) {
            if (!string.IsNullOrEmpty(customMsg)) {
                _pset.SetValue(0, customMsg);
            } else if (_currentWorkingBolt != null) {
                if (_currentWorkingBolt.CurrentParameterSet != null) {
                    _pset.SetValue(0, _currentWorkingBolt.CurrentParameterSet + "");
                } else {
                    _pset.SetValue(0, "未配置程序号");
                }
            } else {
                _pset.SetValue(0, null);
            }
        }
        private void PopUpMissionListForm(List<ProductMissionDTO> missions) {
            Size contentSize = new((int) (WidgetUtils.MainSize.Width * .7), (int) (WidgetUtils.MainSize.Height * .7));
            CustomPopUpForm popUpForm = new() {
                Title = "选择任务",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            MissionListPanel? missionListPanel = null;
            popUpForm.AddButton("确定").Click += (s, e) => {
                if (missionListPanel == null || missionListPanel.CurrentToggledMission == null) {
                    WidgetUtils.ShowErrorPopUp("请选择一个任务");
                } else {
                    // 手动切换任务需要清空一下条码缓存
                    _barCodeObj.Reset();
                    _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
                    SwitchToMission(_apis.QueryProductMissionDetail(new(missionListPanel.CurrentToggledMission.Entity.id)).ProductMissionDTO);
                    popUpForm.Hide();
                }
            };
            popUpForm.AddButton("关闭").Click += (s, e) => {
                popUpForm.Hide();
            };
            popUpForm.PretendToShowToCreateHandlesForChildren();
            popUpForm.SetContentSizeAndSelfSize(contentSize);
        
            Padding contentPadding = popUpForm.ContentPanel.Padding;
            int innerContentWidth = contentSize.Width - contentPadding.Size.Width;
            int innerContentHeight = contentSize.Height - contentPadding.Size.Height;
            missionListPanel = new() {
                Parent = popUpForm.ContentPanel,
                Margin = new Padding(0),
                Size = new(innerContentWidth, innerContentHeight),
            };
            missionListPanel.RefreshMissionBlocks(missions, null, true);

            popUpForm.Show();
        }
        public void SwitchToMission(ProductMissionDTO mission) {
            _mission = mission;
            _resetMissionName(_mission.name);
            _missionSelectedName.SetValue(0, _mission.name);
            SetProductImagePanel();
            ResizeTopLeftBottom();
        }

        // 初始化中间
        private void InitializeMiddle() {
            _tighteningDataPanel = new(gridView => {
                DataGridViewColumn[] columnRange = {};
                List<OperationDataField> operationDataFields = MainUtils.GetOperationDataFields();
                foreach (OperationDataField field in operationDataFields) {
                    if (field.Visible) {
                        DataGridViewTextBoxColumn column = new() {
                            DataPropertyName = field.PropertyName,
                            HeaderText = field.FieldName,
                            ReadOnly = true,
                        };
                        columnRange = columnRange.Append(column).ToArray();
                    } 
                }
                gridView.Columns.Clear();
                gridView.Columns.AddRange(columnRange);
                gridView.Columns[0].Frozen = true;
            }) {
                Parent = _middle,
                HeaderHeight = WidgetUtils.WorkplaceGridViewHeaderHeight(),
                RowsHeight = WidgetUtils.WorkplaceGridViewContentRowHeight(),
                PageHeight = WidgetUtils.WorkplaceGridViewPageInfoHeight(),
                ColumnsPaddingRatio = WidgetUtils.WorkplaceGridViewColumnsPaddingRatio(),
                AutoDown = true,
            };
            _tighteningDataPanel.HandleCreated += (s, e) => {
                _tighteningDataPanel.DataSource = _tighteningDataVOs;
            };
        }

        // 初始化底部
        private void InitializeBottom() {
            _deviceBlocks = new();
            List<DeviceCategory> deviceCategories = new();
            // Reverse because of RightToLeft flow direction
            for (int i = DeviceCategories.Elements.Count - 1; i >= 0; i--) {
                deviceCategories.Add(DeviceCategories.Elements[i]);
            }
            foreach (DeviceCategory category in deviceCategories) {
                DeviceBlock deviceBlock = new(category) {
                    Parent = _bottom,
                    Margin = new(0),
                    Padding = new(0),
                    BlockHoverUp = true,
                    BlockHoverDown = true,
                    ToggledColor = ColorConfigs.COLOR_DEVICE_BLOCK_TOGGLED,
                };
                deviceBlock.MouseMove += (sender, eventArg) => {
                    if (deviceBlock.FloatingForm == null || deviceBlock.FloatingForm.IsDisposed) {
                        int panelHeight = WidgetUtils.TextOrComboBoxHeight();
                        Size contentSize = new();
                        contentSize.Width = (int) (WidgetUtils.MainSize.Width * .225);
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
                                contentSize.Width = (int) (WidgetUtils.MainSize.Width * .45);
                                deviceBlock.FloatingForm = new SerialPortDetailFloatingForm(deviceBlock.CategoryName, _serialPortTasks, panelHeight);
                                contentSize.Height = panelHeight * _serialPortTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.COMMUNICATION) {
                            if (_communicationTask.Count > 0) {
                                deviceBlock.FloatingForm = new CommunicationDetailFloatingForm(deviceBlock.CategoryName, _communicationTask, panelHeight);
                                contentSize.Height = panelHeight * _communicationTask.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
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
                            contentSize.Width = (int) (WidgetUtils.MainSize.Width * .65);
                            if (deviceBlock.Category == DeviceCategories.TOOL) {
                                if (_toolTasks.Count > 0) {
                                    _adminConfirmed = false;
                                    OpenAdminPasswordPopUpForm("手动控制工具。需要管理员操作密码");
                                    if (!_adminConfirmed.Value) {
                                        _adminConfirmed = null;
                                        return;
                                    }
                                    _adminConfirmed = null;
                                    int? currentWorkstationId = null;
                                    int? currentPset = null;
                                    if (_currentWorkingBolt != null) {
                                        currentWorkstationId = _currentWorkingBolt.BoltDTO.workstation_id;
                                        currentPset = _currentWorkingBolt.BoltDTO.parameters_set;
                                    }
                                    deviceBlock.PopUpForm = new ToolOperationPopUpForm(_currentWorkingBolt, SetPset, deviceBlock.CategoryName, 
                                            _workingProcessPanel, _workstationsDTOs, _toolTasks, currentWorkstationId, currentPset);
                                    contentSize.Height = panelHeight * _toolTasks.Count + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;

                                    ToolOperationPopUpForm popUpForm = (ToolOperationPopUpForm) deviceBlock.PopUpForm;
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
                                    deviceBlock.PopUpForm.HandleDestroyed += (sender, eventArgs) => {
                                        armTasks.ForEach(t => t.RetrieveResult = false);
                                        if (_currentWorkingBolt != null && _currentWorkingBolt.BoltDTO.workstation_id != null) {
                                            WorkstationDTO workstationDTO = _workstationsDTOs.Single(w => w.id == _currentWorkingBolt.BoltDTO.workstation_id);
                                            if (_locating_enabled && workstationDTO.arm_id != null) {
                                                _armTasks[workstationDTO.arm_id.Value].RetrieveResult = true;
                                            }
                                        }
                                    };
                                    contentSize.Width = (int) (WidgetUtils.MainSize.Width * .45);
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

            // Time displayer
            _timeDisplayerOuter = new() {
                Parent = _bottom,
                Padding = new(0),
                Margin = new(1, 1, 0, 0),
            };
            _timeDisplayer = new() {
                Parent = _timeDisplayerOuter,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                AutoSize = true,
            };
            _timeDisplayerTimer = new() {
                Interval = 1000,
            };
            _timeDisplayerTimer.Tick += (s, e) => {
                _timeDisplayer.Text = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_CHINESE);
            };
            _timeDisplayerTimer.Start();
        }


        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            if (IsHandleCreated && !IsDisposed) {
                int boxHeight = WidgetUtils.WorkplaceBoxOrButtonHeightRatio();
                int titleHeight = (int) (boxHeight * 1.1);
                int contentVPadding = (int) (boxHeight * .35);
                int contentHPadding = contentVPadding;
                Font titleFont = new Font(WidgetsConfigs.SystemFontFamily, titleHeight * .55f, FontStyle.Bold, GraphicsUnit.Pixel);

            	ResizeOuters(boxHeight, titleHeight, contentVPadding);
            	ResizeTopLeftTop();
            	ResizeTopLeftBottom();
            	ResizeTopRightTop(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
            	ResizeTopRightMiddleLeft();
            	ResizeTopRightMiddleRight();
            	ResizeTopRightBottom(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
            	ResizeMiddle();
            	ResizeBottom();
                Invalidate();
            }
        }

        // 计算尺寸： 外框
        private void ResizeOuters(int boxHeight, int titleHeight, int contentVPadding) {
            int padding = Padding.Left / 2;
            int workplaceWidth = Width - Padding.Left * 2;
            int workplaceHeight = Height - Padding.Top * 2;
            int barCodeHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceBarCodeHeightRatio());
            int imagePanelHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio());
            int topHeight = barCodeHeight + imagePanelHeight + padding;
            int bottomHeight = (int) (workplaceHeight * .045);
            int middleHeight = workplaceHeight - topHeight - bottomHeight - padding * 2; // 为了取整
            int topLeftWidth = (int) (workplaceWidth * WidgetUtils.WorkplaceLeftWidthRatio());
            int topRightWidth = workplaceWidth - topLeftWidth - padding;
            int topRightTopHeight = titleHeight + boxHeight + contentVPadding * 2;
            int topRightBottomHeight = titleHeight + boxHeight * 4 + contentVPadding * 5;
            int topRightMiddleHeight = topHeight - topRightTopHeight - topRightBottomHeight - padding * 2;
            int topRightMiddleLeftWidth = (int) (topRightWidth * .55);
            int topRightMiddleRightWidth = topRightWidth - topRightMiddleLeftWidth - padding;

            // 上方
            _top.Size = new(workplaceWidth, topHeight);
            _top.Margin = new(0, 0, 0, padding);
            // 上方左边
            _topLeft.Size = new(topLeftWidth, topHeight);
            _topLeft.Margin = new(0, 0, padding, 0);
            // 上方左边上面
            _barCodeOuter.Size = new(topLeftWidth, barCodeHeight);
            _barCodeOuter.Margin = new(0, 0, 0, padding);
            // 上方左边下面
            _imageDisplayOuter.Size = new(topLeftWidth, imagePanelHeight);
            // 上方右边
            _topRight.Size = new(topRightWidth, topHeight);
            // 上方右边的上面
            _topRightTop.Size = new(topRightWidth, topRightTopHeight);
            _topRightTop.Margin = new(0, 0, 0, padding);
            // 上方右边的中间
            _topRightMiddle.Size = new(topRightWidth, topRightMiddleHeight);
            _topRightMiddle.Margin = new(0, 0, 0, padding);
            // 上方右边的中间的左边
            _topRightMiddleLeft.Size = new(topRightMiddleLeftWidth, topRightMiddleHeight);
            _topRightMiddleLeft.Margin = new(0, 0, padding, 0);
            // 上方右边的中间的右边
            _topRightMiddleRight.Size = new(topRightMiddleRightWidth, topRightMiddleHeight);
            // 上方右边的下面
            _topRightBottom.Size = new(topRightWidth, topRightBottomHeight);

            // 中间
            _middle.Size = new(workplaceWidth, middleHeight);
            _middle.Margin = new(0, 0, 0, padding);

            // 下方
            _bottom.Size = new(workplaceWidth, bottomHeight);
        }
        
        // 计算尺寸： 条码框
        private void ResizeTopLeftTop() {
            // icon的边长
            int side = (int) (_barCodePictureBox.Parent.Height * .675);
            // 重设icon
            _barCodePictureBox.Image = WidgetUtils.ResizeImage(_barCodeImage, side, side);
            _barCodePictureBox.Margin = new((_barCodePictureBox.Parent.Height - side) / 2);
            _barCodePictureBox.Size = new(side, side);

            // 重设输入框
            int newH = (int) (_barCodePictureBox.Parent.Height * .875);
            _barCodeTextBox.Size = new(_barCodePictureBox.Parent.Width - side * 2, newH);
            _barCodeTextBox.Margin = new(0, (_barCodePictureBox.Parent.Height - newH) / 2, 0, 0);

            // 重新计算弹框的大小
            ResizeBarCodePopUpForm();
        }
        private void ResizeBarCodePopUpForm() {
            if (_barCodePopUpForm != null) {
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

            // 重新计算螺栓点位按钮的大小和位置
            int btnSide = (int) (newPanelSize.Height * .125);
            foreach (BoltButton boltButton in _allBolts) {
                boltButton.Size = new(btnSide, btnSide);
                int newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                int newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                boltButton.Location = new(newX, newY);
            }

            // 重新计算弹框的大小和位置
            ResizeBoltPopUpForm();
        }
        private void ResizeBoltPopUpForm() {
            if (_boltPopUpForm != null) {
                _boltPopUpForm.ResizeSelf();
            }
        }

        // 计算尺寸： 员工信息框
        private void ResizeTopRightTop(int boxHeight, int titleHeight, int contentVPadding, 
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _operatorInfoTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _operatorInfoTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
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
        private void ResizeTopRightMiddleRight() {
            // Resize titles
            _torqueTitle.Size = new(_torqueTitle.Parent.Width - 2, (int) (_torqueTitle.Parent.Height * .225));
            _angleTitle.Size = _torqueTitle.Size;
            // Reset font size
            _torqueTitle.Font = new Font(WidgetsConfigs.SystemFontFamily, _torqueTitle.Height * .55f, FontStyle.Bold, GraphicsUnit.Pixel);
            _angleTitle.Font = _torqueTitle.Font;
            // Resize data text
            int heightRemain = _torqueTitle.Parent.Height - _torqueTitle.Height - _angleTitle.Height - 6; // 2 vertical border, 2 vertical margin of each title
            if (heightRemain > 0) {
                _torque.Size = new(_torqueTitle.Parent.Width - 2, (int) (heightRemain * .6) - 2);
                _angle.Size = new(_torqueTitle.Parent.Width - 2, heightRemain - _torque.Height - 2);
                // Reset font size depends on theirs height
                _torque.Font = new(WidgetsConfigs.SystemFontFamily, _torque.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
                _angle.Font = new(WidgetsConfigs.SystemFontFamily, _angle.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }

        // 计算尺寸： 任务信息框
        private void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding, 
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _missionDetailTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _missionDetailTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
            int boxWidth2 = _operatorInfoTitle.Parent.Width - contentHPadding * 2;
            _productBatch.Size = new(boxWidth2, boxHeight);
            _productBatch.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _missionSelectedName.Size = new(boxWidth2, boxHeight);
            _missionSelectedName.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _productSumPerDay.Size = new(boxWidth, boxHeight);
            _productSumPerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _okSumPerDay.Size = new(boxWidth, boxHeight);
            _okSumPerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _ngRatePerDay.Size = new(boxWidth, boxHeight);
            _ngRatePerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _pset.Size = new(boxWidth, boxHeight);
            _pset.Margin = new(contentHPadding, contentVPadding, 0, 0);
        }

        // 计算尺寸： 数据展示列表区域
        private void ResizeMiddle() {
            _tighteningDataPanel.Size = _tighteningDataPanel.Parent.Size;
        }

        // 计算尺寸： 底部横框
        private void ResizeBottom() {
            int blocksWidth = 0;
            foreach (Control control in _bottom.Controls) {
                if (control is DeviceBlock) {
                    control.Size = new(_bottom.Height, _bottom.Height);
                    blocksWidth += _bottom.Height;
                }
            }
            int timeDisplayerWidth = _bottom.Width - blocksWidth;
            _timeDisplayerOuter.Size = new(timeDisplayerWidth - 2, _bottom.Height - 2);
            _timeDisplayer.Font =new Font(WidgetsConfigs.SystemFontFamily, _bottom.Height * .4f, FontStyle.Regular, GraphicsUnit.Pixel);
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

        private async void LoadDevicesAsync() {
            await Task.Run(async () => {
                // 查询所有站点信息
                _workstationsDTOs = _apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs.ToList();
                // 查询所有设备信息
                _arms = _apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id)).DeviceArmDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _tools = _apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id)).DeviceToolDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _serialPorts = _apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id)).DeviceSerialPortDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
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
                            serialPortTask.ActionAfterDataReceived = async msg => {
                                await Task.Run(() => {
                                    BeginInvoke(() => {
                                        if (!IsDisposed && !_activated || _finished) {
                                            DeviceSerialPortDTO dto = _serialPorts.Single(dto => dto.id == pair.Key);
                                            // 如果有空的数据进来，则跳过
                                            if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
                                                return;
                                            }
                                            if (dto.invalid_char != null) {
                                                msg = String.Concat(msg.Where(c => !dto.invalid_char.Contains(c)));
                                            }

                                            // 交给弹窗处理
                                            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                                                OpenBarCodePopUpForm(msg);
                                            } else {
                                                _barCodePopUpForm.ValidateBarCode(msg);
                                            }
                                        }
                                    });
                                });
                            };
                        }
                    } else if (category == DeviceCategories.COMMUNICATION) {
                        _communicationTask= MainUtils.CommunicationTasks;
                    } else {
                        // TODO
                    }
                }
            });
            // Keep listenging devices
            CheckDeviceConnections();
        }
        // 检查当前条码是否与数据库中任意任务的产品码/追溯码匹配
        private ProductMissionDTO? FindMatchedMission(string barCode, Dictionary<int, List<BarCodeMatchingRuleDTO>> rules) {
            ProductMissionDTO? mission = null;
            foreach (KeyValuePair<int, List<BarCodeMatchingRuleDTO>> pair in rules) {
                foreach (BarCodeMatchingRuleDTO rule in pair.Value) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule.end_char, rule.length, MainUtils.GetKeyMatchingRule(rule.key_position, rule.key_char))) {
                        mission = _apis.QueryProductMissionDetail(new(pair.Key)).ProductMissionDTO;
                        break;
                    }
                }
                if (mission != null) {
                    break;
                }
            }
            return mission;
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
                                Check(block, _communicationTask.Values.ToList());
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
                        if (!task.WorkplaceCheckConnection()) {
                            block.ResetIconByStatus(DeviceStatus.ERROR);
                            return;
                        }
                    }
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                }
            }
        }

        // 激活任务
        public void ActivateMission() {
            if (_sides.Count > 0 && _allBolts.Count > 0) {
                // 将所有点位的序号填好（任务进行过程中会被改成扭矩 - 西艾爱需求）
                _allBolts.ForEach(b => b.BoltStatus = BoltStatus.DEFAULT);
                // 再次确认力臂定位是否开启
                _locating_enabled = MainUtils.IsArmLocatingEnabled();
                // 1. 将当前任务的所有螺栓点位按顺序排好队，并初始化所有螺栓点位的状态
                _allBolts = _allBolts.OrderBy(btn => btn.BoltDTO.side_id).ThenBy(btn => btn.BoltDTO.serial_num).ToList();
                _allBolts.ForEach(btn => btn.ResetStatusWithoutChangingVisible());
                // 2. 设置当前点位为第一个点位
                _currentWorkingBolt = SwitchBolt(0);
                // 3. 检查站点是否配置，或当前站点是否存在工具和力臂，不存在则无法激活任务
                if (_workstationsDTOs.Count == 0) {
                    WidgetUtils.ShowErrorPopUp($"没有配置站点，无法激活任务");
                    _currentWorkingBolt = null;
                    return;
                }
                ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
                if (toolId == null || _tools.SingleOrDefault(tool => tool.id == toolId.Value) == null) {
                    WidgetUtils.ShowErrorPopUp($"点位[{_currentWorkingBolt.BoltDTO.serial_num}]所选择的站点没有配置工具，无法激活任务");
                    _currentWorkingBolt = null;
                    return;
                }
                int? armId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).arm_id;
                if (_locating_enabled && (armId == null || _arms.SingleOrDefault(arm => arm.id == armId.Value) == null)) {
                    WidgetUtils.ShowErrorPopUp($"点位[{_currentWorkingBolt.BoltDTO.serial_num}]所选择的站点没有配置力臂，无法激活任务");
                    _currentWorkingBolt = null;
                    return;
                }
                // 4. 初始化工具拧紧而非反松
                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                // 5. 如果力臂开关开启，则开始读取数据，同时开始监听点位状态
                if (_locating_enabled) {
                    if (armId != null) {
                        ArmTask armTask = _armTasks[armId.Value];
                        armTask.RetrieveResult = true;
                        armTask.OnActionAfterReceiving += ActionAfterArmDataReceived;
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                    }
                } else {
                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                }
                // 6. 切换点位状态（包括一些操作）
                ChangeBoltStatusToWorking(_currentWorkingBolt, _locating_enabled);
                // 7. 清空数据展示列表
                _tighteningDataVOs.Clear();
                RefreshTighteningDataPanel();
                // 8. 将所有点位的NG次数清零
                _allBolts.ForEach(b => b.NgTimes = 0);
                // 9. 修改任务激活状态
                _activated = true;
                _finished = false;
                // 10. 新增一条“任务记录”数据
                _missionRecord = new() {
                    mission_id = _mission.id,
                    product_batch = _productBatch.GetTextBox(0).Box.Text,
                    product_bar_code = _barCodeObj.ProductBarCode,
                    parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
                    mission_result = (int) TighteningStatus.NG,
                    is_redo = _isRedo,
                };
                _apis.AddOrUpdateMissionRecord(new(_missionRecord));
            }
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
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

        // 根据index切换点位
        private BoltButton SwitchBolt(int newIndex) {
            // 通过index切换点位
            return _allBolts[newIndex];
        }
        // 根据index切换点位
        private BoltButton SwitchBoltAndChangeStatus(int newIndex, bool lockTool = true) {
            // 通过index切换点位
            BoltButton newBolt = SwitchBolt(newIndex);
            ChangeBoltStatusToWorking(newBolt, lockTool);
            return newBolt;
        }
        private void ChangeBoltStatusToWorking(BoltButton boltButton, bool lockTool = true) {
            ProductBoltDTO boltDTO = boltButton.BoltDTO;
            int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
            int? pset = boltDTO.parameters_set;
            boltButton.CurrentParameterSet = null;
            // 将当前螺栓点位的serial_num传给process poanel
            _workingProcessPanel.BoltSerialNum = boltButton.BoltDTO.serial_num;
            // 将工具锁住防止误操作（如果没有配置程序号，则默认锁枪）
            ToolTask toolTask = _toolTasks[toolId.Value];
            if (lockTool || pset == null) {
                toolTask.SendLock();
            } else {
                toolTask.SendUnlock();
            }
            // 下发当前螺栓点位的程序号至控制器
            SendPSet(toolTask, pset);
            // 修改点位状态
            boltButton.BoltStatus = BoltStatus.WORKING;
        }
        // 下发程序号，失败时持续尝试
        private async void SendPSet(ToolTask task, int? pset) {
            // 显示信息
            SetPset();
            if (_currentWorkingBolt == null) {
                return;
            }
            if (pset == null) {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetNullError);
                return;
            }
            // 下发程序号
            await Task.Run(() => {
                BeginInvoke(async () => {
                    int sendTimes = 0;
                    while (!IsDisposed && !(await task.SendPSetAsync(pset.Value))) {
                        // 如果手动下发了，则不再尝试下发
                        if (_currentWorkingBolt.CurrentParameterSet != null) {
                            return;
                        }
                        sendTimes++;
                        if (sendTimes >= _resendPsetMaxTimes) {
                            WidgetUtils.ShowWarningPopUp($"同一个点位下发程序号达到{_resendPsetMaxTimes}次，无法再次下发，请检查任务配置");
                            return;
                        }
                        // 显示状态和信息
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                        _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                        _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetFailedError);
                        // 实时显示pset到任务信息框
                        SetPset("程序号下发失败");
                        // 弹出确认框询问是否重发
                        if (!WidgetUtils.ShowConfirmPopUp($"程序号{pset}下发失败，是否重发？")) {
                            return;
                        }
                    }
                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetFailedError);
                    _currentWorkingBolt.CurrentParameterSet = pset;
                    // 经过一些列操作后，更新显示信息
                    SetPset();
                });
            });
        }

        // 读取力臂数据并根据当前螺栓点位配置信息进行解锁、锁枪
        private void ActionAfterArmDataReceived(Coordinates3D armCoordinates) {
            Task.Run(() => {
                BeginInvoke(() => {
                    if (_currentWorkingBolt != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
                        if (toolId != null) {
                            ToolTask toolTask = _toolTasks[toolId.Value];
                            Coordinates3D boltCoordinates = Coordinates3D.FromString(boltDTO.position);
                            _realTimeArmCoordinates = armCoordinates;
                            int x = armCoordinates.X;
                            int y = armCoordinates.Y;
                            int z = armCoordinates.Z;
                            // 力臂位置在点位范围内
                            if (Math.Abs(x - boltCoordinates.X) < _armLocatingAccuracy && Math.Abs(y - boltCoordinates.Y) < _armLocatingAccuracy
                                    && (boltCoordinates.Z == 0 || Math.Abs(z - boltCoordinates.Z) < _armLocatingAccuracy)) {
                                _workingProcessPanel.RemoveDesc(_workingProcessPanel.ArmPositionError);
                                // 需要管理员输入密码并确认
                                if (_adminConfirmed != null) {
                                    // 管理员已确认
                                    if (_adminConfirmed.Value) {
                                        toolTask.SendUnlock();
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                        _workingProcessPanel.RemoveDesc(_workingProcessPanel.AdminConfirmation);
                                        _adminConfirmed = null;
                                    } 
                                    // 管理员未确认
                                    else {
                                        toolTask.SendLock();
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                        _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.AdminConfirmation);
                                        if (_adminPasswordPopUpForm == null || _adminPasswordPopUpForm.IsDisposed) {
                                            _adminConfirmed = false;
                                            NGConfirmPopUp();
                                        }
                                    }
                                } 
                                // 当前点位没有设置程序号
                                else if (_currentWorkingBolt.CurrentParameterSet == null) {
                                    toolTask.SendLock();
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                                    // 如果是没有配置就显示对应错误信息，否则可能是下发失败
                                    if (_currentWorkingBolt.BoltDTO.parameters_set == null) {
                                        _workingProcessPanel.SetDesc(_workingProcessPanel.PsetNullError);
                                    }
                                } 
                                // // 当前下发的程序与点位的不匹配（可能是手动下发）
                                // else if (_currentWorkingBolt.BoltDTO.parameters_set != _currentWorkingBolt.CurrentParameterSet) {
                                //     toolTask.SendLock();
                                //     _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                //     _workingProcessPanel.SetDesc(_workingProcessPanel.PsetNotMatchedError);
                                // } 
                                // 检查是否是需要反松，“需要反松”这个字段用于判断当前点位是否有ng的情况，有时候有ng但不需要输入密码，因此需要保留错误信息
                                else if (_needLoosening) {
                                    toolTask.SendUnlock();
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                    _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                }
                                // 所有检查正常
                                else {
                                    toolTask.SendUnlock();
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                }
                            } 
                            // 力臂位置不在点位范围内
                            else {
                                toolTask.SendLock();
                                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                                _workingProcessPanel.AppendDesc(_workingProcessPanel.ArmPositionError);
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
                        int toolId = workstationDTO.tool_id.Value;
                        DeviceToolDTO toolDTO = _tools.Single(t => t.id == toolId);
                        dataDTO.tool_name = toolDTO.name;
                        dataDTO.tool_ip = $"{toolDTO.ip}:{toolDTO.port}";
                        dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type).Name;
                        dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                        dataDTO.bolt_serial_num = boltDTO.serial_num;
                        dataDTO.mission_record_id = _missionRecord.id;
                        dataDTO.vin_number = _missionRecord.product_bar_code;
                        if (_realTimeArmCoordinates != null) {
                            dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                        }

                        if (_tighteningData.result_type != (int) TightenOrLoosen.LOOSENING) {
                            bool tighteningOK = true;
                            string errorMsg = "";
                            // 检查返回的拧紧状态是否为成功
                            if (_tighteningData.tightening_status != (int) TighteningStatus.OK) {
                                tighteningOK = false;
                                if (_tighteningData.torque_status != (int) TighteningCommonStatus.OK) {
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), _tighteningData.torque_status)}";
                                }
                                if (_tighteningData.angle_status != (int) TighteningCommonStatus.OK) {
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), _tighteningData.angle_status)}";
                                }
                            }
                            // 检查控制器返回数据与螺栓点位配置的数据是否一致
                            // // 程序号（pset）校验
                            // if (boltDTO.parameters_set != null && boltDTO.parameters_set != _tighteningData.parameter_set_number) {
                            //     tighteningOK = false;
                            //     if (!string.IsNullOrEmpty(errorMsg)) {
                            //         errorMsg += "\r\n";
                            //     }
                            //     errorMsg += "程序号与配置不符";
                            // }
                            // 扭矩校验
                            if (boltDTO.torque_max > 0 && (_tighteningData.torque < boltDTO.torque_min || _tighteningData.torque > boltDTO.torque_max)) {
                                tighteningOK = false;
                                _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "扭矩与配置范围不符";
                            }
                            // 角度校验
                            if (boltDTO.angle_max > 0 && (_tighteningData.angle < boltDTO.angle_min || _tighteningData.angle > boltDTO.angle_max)) {
                                tighteningOK = false;
                                _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "角度与配置范围不符";
                            }
                            // 切换下一个点位
                            if (tighteningOK) {
                                // 如果点位NG但并不需要反松，则无法通过反松改回这两个设置，因此每次都改一下
                                _needLoosening = false;
                                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                // 扭矩角度数据颜色改成绿色
                                _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                                _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                                // 当前点位完成后先把设备的状态都复原
                                if (_locating_enabled) {
                                    _toolTasks[toolId].SendLock();
                                }
                                // 修改点位状态并切换点位
                                _currentWorkingBolt.BoltStatus = BoltStatus.DONE;
                                _currentWorkingBolt.Label = _torque.Text;
                                int nextIndex = _allBolts.IndexOf(_currentWorkingBolt) + 1;
                                // 检查是否存在跳点的情况
                                while (nextIndex < _allBolts.Count && _allBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                    nextIndex++;
                                }
                                if (nextIndex < _allBolts.Count) {
                                    _currentWorkingBolt = SwitchBoltAndChangeStatus(nextIndex, _locating_enabled);
                                } else {
                                    // 打完了直接锁枪
                                    _toolTasks[toolId].SendLock();
                                    // 已经打完最后一个点位，任务完成
                                    _activated = false;
                                    _finished = true;
                                    _currentWorkingBolt = null;
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_OK;
                                    _workingProcessPanel.CustomError = null;
                                    _workingProcessPanel.BoltSerialNum = null;
                                    // 扭矩角度数据颜色改回黑色
                                    _torque.ForeColor = Color.Black;
                                    _angle.ForeColor = Color.Black;
                                    // 停止读取力臂数据
                                    if (_locating_enabled) {
                                        int? armId = workstationDTO.arm_id;
                                        if (armId != null) {
                                            ArmTask armTask = _armTasks[armId.Value];
                                            armTask.RetrieveResult = false;
                                            armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                        }
                                    }
                                    // 更新“任务记录”数据的完成情况
                                    _missionRecord.mission_result = (int) TighteningStatus.OK;
                                    _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                                    // 清空缓存的条码
                                    _barCodeObj.Reset();
                                    // 重置任务信息
                                    ResetMissionDetails();
                                }
                                dataDTO.tightening_status = (int) TighteningStatus.OK;
                                // 记录数据
                                StoreTighteningData(dataDTO);
                            } else {
                                // 先锁枪
                                if (_locating_enabled) {
                                    _toolTasks[toolId].SendLock();
                                }
                                _currentWorkingBolt.BoltStatus = BoltStatus.ERROR;
                                _currentWorkingBolt.NgTimes++;
                                if (_currentWorkingBolt.NgTimes >= _mission.max_ng_num) {
                                    // 打完了直接锁枪
                                    _toolTasks[toolId].SendLock();
                                    // 任务失败
                                    _activated = false;
                                    _finished = true;
                                    _currentWorkingBolt.StopFlickering();
                                    _currentWorkingBolt = null;
                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_NG;
                                    _workingProcessPanel.CustomError = errorMsg;
                                    _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                    _workingProcessPanel.BoltSerialNum = null;
                                    // 扭矩角度数据颜色改回黑色
                                    _torque.ForeColor = Color.Black;
                                    _angle.ForeColor = Color.Black;
                                    // 停止读取力臂数据
                                    if (_locating_enabled) {
                                        int? armId = workstationDTO.arm_id;
                                        if (armId != null) {
                                            ArmTask armTask = _armTasks[armId.Value];
                                            armTask.RetrieveResult = false;
                                            armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                        }
                                    }
                                    // 清空缓存的条码
                                    _barCodeObj.Reset();
                                    // 重置任务信息
                                    ResetMissionDetails();
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                    // 先记录数据再弹出提示
                                    WidgetUtils.ShowErrorPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败");
                                } else { 
                                    // 扭矩角度数据颜色改成红色
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    _needLoosening = true;
                                    _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                    // 需要管理员密码弹窗
                                    if (_mission.password_need_time != 0 && _currentWorkingBolt.NgTimes >= _mission.password_need_time) {
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.AdminConfirmation);
                                        _adminConfirmed = false;
                                        // 先记录数据再打开弹窗
                                        NGConfirmPopUp();
                                    } else {
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                        _workingProcessPanel.CustomError = errorMsg;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                    }
                                }
                                dataDTO.tightening_status = (int) TighteningStatus.NG;
                            }
                        } else {
                            // 反松时把扭矩角度改回黑色
                            _torque.ForeColor = Color.Black;
                            _angle.ForeColor = Color.Black;
                            _needLoosening = false;
                            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                            _workingProcessPanel.CustomError = null;
                            if (MainUtils.GetStoreLooseningData()) {
                                // 记录数据
                                StoreTighteningData(dataDTO);
                            }
                        }
                    }
                });
            });
        }
        private void NGConfirmPopUp() {
            OpenAdminPasswordPopUpForm("拧紧错误，工具已锁止。请输入管理员密码解锁。");
        }
        public void OpenAdminPasswordPopUpForm(string msg) {
            _adminPasswordPopUpForm = new() { 
                Title = msg,
            };
            CustomTextBoxGroup _adminPasswordBox = new("管理员密码") {
                Parent = _adminPasswordPopUpForm.ContentPanel,
            };
            _adminPasswordBox.GetTextBox(0).Box.PasswordChar = '*';
            _adminPasswordBox.GetTextBox(0).TextChanged += (s, e) => {
                _adminPasswordBox.GetTextBox(0).IsError = false;
            };
            _adminPasswordBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    Confirm();
                }
            };
            CommonButton confirmButton = _adminPasswordPopUpForm.AddButton("确定");
            confirmButton.Click += (s, e) => Confirm();
            CommonButton closeButton = _adminPasswordPopUpForm.AddButton("取消");
            closeButton.Click += (s, e) => {
                _adminPasswordPopUpForm.Dispose();
            };
            _adminPasswordPopUpForm.PretendToShowToCreateHandlesForChildren();

            int contentWidth = (int) (WidgetUtils.MainSize.Width * .5);
            Padding contentPadding = _adminPasswordPopUpForm.ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            _adminPasswordBox.Size = new(contentWidth - contentPadding.Size.Width - boxMargin * 2, boxHeight);
            _adminPasswordBox.Margin = new(boxMargin);
            int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;

            _adminPasswordPopUpForm.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
            _adminPasswordPopUpForm.Show();

            void Confirm() {
                string password = _adminPasswordBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(password) && _apis.AdminPasswordValidate(new(password)).Succeed) {
                    WidgetUtils.ShowNoticePopUp("验证成功");
                    _adminConfirmed = true;
                    _adminPasswordPopUpForm.Dispose();
                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.AdminConfirmation);
                } else {
                    WidgetUtils.ShowErrorPopUp("密码错误");
                    _adminPasswordBox.GetTextBox(0).IsError = true;
                }
            }
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

    public class ProductImageDisplayPanel: AProductImageDisplayPanel {
        public ProductImageDisplayPanel(Image productDefaultImage) : base() {
            ProductDefaultImage = productDefaultImage;
        }

        protected override void InvokeResizing() {
            // MaxRectSize = MainUtils.GetProperSizeAccordingToSizeRatio((Size * .95F).ToSize(), Size);
            // MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByWidth(Width);
            // if (MaxRectSize.Height > Height) {
            //     MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByHeight(Height);
            // }
            MaxRectSize = Size;
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
                ProductDefaultImageShowing = WidgetUtils.ResizeImage(ProductDefaultImage, newImageSide, newImageSide);
                g.DrawImage(ProductDefaultImageShowing, new Point((Width - ProductDefaultImageShowing.Width) / 2, (Height - newImageSide) / 2));
            } else {
                g.DrawImage(ProductImage, ImageLocation.Value);
            }
        }
    }

    public class BarCodeInputPopUpForm: CustomPopUpForm {
        private WorkplaceContentPanel _workplace;
        private string _initStr;
        private ProductMissionDTO _mission;
        private Dictionary<int, List<BarCodeMatchingRuleDTO>> _productBarCodeRules;
        private Dictionary<int, List<BarCodeMatchingRuleDTO>> _partsBarCodeRules;
        private int _partsIndex = 1;
        private CustomTextBoxButtonGroup _productBarCodeBox;
        private CustomTextBoxButtonGroup? _focusedBox;
        private TitlePanel _productBarCodeTitle;
        private CustomContentPanel _productBarCodeContentPanel;
        private TitlePanel _partsBarCodeTitle;
        private CustomContentPanel _partsBarCodeContentPanel;
        private string? _barCode;

        public CustomTextBoxButtonGroup ProductBarCodeBox { get => _productBarCodeBox; set => _productBarCodeBox = value; }
        public CustomContentPanel PartsBarCodeContentPanel { get => _partsBarCodeContentPanel; set => _partsBarCodeContentPanel = value; }

        public BarCodeInputPopUpForm(WorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules, 
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode) : base() {
            _workplace = workplace;
            _initStr = initStr;
            _mission = mission;
            _productBarCodeRules = productBarCodeRules;
            _partsBarCodeRules = partsBarCodeRules;
            _barCode = barCode;

            _productBarCodeTitle = new("产品条码") {
                Parent = ContentPanel,
            };
            _productBarCodeContentPanel = new() {
                Parent = ContentPanel,
            };
            _partsBarCodeTitle = new("物料条码") {
                Parent = ContentPanel,
            };
            _partsBarCodeContentPanel = new() {
                Parent = ContentPanel,
            };

            _productBarCodeBox = AddProductBarCodeTextBox(); // 产品码/追溯码框
            AddPartsBoxes(_mission.id);
            FillExistingData();
        }
        // 一、没选任务
        // 	1. 手动点击打开弹窗：
        // 		1.1. 手动输入，输入后点击确定进行校验
        // 			1.1.1. 没有匹配到任何任务，直接提示“未检测到对应任务”
        // 			1.1.2. 匹配到某一任务，直接切换到对应的任务，并且将输入框设置为不可输入，但“确定”按钮改为“修改”按钮，可供修改。
        // 				   同时自动根据配置到该任务的物料码的数量提前添加所有的物料码输入口，且除了第一个物料输入框为可输入，其他的均先
        // 				   设定为不可输入
        // 				1.1.2.1. 如果当前条码已经存在于任务记录中，则弹出确认框“是否进行返工？”，是则进行【1.1.2.2】，否则回到【1.1】
        // 				1.1.2.2. 如果点击修改“产品码/追溯码”，则输入框改为可输入，并且物料码输入框设置为不可输入。重新输入产品码后从【1.1】处逻辑开始判定
        // 				1.1.2.3. 如果输入物料码并点击确定，则进行物料码校验
        // 					1.1.2.3.1. 输入的物料码与任务配置的物料码不匹配，直接提示“该物料码与任务配置不符”
        // 					1.1.2.3.2. 输入的物料码能匹配上，则禁用当前物料码，“确定”按钮改为“修改”按钮，可供修改。
        // 							   随后判断当前物料码是否是该任务配置的最后一个物料码
        // 						1.1.2.3.2.1. 不是最后一个物料码，， 并将下一个物料码输入框启用，且再次输入进入【1.1.2.2】的流程
        // 						1.1.2.3.2.2. 是最后一个物料码，则弹出确认框“所有条码扫描完毕，是否激活任务？”
        // 							1.1.2.3.2.2.1. 确认则激活任务
        // 							1.1.2.3.2.2.2. 不确认则可以随意进行任意输入框的“修改”操作
        // 		1.2. 扫码自动填入，不需要点击“确定”按钮，直接进入【1.1】的后续流程
        // 		*1.3. 所有输入框均可进行“手动输入”或“扫描自动填入”，“手动输入”需要手动点击“确认”按钮进行确认，“扫描自动填入”则会自动进行验证
        // 		      “修改”按钮是为了防止扫码枪扫描的条码值虽然符合任务配置的匹配规则，但是某些字符扫描错误，此时需要重新扫描
        // 	2. 直接扫码自动打开弹窗：
        // 		2.1. 自动回填扫到的码，并立即校验，然后走【1.1】的后续流程
        // 		
        // 二、已选择任务
        // 	1. 手动点击打开弹窗：
        // 		1.1. 手动输入并点击“确定”进行校验：
        // 			1.1.1. 没有匹配到任何任务（也包括当前已经选择的任务），直接提示“校验不通过”
        // 			1.1.2. 匹配到了非当前任务，提示“当前条码匹配到另一任务，是否切换？”
        // 				1.1.2.1. 确认切换，则切换后进入【一 - 1.1.2】的流程
        // 				1.1.2.1. 不切换，则流程回到【1.1】
        // 			1.1.3. 与当前任务的“产品码/追溯码”校验通过，则也进入【一 - 1.1.2】的流程
        // 	2. 直接扫码自动打开弹窗：
        // 		2.1. 自动回填扫到的码，并立即校验，然后走【二 - 1.1】的后续流程
        
        // 根据任务ID找到其对应的物料码匹配规则，并根据此规则添加相应的输入框
        private void AddPartsBoxes(int missionId) {
            if (missionId > 0 && _partsBarCodeRules.ContainsKey(missionId)) {
                for (int i = 0; i < _partsBarCodeRules[missionId].Count(); i++) {
                    AddPartsBarCodeTextBox();
                }
            }
        }
        // 根据当前缓存的条码数据，回填到弹窗里的所有输入框
        private void FillExistingData() {
            _productBarCodeBox.SetValue(0, _workplace.BarCodeObj.ProductBarCode);
            for (int i = 0; i < _workplace.BarCodeObj.PartsBarCodes.Count; i++) {
                ((CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[i]).SetValue(0, _workplace.BarCodeObj.PartsBarCodes[i]);
            }
            if (!_workplace.Activated) {
                if (_mission.id <= 0 || string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                    _productBarCodeBox.Enabled = true;
                    _productBarCodeBox.GetTextBox(0).Box.Focus();
                    ActiveControl = _productBarCodeBox.GetTextBox(0).Box;
                } else if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                    CustomTextBoxButtonGroup focusingBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
                    focusingBox.Enabled = true;
                    focusingBox.GetTextBox(0).Box.Focus();
                    ActiveControl = focusingBox.GetTextBox(0).Box;
                }
            }
        }
        // 切换任务
        private void SwitchToMission(ProductMissionDTO mission) {
            _mission = mission;
            _workplace.SwitchToMission(mission);
            _partsIndex = 1;
            // 自动回填产品码
            _productBarCodeBox.SetValue(0, _workplace.BarCodeObj.ProductBarCode);
            // 清除所有物料码输入框并重新根据当前任务添加
            _partsBarCodeContentPanel.Controls.Clear();
            AddPartsBoxes(_mission.id);
            ResizeSelf();
        }
        // 添加产品条码输入框
        private CustomTextBoxButtonGroup AddProductBarCodeTextBox() {
            CustomTextBoxButtonGroup box = new($"产品条码") {
                Parent = _productBarCodeContentPanel,
                Ratio = 8.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            SetupBox(box);
            CommonButton btn = box.AddButton("确定");
            btn.Click += (s, e) => ValidateProductBarCodeAsync();
            box.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ValidateProductBarCodeAsync();
                }
            };
            return box;
        }
        // 添加物料条码输入框
        private CustomTextBoxButtonGroup AddPartsBarCodeTextBox() {
            CustomTextBoxButtonGroup box = new($"物料条码{_partsIndex++}") {
                Parent = _partsBarCodeContentPanel,
                Ratio = 8.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            SetupBox(box);
            CommonButton btn = box.AddButton("确定");
            btn.Click += (s, e) => ValidatePartsBarCode(box);
            box.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ValidatePartsBarCode(box);
                }
            };
            return box;
        }
        private void SetupBox(CustomTextBoxButtonGroup box) {
            CustomTextBox innerBox = box.GetTextBox(0);
            innerBox.TabStop = false;
            innerBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(innerBox.Text) && innerBox.Text != _initStr) {
                    innerBox.IsError = false;
                }
            };
            TextBox iBox = innerBox.Box;
            iBox.GotFocus += (s, e) => {
                _focusedBox = box;
                if (iBox.Text == _initStr) {
                    iBox.Text = "";
                }
            };
            iBox.LostFocus += (s, e) => {
                if (iBox.Text == "") {
                    iBox.Text = _initStr;
                }
            };
        }
        private async void ValidateProductBarCodeAsync() {
            string barCode = _productBarCodeBox.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请输入或扫描条码");
                _productBarCodeBox.GetTextBox(0).IsError = true;
                return;
            }
            // 对产品码进行校验
            bool checkPassed = true;
            ProductMissionDTO? mission = null;
            // 已选任务
            if (_mission.id > 0) {
                // 校验不通过，检查是否匹配其他任务
                if (!CheckBarCodeMatchedMission(barCode)) {
                    mission = FindBarCodeMatchedMission(barCode);
                    // 没匹配到其他任务，不专门提示，只提示与当前任务不匹配
                    if (mission == null) {
                        checkPassed = false;
                        WidgetUtils.ShowWarningPopUp($"当前条码【{barCode}】与选择的任务不匹配");
                        _productBarCodeBox.GetTextBox(0).IsError = true;
                    } 
                    // 如果匹配到其他任务，则做出特定提示
                    else {
                        checkPassed = WidgetUtils.ShowConfirmPopUp($"检测到当前条码【{barCode}】与另一任务【{mission.name}】匹配，是否切换任务？");
                    }
                }
            } 
            // 没选任务
            else {
                mission = FindBarCodeMatchedMission(barCode);
                // 匹配不到任何任务，则提示没匹配上
                if (mission == null) {
                    checkPassed = false;
                    WidgetUtils.ShowWarningPopUp($"没有检索到匹配条码【{barCode}】的任务");
                    _productBarCodeBox.GetTextBox(0).IsError = true;
                }
            }
            // 条码校验通过，再检查下是否需要返工
            if (checkPassed) {
                // 如果存在前置任务，则先查询前置任务是否完成
                if (_mission.predecessor_mission_id != null) {
                    bool yes = _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(_mission.predecessor_mission_id.Value, (int) TighteningStatus.OK) { ProductBarCode = barCode }).Yes;
                    if (!yes) {
                        WidgetUtils.ShowWarningPopUp("未检测到前置任务的加工记录，请先完成前置任务");
                        checkPassed = false;
                    }
                }
                // 不管是否有前置任务，只要前面的校验过了，就查询自身的加工记录
                if (checkPassed && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(_mission.id, (int) TighteningStatus.OK) { ProductBarCode = barCode }).Yes) {
                    bool needRedo;
                    if (WidgetUtils.ShowConfirmPopUp("检测到已对该产品进行过加工，是否需要返工？")) {
                        // 需要管理员密码弹窗
                        _workplace.AdminConfirmed = false;
                        _workplace.OpenAdminPasswordPopUpForm("产品返工确认，请输入管理员密码解锁");
                        needRedo = _workplace.AdminConfirmed.Value;
                    } else {
                        needRedo = false;
                    }
                    // 需要返工，修改是否返工的标识
                    if (needRedo) {
                        _workplace.IsRedo = (int) YesOrNo.YES;
                    } else {
                        _workplace.IsRedo = (int) YesOrNo.NO;
                        // 存在确认返工的情况但取消返工，则将校验结果改为不通过
                        checkPassed = false;
                    }
                }
            }
            // 所有检查完毕，回填、或切换任务后再回填
            if (checkPassed) {
                // 存入缓存并回填到主界面
                _workplace.BarCodeObj.ProductBarCode = barCode;
                WriteBackBarCodes();
                // 禁用产品条码输入框
                _productBarCodeBox.Enabled = false;
                // 是否需要切换任务
                if (mission != null) {
                    SwitchToMission(mission);
                }
                // 如果有物料码，则聚焦于第一个物料码输入框
                if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                    _workplace.BarCodeObj.PartsRulesCount = _partsBarCodeRules[_mission.id].Count;
                }
                if (_workplace.BarCodeObj.PartsRulesCount > 0) {
                    CustomTextBoxButtonGroup firstPartsBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[0];
                    firstPartsBox.Enabled = true;
                    firstPartsBox.GetTextBox(0).Box.Focus();
                    ActiveControl = firstPartsBox.GetTextBox(0).Box;
                }
                // 检查是否可以激活任务
                if (CheckCanActivateMission()) {
                    // 激活任务
                    _workplace.ActivateMission();
                    await Task.Delay(200);
                    Hide();
                }
            } else {
                _productBarCodeBox.GetTextBox(0).IsError = true;
            }
        }
        public void ValidateProductBarCode(string barCode) {
            // 先回填，不然校验不到
            _productBarCodeBox.SetValue(0, barCode);
            // 校验条码
            ValidateProductBarCodeAsync();
        }

        public async void ValidatePartsBarCode(CustomTextBoxButtonGroup box) {
            string barCode = box.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请输入或扫描条码");
                box.GetTextBox(0).IsError = true;
                return;
            } else if (_workplace.BarCodeObj.PartsBarCodes.Contains(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请勿重复录入物料");
                box.GetTextBox(0).IsError = true;
                return;
            }

            // 物料条码校验不通过
            int ruleId = CheckPartsBarCodeMatchedMission(barCode);
            if (ruleId < 0) {
                WidgetUtils.ShowWarningPopUp($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配");
                box.GetTextBox(0).IsError = true;
            } 
            // 物料条码校验通过
            else {
                // 物料码返工确认
                if (_workplace.IsRedo != (int) YesOrNo.YES && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(_mission.id, (int) TighteningStatus.OK) { PartsBarCode = barCode }).Yes) {
                    bool needRedo;
                    if (WidgetUtils.ShowConfirmPopUp($"检测到数据库已存在此物料，是否需要返工？")) {
                        // 需要管理员密码弹窗
                        _workplace.AdminConfirmed = false;
                        _workplace.OpenAdminPasswordPopUpForm("物料返工确认。请输入管理员密码解锁。");
                        needRedo = _workplace.AdminConfirmed.Value;
                    } else {
                        needRedo = false;
                    }
                    // 需要返工，修改是否返工的标识
                    if (needRedo) {
                        // 由于追溯码也有这个校验，因此如果不需要返工，则不动已经校验过的状态
                        _workplace.IsRedo = (int) YesOrNo.YES;
                    } else {
                        box.GetTextBox(0).IsError = true;
                        return;
                    }
                }
                // 存入缓存并回填到主界面
                _workplace.BarCodeObj.PartsBarCodes.Add(barCode);
                _workplace.BarCodeObj.PartsMatchingRulesCached.Add(ruleId);
                WriteBackBarCodes();
                // 禁用当前输入框
                box.Enabled = false;
                // 如果还有下一个物料需要录入，则自动聚焦到下一个物料输入框
                if (_workplace.BarCodeObj.PartsBarCodes.Count < _workplace.BarCodeObj.PartsRulesCount) {
                    CustomTextBoxButtonGroup nextBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
                    nextBox.Enabled = true;
                    nextBox.GetTextBox(0).Box.Focus();
                    ActiveControl = nextBox.GetTextBox(0).Box;
                }
                // 检查是否可以激活任务
                if (CheckCanActivateMission()) {
                    // 激活任务
                    _workplace.ActivateMission();
                    await Task.Delay(1000);
                    Hide();
                }
            }
        }
        public void ValidatePartsBarCode(string barCode) {
            if (_focusedBox == null) {
                _focusedBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
            }
            // 先回填，不然校验不到
            _focusedBox.SetValue(0, barCode);
            // 校验条码
            ValidatePartsBarCode(_focusedBox);
        }
        private void WriteBackBarCodes() {
            string barCodes = _workplace.BarCodeObj.ProductBarCode;
            if (_workplace.BarCodeObj.PartsBarCodes.Count > 0) {
                barCodes += " | " + string.Join(", ", _workplace.BarCodeObj.PartsBarCodes);
            }
            _workplace.BarCodeTextBox.Text = barCodes;
        }

        // 检查当前条码是否与当前已选择任务匹配（没有选择任务则不匹配）
        private bool CheckBarCodeMatchedMission(string barCode) {
            if (_productBarCodeRules.ContainsKey(_mission.id)) {
                foreach (BarCodeMatchingRuleDTO rule in _productBarCodeRules[_mission.id]) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule)) {
                        return true;
                    }
                }
                return false;
            }
            // 如果没有配置条码匹配规则，则也可以通过
            return true;
        }
        // 检查当前条码是否与数据库中任意任务的产品码/追溯码匹配
        private ProductMissionDTO? FindBarCodeMatchedMission(string barCode) {
            ProductMissionDTO? mission = null;
            foreach (KeyValuePair<int, List<BarCodeMatchingRuleDTO>> pair in _productBarCodeRules) {
                foreach (BarCodeMatchingRuleDTO rule in pair.Value) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule.end_char, rule.length, MainUtils.GetKeyMatchingRule(rule.key_position, rule.key_char))) {
                        mission = _workplace.Apis.QueryProductMissionDetail(new(pair.Key)).ProductMissionDTO;
                        break;
                    }
                }
                if (mission != null) {
                    break;
                }
            }
            return mission;
        }
        // 检查当前物料条码是否匹配当前任务
        private int CheckPartsBarCodeMatchedMission(string barCode) {
            if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                // 校验时需要剔除已经校验过的规则，以免扫过的码重复也能通过
                foreach (BarCodeMatchingRuleDTO rule in _partsBarCodeRules[_mission.id].Where(r => !_workplace.BarCodeObj.PartsMatchingRulesCached.Contains(r.id))) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule)) {
                        return rule.id;
                    }
                }
                return -1;
            }
            // 如果没有配置条码匹配规则，则也可以通过
            return 0;
        }

        // 检查是否可以激活任务
        public bool CheckCanActivateMission() {
            // 没选任务，pass
            if (_mission.id > 0) {
                // 没录入产品码，pass
                if (!string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                    // 配置了物料码但是录入的物料码与配置的数量不一致，pass
                    if (!_partsBarCodeRules.ContainsKey(_mission.id) || _partsBarCodeRules[_mission.id].Count == _workplace.BarCodeObj.PartsBarCodes.Count) {
                        // 重置所有带红框提示的输入框
                        _productBarCodeBox.GetTextBox(0).IsError = false;
                        foreach (Control ctrl in _partsBarCodeContentPanel.Controls) {
                            ((CustomTextBoxButtonGroup) ctrl).GetTextBox(0).IsError = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        // 根据传入的条码智能校验
        public void ValidateBarCode(string barCode) {
            // 如果没有录入产品码，则校验产品码
            if (string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                ValidateProductBarCode(barCode);
            }
            // 否则校验物料码
            else {
                ValidatePartsBarCode(barCode);
            }
        }

        public new void Show() {
            // 弹窗会阻塞，因此异步判断可以让里面可能出现的弹窗不阻塞后面的逻辑
            BeginInvoke(new(() => {
                if (_barCode != null) {
                    ValidateBarCode(_barCode);
                }
            }));
            base.Show();
        }

        public void ResizeSelf() {
            CalculateDetailProperties();

            Size mainSize = WidgetUtils.MainSize;
            Padding contentPadding = ContentPanel.Padding;
            int contentWidth = (int) (mainSize.Width * .7);
            int contentHeight = 0;

            int titleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
            int titleVPadding = titleHeight / 5;
            Size titleSize = new(contentWidth - contentPadding.Size.Width, titleHeight);

            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxPadding = boxHeight / 5;
            int innerContentWidth = contentWidth - contentPadding.Size.Width;
            Size boxSize = new(innerContentWidth - boxPadding * 2, boxHeight);

            int productPanelHeight = (boxHeight + boxPadding * 2) * _productBarCodeContentPanel.Controls.Count;
            int partsPanelHeight = (boxHeight + boxPadding * 2) * _partsBarCodeContentPanel.Controls.Count;

            _productBarCodeTitle.Size = titleSize;
            _partsBarCodeTitle.Size = titleSize;
            _partsBarCodeTitle.Margin = new(0, titleVPadding, 0, 0);;

            foreach (Control ctrl in _productBarCodeContentPanel.Controls) {
                CustomTextBoxButtonGroup box = (CustomTextBoxButtonGroup) ctrl;
                box.Size = boxSize;
                box.Margin = new(boxPadding);
            }
            _productBarCodeContentPanel.Size = new(innerContentWidth, productPanelHeight);

            foreach (Control ctrl in _partsBarCodeContentPanel.Controls) {
                CustomTextBoxButtonGroup box = (CustomTextBoxButtonGroup) ctrl;
                box.Size = boxSize;
                box.Margin = new(boxPadding);
            }
            _partsBarCodeContentPanel.Size = new(innerContentWidth, partsPanelHeight);

            contentHeight += (titleHeight + titleVPadding) * 2 + contentPadding.Size.Height;
            contentHeight += productPanelHeight + partsPanelHeight;
            SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
        }
    }

    public class WorkingProcessPanel: Panel {
        private readonly int _loopingInterval       = 50;
        public readonly string TighteningDesc       = "正在拧紧{0}号螺丝";
        public readonly string LooseningDesc        = "正在反松{0}号螺丝";
        public readonly string ArmPositionError     = "力臂未在指定位置";
        public readonly string AdminConfirmation    = "需要管理员确认";
        public readonly string PsetNullError        = "{0}号螺丝未配置程序号，工具锁定";
        public readonly string PsetFailedError      = "{0}号螺丝程序号下发失败，工具锁定";
        public readonly string PsetNotMatchedError  = "{0}号螺丝程序号与控制器不匹配，工具锁定";
        public string? CustomError                  = null;

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
        private List<string> _descList = new();

        private WorkplaceProcessStatus _workplaceProcessStatus;
        private int? _boltSerialNum;
        private TightenOrLoosen _tightenOrLoosen;

        public TightenOrLoosen TightenOrLoosen { 
            get => _tightenOrLoosen; 
            set {
                _tightenOrLoosen = value; 
                InvokeResizing();
            }
        }
        public int? BoltSerialNum { get => _boltSerialNum; set => _boltSerialNum = value; }
        public WorkplaceProcessStatus WorkplaceProcessStatus {
            get => _workplaceProcessStatus;
            set {
                _workplaceProcessStatus = value;
                switch (_workplaceProcessStatus) {
                    case WorkplaceProcessStatus.UNACTIVATED:
                        _statusTxt = "未激活";
                        ClearDesc();
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.ACTIVATED:
                        _statusTxt = "已激活";
                        ClearDesc();
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.OPERATION_ENABLE:
                        if (_tightenOrLoosen == TightenOrLoosen.TIGHTENING) {
                            SetDesc(TighteningDesc);
                        } else {
                            SetDesc(LooseningDesc);
                        }
                        _picturePanel.Visible = true;
                        break;
                    case WorkplaceProcessStatus.OPERATION_DISABLE:
                        _statusTxt = "已锁定";
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.FINISHED_NG:
                        _statusTxt = "NG";
                        SetDesc("任务失败");
                        _picturePanel.Visible = false;
                        break;
                    case WorkplaceProcessStatus.FINISHED_OK:
                        _statusTxt = "OK";
                        SetDesc("任务完成");
                        _picturePanel.Visible = false;
                        break;
                    default:
                        break;
                }
            }
        }
        public void SetDesc(string desc) {
            if (!string.IsNullOrEmpty(desc)) {
                ClearDesc();
                _descList.Add(desc);
                _statusDesc = desc;
                if (_boltSerialNum != null) {
                    _statusDesc = string.Format(_statusDesc, _boltSerialNum);
                }
            }
        }
        public void AppendDesc(string? desc) {
            if (!string.IsNullOrEmpty(desc)) {
                if (!_descList.Contains(desc)) {
                    _descList.Add(desc);
                }
                _statusDesc = string.Join("\r\n", _descList);
                if (_boltSerialNum != null) {
                    _statusDesc = string.Format(_statusDesc, _boltSerialNum);
                }
            }
        }
        public void RemoveDesc(string desc) {
            if (!string.IsNullOrEmpty(desc)) {
                if (_descList.Contains(desc)) {
                    _descList.Remove(desc);
                }
                _statusDesc = string.Join("\r\n", _descList);
                if (_boltSerialNum != null) {
                    _statusDesc = string.Format(_statusDesc, _boltSerialNum);
                }
            }
        }
        public void ClearDesc() {
            _descList.Clear();
            _statusDesc = string.Empty;
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

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            if (IsHandleCreated && !IsDisposed) {
                InvokeResizing();
            }
        }

        private void InvokeResizing() {
            _borderRect.Size = Size;
            _borderSize = Width / 40 + Height / 80;
            _picturePanelHeight = (int) ((Height - _borderSize * 2) * .6F);
            _picturePanel.Size = new(Width - _borderSize * 2, _picturePanelHeight);
            _picturePanel.Location = new(_borderSize, _borderSize);

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
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.Clear(BackColor);
            _statusFont = WidgetUtils.GetProperFont(Size, _statusTxt, .325f);
            _statusDescFont = WidgetUtils.GetProperFont(Size, _statusDesc, .1f - _descList.Count * .005F);
            int statusWidth = (int) (graphics.MeasureString(_statusTxt, _statusFont).Width);
            int statusDescWidth = (int) (graphics.MeasureString(_statusDesc, _statusDescFont).Width);
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
                    graphics.DrawRectangle(new(_correctColor, _borderSize), _borderRect);
                    string descShowing = _statusDesc;
                    // 设置当前点位信息
                    if (_boltSerialNum != null) {
                        descShowing = string.Format(descShowing, _boltSerialNum);
                    }
                    // 使用 StringFormat 进行居中时，是以坐标点位为中心，因此 x，y 都要设置为中心点
                    statusDescPoint = new Point(Width / 2, otherHeihgt + (Height - otherHeihgt) / 2);
                    graphics.DrawString(descShowing, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_GREEN), statusDescPoint, stringFormat);
                    break;
                case WorkplaceProcessStatus.OPERATION_DISABLE:
                case WorkplaceProcessStatus.FINISHED_NG:
                case WorkplaceProcessStatus.FINISHED_OK:
                    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 3 - _descList.Count * (int) (_statusFont.Height * 0.05));
                    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                    // 使用 StringFormat 进行居中时，是以坐标点位为中心，因此 x，y 都要设置为中心点
                    statusDescPoint = new Point(Width / 2, otherHeihgt + (Height - otherHeihgt) / 2);
                    graphics.DrawString(_statusDesc, _statusDescFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusDescPoint, stringFormat);
                    break;
                //case WorkplaceProcessStatus.FINISHED_NG:
                //case WorkplaceProcessStatus.FINISHED_OK:
                //    statusPoint = new Point((Width - statusWidth) / 2, (Height - _statusFont.Height) / 2);
                //    graphics.DrawString(_statusTxt, _statusFont, new SolidBrush(ColorConfigs.COLOR_WORKING_PROCESS_WHITE), statusPoint);
                //    break;
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
            Icon = CommonUtils.ImageBase64ToImage(category.IconEmptyStr);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _borderRect = new(0, 0, Width, Height);
        }

        public void ResetIconByStatus(DeviceStatus status) {
            switch (status) {
                case DeviceStatus.NORMAL:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconStr);
                    break;
                case DeviceStatus.ERROR:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconErrorStr);
                    break;
                case DeviceStatus.EMPTY:
                    Icon = CommonUtils.ImageBase64ToImage(_category.IconEmptyStr);
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
                ImageShowing = WidgetUtils.ResizeImage(Icon, newSize);
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
                        icon = WidgetUtils.ResizeImage(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImage(_statusIconDisconnected, imageSide, imageSide);
                    }
                    int imageY = (_panelHeight - imageSide) / 2;
                    g.DrawImage(icon, new Point(0, imageY));
                    g.DrawString($"{task.Ip} : {task.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), imageY));
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
            foreach (KeyValuePair<int, ArmTask> pair in _armTasks) {
                int armId = pair.Key;
                ArmTask task = pair.Value;
                WorkstationDTO? dto = _workstationDTOs.SingleOrDefault(dto => dto.arm_id == armId);
                CoordinatesPanel panel = new(dto != null ? dto.name : "未配置站点", _panelHeight, ResetCoordinatesPositionX) {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, this._panelHeight);
                };
                armPanels.Add(panel);
                // Bind delegate 
                task.OnActionAfterReceiving += panel.SetCoordinates;
                // Remove delegate
                panel.HandleDestroyed += (sender, eventArgs) => {
                    task.OnActionAfterReceiving -= panel.SetCoordinates;
                };
            }
            void ResetCoordinatesPositionX() {
                int maxX = armPanels.Select(p => p.CoordinatesX).Max();
                foreach (CoordinatesPanel panel in armPanels) {
                    if (panel.CoordinatesX < maxX) {
                        panel.CoordinatesX = maxX;
                    }
                }
            }
        }

        private class CoordinatesPanel: CustomContentPanel {
            private int _panelHeight;
            private string _content;
            private int _coordinatesX = 0;
            private Action _resetPositionX;

            public string XStr { get; set; }
            public string YStr { get; set; }
            public string ZStr { get; set; }
            public int CoordinatesX { get => _coordinatesX; set => _coordinatesX = value; }

            public CoordinatesPanel(string workstationName, int panelHeight, Action resetPositionX) {
                _content = $"站点：{workstationName}";
                _panelHeight = panelHeight;
                _resetPositionX = resetPositionX;

                XStr = "0";
                YStr = "0";
                ZStr = "0";
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                base.ResizeChildren(sender, eventArgs);
                Font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);
                _coordinatesX = (int) (TextRenderer.MeasureText(_content, Font).Width * 1.2);
                _resetPositionX();
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                Graphics g = e.Graphics;

                string coordinates = $"坐标：  X-{XStr}    Y-{YStr}";
                if (ZStr != "0") {
                    coordinates += $"    Z-{ZStr}";
                }
                g.DrawString(_content, Font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point(0, 0));
                g.DrawString(coordinates, Font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point(_coordinatesX, 0));
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
                        icon = WidgetUtils.ResizeImage(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImage(_statusIconDisconnected, imageSide, imageSide);
                    }
                    int imageY = (_panelHeight - imageSide) / 2;
                    g.DrawImage(icon, new Point(0, imageY));
                    g.DrawString($"{task.Ip} : {task.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), imageY));
                };
            }
        }
    }

    public class ToolOperationPopUpForm: CustomPopUpForm {
        private ILog logger = MainUtils.GetLogger(typeof(ToolOperationPopUpForm));

        private List<WorkstationDTO> _workstationDTOs;
        private Dictionary<int, ToolTask> _toolTasks;
        private BoltButton? _currentWorkingBolt;
        private WorkingProcessPanel _workingProcessPanel;
        private Action _setPset;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private CustomComboBoxGroup<int> _stationComboBox;
        private CustomTextBoxGroup _parameterSetTextBox;

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }

        public ToolOperationPopUpForm(BoltButton? currentWorkingBolt, Action setPset, string categoryName, WorkingProcessPanel workingProcessPanel,
                List<WorkstationDTO> workstationDTOs, Dictionary<int, ToolTask> toolTasks, int? currentWorkstationId = null, int? currentPset = null) {
            _workstationDTOs = workstationDTOs;
            _toolTasks = toolTasks;
            _currentWorkingBolt = currentWorkingBolt;
            _workingProcessPanel = workingProcessPanel;
            _setPset = setPset;

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
                PositiveIntOnly = true,
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
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 工具已经是锁止状态\r\n2. 未给当前工具型号配置命令");
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
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 工具已经是解锁状态\r\n2. 未给当前工具型号配置命令\r\n3. 控制器未配置【程序{parameterSet}】, 工具锁定");
                    }
                });
            };
            CommonButton btnPSet = AddButton("下发");
            btnPSet.Click += (s, e) => {
                SendCommand(async toolTask => {
                    string parameterSet = _parameterSetTextBox.GetTextBox(0).Text;
                    int pset = int.Parse(parameterSet);
                    if (await toolTask.SendPSetAsync(pset)) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                        // 下发成功自动解锁
                        toolTask.SendUnlock();
                        canUnlock = true;
                        if (_currentWorkingBolt != null) {
                            _currentWorkingBolt.CurrentParameterSet = pset;
                            _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetFailedError);
                            _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetNullError);
                            _setPset();
                            // 如果当前没有点位，则代表任务未激活，因此不关闭弹窗
                            Dispose();
                        }
                    } else {
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 未给当前工具型号配置命令\r\n2. 控制器未配置【程序{parameterSet}】, 工具锁定");
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
                        WidgetUtils.ShowErrorPopUp($"操作失败！未找到当前站点配置的工具。");
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
                        icon = WidgetUtils.ResizeImage(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImage(_statusIconDisconnected, imageSide, imageSide);
                    }
                    int imageY = (_panelHeight - imageSide) / 2;
                    g.DrawImage(icon, new Point(0, imageY));
                    g.DrawString($"{task.PortName} - {task.FullName}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), imageY));
                };
            }
        }
    }

    public class CommunicationDetailFloatingForm: CustomFloatingForm {
        private readonly Image _statusIconConnected = Properties.Resources.device_connected;
        private readonly Image _statusIconDisconnected = Properties.Resources.device_disconnected;

        private int _panelHeight;

        public CommunicationDetailFloatingForm(string categoryName, Dictionary<int, CommunicationTask> armTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            ContentPanel.FlowDirection = FlowDirection.TopDown;
            _panelHeight = panelHeight;

            DisplayCommunicationDetails(armTasks);
        }

        private void DisplayCommunicationDetails(Dictionary<int, CommunicationTask> armTasks) {
            Font font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (KeyValuePair<int, CommunicationTask> armTask in armTasks) {
                CustomContentPanel panel = new() {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                };
                panel.Paint += (sender, eventArgs) => {
                    Graphics g = eventArgs.Graphics;
                    Image icon;
                    CommunicationTask task = armTask.Value;
                    int imageSide = (int) (_panelHeight * .8);
                    if (task.Connected) {
                        icon = WidgetUtils.ResizeImage(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImage(_statusIconDisconnected, imageSide, imageSide);
                    }
                    int imageY = (_panelHeight - imageSide) / 2;
                    g.DrawImage(icon, new Point(0, imageY));
                    g.DrawString($"{task.Ip} : {task.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), imageY));
                };
            }
        }
    }

    public class BarCodeObj {
        public string ProductBarCode { get; set; } = string.Empty;
        public List<string> PartsBarCodes { get; } = new();
        public List<int> PartsMatchingRulesCached { get; } = new();
        public int PartsRulesCount { get; set; } = 0;

        public void Reset() {
            ProductBarCode = string.Empty;
            PartsBarCodes.Clear();
            PartsMatchingRulesCached.Clear();
            PartsRulesCount = 0;
        }
    }
}
