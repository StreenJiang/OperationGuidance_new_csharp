using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Extensions;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.Collections.Concurrent;
using System.Reflection;

namespace OperationGuidance_new.Views.AbstractViews {
    public abstract class AWorkplaceContentPanel: CustomContentPanel {
        protected ILog logger;

        #region Fields
        private readonly object _syncLock = new object();
        protected OperationGuidanceApis _apis;
        public ProductMissionDTO _mission;
        protected Action<string> _resetMissionName;
        protected volatile bool _activated;
        public Image _defaultImage;
        protected readonly int _lockCheckingTaskDelay = 50;
        protected readonly int _checkDevicesConnectionDelay = 2000;
        protected readonly int _resendPsetMaxTimes = 5;
        protected readonly int _resendSignalToArrangerMaxTimes = 5;
        protected readonly int _resendSignalToSetterSelectorMaxTimes = 5;
        protected readonly int _checkIoBoxSignalDelay = 100;

        protected List<WorkstationDTO> _workstationsDTOs = new();
        protected List<DeviceArmDTO> _arms;
        protected List<DeviceToolDTO> _tools;
        protected List<DeviceSerialPortDTO> _serialPorts;
        protected List<DeviceCommunicationDTO> _communications;
        protected List<DeviceIoDTO> _ioBoxes;
        protected Dictionary<int, ToolTask> _toolTasks = new();
        protected Dictionary<int, SerialPortTask> _serialPortTasks = new();
        protected Dictionary<int, CommunicationTask> _communicationTasks = new();
        protected Dictionary<string, IoBoxTask> _ioBoxTasks;
        protected CommunicationTask? _communicationTask;
        protected MissionRecordDTO? _missionRecord;
        protected bool _needLoosening = false;
        protected bool? _adminConfirmed = null;
        protected CustomPopUpForm? _adminPasswordPopUpForm;
        protected int _isRedo = (int) YesOrNo.NO;
        protected Coordinates3D? _realTimeArmCoordinates;
        protected readonly object DataStorageLockObj = new();

        protected bool _locating_enabled;
        protected int _armLocatingAccuracy;

        // 任务取消相关
        protected CancellationTokenSource _activeMissionCts = new();
        protected List<CancellationTokenSource> _backgroundTaskCts = new();

        // Widgets
        protected Image _barCodeImage;
        protected PictureBox _barCodePictureBox;

        public ProductImageDisplayPanel _productImageDisplayPanel;
        protected List<ProductImageFile> _productImageFiles;
        public List<Image?> _missionImages;

        protected Label _operatorInfoTitle;
        protected CustomTextBoxGroup _operatorName;
        protected CustomTextBoxGroup _operatorId;

        protected DataPanel _torquePanel;
        protected DataPanel _anglePanel;
        protected Label _torqueTitle;
        protected Label _torque;
        protected Label _angleTitle;
        protected Label _angle;

        protected Label _missionDetailTitle;
        protected CustomTextBoxGroup _productBatch;
        protected CustomTextBoxButtonGroup _missionSelectedName;
        protected CustomTextBoxGroup _productSumPerDay;
        protected CustomTextBoxGroup _okSumPerDay;
        protected CustomTextBoxGroup _ngRatePerDay;
        protected CustomTextBoxGroup _pset;
        protected CustomTextBoxButtonGroup _currentSideName;

        protected DataGridViewPanel<OperationDataVO> _tighteningDataPanel;
        protected ConcurrentBag<OperationDataVO> _tighteningDataVOs = new();

        protected WorkingProcessPanel _workingProcessPanel;

        protected CustomContentPanel _timeDisplayerOuter;
        protected Label _timeDisplayer;
        protected System.Windows.Forms.Timer _timeDisplayerTimer;

        // 条码相关
        public bool _checkRedo = false;
        protected ABarCodeInputPopUpForm? _barCodePopUpForm;
        protected readonly BarCodeObj _barCodeObj = new();
        protected CustomTextBox _barCodeTextBox;
        // 条码匹配规则
        protected List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _productBarCodeMatchingRules;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _partsBarCodeMatchingRules;
        protected List<BarCodeMatchingRuleDTO> _rulesExcluded;
        protected List<int>? _ruleIdsCheckedCached;
        protected bool _barcodeRelatedDone;

        // 设备相关
        protected List<DeviceBlock> _deviceBlocks;
        protected bool _toolControlNeedAdminPasswor;
        protected Action? _actionAfterSendingPset;
        protected ModBusServerBase? ModBusServer;
        protected PlcServerBase? plcServer;

        // 产品面相关
        public int _currentSideIndex;
        protected List<ProductSideDTO> _sides;
        private SidePopUpForm? _sidePopUpForm = null;

        // 点位相关
        protected Dictionary<int, List<BoltButton>> _allBolts; // side_id - bolts
        protected Dictionary<int, Dictionary<int, List<BoltButton>>> _allBoltsIndependence; // side_id - workstation_id - bolts
        protected List<BoltButton> _showingBoltButtons; // Cache variable that used for changing side
        protected BoltButton? _currentWorkingBolt;
        protected Dictionary<int, BoltButton> _currentWorkingBoltIndependence = new();
        protected BoltPopUpForm _boltPopUpForm; // 如果以后要支持软件尺寸可拖拽改变，则需要在打开时动态改变
        protected String? _matCode;
        protected int? _rundownTime;
        protected string? _errorMsg;
        protected int _sumBoltDone = 0;
        // Specification
        protected bool _arrangerNeeded = false;
        protected Dictionary<float, bool>? _arrangerPositionOk = null;
        protected bool _arrangerPositionTimedOut = false;
        private int _resendSignalToArrangerTimes = 0;
        private List<float> _specifications;
        private List<int> _arrangerIds;
        // BitSpecification
        protected bool _setterSelectorNeeded = false;
        protected bool? _bitPositionOk;
        protected bool _bitPositionTimedOut = false;
        protected int _resendSignalToSetterSelectorTimes = 0;

        // 任务相关
        protected List<String> lockMsgs = new();
        protected List<String> informationMsgs = new();
        protected OperationDataDTO? currentOperationData;
        #endregion

        #region Properties
        public OperationGuidanceApis Apis { get => _apis; set => _apis = value; }
        public bool Activated { get => _activated; set => _activated = value; }
        public bool? AdminConfirmed { get => _adminConfirmed; set => _adminConfirmed = value; }
        public int IsRedo { get => _isRedo; set => _isRedo = value; }
        public BarCodeObj BarCodeObj => _barCodeObj;
        public CustomTextBox BarCodeTextBox { get => _barCodeTextBox; set => _barCodeTextBox = value; }
        public MissionRecordDTO? MissionRecord => _missionRecord;
        #endregion

        public AWorkplaceContentPanel() { }
        public AWorkplaceContentPanel(int? missionId, Action<string> resetMissionName) : base() {
            logger = MainUtils.GetLogger(GetType());
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
            _defaultImage = Properties.Resources.image_choose;
            _currentSideIndex = 0;
            _sides = new();

            _locating_enabled = MainUtils.IsArmLocatingEnabled();
            _armLocatingAccuracy = MainUtils.GetArmLocatingAccuracy();

            _missionImages = new();
            _productImageFiles = new();
            _allBolts = new();
            _allBoltsIndependence = new();

            InitializeBarCodePanel();
            InitializeImageDisplayPanel();
            InitializeUserInfoPanel();
            InitializeWorkingProcessPanel();
            InitializeTighteningDataPanel();
            InitializeMissionInfoPanel();
            InitializeDataGridPanel();
            InitializeDeviceBlocks();
            InitializeTimeDisplayer();

            ActionAfterAllInitialized();
        }

        protected virtual void ActionAfterAllInitialized() { }

        protected virtual void InitializeBarCodePanel() {
            _barCodeImage = Properties.Resources.bar_code_icon;
            _barCodePictureBox = new() {
                Margin = new(0),
                Padding = new(0),
            };
            _barCodeTextBox = new() {
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                DisabledBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
            _barCodeTextBox.Enabled = false;
            _barCodePictureBox.Click += barCodePopUp;
            _barCodeTextBox.Click += barCodePopUp;

        }
        protected void barCodePopUp(object? s, EventArgs e) {
            OpenBarCodePopUpForm();
        }

        private void InitializeImageDisplayPanel() {
            _productImageDisplayPanel = new(_defaultImage) {
                Margin = new(1, 1, 0, 0),
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
            };
            _productImageDisplayPanel.ParentChanged += (s, e) => {
                if (_productImageDisplayPanel.Parent != null) {
                    SetProductImagePanel();
                }
            };
        }

        private void InitializeUserInfoPanel() {
            _operatorInfoTitle = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "操作员信息",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            _operatorName = new("操作员") {
                ReadOnly = true,
                Enabled = false,
            };
            _operatorId = new("员工号") {
                ReadOnly = true,
                Enabled = false,
            };
            SetOperatorInfo();
        }
        protected void SetOperatorInfo() {
            _operatorName.SetValue(0, SystemUtils.LoggedUserName);
            _operatorId.SetValue(0, SystemUtils.LoggedUserId + "");
        }

        private void InitializeWorkingProcessPanel() {
            _workingProcessPanel = new() {
                Margin = new(0),
                Padding = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
            };
        }

        private void InitializeTighteningDataPanel() {
            _torquePanel = new("扭矩（N*m）") {
                Data = "0.00",
                ConerRadius = WidgetUtils.ContainerRadius(),
            };
            _anglePanel = new("角度（°）") {
                Data = "0",
                ConerRadius = WidgetUtils.ContainerRadius(),
            };


            _torqueTitle = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "扭矩（N*m）",
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _torque = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "0.0",
                TextAlign = ContentAlignment.MiddleRight,
            };
            _angleTitle = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "角度（°）",
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _angle = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "0",
                TextAlign = ContentAlignment.MiddleRight,
            };
        }

