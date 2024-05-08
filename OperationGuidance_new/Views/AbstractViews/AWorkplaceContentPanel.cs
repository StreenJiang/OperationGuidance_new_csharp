using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.Collections;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.AbstractViews {
    public abstract class AWorkplaceContentPanel: CustomContentPanel {
        protected ILog logger;

        #region Fields
        protected OperationGuidanceApis _apis;
        protected ProductMissionDTO _mission;
        protected Action<string> _resetMissionName;
        protected bool _activated;
        protected bool _finished;
        protected Image _defaultImage;
        protected readonly int _checkDevicesConnectionDelay = 2500;
        protected readonly int _resendPsetMaxTimes = 5;

        protected List<WorkstationDTO> _workstationsDTOs = new();
        protected List<DeviceArmDTO> _arms;
        protected List<DeviceToolDTO> _tools;
        protected List<DeviceSerialPortDTO> _serialPorts;
        protected Dictionary<int, ArmTask> _armTasks = new();
        protected Dictionary<int, ToolTask> _toolTasks = new();
        protected Dictionary<int, SerialPortTask> _serialPortTasks = new();
        protected Dictionary<int, CommunicationTask> _communicationTasks = new();
        protected CommunicationTask? _communicationTask;
        protected MissionRecordDTO? _missionRecord;
        protected bool _needLoosening = false;
        protected bool? _adminConfirmed = null;
        protected CustomPopUpForm? _adminPasswordPopUpForm;
        protected int _isRedo = (int) YesOrNo.NO;
        protected Coordinates3D? _realTimeArmCoordinates;
        protected readonly object DataStorageLockObj = new();

        protected Dictionary<int, List<BoltButton>> _allBolts; // side_id - bolts
        protected Dictionary<int, Dictionary<int, List<BoltButton>>> _allBoltsIndependence; // side_id - workstation_id - bolts
        protected List<BoltButton> _showingBoltButtons; // Cache variable that used for changing side
        protected BoltButton? _currentWorkingBolt;
        protected Dictionary<int, BoltButton> _currentWorkingBoltIndependence = new();
        protected BoltPopUpForm _boltPopUpForm; // 如果以后要支持软件尺寸可拖拽改变，则需要在打开时动态改变
        protected bool _locating_enabled;
        protected int _armLocatingAccuracy;

        // Widgets
        protected Image _barCodeImage;
        protected PictureBox _barCodePictureBox;

        protected ProductImageDisplayPanel _productImageDisplayPanel;
        protected List<ProductImageFile> _productImageFiles;
        protected List<Image?> _missionImages;

        protected Label _operatorInfoTitle;
        protected CustomTextBoxGroup _operatorName;
        protected CustomTextBoxGroup _operatorId;

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

        protected DataGridViewPanel<OperationDataVO> _tighteningDataPanel;
        protected List<OperationDataVO> _tighteningDataVOs = new();

        protected WorkingProcessPanel _workingProcessPanel;

        protected CustomContentPanel _timeDisplayerOuter;
        protected Label _timeDisplayer;
        protected System.Windows.Forms.Timer _timeDisplayerTimer;

        // 条码相关
        public bool _checkRedo = false;
        protected BarCodeInputPopUpForm? _barCodePopUpForm;
        protected readonly BarCodeObj _barCodeObj = new();
        protected CustomTextBox _barCodeTextBox;
        // 条码匹配规则
        protected List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _productBarCodeMatchingRules;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _partsBarCodeMatchingRules;

        // 设备相关
        protected List<DeviceBlock> _deviceBlocks;
        protected Action? _actionAfterSendingPset;
        protected ModBusServer_YF? ModBusServer;

        // 产品面相关
        protected int _currentSideIndex;
        protected List<ProductSideDTO> _sides;

        // 点位相关
        #endregion

        #region Properties
        public OperationGuidanceApis Apis { get => _apis; set => _apis = value; }
        public bool Activated { get => _activated; set => _activated = value; }
        public bool Finished { get => _finished; set => _finished = value; }
        public bool? AdminConfirmed { get => _adminConfirmed; set => _adminConfirmed = value; }
        public int IsRedo { get => _isRedo; set => _isRedo = value; }
        public BarCodeObj BarCodeObj => _barCodeObj;
        public CustomTextBox BarCodeTextBox { get => _barCodeTextBox; set => _barCodeTextBox = value; }
        #endregion

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

            _locating_enabled = MainUtils.IsArmLocatingEnabled();
            _armLocatingAccuracy = MainUtils.GetArmLocatingAccuracy();

            _allBolts = new();
            _allBoltsIndependence = new();

            InitializeBarCodePanel();
            InitializeDeviceBlocks();
            InitializeTimeDisplayer();
        }

        private void InitializeBarCodePanel() {
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

        private void InitializeDeviceBlocks() {
            _deviceBlocks = new();
            List<DeviceCategory> deviceCategories = new();
            // Reverse because of RightToLeft flow direction
            for (int i = DeviceCategories.Elements.Count - 1; i >= 0; i--) {
                deviceCategories.Add(DeviceCategories.Elements[i]);
            }
            foreach (DeviceCategory category in deviceCategories) {
                DeviceBlock deviceBlock = new(category) {
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
                            if (_communicationTasks.Count > 0) {
                                deviceBlock.FloatingForm = new CommunicationDetailFloatingForm(deviceBlock.CategoryName, _communicationTasks, panelHeight);
                                contentSize.Height = panelHeight * _communicationTasks.Count + deviceBlock.FloatingForm.ContentPanel.Padding.Size.Height;
                            }
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
                                    _adminConfirmed = false;
                                    OpenAdminPasswordPopUpForm("手动控制工具。需要管理员操作密码");
                                    if (!_adminConfirmed.Value) {
                                        _adminConfirmed = null;
                                        return;
                                    }
                                    _adminConfirmed = null;
                                    int? currentWorkstationId = null;
                                    deviceBlock.PopUpForm = new ToolOperationPopUpForm(_currentWorkingBolt, _currentWorkingBoltIndependence, CheckIfIsMultiDeviceIndependenceMode(),
                                            deviceBlock.CategoryName, _workingProcessPanel, _workstationsDTOs, _toolTasks, currentWorkstationId, _actionAfterSendingPset);
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
                                        if (_currentWorkingBolt != null) {
                                            WorkstationDTO workstationDTO = _workstationsDTOs.Single(w => w.id == _currentWorkingBolt.BoltDTO.workstation_id);
                                            if (_locating_enabled && workstationDTO.arm_id != null) {
                                                _armTasks[workstationDTO.arm_id.Value].RetrieveResult = true;
                                            }
                                        }
                                        if (_currentWorkingBoltIndependence.Count > 0) {
                                            foreach (int id in _currentWorkingBoltIndependence.Keys) {
                                                WorkstationDTO workstationDTO = _workstationsDTOs.Single(w => w.id == id);
                                                if (_locating_enabled && workstationDTO.arm_id != null) {
                                                    _armTasks[workstationDTO.arm_id.Value].RetrieveResult = true;
                                                }
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
        }

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
        // 载入设备
        private async void LoadDevicesAsync() {
            await Task.Run(() => {
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
                            // 绑定数据处理代理
                            toolTask.ActionAfterAnalysis = DoAfterRecevingTighteningDataAsync;
                            // 进入工作台先把所有工具都锁住
                            if (toolTask.Connected) {
                                toolTask.SendLock();
                            }
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
                        _communicationTasks = MainUtils.CommunicationTasks;
                        foreach (KeyValuePair<int, CommunicationTask> pair in _communicationTasks) {
                            CommunicationTask communicationTask = pair.Value;
                            communicationTask.ModBusServer = ModBusServer;
                            // Reset all
                            if (ModBusServer != null) {
                                WriteRequestMessage req = new();
                                req.Data.MessageHexBytes = ModBusServer.ResetBytes();
                                req.DataLength.MessageHexBytes = MainUtils.ToSingleBytes(req.Data.Length);
                                req.RegisterNum.MessageHexBytes = MainUtils.ToBytes(req.Data.Length / Register.Bytes);
                                req.SetLength();
                                communicationTask.WriteToServer(req);
                            }
                            _communicationTask = communicationTask;
                            break;
                        }
                    } else {
                        // TODO
                    }
                }
            });
            // Keep listenging devices
            CheckDeviceConnections();
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
                                Check(block, _armTasks.Values.ToList());
                            } else if (category == DeviceCategories.SERIAL_PORT) {
                                Check(block, _serialPortTasks.Values.ToList());
                            } else if (category == DeviceCategories.COMMUNICATION) {
                                Check(block, _communicationTasks.Values.ToList());
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
                } else if (tasks.Find(t => !t.WorkplaceCheckConnection()) != null) {
                    block.ResetIconByStatus(DeviceStatus.ERROR);
                } else {
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                }
                // foreach (T task in tasks) {
                //     if (!task.WorkplaceCheckConnection()) {
                //         block.ResetIconByStatus(DeviceStatus.ERROR);
                //         return;
                //     }
                // }
                // block.ResetIconByStatus(DeviceStatus.NORMAL);
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
            int sideId = _sides[_currentSideIndex].id;
            if (sideId > 0) {
                _showingBoltButtons = _allBolts[_sides[_currentSideIndex].id];
                _showingBoltButtons.ForEach(btn => {
                    btn.Visible = true;
                    btn.ShowingWhileWorking = true;
                });
            }
        }
        protected virtual void ResizeBoltPopUpForm() {
            if (_boltPopUpForm != null) {
                _boltPopUpForm.ResizeSelf();
            }
        }

        // 切换任务
        public abstract void SwitchToMission(ProductMissionDTO mission);
        // 打开条码弹窗
        protected virtual void OpenBarCodePopUpForm(string? barCode = null) {
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
        // 激活任务
        public virtual void ActivateMission() {
            // *0. Reset all before activating mission
            PrepareBeforeActivatingMission();

            // 1. Check can activate mission
            if (ValidationBeforeActivatingMission()) {
                // 2. Initialize variables
                InitializeBeforeActivatingMission();

                // 3. Activate mission
                DoActivateMission();

                // 4. Action after activating mission
                ActionAfterActivatingMission();
            } else {
                // Clear current bolts
                _currentWorkingBolt = null;
                _currentWorkingBoltIndependence.Clear();
            }
        }
        protected virtual void PrepareBeforeActivatingMission() {
            // Recheck locating enabled
            _locating_enabled = MainUtils.IsArmLocatingEnabled();

            // Reset status of working proccess panel
            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;

            // Reset all bolts
            foreach (int sideId in _allBolts.Keys) {
                // Sort all bolts
                _allBolts[sideId] = _allBolts[sideId].OrderBy(btn => btn.BoltDTO.serial_num).ToList();

                // Reset
                _allBolts[sideId].ForEach(b => {
                    // Reset status (will rename with serial number automatically)
                    b.BoltStatus = BoltStatus.DEFAULT;
                    // b.ResetStatusWithoutChangingVisible();
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

        protected virtual bool ValidationBeforeActivatingMission() {
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

            // All checks passed, can activate mission
            return true;
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
        }

        protected virtual void DoActivateMission() {
            // Change mission status
            _activated = true;
            _finished = false;
        }

        protected virtual void ActionAfterActivatingMission() {
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
                _toolTasks.Values.Where(t => list.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                // Start listening coordinates
                workstationDTOs.ForEach(dto => {
                    int? armId = dto.arm_id;
                    if (armId != null) {
                        ArmTask armTask = _armTasks[armId.Value];
                        armTask.RetrieveResult = true;
                        armTask.OnActionAfterReceiving += ActionAfterArmDataReceived;
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
                    }
                });
            } else {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.ACTIVATED;
            }
        }

        // 读取力臂数据并根据当前螺栓点位配置信息进行解锁、锁枪
        protected virtual void ActionAfterArmDataReceived(Coordinates3D armCoordinates) {
            Task.Run(() => {
                BeginInvoke(() => {
                    // FIXME: Should use id to handle separately
                    if (_activated && !_finished && _currentWorkingBolt != null) {
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
                                    } else {
                                        // 如果下发失败则尝试重新下发

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
                                // Remove '*ing' desc
                                _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                                _workingProcessPanel.RemoveDesc(_workingProcessPanel.LooseningDesc);
                                // Append position error desc
                                _workingProcessPanel.AppendDesc(_workingProcessPanel.ArmPositionError);

                                // Remove admin confirmation message if _adminConfirmed is null or is true
                                if (_adminConfirmed == null || _adminConfirmed.Value) {
                                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.AdminConfirmation);
                                }
                            }
                        }
                    }
                });
            });
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
            ProductBoltDTO boltDTO = boltButton.BoltDTO;
            int toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id.Value;
            boltButton.CurrentParameterSet = null;

            // Tell working proccess panel what serial number is currently
            _workingProcessPanel.BoltSerialNum = boltButton.BoltDTO.serial_num;

            // Send pset of current working bolt to controller
            SendPSet(boltButton, _toolTasks[toolId], boltDTO.parameters_set);

            // Change status of current working bolt
            boltButton.BoltStatus = BoltStatus.WORKING;
        }

        // Send pset to controller
        protected virtual async void SendPSet(BoltButton boltButton, ToolTask task, int? pset) {
            // // Initialize pset text box first
            // SetPset();

            // If pset is null, show error message in working proccess panel
            if (pset == null) {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetNullError);
                return;
            }

            // Do send pset to controller
            await Task.Run(() => {
                BeginInvoke(async () => {
                    int sendTimes = 0;
                    while (!IsDisposed && !(await task.SendPSetAsync(pset.Value))) {
                        if (boltButton.CurrentParameterSet != null) {
                            return;
                        }

                        // Count failure times
                        sendTimes++;

                        // If sending times reaches maximun, show pop up form
                        if (_resendPsetMaxTimes > 0 && sendTimes >= _resendPsetMaxTimes) {
                            WidgetUtils.ShowWarningPopUp($"同一个点位下发程序号达到{_resendPsetMaxTimes}次，请检查任务配置");
                            return;
                        }

                        // Show reason of sending failure
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                        _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                        _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetFailedError);

                        // // 实时显示pset到任务信息框
                        // SetPset("程序号下发失败");

                        // Confirm if retry needed
                        if (!WidgetUtils.ShowConfirmPopUp($"程序号{pset}下发失败，是否重发？")) {
                            return;
                        }
                    }

                    // Send successfully if step to here
                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetFailedError);
                    boltButton.CurrentParameterSet = pset;

                    // SetPset();
                });
            });
        }

        // 打开管理员密码输入弹框
        public void OpenAdminPasswordPopUpForm(string title) {
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
                } else {
                    WidgetUtils.ShowErrorPopUp("密码错误");
                    _adminPasswordBox.GetTextBox(0).IsError = true;
                }
            }
        }
        // 读取到控制器传回的数据后进行处理
        protected abstract void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId);
        protected void StopMissionManually() {
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

            // Lock all tools
            _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

            // Change mission status
            _activated = false;
            _finished = true;

            if (CheckIfIsMultiDeviceIndependenceMode()) {
                foreach (int sideId in _allBoltsIndependence.Keys) {
                    foreach (int workstationId in _allBoltsIndependence[sideId].Keys) {
                        _allBoltsIndependence[sideId][workstationId].ForEach(b => logger.Debug($"bolt - {b.BoltDTO.serial_num} ShowingWhileWorking: {b.ShowingWhileWorking}"));
                        _allBoltsIndependence[sideId][workstationId].ForEach(b => b.BoltStatus = BoltStatus.DEFAULT);
                    }
                }
                _currentWorkingBoltIndependence.Clear();
            } else {
                foreach (int sideId in _allBolts.Keys) {
                    _allBolts[sideId].ForEach(b => logger.Debug($"bolt - {b.BoltDTO.serial_num} ShowingWhileWorking: {b.ShowingWhileWorking}"));
                    _allBolts[sideId].ForEach(b => b.BoltStatus = BoltStatus.DEFAULT);
                }
                _currentWorkingBolt = null;
            }

            _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.UNACTIVATED;
            _workingProcessPanel.ClearDesc();
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

            // Clear all cached bar codes
            _barCodeObj.Reset();
        }
        // 螺栓拧紧NG时，如果需要管理员输入密码，则调用此方法
        protected void NGConfirmPopUp() => OpenAdminPasswordPopUpForm("拧紧错误，工具已锁止。请输入管理员密码解锁。");
        protected virtual void InitializeAfterHandelCreated() { }
        #endregion

        #region Events
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(GetBarCodeMatchingRules));
            // Load devices asynchronously to avoid delay UI creating
            LoadDevicesAsync();
            // Initialize others
            InitializeAfterHandelCreated();
        }
        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);

            foreach (KeyValuePair<int, ToolTask> tool in _toolTasks) {
                // Clear all delegates once this workplace handle has been destroyed to ensure running performance
                tool.Value.ActionAfterAnalysis = null;
                // Lock all tools
                tool.Value.SendLock();
            }
            foreach (KeyValuePair<int, ArmTask> pair in _armTasks) {
                // Clear all delegates once this workplace handle has been destroyed to ensure running performance
                pair.Value.ActionAfterReceiving = new(c => { });
            }
            _serialPortTasks = MainUtils.SerialPortTasks;
            foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                // Clear all delegates once this workplace handle has been destroyed to make sure it won't throw any exception
                pair.Value.ActionAfterDataReceived = null;
            }
        }
        #endregion
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
        private AWorkplaceContentPanel _workplace;
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

        public BarCodeInputPopUpForm(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated,
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
                mission = _mission;

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
                mission = CommonUtils.CannotBeNull(mission);

                // 如果存在前置任务，则先查询前置任务是否完成
                if (mission.predecessor_mission_id != null) {
                    bool yes = _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(mission.predecessor_mission_id.Value, (int) TighteningStatus.OK) { ProductBarCode = barCode }).Yes;
                    if (!yes) {
                        WidgetUtils.ShowWarningPopUp("未检测到前置任务的加工完成记录，请先完成前置任务");
                        checkPassed = false;
                    }
                }
                // 不管是否有前置任务，只要前面的校验过了，就查询自身的加工记录
                if (checkPassed && _workplace._checkRedo && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(mission.id) { ProductBarCode = barCode }).Yes) {
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
                mission = CommonUtils.CannotBeNull(mission);
                SwitchToMission(mission);
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
                if (_workplace._checkRedo && _workplace.IsRedo != (int) YesOrNo.YES && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(_mission.id) { PartsBarCode = barCode }).Yes) {
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
            _partsBarCodeTitle.Margin = new(0, titleVPadding, 0, 0);

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
        private readonly int _loopingInterval = 50;
        public readonly string TighteningDesc = "正在拧紧{0}号螺丝";
        public readonly string LooseningDesc = "正在反松{0}号螺丝";
        public readonly string ArmPositionError = "力臂未在指定位置";
        public readonly string AdminConfirmation = "需要管理员确认";
        public readonly string PsetNullError = "{0}号螺丝未配置程序号，工具锁定";
        public readonly string PsetFailedError = "{0}号螺丝程序号下发失败，工具锁定";
        public readonly string PsetNotMatchedError = "{0}号螺丝程序号与控制器不匹配，工具锁定";
        public string? CustomError = null;

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
        public void RemoveDesc(string? desc) {
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
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);
            _statusFont = WidgetUtils.GetProperFont(Size, _statusTxt, .375f);
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
        private WorkingProcessPanel _workingProcessPanel;
        private Action? _setPset;
        private BoltButton? _currentWorkingBolt;
        private Dictionary<int, BoltButton> _currentWorkingBoltIndependence;
        private bool _isMultiDeviceIndependenceMode;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private CustomComboBoxGroup<int> _stationComboBox;
        private CustomTextBoxGroup _parameterSetTextBox;

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public Action? SetPset { get => _setPset; set => _setPset = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }

        public ToolOperationPopUpForm(BoltButton? currentWorkingBolt, Dictionary<int, BoltButton> currentWorkingBoltIndependence,
                bool isMultiDeviceIndependenceMode, string categoryName, WorkingProcessPanel workingProcessPanel,
                List<WorkstationDTO> workstationDTOs, Dictionary<int, ToolTask> toolTasks, int? currentWorkstationId, Action? setPset) {
            _currentWorkingBolt = currentWorkingBolt;
            _currentWorkingBoltIndependence = currentWorkingBoltIndependence;
            _isMultiDeviceIndependenceMode = isMultiDeviceIndependenceMode;
            _workstationDTOs = workstationDTOs;
            _toolTasks = toolTasks;
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
            _parameterSetTextBox = new("程序") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                PositiveIntOnly = true,
            };
            _parameterSetTextBox.SetValue(0, "1");

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

                        BoltButton? boltButton = null;
                        int workstationId = _stationComboBox.Value;
                        if (_isMultiDeviceIndependenceMode && _currentWorkingBoltIndependence.ContainsKey(workstationId)) {
                            boltButton = _currentWorkingBoltIndependence[workstationId];
                        } else {
                            boltButton = _currentWorkingBolt;
                        }
                        if (boltButton != null) {
                            boltButton.CurrentParameterSet = pset;
                            _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetFailedError);
                            _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetNullError);
                            if (_setPset != null) {
                                _setPset();
                            }
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
            for (int i = 0; i < list.Count; i++) {
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