        private void InitializeMissionInfoPanel() {
            _missionDetailTitle = new() {
                Margin = new(1),
                Padding = new(0),
                Text = "任务信息",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            _productBatch = new("产品批次") {
                NameAlignment = HorizontalAlignment.Right,
            };
            _productBatch.GetTextBox(0).Box.TextChanged += (s, e) => {
                _productBatch.GetTextBox(0).IsError = false;
                if (_activated && _missionRecord != null && _productBatch.GetTextBox(0).Box.Text != _missionRecord.product_batch) {
                    WidgetUtils.ShowErrorPopUp("任务已激活，无法修改产品批次");
                    _productBatch.GetTextBox(0).Box.Text = _missionRecord.product_batch;
                }
            };
            _missionSelectedName = new("任务名称") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };
            CommonButton missionSelectBtn = _missionSelectedName.AddButton<CommonButton>("切换");
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
            _currentSideName = new("产品面") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };
            CommonButton sidesDetials = _currentSideName.AddButton<CommonButton>("详情");
            sidesDetials.Enabled = true;
            sidesDetials.Click += (s, e) => {
                PopUpSideListForm(sidesDetials);
            };
            _productSumPerDay = new("今日生产") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };
            _okSumPerDay = new("合格数") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };
            _ngRatePerDay = new("不良品率") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };
            _pset = new("程序号") {
                ReadOnly = true,
                Enabled = false,
                NameAlignment = HorizontalAlignment.Right,
            };

            SetMissionDetails();
        }
        private void PopUpMissionListForm(List<ProductMissionDTO> missions) {
            Size contentSize = new((int) (WidgetUtils.MainSize.Width * .7), (int) (WidgetUtils.MainSize.Height * .7));
            CustomPopUpForm popUpForm = new() {
                Title = "选择任务",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
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

        protected virtual void ActionAfterSwitchMission() {
            // If is self looping mode, then activate mission automatically
            ActivateMissionAutomatically();
        }

        private void PopUpSideListForm(CommonButton sidesDetialsBtn) {
            _sidePopUpForm = new(this) {
                Title = "产品面详情",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                ButtonAlignment = HorizontalAlignment.Right,
            };
            _sidePopUpForm.AddButton("关闭").Click += (s, e) => _sidePopUpForm.Dispose();
            _sidePopUpForm.PretendToShowToCreateHandlesForChildren();
            _sidePopUpForm.ResizeSelf();
            Point point = sidesDetialsBtn.PointToScreen(Point.Empty);
            _sidePopUpForm.Location = new(point.X - _sidePopUpForm.Width, point.Y - _sidePopUpForm.Height + sidesDetialsBtn.Height);
            _sidePopUpForm.Show();
        }
        public virtual void SwitchToMission(ProductMissionDTO mission) {
            _mission = mission;
            _resetMissionName(_mission.name);
            _missionSelectedName.SetValue(0, _mission.name);
            SetProductImagePanel();
            RefreshImageDisplayPanel();
            _currentSideName.SetValue(0, _sides[_currentSideIndex].name);

            ActionAfterSwitchMission();
        }
        public virtual void ChangeSideAndInvalidate() {
            // List<BoltButton> currentSideBolts;
            if (CheckIfIsMultiDeviceIndependenceMode()) {
                // currentSideBolts = _allBoltsIndependence[_sides[_currentSideIndex].id][workstationId];
            } else {
                // currentSideBolts = _allBolts[_sides[_currentSideIndex].id];

                if (_currentWorkingBolt != null) {
                    if (_currentWorkingBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                        _currentWorkingBolt.ShowingWhileWorking = false;
                    } else {
                        _currentWorkingBolt.ShowingWhileWorking = true;
                    }
                }

                // 切换side后也切换点位
                _showingBoltButtons.ForEach(btn => btn.Visible = false);
                _showingBoltButtons = _allBolts[_sides[_currentSideIndex].id];
                _showingBoltButtons.ForEach(btn => btn.Visible = true);
            }

            // 切换产品图片
            _productImageDisplayPanel.SetImage(_productImageFiles[_currentSideIndex].Image, _productImageFiles[_currentSideIndex].CenterLocation);
            _productImageFiles[_currentSideIndex].RefreshImage();

            // Change side name
            _currentSideName.SetValue(0, _sides[_currentSideIndex].name);

            if (_sidePopUpForm != null && !_sidePopUpForm.IsDisposed) {
                _sidePopUpForm.ResetImage();
            }
        }
        protected abstract void RefreshImageDisplayPanel();
        protected abstract void SetMissionDetails();

        private void InitializeDataGridPanel() {
            _tighteningDataPanel = new(gridView => {
                DataGridViewColumn[] columnRange = { };
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
                HeaderHeight = WidgetUtils.WorkplaceGridViewHeaderHeight(),
                RowsHeight = WidgetUtils.WorkplaceGridViewContentRowHeight(),
                PageHeight = WidgetUtils.WorkplaceGridViewPageInfoHeight(),
                ColumnsPaddingRatio = WidgetUtils.WorkplaceGridViewColumnsPaddingRatio(),
                AutoDown = true,
            };
            _tighteningDataPanel.HandleCreated += (s, e) => {
                _tighteningDataPanel.DataSource = _tighteningDataVOs.ToList();
            };
        }

        protected virtual List<DeviceCategory>? CustomCategories() {
            return null;
        }

        private void InitializeDeviceBlocks() {
            _toolControlNeedAdminPasswor = false;
            _deviceBlocks = new();
            List<DeviceCategory> deviceCategories = new();
            // Reverse because of RightToLeft flow direction
            for (int i = DeviceCategories.Elements.Count - 1; i >= 0; i--) {
                deviceCategories.Add(DeviceCategories.Elements[i]);
            }

            List<DeviceCategory>? customCategoreis = CustomCategories();
            if (customCategoreis != null) {
                deviceCategories.AddRange(customCategoreis);
            }

            foreach (DeviceCategory category in deviceCategories) {
                DeviceBlock deviceBlock = new(category) {
                    Margin = new(0),
                    Padding = new(0),
                    BlockHoverUp = true,
                    BlockHoverDown = true,
                    BackColor = ColorConfigs.COLOR_DEVICE_BLOCK_BACKGROUND,
                    ToggledColor = ColorConfigs.COLOR_DEVICE_BLOCK_TOGGLED,
                };
                deviceBlock.MouseMove += (sender, eventArg) => {
                    if (deviceBlock.FloatingForm == null || deviceBlock.FloatingForm.IsDisposed) {
                        int panelHeight = WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight();
                        Size contentSize = new();
                        contentSize.Width = (int) (WidgetUtils.MainSize.Width * .25);
                        if (deviceBlock.Category == DeviceCategories.TOOL) {
                            if (_toolTasks.Count > 0) {
                                deviceBlock.BlockHoverUp = false;
                                deviceBlock.BlockHoverDown = false;
                                deviceBlock.FloatingForm = new ToolDetailFloatingForm(deviceBlock.CategoryName, _toolTasks, panelHeight);
                                contentSize.Height = panelHeight * _toolTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.ARM) {
                            List<IoBoxTask> tasks = _ioBoxTasks.Values.ToList().Where(task => task.ArmType != null).ToList();
                            if (tasks.Count > 0) {
                                deviceBlock.BlockHoverUp = false;
                                deviceBlock.BlockHoverDown = false;
                                deviceBlock.FloatingForm = new ArmDetailFloatingForm(deviceBlock.CategoryName, tasks, panelHeight);
                                contentSize.Height = panelHeight * tasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.SERIAL_PORT) {
                            if (_serialPortTasks.Count > 0) {
                                contentSize.Width = (int) (WidgetUtils.MainSize.Width * .475);
                                deviceBlock.FloatingForm = new SerialPortDetailFloatingForm(deviceBlock.CategoryName, _serialPortTasks, panelHeight);
                                contentSize.Height = panelHeight * _serialPortTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.COMMUNICATION) {
                            if (_communicationTasks.Count > 0) {
                                deviceBlock.FloatingForm = new CommunicationDetailFloatingForm(deviceBlock.CategoryName, _communicationTasks, panelHeight);
                                contentSize.Height = panelHeight * _communicationTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
                        } else if (deviceBlock.Category == DeviceCategories.IOBOX_ARRANGER) {
                            // TODO:
                        } else if (deviceBlock.Category == DeviceCategories.IOBOX_SETTERSELECTOR) {
                            // TODO:
                        } else {
                            // TODO:
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
                                    if (_toolControlNeedAdminPasswor) {
                                        bool confirmed = OpenAdminPasswordPopUpForm("手动控制工具。需要管理员操作密码", false);
                                        if (!confirmed) {
                                            return;
                                        }
                                    }

                                    int? currentWorkstationId = null;
                                    ToolOperationPopUpForm toolOperationPopUpForm = new ToolOperationPopUpForm(_currentWorkingBolt, _currentWorkingBoltIndependence, CheckIfIsMultiDeviceIndependenceMode(),
                                            deviceBlock.CategoryName, this, _workstationsDTOs, _toolTasks, currentWorkstationId, _actionAfterSendingPset);
                                    deviceBlock.PopUpForm = toolOperationPopUpForm;
                                    contentSize.Height = panelHeight * _toolTasks.Count + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;

                                    TableLayoutPanel tablePanel = toolOperationPopUpForm.TablePanel;
                                    Panel contentPanel = deviceBlock.PopUpForm.ContentPanel;
                                    int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                                    int boxMargin = boxHeight / 5;
                                    int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                                    contentSize.Height = tableHeight + contentPanel.Padding.Size.Height;
                                    int tableWidth = contentSize.Width - contentPanel.Padding.Size.Width;
                                    toolOperationPopUpForm.BoxHeight = boxHeight;
                                    toolOperationPopUpForm.BoxMargin = boxMargin;
                                    toolOperationPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);

                                    ToolOperationPopUpFormExtraActions(toolOperationPopUpForm);
                                }
                            } else if (deviceBlock.Category == DeviceCategories.ARM) {
                                List<IoBoxTask> tasks = _ioBoxTasks.Values.ToList().Where(task => task.ArmType != null).ToList();
                                if (tasks.Count > 0) {
                                    SetArmRetrieve(true);
                                    deviceBlock.PopUpForm = new ArmDetailPopUpForm(deviceBlock.CategoryName, _workstationsDTOs, tasks, panelHeight);
                                    deviceBlock.PopUpForm.HandleDestroyed += (sender, eventArgs) => {
                                        SetArmRetrieve(false);
                                        if (_currentWorkingBolt != null) {
                                            WorkstationDTO workstationDTO = _workstationsDTOs.Single(w => w.id == _currentWorkingBolt.BoltDTO.workstation_id);
                                            if (_locating_enabled && workstationDTO.arm_id != null) {
                                                SetArmRetrieve2(workstationDTO.arm_id.Value, true);
                                            }
                                        }
                                        if (_currentWorkingBoltIndependence.Count > 0) {
                                            foreach (int id in _currentWorkingBoltIndependence.Keys) {
                                                WorkstationDTO workstationDTO = _workstationsDTOs.Single(w => w.id == id);
                                                if (_locating_enabled && workstationDTO.arm_id != null) {
                                                    SetArmRetrieve2(workstationDTO.arm_id.Value, true);
                                                }
                                            }
                                        }
                                    };
                                    contentSize.Width = (int) (WidgetUtils.MainSize.Width * .65);
                                    contentSize.Height = panelHeight * tasks.Count + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;
                                }
                                void SetArmRetrieve(bool yes) {
                                    tasks.ForEach(t => {
                                        IoBoxTypeArm? armType = t.ArmType;
                                        if (armType != null && !armType.RetrieveResult) {
                                            armType.RetrieveResult = yes;
                                        }
                                    });
                                }
                                void SetArmRetrieve2(int id, bool yes) {
                                    tasks.ForEach(t => {
                                        IoBoxTypeArm? armType = t.ArmType;
                                        if (armType != null && armType.DeviceId == id && !armType.RetrieveResult) {
                                            armType.RetrieveResult = yes;
                                        }
                                    });
                                }
                            } else if (deviceBlock.Category == DeviceCategories.SERIAL_PORT) {
                                // TODO: 
                            } else if (deviceBlock.Category == DeviceCategories.COMMUNICATION) {
                                // TODO: 
                            } else if (deviceBlock.Category == DeviceCategories.IOBOX_ARRANGER) {
                                if (_ioBoxTasks.Count > 0) {
                                    IoBoxTask? ioBoxTask = null;
                                    foreach (IoBoxTask ioTask in _ioBoxTasks.Values) {
                                        if (ioTask.ArrangerType != null) {
                                            ioBoxTask = ioTask;
                                            break;
                                        }
                                    }

                                    if (ioBoxTask == null) {
                                        WidgetUtils.ShowConfirmPopUp("没有配置螺丝机");
                                        return;
                                    }

                                    bool confirmed = OpenAdminPasswordPopUpForm("螺丝机信号点测试需要管理员操作密码", false);
                                    if (!confirmed) {
                                        return;
                                    }

                                    ArrangerOperationPopUpForm popUpForm = new(deviceBlock.CategoryName, this, ioBoxTask);
                                    deviceBlock.PopUpForm = popUpForm;
                                    contentSize.Height = panelHeight * 2 + deviceBlock.PopUpForm.ContentPanel.Padding.Size.Height;


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
                            } else if (deviceBlock.Category == DeviceCategories.IOBOX_SETTERSELECTOR) {
                                // TODO: 
                            } else {
                                // TODO: 
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

        protected virtual void ToolOperationPopUpFormExtraActions(ToolOperationPopUpForm popUpForm) { }

        private void InitializeTimeDisplayer() {
            // Time displayer
            _timeDisplayerOuter = new() {
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

        #region Methods
        // 获取条码匹配规则码匹配规则
        protected async void GetBarCodeMatchingRules() {
            await Task.Run(() => {
                _barcodeRelatedDone = false;

                _barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id)).BarCodeMatchingRuleDTOs;
                _barCodeMatchingRuleDTOs = _barCodeMatchingRuleDTOs.OrderBy(dto => dto.id).ToList();
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

                _barcodeRelatedDone = true;
            });
        }
        // 载入设备
        private async Task LoadDevicesAsync() {
            await Task.Run(() => {
                // 查询所有站点信息
                _workstationsDTOs = _apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs.ToList();

                // 查询所有设备信息
                _arms = _apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id)).DeviceArmDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _tools = _apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id)).DeviceToolDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _serialPorts = _apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id)).DeviceSerialPortDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _communications = _apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id)).DeviceCommunicationDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
                _ioBoxes = _apis.QueryDeviceIoList(new(SystemUtils.MacAddressesDTO.id)).DeviceIoDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();

                // Load tools
                _toolTasks = MainUtils.ToolTasks;
                foreach (KeyValuePair<int, ToolTask> pair in _toolTasks) {
                    ToolTask toolTask = pair.Value;
                    // 绑定数据处理代理
                    toolTask.ActionAfterAnalysis = DoAfterRecevingTighteningDataAsync;
                    if (toolTask.ToolType is ToolPFSeries) {
                        toolTask.ActionAfterCurveDataReceived = DoAfterRecevingCurveDataAsync;
                    }
                    if (MainUtils.IsAutoLockToolEnabled()) {
                        toolTask.ForceSendLock();
                    }
                }

                // Load io boxes, included arms
                _ioBoxTasks = MainUtils.IoBoxTasks;
                ReseetIoBox();

                // Load serial port devices
                _serialPortTasks = MainUtils.SerialPortTasks;
                foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                    InitSerialPortTasks(pair);
                }

                // Load communication devices
                _communicationTasks = MainUtils.CommunicationTasks;
            });

            // Keep listenging devices
            CheckDeviceConnections();

            // Action after loading devices
            ActionAfterLoadingDevices();
        }
        protected virtual void InitSerialPortTasks(KeyValuePair<int, SerialPortTask> pair) {
            SerialPortTask serialPortTask = pair.Value;
            serialPortTask.ActionAfterDataReceived = async msg => {
                await Task.Run(() => {
                    BeginInvoke(() => {
                        if (!IsDisposed) {
                            DeviceSerialPortDTO dto = _serialPorts.Single(dto => dto.id == pair.Key);
                            // 如果有空的数据进来，则跳过
                            if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
                                logger.Warn("Message is null from serial port device, please check.");
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
        // 持续检查设备连接状态的task
        private async void CheckDeviceConnections() {
            await Task.Run(async () => {
                while (!IsDisposed) {
                    if (Visible) {
                        foreach (DeviceBlock block in _deviceBlocks) {
                            DeviceCategory category = block.Category;
                            if (category == DeviceCategories.TOOL) {
                                Check(block, _toolTasks.Values.ToList());
                            } else if (category == DeviceCategories.ARM) {
                                Check(block, _ioBoxTasks.Values.Where(task => task.ArmType != null).ToList(), connected => {
                                    if (MainUtils.IsArmLocatingEnabled()) {
                                        if (connected) {
                                            RemoveLockMsg(WorkingProcessPanel.LockedArmDisconnected);
                                        } else {
                                            AddLockMsg(WorkingProcessPanel.LockedArmDisconnected);
                                        }
                                    }
                                });
                            } else if (category == DeviceCategories.SERIAL_PORT) {
                                Check(block, _serialPortTasks.Values.ToList());
                            } else if (category == DeviceCategories.COMMUNICATION) {
                                Check(block, _communicationTasks.Values.ToList());
                            } else if (category == DeviceCategories.IOBOX_ARRANGER) {
                                Check(block, _ioBoxTasks.Values.Where(task => task.ArrangerType != null).ToList());
                            } else if (category == DeviceCategories.IOBOX_SETTERSELECTOR) {
                                Check(block, _ioBoxTasks.Values.Where(task => task.SetterSelectorType != null).ToList());
                            } else {
                                CheckCustomConnections(block, category);
                            }
                        }
                    }
                    await Task.Delay(_checkDevicesConnectionDelay);
                }
            });
            void Check<T>(DeviceBlock block, List<T> tasks, Action<bool>? action = null) where T : ATaskBase {
                if (tasks.Count == 0) {
                    block.ResetIconByStatus(DeviceStatus.EMPTY);
                } else if (tasks.Find(t => !t.WorkplaceCheckConnection()) != null) {
                    block.ResetIconByStatus(DeviceStatus.ERROR);
                    if (action != null) {
                        action(false);
                    }
                } else {
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                    if (action != null) {
                        action(true);
                    }
                }
            }
        }

        protected virtual void CheckCustomConnections(DeviceBlock block, DeviceCategory category) { }

        protected virtual void ActionAfterLoadingDevices() { }

        // Reset io box status
        protected virtual void ReseetIoBox() {
            if (_ioBoxTasks != null && _ioBoxTasks.Count > 0) {
                foreach (IoBoxTask task in _ioBoxTasks.Values) {
                    if (task.ArmType != null) {
                        task.ArmType.ActionAfterCoordinatesReceived = null;
                    }
                    if (task.ArrangerType != null) {
                        task.ArrangerType.Reset();
                        task.ArrangerType.ActionAfterIoSignalReceived = null;
                    }
                    if (task.SetterSelectorType != null) {
                        if (task.SetterSelectorType is IoBoxTypeSetterSelectorPlus setterSelectorPlus) {
                            setterSelectorPlus.Reset();
                        } else {
                            task.SetterSelectorType.Reset();
                            task.SetterSelectorType.ActionAfterIoSignalReceived = null;
                        }
                    }
                }
            }
        }

        // Check if is multi device independence mode
        protected bool CheckIfIsMultiDeviceIndependenceMode() => (int) YesOrNo.YES == _mission.multi_device_independence;

        protected virtual void SetProductImagePanel() {
            if (_productImageDisplayPanel.IsHandleCreated) {
                _sides.Clear();
                _allBolts.Clear();
                _allBoltsIndependence.Clear();
                _productImageDisplayPanel.Controls.Clear();
                _productImageFiles.Clear();
                _missionImages.Clear();
            }
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
                        Dictionary<int, List<BoltButton>> dict = new();
                        List<BoltButton> list = new();
                        foreach (ProductBoltDTO boltDTO in bolts) {
                            BoltButton boltBtn = new(boltDTO) {
                                Parent = _productImageDisplayPanel,
                                Label = boltDTO.serial_num + "",
                                ShowingWhileWorking = false,
                                Visible = false,
                            };
                            boltBtn.MissionIsActivated = () => _activated;
                            boltBtn.Click += (s, e) => OpenBoltPopUpForm(boltDTO, boltBtn);
                            // Multi device independence: Group by workstation first, then group by side id
                            if (!dict.ContainsKey(boltDTO.workstation_id)) {
                                dict.Add(boltDTO.workstation_id, new());
                            }
                            dict[boltDTO.workstation_id].Add(boltBtn);
                            // Normal mode
                            list.Add(boltBtn);
                        }
                        // For multi device independence mode and normal mode:
                        //     each mode uses different containers
                        _allBolts.Add(sideDTO.id, list);
                        _allBoltsIndependence.Add(sideDTO.id, dict);
                    }
                }
            }

            // 默认显示第一个产品面和对应的螺栓点位
            _currentSideName.SetValue(0, _sides[_currentSideIndex].name);
            int sideId = _sides[_currentSideIndex].id;
            if (sideId > 0) {
                _showingBoltButtons = _allBolts[_sides[_currentSideIndex].id];
                _showingBoltButtons.ForEach(btn => {
                    btn.Visible = true;
                    btn.ShowingWhileWorking = true;
                });
            }
        }
        protected virtual void OpenBoltPopUpForm(ProductBoltDTO boltDTO, BoltButton boltBtn) {
            _boltPopUpForm = new(boltDTO) {
                Title = boltDTO.serial_num + " - " + boltDTO.name,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                ClickOutsideToClose = true,
            };
            // 添加按钮
            AddBtnToBoltPopUpForm(boltDTO, boltBtn);

            // Show form but make it transparent to create handles for its children
            _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
            // Resize all widgets
            ResizeBoltPopUpForm();
            // Real show
            _boltPopUpForm.Show();
        }
        protected virtual void AddBtnToBoltPopUpForm(ProductBoltDTO boltDTO, BoltButton boltBtn) {
            if (_currentWorkingBolt == null || _currentWorkingBolt.BoltDTO.serial_num != boltDTO.serial_num) {
                CommonButton switchBtn = _boltPopUpForm.AddButton("切换到此点位");
                switchBtn.Click += (s, e) => {
                    if (!_activated) {
                        WidgetUtils.ShowErrorPopUp("任务未激活或已完成，无法切换点位！");
                        _boltPopUpForm.Dispose();
                    } else {
                        BoltButton? currentBoltBtn;
                        int sideId = _sides[_currentSideIndex].id;
                        int selectingBoltWorkstationId = boltBtn.BoltDTO.workstation_id;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            currentBoltBtn = _currentWorkingBoltIndependence[selectingBoltWorkstationId];
                        } else {
                            currentBoltBtn = _currentWorkingBolt;
                        }
                        if (currentBoltBtn != null) {
                            // Switch to certain bolts
                            if (CheckIfIsMultiDeviceIndependenceMode()) {
                                BoltButton boltButton = SwitchBolt(selectingBoltWorkstationId, _allBoltsIndependence[sideId][selectingBoltWorkstationId].IndexOf(boltBtn));

                                // Change status of bolt
                                ChangeBoltStatusToWorking(boltButton);

                                // Cache it
                                _currentWorkingBoltIndependence[selectingBoltWorkstationId] = boltButton;
                            } else {
                                BoltButton boltButton = SwitchBolt(_allBolts[sideId].IndexOf(boltBtn));

                                // Change status of bolt
                                ChangeBoltStatusToWorking(boltButton);

                                // Cache it
                                _currentWorkingBolt = boltButton;
                            }
                            currentBoltBtn.ResetStatusWithoutChangingVisible();
                            currentBoltBtn.StopFlickering();
                            _boltPopUpForm.Dispose();
                        }
                    }
                };
            }
            CommonButton closeBtn = _boltPopUpForm.AddButton("关闭");
            closeBtn.Click += (s, e) => {
                _boltPopUpForm.Dispose();
            };
        }
        protected virtual void ResizeBoltPopUpForm() {
            if (_boltPopUpForm != null) {
                _boltPopUpForm.ResizeSelf();
            }
        }

        // 打开条码弹窗
        protected virtual void OpenBarCodePopUpForm(string? barCode = null) {
            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                }

                _barCodePopUpForm = new BarCodeInputPopUpForm(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated,
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode, _rulesExcluded, CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode)) {
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

        public bool CheckErrorPromptForArmEnabled() {
            if (!MainUtils.IsArmLocatingEnabled() && MainUtils.IsErrorPromptForArmEnabled()) {
                WidgetUtils.ShowWarningPopUp("未开启【力臂定位】，请检查配置！");
                return false;
            }
            return true;
        }

        public bool CheckChallengeMissionConfirmation() {
            List<ProductMissionDTO> allOtherMissions = _apis.QueryProductMissions(new()).ProductMissionsDTOs.Where(m => m.id != _mission.id).ToList();
            ProductMissionDTO? challengeMission = allOtherMissions.Find(m => m.challenge_mission_id == _mission.id);

            // Check if current mission has challenge mission
            if (challengeMission != null) {
                // Check if it's first mission
                if (challengeMission.is_first_mission == (int) YesOrNo.YES) {
                    // Check if current mission has predecessor_mission_id
                    if (_mission.predecessor_mission_id != null) {
                        WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 首档岗位】，但此任务存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                        return false;
                    } else {
                        // Check if challenge mission is finished
                        if (!ChallengeChecks(challengeMission.id, false)) {
                            return false;
                        }
                    }
                } else {
                    // Check if current mission has predecessor_mission_id
                    if (_mission.predecessor_mission_id != null) {
                        if (challengeMission.predecessor_mission_id == null) {
                            WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 非首档岗位】且当前任务存在前置任务，但挑战任务不存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                            return false;
                        } else {
                            // Check if challenge mission's predecessor mission is finished
                            ProductMissionDTO? predecessorMissionForChallengeMission =
                                allOtherMissions.Find(m => m.predecessor_mission_id == challengeMission.predecessor_mission_id);
                            if (!ChallengeChecks(challengeMission.id, predecessorMissionForChallengeMission != null)) {
                                return false;
                            }
                        }
                    } else {
                        if (challengeMission.predecessor_mission_id != null) {
                            WidgetUtils.ShowWarningPopUp("当前任务绑定了【挑战任务 - 非首档岗位】且当前任务不存在前置任务，但挑战任务存在前置任务，配置出错，请联系开发人员检查软件逻辑！");
                            return false;
                        }

                        // Check if challenge mission is finished
                        if (!ChallengeChecks(challengeMission.id, false)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool ChallengeChecks(int challengeMissionId, bool hasPredecessorMission) {
            string jsonObj = MainUtils.ChallengeTaskUtil.Read(challengeMissionId.ToString());
            ChallengeTask? task = JsonConvert.DeserializeObject<ChallengeTask>(jsonObj);

            bool hasPartsBarCode = false;
            if (_partsBarCodeMatchingRules.ContainsKey(_mission.id)) {
                hasPartsBarCode = _partsBarCodeMatchingRules[_mission.id].Count > 0;
            }

            if (task == null || !task.IsToday()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务校验！");
                return false;
            } else if (!hasPredecessorMission && !task.ProductBarCodeErrorOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-错码】校验！");
                return false;
            } else if (hasPredecessorMission && !task.ProductPredecessorOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-上一道岗位未完成】校验！");
                return false;
            } else if (!hasPredecessorMission && !task.ProductBarCodeRedoOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【追溯码-重码】校验！");
                return false;
            } else if (hasPartsBarCode && !task.PartsBarCodeErrorOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-错码】校验！");
                return false;
            } else if (hasPredecessorMission && !task.PartsPredecessorOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-上一道岗位未完成】校验！");
                return false;
            } else if (hasPartsBarCode && !task.PartsBarCodeRedoOK()) {
                WidgetUtils.ShowWarningPopUp("此任务还未通过挑战任务【物料码-重码】校验！");
                return false;
            } else if (!task.MissionOK()) {
                WidgetUtils.ShowWarningPopUp("此任务对应挑战任务未完成！");
                return false;
            }
            return true;
        }

        public void AddChallengeResult(int challengeMissionId, ChallengeTaskEnum type) {
            string jsonObj = MainUtils.ChallengeTaskUtil.Read(challengeMissionId.ToString());
            ChallengeTask? task = JsonConvert.DeserializeObject<ChallengeTask>(jsonObj);

            if (task == null) {
                task = new();
            }

            task.MissionId = challengeMissionId;
            task.AddResult(type);

            MainUtils.ChallengeTaskUtil.Write(challengeMissionId.ToString(), JsonConvert.SerializeObject(task));
        }

        public virtual List<BarCodeMatchingRuleDTO> GetCurrentExcludedRules(ProductBoltDTO? boltDTO = null) {
            _rulesExcluded = new();

            if (_mission.id > 0 && _mission.ProductSides != null && _partsBarCodeMatchingRules.ContainsKey(_mission.id)) {
                List<int> ids = new();

                // Collate all bolts of current mission into a List
                List<ProductBoltDTO> allBolts = new();
                foreach (ProductSideDTO side in _mission.ProductSides) {
                    if (side.Bolts != null) {
                        allBolts.AddRange(side.Bolts);
                    }
                }

                if (boltDTO != null) {
                    allBolts.RemoveAll(b => b.id == boltDTO.id);

                    // Add current bolt id into cache list
                    if (_ruleIdsCheckedCached == null) {
                        _ruleIdsCheckedCached = new();
                    }
                    _ruleIdsCheckedCached.Add(boltDTO.id);
                }

                // Check all bolts see if any rule needs to be excluded
                List<string?> idsList = allBolts.Select(b => b.parts_bar_code_ids).ToList();
                foreach (string? idsStr in idsList) {
                    if (!string.IsNullOrEmpty(idsStr)) {
                        ids.AddRange(CommonUtils.StringToList(idsStr));
                    }
                }

                // Remove id(s) if it's duplicated in current bolt
                if (boltDTO != null && !string.IsNullOrEmpty(boltDTO.parts_bar_code_ids)) {
                    ids.RemoveAll(id => CommonUtils.StringToList(boltDTO.parts_bar_code_ids).Contains(id));
                }

                _rulesExcluded = _partsBarCodeMatchingRules[_mission.id].Where(rule => ids.Contains(rule.id)).ToList();
            }

            return _rulesExcluded;
        }

        // 激活任务
        public virtual async void ActivateMission() {
            // *0. Reset all before activating mission
            // 清理之前的后台任务
            _backgroundTaskCts.ForEach(cts => {
                cts.Cancel();
                cts.Dispose();
            });
            _backgroundTaskCts.Clear();

            // 重置主取消令牌
            _activeMissionCts.Cancel();
            _activeMissionCts.Dispose();
            _activeMissionCts = new CancellationTokenSource();

            PrepareBeforeActivatingMission();

            // 1. Check can activate mission
            if (await ValidationBeforeActivatingMission()) {
                // 2. Initialize variables
                InitializeBeforeActivatingMission();

                // 3. Activate mission
                _activated = true;

                // 4. Action after activating mission
                await ActionAfterActivatingMission();
            } else {
                // Clear current bolts
                _currentWorkingBolt = null;
                _currentWorkingBoltIndependence.Clear();

                _ = TerminateMission(WorkplaceProcessStatus.UNACTIVATED);
            }
        }
        protected virtual void PrepareBeforeActivatingMission() {
            // Reset side page
            _currentSideIndex = 0;
            ChangeSideAndInvalidate();

            // Recheck locating enabled
            _locating_enabled = MainUtils.IsArmLocatingEnabled();

            // Reset status of working proccess panel
            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;

            // Reset other variables
            _sumBoltDone = 0;

            // Reset 'need loosening'
            _needLoosening = false;

            // Reset all bolts
            foreach (int sideId in _allBolts.Keys) {
                // Sort all bolts
                _allBolts[sideId] = _allBolts[sideId].OrderBy(btn => btn.BoltDTO.serial_num).ToList();

                // WHYC
                _sumBoltDone += _allBolts[sideId].Count;

                // Reset
                _allBolts[sideId].ForEach(b => {
                    // Reset status (will rename with serial number automatically)
                    b.ResetStatusWithoutChangingVisible();
                    // Reset ng times
                    b.NgTimes = 0;
                });
            }

            // Sort all bolts in _allBoltsIndependence
            foreach (int sideId in _allBoltsIndependence.Keys) {
                foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
                    _allBoltsIndependence[sideId][workstationId] = _allBoltsIndependence[sideId][workstationId].OrderBy(btn => btn.BoltDTO.serial_num).ToList();
                }
            }
        }

        protected virtual async Task<bool> ValidationBeforeActivatingMission() {
            return await Task<bool>.Run(() => {
                // Check if any workstation has been setup, if no then can not activate mission
                if (_workstationsDTOs.Count == 0) {
                    WidgetUtils.ShowErrorPopUp($"没有配置站点，无法激活任务");
                    return false;
                }

                // Check if any bolt has no tool setup
                foreach (KeyValuePair<int, List<BoltButton>> pair in _allBolts) {
                    foreach (BoltButton bolt in pair.Value) {
                        int? toolId = _workstationsDTOs.Single(dto => dto.id == bolt.BoltDTO.workstation_id).tool_id;
                        if (toolId == null || _tools.SingleOrDefault(tool => tool.id == toolId.Value) == null) {
                            WidgetUtils.ShowErrorPopUp($"存在点位所配置的站点没有配置工具，无法激活任务");
                            return false;
                        }
                    }
                }

                // Check if any bolt has no arm setup while locating enabled
                if (_locating_enabled) {
                    foreach (KeyValuePair<int, List<BoltButton>> pair in _allBolts) {
                        foreach (BoltButton bolt in pair.Value) {
                            int? armId = _workstationsDTOs.Single(dto => dto.id == bolt.BoltDTO.workstation_id).arm_id;
                            if (armId == null || _arms.SingleOrDefault(tool => tool.id == armId.Value) == null) {
                                WidgetUtils.ShowErrorPopUp($"存在点位所配置的站点没有配置力臂，无法激活任务");
                                return false;
                            }
                        }
                    }
                }

                // Check io box related issues
                // Use try-catch to return earlier if can not activate mission
                try {
                    _arrangerNeeded = false;
                    _setterSelectorNeeded = false;

                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                        // If bit specification of any is not null and grater than 0, need to receive data from io box and keep checking the status of setter selector
                        foreach (Dictionary<int, List<BoltButton>> pair in _allBoltsIndependence.Values) {
                            CheckBoltList(pair.Values.ToList());
                        }
                    } else {
                        CheckBoltList(_allBolts.Values.ToList());
                    }

                    void CheckBoltList(List<List<BoltButton>> allBolts) {
                        foreach (List<BoltButton> bolts in allBolts) {
                            foreach (BoltButton bolt in bolts) {
                                ProductBoltDTO dto = bolt.BoltDTO;
                                if (dto.specification != null && dto.specification > 0) {
                                    CheckArranger(dto.arranger_id);
                                }
                                if (dto.specification2 != null && dto.specification2 > 0) {
                                    CheckArranger(dto.arranger_id2);
                                }
                                if (dto.bit_specification != null && dto.bit_specification > 0) {
                                    CheckSetterSelector(dto.setter_selector_id);
                                }
                            }
                        }
                    }

                    void CheckArranger(int? arranger_id) {
                        if (arranger_id == null || arranger_id <= 0) {
                            WidgetUtils.ShowErrorPopUp($"存在点位的排列机组配置出错，无法激活任务");
                            throw new Exception();
                        }
                        if (_ioBoxTasks.Values.ToList().Find(box => box.ArrangerType != null && box.ArrangerType.DeviceId == arranger_id) == null) {
                            WidgetUtils.ShowErrorPopUp($"存在点位所选择的排列机组找不到配置或被删除，无法激活任务");
                            throw new Exception();
                        }
                        _arrangerNeeded = true;
                    }
                    void CheckSetterSelector(int? setter_selector_id) {
                        if (setter_selector_id == null || setter_selector_id <= 0) {
                            WidgetUtils.ShowErrorPopUp($"存在点位的套筒选择器配置出错，无法激活任务");
                            throw new Exception();
                        }
                        if (_ioBoxTasks.Values.ToList().Find(box => box.SetterSelectorType != null && box.SetterSelectorType.DeviceId == setter_selector_id) == null) {
                            WidgetUtils.ShowErrorPopUp($"存在点位所选择的套筒选择器找不到配置或被删除，无法激活任务");
                            throw new Exception();
                        }
                        _setterSelectorNeeded = true;
                    }
                } catch {
                    return false;
                }

                // All checks passed, can activate mission
                return true;
            });
        }

        protected virtual void InitializeBeforeActivatingMission() {
            // Switch to certain bolts
            if (CheckIfIsMultiDeviceIndependenceMode()) {
                foreach (int key in _allBoltsIndependence[_sides[_currentSideIndex].id].Keys) {
                    BoltButton boltButton = SwitchBolt(key, 0);

                    // Change status of bolt
                    ChangeBoltStatusToWorking(boltButton);

                    // Cache it
                    _currentWorkingBoltIndependence.Add(key, boltButton);
                }
            } else {
                BoltButton boltButton = SwitchBolt(0);

                // Change status of bolt
                ChangeBoltStatusToWorking(boltButton);

                // Cache it
                _currentWorkingBolt = boltButton;
            }

            // Reset current operation data
            currentOperationData = null;

            // Initialize setter selector
            foreach (IoBoxTask ioTask in _ioBoxTasks.Values) {
                if (ioTask.SetterSelectorType is IoBoxTypeSetterSelectorPlus) {
                    IoBoxSetterSelectorPlus deviceType = (IoBoxSetterSelectorPlus) ioTask.SetterSelectorType.DeviceType;
                    deviceType.PositionsInUse = new int[] { 0, 0, 0, 0 };

                    foreach (Dictionary<int, List<BoltButton>> boltsInWorkstations in _allBoltsIndependence.Values) {
                        foreach (List<BoltButton> bolts in boltsInWorkstations.Values) {
                            foreach (ProductBoltDTO dto in bolts.Where(b => b.BoltDTO.bit_specification != null).Select(b => b.BoltDTO)) {
                                if (dto.bit_specification != null && dto.bit_specification > 0) {
                                    deviceType.PositionsInUse[(int) dto.bit_specification.Value - 1] = 1;
                                }
                            }
                        }
                    }
                    foreach (List<BoltButton> bolts in _allBolts.Values) {
                        foreach (ProductBoltDTO dto in bolts.Where(b => b.BoltDTO.setter_selector_id != null).Select(b => b.BoltDTO)) {
                            if (dto.bit_specification != null && dto.bit_specification > 0) {
                                deviceType.PositionsInUse[(int) dto.bit_specification.Value - 1] = 1;
                            }
                        }
                    }
                }
            }
        }

        protected virtual async Task ActionAfterActivatingMission() {
            // Add a new record into: mission_record
            _missionRecord = new() {
                mission_id = _mission.id,
                product_bar_code = _barCodeObj.ProductBarCode,
                parts_bar_code = string.Join(",", _barCodeObj.PartsBarCodes),
                mission_result = (int) TighteningStatus.NG,
                is_redo = _isRedo,
            };
            _apis.AddOrUpdateMissionRecord(new(_missionRecord));

            // If locating enabled
            if (_locating_enabled) {
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

                // Lock all tools here
                List<int?> list = workstationDTOs.Select(dto => dto.tool_id).ToList();
                if (MainUtils.IsArmLocatingEnabled()) {
                    _toolTasks.Values.Where(t => list.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.ForceSendLock());
                } else {
                    _toolTasks.Values.Where(t => list.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.ForceSendUnlock());
                }

                // Start listening coordinates
                workstationDTOs.ForEach(dto => {
                    int? armId = dto.arm_id;
                    if (armId != null) {
                        IoBoxTask? ioBoxTask = _ioBoxTasks.Values.SingleOrDefault(task => task.ArmType != null && task.ArmType.DeviceId == armId);
                        if (ioBoxTask != null) {
                            ioBoxTask.ArmType.RetrieveResult = true;
                            ioBoxTask.ArmType.ActionAfterCoordinatesReceived += ActionAfterArmDataReceived;
                        }
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
                    }
                });
            } else {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
            }

            // Delay a litter bit
            await Task.Delay(500);

            // Start lock checking task
            StartLockCheckingTask();

            // Start looping task if arranger is needed
            StartArrangerTask();

            // Start looping task if setter selector is needed
            StartSetterSelectorTask();
        }

        protected virtual async void ActivateMissionAutomatically() {
            // If is self looping mode, then activate mission automatically
            if (MainUtils.IsMissionSelfLoopingModeEnabled() && _mission.id > 0) {
                // Wait for .5 seconds
                await Task.Delay(500);

                ActivateMission();
            }
            // If USB scanner is enabled, then open bar code input pop up form automatically
            else if (MainUtils.IsUSBScannerEnabled()) {
                while (!_barcodeRelatedDone) {
                    await Task.Delay(50);
                }
                OpenBarCodePopUpForm();
            }
        }

        protected virtual void StartLockCheckingTask() {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_activeMissionCts.Token);
            _backgroundTaskCts.Add(cts);

            BeginInvoke(() => {
                Task.Run(async () => {
                    while (!IsDisposed && _activated && !cts.Token.IsCancellationRequested) {
                        try {
                            // Check if is multi-device independence mode
                            bool isMultiDeviceMode = CheckIfIsMultiDeviceIndependenceMode();

                            if (isMultiDeviceMode) {
                                // Multi-device mode: process all bolts in _currentWorkingBoltIndependence dictionary
                                foreach (var kvp in _currentWorkingBoltIndependence) {
                                    int workstationId = kvp.Key;
                                    BoltButton boltButton = kvp.Value;

                                    try {
                                        // Skip if boltButton is null
                                        if (boltButton == null) {
                                            continue;
                                        }

                                        ProductBoltDTO boltDTO = boltButton.BoltDTO;
                                        ToolTask toolTask = _toolTasks[_workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value];

                                        // Clear messages for this bolt
                                        ClearLockMsgs();
                                        ClearInformationMsgs();

                                        CheckCurrentPSetForLockMsg();
                                        CheckAdminConfirmationForLockMsg();

                                        string statusDesc = string.Empty;
                                        if (lockMsgs.Count > 0) {
                                            statusDesc = string.Join("\r\n", lockMsgs);
                                            statusDesc = string.Format(statusDesc, boltDTO.serial_num);
                                            // Set status to working proccess panel
                                            _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;

                                            // Lock tools
                                            toolTask.ForceSendLock();
                                        } else {
                                            if (_needLoosening) {
                                                statusDesc = string.Format(WorkingProcessPanel.LooseningDesc, boltDTO.serial_num);
                                                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                            } else {
                                                statusDesc = string.Format(WorkingProcessPanel.TighteningDesc, boltDTO.serial_num);
                                                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                            }
                                            // Set status to working proccess panel
                                            _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;

                                            // Unlock tools
                                            toolTask.ForceSendUnlock();
                                        }

                                        // Add information
                                        if (informationMsgs.Count > 0 && statusDesc.Length > 0) {
                                            statusDesc += "\r\n" + string.Join("\r\n", informationMsgs);
                                        }

                                        // Set description to working proccess panel
                                        _workingProcessPanel.StatusDesc = statusDesc;
                                    } catch (OperationCanceledException) {
                                        // 任务被取消，退出循环
                                        throw;
                                    } catch (Exception e) {
                                        // Log error for this specific bolt but continue processing others
                                        logger.Error($"StartLockCheckingTask [Multi-Device Mode, WorkstationId={workstationId}]: e = {e}");
                                    }
                                }
                            } else {
                                // Normal mode: process single _currentWorkingBolt
                                try {
                                    // Skip if _currentWorkingBolt is null
                                    if (_currentWorkingBolt == null) {
                                        await Task.Delay(_lockCheckingTaskDelay, cts.Token);
                                        continue;
                                    }

                                    ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                                    ToolTask toolTask = _toolTasks[_workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value];

                                    CheckCurrentPSetForLockMsg();
                                    CheckAdminConfirmationForLockMsg();

                                    string statusDesc = string.Empty;
                                    if (lockMsgs.Count > 0) {
                                        statusDesc = string.Join("\r\n", lockMsgs);
                                        statusDesc = string.Format(statusDesc, _workingProcessPanel.BoltSerialNum);
                                        // Set status to working proccess panel
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;

                                        // Lock tools
                                        toolTask.ForceSendLock();
                                    } else {
                                        if (_needLoosening) {
                                            statusDesc = string.Format(WorkingProcessPanel.LooseningDesc, _workingProcessPanel.BoltSerialNum);
                                            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                        } else {
                                            statusDesc = string.Format(WorkingProcessPanel.TighteningDesc, _currentWorkingBolt.BoltDTO.serial_num);
                                            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                        }
                                        // Set status to working proccess panel
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;

                                        // Unlock tools
                                        toolTask.ForceSendUnlock();
                                    }

                                    // Add information
                                    if (informationMsgs.Count > 0 && statusDesc.Length > 0) {
                                        statusDesc += "\r\n" + string.Join("\r\n", informationMsgs);
                                    }

                                    // Set description to working proccess panel
                                    _workingProcessPanel.StatusDesc = statusDesc;
                                } catch (OperationCanceledException) {
                                    // 任务被取消，退出循环
                                    throw;
                                } catch (Exception e) {
                                    // Sometimes will throw 'System.InvalidOperationException: cross-thread operation not valid' but don't know why
                                    logger.Error($"StartLockCheckingTask [Normal Mode]: e = {e}");
                                }
                            }
                        } catch (OperationCanceledException) {
                            // 任务被取消，退出循环
                            break;
                        } catch (Exception e) {
                            // Catch any outer exceptions
                            logger.Error($"StartLockCheckingTask: e = {e}");
                        } finally {
                            // Delay a little bit and check again
                            await Task.Delay(_lockCheckingTaskDelay, cts.Token);
                        }
                    }
                }, cts.Token);
            });
        }

        public void AddLockMsg(string? msg) {
            if (!string.IsNullOrEmpty(msg) && !lockMsgs.Contains(msg)) {
                lockMsgs.Add(msg);
            }
        }
        public void AddInformationMsg(string? msg) {
            if (!string.IsNullOrEmpty(msg) && !informationMsgs.Contains(msg)) {
                informationMsgs.Add(msg);
            }
        }
        public bool CheckLockMsg(string? msg) => !string.IsNullOrEmpty(msg) && lockMsgs.Contains(msg);
        public bool CheckInformationMsg(string? msg) => !string.IsNullOrEmpty(msg) && informationMsgs.Contains(msg);
        public void RemoveLockMsg(string? msg) {
            if (!string.IsNullOrEmpty(msg) && lockMsgs.Contains(msg)) {
                lockMsgs.Remove(msg);
            }
        }
        public void RemoveInformationMsg(string? msg) {
            if (!string.IsNullOrEmpty(msg) && informationMsgs.Contains(msg)) {
                informationMsgs.Remove(msg);
            }
        }
        public void ClearLockMsgs() => lockMsgs.Clear();
        public void ClearInformationMsgs() => informationMsgs.Clear();

        protected void StartArrangerTask() {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_activeMissionCts.Token);
            _backgroundTaskCts.Add(cts);

            _resendSignalToArrangerTimes = 0;
            BeginInvoke(async () => {
                while (!IsDisposed && _activated && _arrangerNeeded && !cts.Token.IsCancellationRequested) {
                    if (_arrangerPositionOk != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        ToolTask toolTask = _toolTasks[_workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value];

                        if (_arrangerPositionOk.Values.ToList().Count(ok => !ok) > 0) {
                            if (!_arrangerPositionTimedOut) {
                                AddLockMsg(WorkingProcessPanel.LockedArrangerNotDone);
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.LockedArrangerNotDone);
                                AddLockMsg(WorkingProcessPanel.LockedArrangerTimedOut);

                                // Retry confirmation
                                if (_resendSignalToArrangerMaxTimes > 0) {
                                    if (_resendSignalToArrangerTimes < _resendSignalToArrangerMaxTimes) {
                                        // Confirm if retry needed
                                        if (WidgetUtils.ShowConfirmPopUp($"送钉时间已达到[{_currentWorkingBolt.Arranger_time_out / 1000}]秒，送钉失败，是否重试？")) {
                                            _arrangerPositionTimedOut = false;
                                            RemoveLockMsg(WorkingProcessPanel.LockedArrangerTimedOut);

                                            // Counter plus 1
                                            _resendSignalToArrangerTimes++;

                                            // Resend signal
                                            SendSignalToArrager(_currentWorkingBolt);
                                        } else {
                                            _arrangerPositionOk = null;
                                        }
                                    } else {
                                        // Retry times reaches max, stop the mission
                                        _ = TerminateMission(WorkplaceProcessStatus.FINISHED_NG);

                                        // Show notice
                                        WidgetUtils.ShowWarningPopUp($"重试次数已达到{_resendSignalToArrangerMaxTimes}次，请检查任务及设备状态是否正常");
                                    }
                                } else {
                                    // Do not have any retry chance then terminate mission
                                    _ = TerminateMission(WorkplaceProcessStatus.FINISHED_NG);
                                }
                            }
                        } else {
                            RemoveLockMsg(WorkingProcessPanel.LockedArrangerNotDone);
                            RemoveLockMsg(WorkingProcessPanel.LockedArrangerTimedOut);
                            _arrangerPositionOk = null;
                        }
                    }

                    await Task.Delay(_checkIoBoxSignalDelay, cts.Token);
                }
            });
        }

        protected void StartSetterSelectorTask() {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_activeMissionCts.Token);
            _backgroundTaskCts.Add(cts);

            _resendSignalToSetterSelectorTimes = 0;
            BeginInvoke(async () => {
                while (!IsDisposed && _activated && _setterSelectorNeeded && !cts.Token.IsCancellationRequested) {
                    if (_bitPositionOk != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        ToolTask toolTask = _toolTasks[_workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value];
                        if (!_bitPositionOk.Value) {
                            if (!_bitPositionTimedOut) {
                                AddLockMsg(WorkingProcessPanel.LockedSetterSelectorNotMatched);
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.LockedSetterSelectorNotMatched);
                                AddLockMsg(WorkingProcessPanel.LockedSetterSelectorTimedOut);

                                // Retry confirmation
                                if (_resendSignalToSetterSelectorMaxTimes > 0) {
                                    if (_resendSignalToSetterSelectorTimes < _resendSignalToSetterSelectorMaxTimes) {
                                        // Confirm if retry needed
                                        if (WidgetUtils.ShowConfirmPopUp($"套筒选择时间已达到[{_currentWorkingBolt.Setter_selector_time_out / 1000}]秒，是否重试？")) {
                                            _bitPositionTimedOut = false;
                                            RemoveLockMsg(WorkingProcessPanel.LockedSetterSelectorTimedOut);

                                            // Counter plus 1
                                            _resendSignalToSetterSelectorTimes++;

                                            // Resend signal
                                            SendSignalToSetterSelector(_currentWorkingBolt);
                                        } else {
                                            _bitPositionOk = null;
                                        }
                                    } else {
                                        // Retry times reaches max, stop the mission
                                        TerminateMission(WorkplaceProcessStatus.FINISHED_NG);

                                        // Show notice
                                        WidgetUtils.ShowWarningPopUp($"重试次数已达到{_resendSignalToSetterSelectorMaxTimes}次，请检查任务及设备状态是否正常");
                                    }
                                } else {
                                    // Do not have any retry chance then terminate mission
                                    TerminateMission(WorkplaceProcessStatus.FINISHED_NG);
                                }
                            }
                        } else {
                            RemoveLockMsg(WorkingProcessPanel.LockedSetterSelectorTimedOut);
                            RemoveLockMsg(WorkingProcessPanel.LockedSetterSelectorNotMatched);
                            _bitPositionOk = null;
                        }
                    }

                    await Task.Delay(_checkIoBoxSignalDelay, cts.Token);
                }
            });
        }

        protected bool CheckArmPosition(int maxValue, Coordinates3D armCoordinates, Coordinates3D boltCoordinates) {
            int x = Math.Abs(armCoordinates.X - boltCoordinates.X);
            int y = Math.Abs(armCoordinates.Y - boltCoordinates.Y);
            int z = Math.Abs(armCoordinates.Z - boltCoordinates.Z);

            bool xOk;
            bool yOk;
            bool zOk;
            if (maxValue > 0) {
                xOk = x < _armLocatingAccuracy || maxValue - x < _armLocatingAccuracy;
                yOk = y < _armLocatingAccuracy || maxValue - y < _armLocatingAccuracy;
                zOk = boltCoordinates.Z == 0 || z < _armLocatingAccuracy || maxValue - z < _armLocatingAccuracy;
            } else {
                xOk = x < _armLocatingAccuracy;
                yOk = y < _armLocatingAccuracy;
                zOk = boltCoordinates.Z == 0 || z < _armLocatingAccuracy;
            }

            return xOk && yOk && zOk;
        }

        // 读取力臂数据并根据当前螺栓点位配置信息进行解锁、锁枪
        protected virtual void ActionAfterArmDataReceived(int maxValue, Coordinates3D armCoordinates) {
            Task.Run(() => {
                BeginInvoke(() => {
                    // FIXME: Should use id to handle separately
                    if (_activated && _currentWorkingBolt != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
                        if (toolId != null) {
                            ToolTask toolTask = _toolTasks[toolId.Value];
                            Coordinates3D boltCoordinates = Coordinates3D.FromString(boltDTO.position);
                            _realTimeArmCoordinates = armCoordinates;

                            if (CheckArmPosition(maxValue, armCoordinates, boltCoordinates)) {
                                RemoveLockMsg(WorkingProcessPanel.LockedArmPosition);
                            } else {
                                if (CheckInformationMsg(WorkingProcessPanel.UnlockedManually)) {
                                    RemoveLockMsg(WorkingProcessPanel.LockedArmPosition);
                                } else {
                                    AddLockMsg(WorkingProcessPanel.LockedArmPosition);
                                }
                            }
                        }
                    }
                });
            });
        }

        // 当前点位没有设置程序号
        protected void CheckCurrentPSetForLockMsg() {
            if (_currentWorkingBolt.CurrentParameterSet == null) {
                // 如果是没有配置就显示对应错误信息，否则可能是下发失败
                if (_currentWorkingBolt.BoltDTO.parameters_set == null) {
                    AddLockMsg(WorkingProcessPanel.LockedPsetNull);
                } else {
                    RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                }
            } else {
                RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
            }
        }

        // 需要管理员输入密码并确认
        protected void CheckAdminConfirmationForLockMsg() {
            if (_adminConfirmed != null) {
                // 管理员已确认
                if (_adminConfirmed.Value) {
                    RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
                    _adminConfirmed = null;
                }
                // 管理员未确认
                else {
                    AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                    if (_adminPasswordPopUpForm == null || _adminPasswordPopUpForm.IsDisposed) {
                        _adminConfirmed = false;
                        BoltNGConfirmPopUp();
                    }
                }
            } else {
                RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
            }
        }

        // Switch bolt according to index
        protected virtual BoltButton SwitchBolt(int newIndex) {
            return _allBolts[_sides[_currentSideIndex].id][newIndex];
        }
        // Switch bolt according to index and workstation id
        protected virtual BoltButton SwitchBolt(int workstationId, int newIndex) {
            return _allBoltsIndependence[_sides[_currentSideIndex].id][workstationId][newIndex];
        }

        // Change status of bolt
        protected virtual void ChangeBoltStatusToWorking(BoltButton boltButton) {
            logger.Info("ChangeBoltStatusToWorking start ............");

            // Clear all messages
            // Do this first because some other lock message will occurred later
            ClearLockMsgs();
            ClearInformationMsgs();

            // Send signal to arrager if specification is not null and grater than 0
            SendSignalToArrager(boltButton);

            // Send signal to setter selector if bit_specification is not null and grater than 0
            SendSignalToSetterSelector(boltButton);

            // Check if any parts bar code bound to current bolt
            CheckBoltBoundPartsBarCode(boltButton);

            ProductBoltDTO boltDTO = boltButton.BoltDTO;
            int toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value;
            boltButton.CurrentParameterSet = null;

            // Tell working proccess panel what serial number is currently
            _workingProcessPanel.BoltSerialNum = boltButton.BoltDTO.serial_num;

            // Send pset of current working bolt to controller
            _ = Task.Run(async () => {
                try {
                    await SendPSet(boltButton, _toolTasks[toolId], boltDTO.parameters_set);
                } catch (Exception ex) {
                    logger.Error($"SendPSet fire-and-forget failed: {ex.Message}", ex);
                }
            });

            // Change status of current working bolt
            boltButton.BoltStatus = BoltStatus.WORKING;

            logger.Info("ChangeBoltStatusToWorking end ............");
        }

        // Check if any parts bar code bound to current bolt
        protected virtual async void CheckBoltBoundPartsBarCode(BoltButton boltButton) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    if (!string.IsNullOrEmpty(boltButton.BoltDTO.parts_bar_code_ids)) {
                        List<int> list = CommonUtils.StringToList(boltButton.BoltDTO.parts_bar_code_ids);
                        if (!list.All(_barCodeObj.PartsMatchingRulesCached.Contains)) {
                            AddLockMsg(WorkingProcessPanel.LockedBoltBarCode);
                            OpenBarCodePopUpForm(null);
                        }
                    }
                });
            });
        }

        // Send pset to controller
        protected virtual async Task SendPSet(BoltButton boltButton, ToolTask task, int? pset) {
            logger.Info("SendPSet start ......");

            try {
                // 直接在UI线程启动异步操作，避免Task.Run包装
                BeginInvoke(() => {
                    _pset.SetValue(0, null);
                });

                // === 保持原有MatCode逻辑 ===
                if (pset == null && !string.IsNullOrEmpty(_matCode)) {
                    MatCodeMapWhycDTO? matCodeMapWhycDTO = _apis.FindMatCodeMapByMatCode(new(_matCode)).MatCodeMapWhycDTO;
                    if (matCodeMapWhycDTO != null) {
                        logger.Info($"Get parameter set[{matCodeMapWhycDTO.parameter_set}] by mat code[{_matCode}]");
                        pset = matCodeMapWhycDTO.parameter_set;
                    } else {
                        logger.Info($"Get parameter set[null] by mat code[{_matCode}]");
                    }
                }

                // === 保持原有null检查逻辑 ===
                if (pset == null) {
                    BeginInvoke(() => AddLockMsg(WorkingProcessPanel.LockedPsetNull));
                    return;
                }

                // === 使用新的通用重试策略 ===
                var retryStrategy = RetryStrategy.IncrementalDelay(_resendPsetMaxTimes, 1000);

                bool success = false;
                try {
                    success = await retryStrategy.ExecuteAsync(
                        async () => {
                            // 每次尝试前更新UI状态
                            BeginInvoke(() => {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetFailed);
                                AddLockMsg(WorkingProcessPanel.LockedPsetSending);
                                _pset.SetValue(0, $"程序号下发中... ({pset})");
                            });

                            // 执行发送
                            bool result = await task.SendPSetAsync(pset.Value);

                            if (result) {
                                BeginInvoke(() => {
                                    // === 成功时保持原有逻辑 ===
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetFailed);
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                    boltButton.CurrentParameterSet = pset;
                                    _pset.SetValue(0, $"程序号 {pset} (发送成功)");
                                });
                                return true;
                            }

                            // 检查是否已通过其他方式设置成功（保持原有逻辑）
                            if (boltButton.CurrentParameterSet != null) {
                                BeginInvoke(() => {
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetFailed);
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                    boltButton.CurrentParameterSet = pset;
                                    _pset.SetValue(0, $"程序号 {pset} (已存在)");
                                });
                                return true;
                            }

                            return false;
                        },
                        (currentAttempt, maxAttempts) => {
                            // === 实时显示重试进度 ===
                            BeginInvoke(() => {
                                _pset.SetValue(0, $"程序号下发中... 第{currentAttempt}次尝试 (共{maxAttempts}次)");
                            });
                        },
                        () => {
                            // === 每次失败时更新UI（但不阻塞） ===
                            BeginInvoke(() => {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);
                                AddLockMsg(WorkingProcessPanel.LockedPsetFailed);
                                _pset.SetValue(0, "程序号下发失败，正在重试...");
                            });
                        },
                        null,
                        _activeMissionCts.Token);
                } catch (RetryException ex) {
                    // 处理重试异常
                    logger.Warn($"程序号{pset}发送失败，已重试{_resendPsetMaxTimes}次: {ex.Message}");
                    success = false;
                }

                // === 失败后处理（无对话框，改为状态提示） ===
                if (!success && boltButton.CurrentParameterSet == null) {
                    BeginInvoke(() => {
                        // 移除失败锁定状态
                        RemoveLockMsg(WorkingProcessPanel.LockedPsetFailed);
                        RemoveLockMsg(WorkingProcessPanel.LockedPsetSending);

                        // 添加失败状态提示（但不阻塞）
                        AddLockMsg(string.Format(WorkingProcessPanel.LockedPsetFailed, pset));
                        _pset.SetValue(0, $"程序号 {pset} (失败 - 已达最大重试次数)");

                        // 显示一次性警告（不阻塞）
                        WidgetUtils.ShowWarningPopUp($"程序号{pset}下发失败，已自动重试{_resendPsetMaxTimes}次，请检查设备连接");
                    });
                }
            } catch (OperationCanceledException) {
                logger.Info("SendPSet operation was cancelled");
            } catch (Exception ex) {
                logger.Error($"SendPSet发生未预期异常: {ex.Message}", ex);
                BeginInvoke(() => {
                    _pset.SetValue(0, $"程序号下发异常: {ex.Message}");
                });
            } finally {
                logger.Info("SendPSet end ......");
            }
        }

        // Send bit position to arranger
        protected virtual async void SendSignalToArrager(BoltButton boltButton) {
            await Task.Run(() => {
                BeginInvoke((Delegate) (() => {
                    Task.Run(() => {
                        // Prepare all variables
                        ProductBoltDTO boltDTO = boltButton.BoltDTO;
                        _specifications = new List<float>();
                        _arrangerIds = new List<int>();
                        if (boltDTO.specification != null && boltDTO.specification > 0) {
                            _specifications.Add(boltDTO.specification.Value);
                            _arrangerIds.Add(boltDTO.arranger_id.Value);
                        }
                        if (boltDTO.specification2 != null && boltDTO.specification2 > 0) {
                            _specifications.Add(boltDTO.specification2.Value);
                            _arrangerIds.Add(boltDTO.arranger_id2.Value);
                        }

                        logger.Info($"Sending signal(s) to arranger(s) for specification(s) = [{string.Join(", ", _specifications)}]...");

                        // Do action if specifications is not equal to 0
                        if (_specifications.Count > 0) {
                            // Initialize variables
                            if (_arrangerPositionOk == null) {
                                _arrangerPositionOk = new();
                                foreach (float specification in _specifications) {
                                    _arrangerPositionOk.Add(specification, false);
                                }
                            } else {
                                List<float>.Enumerator enumerator = _specifications.GetEnumerator();
                                while (enumerator.MoveNext()) {
                                    float specification = enumerator.Current;
                                    if (_arrangerPositionOk.ContainsKey(specification)) {
                                        if (_arrangerPositionOk[specification]) {
                                            _arrangerIds.RemoveAt(_specifications.IndexOf(specification));
                                            _specifications.Remove(specification);
                                            continue;
                                        }
                                    }
                                    _arrangerPositionOk.Add(specification, false);
                                }
                            }

                            // Do send signal
                            if (_arrangerIds.Distinct().Count() == 1) {
                                SendSignal(_arrangerIds[0], _specifications);
                            } else {
                                for (int i = 0; i < _arrangerIds.Count; i++) {
                                    SendSignal(_arrangerIds[i], _specifications.Skip(i).Take(1).ToList());
                                }
                            }
                        } else {
                            _arrangerPositionOk = null;
                            _arrangerPositionTimedOut = false;
                        }
                    });
                }));
            });

            // Action of sending signal
            void SendSignal(int arrangerId, List<float> specifications) {
                DeviceIoDTO ioDto = _ioBoxes.Single(box => box.id == arrangerId);
                IoBoxTypeArranger? arrangerType = _ioBoxTasks[MainUtils.GetTCPClientKey(ioDto.ip, ioDto.port)].ArrangerType;
                if (arrangerType != null) {
                    boltButton.SendSignalToArragner(specifications, arrangerType, (isOks, isTimedOut) => {
                        foreach (float position in _arrangerPositionOk.Keys) {
                            bool? ok = isOks[(int) position - 1];
                            if (ok != null) {
                                _arrangerPositionOk[position] = ok.Value;
                                logger.Info($"Position for specification sent OK!");
                            }
                        }
                        _arrangerPositionTimedOut = isTimedOut;
                    });
                }
            }
        }

        // Send bit position to setter selector
        protected virtual async void SendSignalToSetterSelector(BoltButton boltButton) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    Task.Run(() => {
                        ProductBoltDTO boltDTO = boltButton.BoltDTO;
                        if (boltDTO.bit_specification != null && boltDTO.bit_specification > 0) {
                            DeviceIoDTO ioDto = _ioBoxes.Single(box => box.id == boltDTO.setter_selector_id);
                            IoBoxTypeSetterSelector? setterSelectorType = _ioBoxTasks[MainUtils.GetTCPClientKey(ioDto.ip, ioDto.port)].SetterSelectorType;
                            if (setterSelectorType != null) {
                                _bitPositionOk = false;

                                if (setterSelectorType is IoBoxTypeSetterSelectorPlus setterSelectorPlus) {
                                    _bitPositionTimedOut = false;
                                    boltButton.SendSignalToSetterSelectorPlus(boltDTO.bit_specification.Value, setterSelectorPlus, isOk => _bitPositionOk = isOk);
                                } else {
                                    boltButton.SendSignalToSetterSelector(boltDTO.bit_specification.Value, setterSelectorType, (isOk, isTimedOut) => {
                                        _bitPositionOk = isOk;
                                        _bitPositionTimedOut = isTimedOut;
                                    });
                                }
                            }
                        } else {
                            _bitPositionOk = null;
                            _bitPositionTimedOut = false;
                        }
                    });
                });
            });
        }

        // 打开管理员密码输入弹框
        public bool OpenAdminPasswordPopUpForm(string title, bool needExctraActions, Action<bool>? actionAfterTrue = null) {
            _adminPasswordPopUpForm = new() {
                Title = title,
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

            int contentWidth = (int) (WidgetUtils.MainSize.Width * .75);
            Padding contentPadding = _adminPasswordPopUpForm.ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            _adminPasswordBox.Size = new(contentWidth - contentPadding.Size.Width - boxMargin * 2, boxHeight);
            _adminPasswordBox.Margin = new(boxMargin);
            int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;
            _adminPasswordPopUpForm.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));

            // Extra actions
            if (needExctraActions) {
                AdminPopUpExtraActions();
            }

            bool result = false;
            DialogResult dialogResult = _adminPasswordPopUpForm.ShowDialog();
            _adminPasswordPopUpForm.Dispose();

            return DialogResult.OK == dialogResult && result;

            void Confirm() {
                string password = _adminPasswordBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(password) && _apis.AdminPasswordValidate(new(password)).Succeed) {
                    WidgetUtils.ShowNoticePopUp("验证成功");
                    if (actionAfterTrue != null) {
                        actionAfterTrue(true);
                    }

                    _adminConfirmed = true;
                    result = true;
                    _adminPasswordPopUpForm.DialogResult = DialogResult.OK;
                } else {
                    WidgetUtils.ShowErrorPopUp("密码错误");
                    _adminPasswordBox.GetTextBox(0).IsError = true;

                    _adminPasswordPopUpForm.DialogResult = DialogResult.No;
                }
            }
        }

        protected virtual void AdminPopUpExtraActions() { }

        // 螺栓拧紧NG时，如果需要管理员输入密码，则调用此方法
        protected void BoltNGConfirmPopUp() => OpenAdminPasswordPopUpForm("拧紧错误，工具已锁止。请输入管理员密码解锁。", false);

        // 读取到控制器传回的数据后进行处理
        protected virtual void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            BeginInvoke(() => {
                // Nonactivated or finished will not handle any received data
                if (!_activated) {
                    return;
                }

                try {
                    ToolTask toolTask = _toolTasks[deviceId];
                    // Lock first
                    if (MainUtils.IsArmLocatingEnabled()) {
                        toolTask.ForceSendLock();
                    }
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
                        // Set pset manualy if tool type is sudong x7
                        if (toolTask.ToolType is ToolSudongX7 toolX7) {
                            dataDTO.parameter_set_number = currentBolt.CurrentParameterSet;
                        }

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

                        // WHYC
                        // TZYX
                        _rundownTime = data.rundown_time;

                        // If result type is tightening
                        if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                            bool tighteningOK = true;
                            string errorMsg = "";
                            // Initialize color to ok
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

                            // Check tightening status
                            if (data.tightening_status != (int) TighteningStatus.OK) {
                                tighteningOK = false;
                                if (data.tightening_error_status != null &&
                                        data.tightening_error_status != (int) TighteningErrorStatus_SuDong.NO_ERROR) {
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    string errorMsgTemp;
                                    if (Enum.IsDefined(typeof(TighteningErrorStatus_SuDong), data.tightening_error_status)) {
                                        TighteningErrorStatus_SuDong errorStatus_SuDong = (TighteningErrorStatus_SuDong) data.tightening_error_status;
                                        switch (errorStatus_SuDong) {
                                            case TighteningErrorStatus_SuDong.SLIPPAGE:
                                                errorMsgTemp = "滑丝/滑牙";
                                                break;
                                            case TighteningErrorStatus_SuDong.FALSE_LOCKING:
                                                errorMsgTemp = "浮锁";
                                                break;
                                            case TighteningErrorStatus_SuDong.TORQUE_NOK:
                                                errorMsgTemp = "扭矩不良";
                                                break;
                                            case TighteningErrorStatus_SuDong.ANGLE_NOK:
                                                errorMsgTemp = "拧紧角度不良";
                                                break;
                                            case TighteningErrorStatus_SuDong.SEND_UNLOCK_IN_TIGTHENING:
                                                errorMsgTemp = "中途提前释放启动信号";
                                                break;
                                            default:
                                                errorMsgTemp = $"未知错误代码【{data.tightening_error_status}】";
                                                break;
                                        }
                                    } else {
                                        errorMsgTemp = $"未知错误代码【{data.tightening_error_status}】";
                                    }
                                    errorMsg += $"拧紧出错，错误信息：{errorMsgTemp}";
                                }
                                if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                    _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                }
                                if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                    _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                }
                            }

                            // Check torque
                            if (boltDTO.torque_max > 0 && (data.torque < boltDTO.torque_min || data.torque > boltDTO.torque_max)) {
                                tighteningOK = false;
                                _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "扭矩与配置范围不符";
                            }

                            // Check angle
                            if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                tighteningOK = false;
                                _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "角度与配置范围不符";
                            }

                            // Switch to next bolt
                            if (tighteningOK) {
                                _errorMsg = null;

                                // Reset tightening type to tightening in case somewhere did some changes
                                _needLoosening = false;
                                RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                _workingProcessPanel.NGReasons = null;

                                currentBolt.BoltStatus = BoltStatus.DONE;

                                // Check next index
                                List<BoltButton> currentSideBolts;
                                if (CheckIfIsMultiDeviceIndependenceMode()) {
                                    currentSideBolts = _allBoltsIndependence[_sides[_currentSideIndex].id][workstationId];
                                } else {
                                    currentSideBolts = _allBolts[_sides[_currentSideIndex].id];
                                }
                                int nextIndex = currentSideBolts.IndexOf(currentBolt) + 1;
                                // 检查是否存在跳点的情况
                                while (nextIndex < currentSideBolts.Count && currentSideBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                    nextIndex++;
                                }

                                // Store data
                                dataDTO.tightening_status = (int) TighteningStatus.OK;
                                StoreTighteningData(dataDTO);

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
                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                        // Checks for challenge mission
                                        if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                            AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                        }

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
                                }
                            } else {
                                // Change bolt status
                                currentBolt.BoltStatus = BoltStatus.ERROR;

                                // Count ng times
                                currentBolt.NgTimes++;

                                // Set error message
                                _workingProcessPanel.NGReasons = errorMsg;
                                AddInformationMsg(_workingProcessPanel.NGReasons);

                                // WHYC
                                // TZYX
                                _errorMsg = errorMsg;

                                // Set status of data to ng
                                dataDTO.tightening_status = (int) TighteningStatus.NG;

                                // 记录数据
                                StoreTighteningData(dataDTO);

                                // Should not lock in the first place when it has error
                                if (MainUtils.IsArmLocatingEnabled()) {
                                    toolTask.ForceSendUnlock();
                                }
                            }
                        } else {
                            _needLoosening = false;

                            // 反松结束后把扭矩角度改回黑色
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                            // Remove error message
                            RemoveLockMsg(_workingProcessPanel.NGReasons);
                            _workingProcessPanel.NGReasons = null;

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
        }
        protected virtual async void DoAfterRecevingCurveDataAsync(CurveDataTemp data, int deviceId) {
            await Task.Run(() => {
                BeginInvoke(async () => {
                    try {
                        int max = 50;
                        int count = 0;
                        while (currentOperationData == null && count < max) {
                            await Task.Delay(100);
                        }

                        if (currentOperationData != null) {
                            CurveDataDTO dataDTO = new();
                            CommonUtils.ObjectConverter<CurveDataTemp, CurveDataDTO>(data, dataDTO);

                            dataDTO.operation_data_id = currentOperationData.id;
                            _apis.AddOrUpdateCurveData(new(dataDTO));
                        } else {
                            string errorMsg = $"Can't get current operation data after receiving curve data, data time stamp = {data.time_stamp}, id = {data.result_data_identifier}, type = {data.data_type}";
                            logger.Error(errorMsg);
                            throw new System.IO.InvalidDataException(errorMsg);
                        }
                    } catch (Exception e) {
                        logger.Error($"Error occurred while handling curve data, e: {e}");
                    }
                });
            });
        }

        protected virtual async Task StoreTighteningData(OperationDataDTO operationDataDTO) {
            logger.Info("StoreTighteningData start ........");

            try {
                // 并行执行数据库和文件存储操作，直接await异步方法
                await Task.WhenAll(
                    StoreDataToDatabaseAsync(operationDataDTO),
                    StoreDataToFilesAsync(operationDataDTO)
                );

                // 转换数据并更新UI（在UI线程）
                BeginInvoke(() => {
                    try {
                        OperationDataVO dataFormatted = new();
                        CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(operationDataDTO, dataFormatted);

                        // 使用线程安全的ConcurrentBag
                        _tighteningDataVOs.Add(dataFormatted);

                        // 创建快照用于UI显示
                        RefreshTighteningDataPanel(_tighteningDataVOs.ToList());
                        logger.Info("StoreTighteningData showing to panel end ........");
                    } catch (Exception e) {
                        logger.Error($"Error in data conversion or UI update: {e}");
                    }
                });
            } catch (Exception e) {
                logger.Error($"Error during data storage operations: {e}");
            } finally {
                logger.Info("StoreTighteningData end ........");
            }
        }

        protected virtual async Task StoreDataToDatabaseAsync(OperationDataDTO operationDataDTO) {
            logger.Info("StoreTighteningData save to database start ........");

            try {
                currentOperationData = _apis.AddOrUpdateOperationData(new(operationDataDTO)).OperationDataDTO;
            } catch (Exception e) {
                logger.Error($"StoreTighteningData save to database error: {e}");
                throw; // 重新抛出异常以便调用者处理
            } finally {
                logger.Info("StoreTighteningData save to database end ........");
            }
        }

        protected virtual async Task StoreDataToFilesAsync(OperationDataDTO operationDataDTO) {
            logger.Info("StoreDataToFiles start ........");

            try {
                // 直接执行，让 BeginInvoke 处理UI线程异步性
                // 避免多余的 Task.Run 包装，减少线程池压力
                StoreDataToFilesCore(operationDataDTO);
                logger.Info("StoreDataToFiles end ........");
            } catch (Exception e) {
                logger.Error($"StoreDataToFiles error: {e}");
                throw; // 重新抛出异常以便调用者处理
            }
        }

        // 核心文件存储逻辑（在UI线程中执行）
        private void StoreDataToFilesCore(OperationDataDTO operationDataDTO) {
            // 使用BeginInvoke确保在UI线程中执行（因为涉及UI相关操作）
            BeginInvoke(() => {
                try {
                    OperationDataVO dataFormatted = new();
                    CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(operationDataDTO, dataFormatted);

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
                    // 先根据每个字段的排序，将排序值和数据值作为一个dictionary存入一个集合
                    Dictionary<int, object?> record = new();
                    for (int i = 0; i < propertyNames.Count; i++) {
                        string pName = propertyNames[i];
                        PropertyInfo? propertyInfo = dataFormatted.GetType().GetProperty(pName);
                        if (propertyInfo != null) {
                            record.Add(i, propertyInfo.GetValue(CommonUtils.CannotBeNull(dataFormatted)));
                        }
                    }
                    dataWithConfigFields.Add(record);
                    // 组装最终数据
                    List<List<object?>> finalData = new();
                    dataWithConfigFields.ForEach(dict => {
                        IOrderedEnumerable<KeyValuePair<int, object?>> orderedEnumerable = from pair in dict orderby pair.Key select pair;
                        finalData.Add(orderedEnumerable.Select(pair => pair.Value).ToList());
                    });

                    finalData.ExportToTextFile(headers, textFilePath, textFileExists);
                    finalData.ExportToExcelFile(headers, excelFilePath, excelFileExists);
                } catch (Exception e) {
                    logger.Error($"StoreDataToFiles core error: {e}");
                    throw; // 重新抛出到外部catch块
                }
            });
        }

        protected void RefreshTighteningDataPanel(IEnumerable<OperationDataVO> vos) {
            // 提前创建快照，避免在UI线程中多次枚举ConcurrentBag
            if (vos == null) return;
            var snapshot = vos.ToList();
            _tighteningDataPanel.DataSource = snapshot;
        }

        // 获取_tighteningDataVOs的线程安全快照
        protected List<OperationDataVO> GetTighteningDataSnapshot() {
            return _tighteningDataVOs.ToList();
        }

        protected virtual void ResetMissionToDefault() => TerminateMission(WorkplaceProcessStatus.UNACTIVATED);

        public virtual async Task TerminateMission(WorkplaceProcessStatus status) {
            // Cancel all background tasks before locking tools to avoid race condition
            // where lock checking task might call ForceSendUnlock() after we lock tools
            if (_backgroundTaskCts.Count > 0) {
                foreach (var cts in _backgroundTaskCts) {
                    cts.Cancel();
                }

                // Wait for tasks to actually stop (with timeout to avoid hanging)
                var stopTasks = new List<Task>();
                foreach (var cts in _backgroundTaskCts) {
                    stopTasks.Add(Task.Run(async () => {
                        // Wait for cancellation to propagate and task to exit
                        await Task.Delay(100);
                    }));
                }

                try {
                    await Task.WhenAll(stopTasks);
                } catch (Exception) {
                    // Ignore exceptions during task cancellation
                }

                // Dispose and clear all cancellation token sources
                foreach (var cts in _backgroundTaskCts) {
                    cts.Dispose();
                }
                _backgroundTaskCts.Clear();
            }

            // Lock all tools
            if (MainUtils.IsAutoLockToolEnabled() && _activated) {
                LockAllTools();
            }

            // Reset IoBox
            ReseetIoBox();

            bool resetToDefault = status == WorkplaceProcessStatus.UNACTIVATED;

            // Reset variables
            _arrangerNeeded = false;
            _setterSelectorNeeded = false;

            // Change mission status
            _activated = false;

            // Delay a bit to make sure [WorkplaceProcessStatus] won't be changed by arm device incorrectly
            await Task.Delay(300);

            // Clear current working bolts
            ClearAndResetAllCurrentBolts(resetToDefault);

            // Change status of working process panel
            ResetWorkingProcessPanel(resetToDefault, status);

            // Change colors of torque and angle text back to normal
            if (resetToDefault) {
                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
            }

            // Stop retrieve coordinates data or listening coordinates
            StopRetrivingDataFromArmDevice();

            // Clear all cached bar codes
            _barCodeObj.Reset();
            _ruleIdsCheckedCached = null;
            _isRedo = (int) YesOrNo.NO;

            // Reset current operation data
            currentOperationData = null;

            // If it's not challenge mission, then check auto activation logic
            if (_mission.is_challenge_mission != (int) YesOrNo.YES
                    && _missionRecord != null
                    && _missionRecord.mission_result == (int) TighteningStatus.OK) {
                // If is self looping mode, then activate mission automatically
                ActivateMissionAutomatically();
            }
        }

        protected async void LockAllTools() {
            await Task.Run(() => {
                // Lock multiple times to ensure lock correctly
                int lockTimesSum = 3;
                int lockTimes = 0;
                while (lockTimes < lockTimesSum) {
                    _toolTasks.Values.ToList().ForEach(toolTask => toolTask.ForceSendLock());
                    lockTimes++;
                }
            });
        }

        protected void ClearAndResetAllCurrentBolts(bool resetToDefault) {
            if (CheckIfIsMultiDeviceIndependenceMode()) {
                foreach (int sideId in _allBoltsIndependence.Keys) {
                    foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
                        // Reset status to default of all bolts
                        _allBoltsIndependence[sideId][workstationId].ForEach(b => ChangeStatus(b));
                    }
                }
                _currentWorkingBoltIndependence.Clear();
            } else {
                foreach (int sideId in _allBolts.Keys) {
                    // Reset status to default of all bolts
                    _allBolts[sideId].ForEach(b => ChangeStatus(b));
                }
                _currentWorkingBolt = null;
            }

            void ChangeStatus(BoltButton bolt) {
                if (bolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                    bolt.ShowingWhileWorking = false;
                } else {
                    bolt.ShowingWhileWorking = true;
                }
                if (resetToDefault) {
                    bolt.BoltStatus = BoltStatus.DEFAULT;
                } else {
                    if (bolt.BoltStatus == BoltStatus.WORKING) {
                        bolt.BoltStatus = BoltStatus.ERROR;
                    }
                }
                bolt.StopFlickering();
            }
        }

        protected void ResetWorkingProcessPanel(bool resetToDefault, WorkplaceProcessStatus status) {
            _workingProcessPanel.BoltSerialNum = null;
            if (resetToDefault) {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.UNACTIVATED;
                _workingProcessPanel.StatusDesc = string.Empty;
            } else {
                _workingProcessPanel.WorkplaceProcessStatus = status;
            }
        }

        protected void StopRetrivingDataFromArmDevice() {
            if (_locating_enabled) {
                List<IoBoxTask> ioBoxTasks = _ioBoxTasks.Values.Where(task => task.ArmType != null).ToList();
                ioBoxTasks.ForEach(armTask => {
                    IoBoxTypeArm? armType = armTask.ArmType;
                    if (armType != null) {
                        armType.RetrieveResult = false;
                        if (armType.ActionAfterCoordinatesReceived != null && armType.ActionAfterCoordinatesReceived.GetInvocationList().Contains(ActionAfterArmDataReceived)) {
                            armType.ActionAfterCoordinatesReceived -= ActionAfterArmDataReceived;
                        }
                    }
                });
            }
        }

        protected virtual void InitializeAfterHandelCreated() { }
        #endregion

        #region Events
        protected override async void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(GetBarCodeMatchingRules));

            await Task.Run(() => {
                BeginInvoke(async () => {
                    // Load devices asynchronously to avoid delay UI creating
                    await LoadDevicesAsync();

                    // Initialize others
                    InitializeAfterHandelCreated();

                    // If is self looping mode, then activate mission automatically
                    ActivateMissionAutomatically();
                });
            });
        }
        protected override void OnHandleDestroyed(EventArgs e) {
            // 取消所有后台任务
            _activeMissionCts.Cancel();
            _backgroundTaskCts.ForEach(cts => {
                cts.Cancel();
                cts.Dispose();
            });
            _backgroundTaskCts.Clear();
            _activeMissionCts.Dispose();

            base.OnHandleDestroyed(e);

            if (MainUtils.IsAutoLockToolEnabled() || MainUtils.IsArmLocatingEnabled()) {
                if (_toolTasks != null && _toolTasks.Count > 0) {
                    foreach (KeyValuePair<int, ToolTask> tool in _toolTasks) {
                        // Clear all delegates once this workplace handle has been destroyed to ensure running performance
                        tool.Value.ActionAfterAnalysis = null;
                        // Lock all tools
                        tool.Value.ForceSendLock();
                    }
                }
            }
            // Clear all delegates once this workplace handle has been destroyed to ensure running performance
            ReseetIoBox();

            if (_serialPortTasks != null && _serialPortTasks.Count > 0) {
                foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                    // Clear all delegates once this workplace handle has been destroyed to make sure it won't throw any exception
                    pair.Value.ActionAfterDataReceived = null;
                }
            }

            if (_communicationTasks != null && _communicationTasks.Count > 0) {
                foreach (CommunicationTask task in _communicationTasks.Values) {
                    task.Reading = false;
                }
            }

            // Dispose all bolt buttons to stop all task they have
            if (CheckIfIsMultiDeviceIndependenceMode()) {
                foreach (Dictionary<int, List<BoltButton>> values in _allBoltsIndependence.Values) {
                    foreach (List<BoltButton> btns in values.Values) {
                        btns.ForEach(btn => btn.Dispose());
                    }
                }
            } else {
                foreach (List<BoltButton> btns in _allBolts.Values) {
                    btns.ForEach(btn => btn.Dispose());
                }
            }
        }
        #endregion
    }
}
