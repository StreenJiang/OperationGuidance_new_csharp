using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_SCII: AWorkplaceMissionView<WorkplaceContentPanel_SCII, WorkplaceTopBar_SCII> {
        public WorkplaceMissionView_SCII() { }
        public WorkplaceMissionView_SCII(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_SCII GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
                PaddingWithoutBorder = true,
            };
        }
    }

    public class WorkplaceContentPanel_SCII: AWorkplaceContentPanel {
        private WorkplaceMissionView_SCII _view;

        // 上方
        protected CustomContentPanel _top;
        // 上方左边
        protected CustomContentPanel _topLeft;
        // 上方左边上面
        protected WorkplacePiece _barCodeOuter;
        // 上方左边下面
        protected WorkplacePiece _imageDisplayOuter;
        // 上方右边
        protected CustomContentPanel _topRight;
        // 上方右边的上面
        protected WorkplacePiece _topRightTop;
        // 上方右边的中间
        protected CustomContentPanel _topRightMiddle;
        // 上方右边的中间的左边
        protected WorkplacePiece _topRightMiddleLeft;
        // 上方右边的中间的右边
        protected WorkplacePiece _topRightMiddleRight;
        // 上方右边的下面
        protected WorkplacePiece _topRightBottom;

        // 中间
        protected WorkplacePiece _middle;

        // 下方
        protected WorkplacePiece _bottom;

        // 其他自定义组件
        private CustomComboBoxGroup<string> _batchDropDownBox;
        private volatile bool _missionNGAdminConfirmed = true;

        public WorkplaceMissionView_SCII View { get => _view; set => _view = value; }


        protected List<ScrewBitCounterDTO> screwBitCounterDTOsCached;
        protected Dictionary<int, CustomTextBoxGroup> _screwBitCounterBoxes;
        protected Dictionary<int, CustomTextBoxGroup> _screwBitRemainingBoxes;
        protected Dictionary<int, ScrewBitCounterDTO> _screwBitCounterDtos;

        // private Label _productSideTitle;
        // private List<Image?> _smallSideImagesForShowing;
        // private PictureBox _smallSideImage;
        // private TableLayoutPanel _buttonPanel;
        // private PageSwitchButton _first;
        // private PageSwitchButton _backward;
        // private PageSwitchButton _forward;
        // private PageSwitchButton _last;
        // private Label _pageInfo;
        public WorkplaceContentPanel_SCII() { }
        public WorkplaceContentPanel_SCII(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _actionAfterSendingPset = SetPset;

            // 初始化所有组件
            InitializeOuters();
            InitializeTopRightMiddleRight();
            InitializeTopRightBottom();
            InitializeMiddle();
            InitializeBottom();

            _checkRedo = true;
            _toolControlNeedAdminPasswor = true;
        }

        protected override void InitSerialPortTasks(KeyValuePair<int, SerialPortTask> pair) {
            logger.Debug($"[SCII:InitSerialPortTask] Initializing serial port task for device ID: {pair.Key}");

            SerialPortTask serialPortTask = pair.Value;
            serialPortTask.ActionAfterDataReceived = async msg => {
                await Task.Run(() => {
                    BeginInvoke(() => {
                        if (!IsDisposed) {
                            // Check for mission ng admin confirmation
                            if (!_missionNGAdminConfirmed) {
                                logger.Debug($"[SCII:InitSerialPortTask] Mission NG admin not confirmed, skipping message");
                                return;
                            }

                            DeviceSerialPortDTO dto = _serialPorts.Single(dto => dto.id == pair.Key);
                            // 如果有空的数据进来，则跳过
                            if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
                                logger.Warn($"[SCII:InitSerialPortTask] Message is null from serial port device ID {pair.Key}, please check.");
                                return;
                            }
                            if (dto.invalid_char != null) {
                                msg = String.Concat(msg.Where(c => !dto.invalid_char.Contains(c)));
                                logger.Debug($"[SCII:InitSerialPortTask] Removed invalid characters from message for device ID {pair.Key}");
                            }

                            // 交给弹窗处理
                            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                                logger.Info($"[SCII:InitSerialPortTask] Opening barcode popup form with message for device ID {pair.Key}");
                                OpenBarCodePopUpForm(msg);
                            } else {
                                logger.Debug($"[SCII:InitSerialPortTask] Validating barcode message for device ID {pair.Key}");
                                _barCodePopUpForm.ValidateBarCode(msg);
                            }
                        }
                    });
                });
            };
            logger.Debug($"[SCII:InitSerialPortTask] Serial port task initialized for device ID: {pair.Key}");
        }

        protected override void ActionAfterAllInitialized() {
            logger.Debug($"[SCII:ActionAfterAllInitialized] Adding interrupt button and setting up event handlers");

            CommonButton terminateMissionBtn = _missionSelectedName.AddButton<CommonButton>("中断");
            terminateMissionBtn.Enabled = true;
            terminateMissionBtn.Click += (s, e) => {
                if (_activated) {
                    logger.Info($"[SCII:ActionAfterAllInitialized] Interrupt button clicked, mission is active, requesting admin password");
                    if (OpenAdminPasswordPopUpForm("任务异常重置任务，请管理员输入权限密码", false)) {
                        logger.Info($"[SCII:ActionAfterAllInitialized] Admin password confirmed, terminating mission with NG status");
                        _ = TerminateMission(WorkplaceProcessStatus.FINISHED_NG);
                    } else {
                        logger.Debug($"[SCII:ActionAfterAllInitialized] Admin password confirmation failed or cancelled");
                    }
                } else {
                    logger.Warn($"[SCII:ActionAfterAllInitialized] Interrupt button clicked but mission is not activated");
                    WidgetUtils.ShowNoticePopUp("任务未激活");
                }
            };
            logger.Debug($"[SCII:ActionAfterAllInitialized] Interrupt button event handler configured");
        }

        protected override async void ActivateMissionAutomatically() {
            logger.Debug($"[SCII:ActivateMissionAutomatically] Checking if USB scanner is enabled");

            if (MainUtils.IsUSBScannerEnabled()) {
                logger.Info($"[SCII:ActivateMissionAutomatically] USB scanner is enabled, waiting for barcode-related initialization to complete");

                while (!_barcodeRelatedDone) {
                    await Task.Delay(50);
                }
                logger.Info($"[SCII:ActivateMissionAutomatically] Barcode-related initialization completed, opening barcode popup form");
                BeginInvoke(() => OpenBarCodePopUpForm());
            } else {
                logger.Debug($"[SCII:ActivateMissionAutomatically] USB scanner is not enabled, skipping automatic activation");
            }
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
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _barCodeOuter.Controls.Add(_barCodePictureBox);
            _barCodeOuter.Controls.Add(_barCodeTextBox);
            _barCodeOuter.Click += barCodePopUp;

            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
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
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _topRightTop.Controls.Add(_operatorInfoTitle);
            _topRightTop.Controls.Add(_operatorName);
            _topRightTop.Controls.Add(_operatorId);

            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的左边
            _topRightMiddleLeft = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _workingProcessPanel.ConerRadius = 0;
            _topRightMiddleLeft.Controls.Add(_workingProcessPanel);

            // 上方右边的中间的右边
            _topRightMiddleRight = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };

            // 中间
            _middle = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
        }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            logger.Debug($"[SCII:OpenBarCodePopUpForm] Opening barcode popup form, activated: {_activated}, barcode: {barCode ?? "null"}");

            if (!_activated) {
                string batchNum = _productBatch.GetTextBox(0).Box.Text;
                logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission not activated, checking product batch: {batchNum}");

                if (string.IsNullOrEmpty(batchNum)) {
                    logger.Warn($"[SCII:OpenBarCodePopUpForm] Product batch is empty, showing error and focusing batch field");
                    WidgetUtils.ShowErrorPopUp("产品批次还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _productBatch.GetTextBox(0).IsError = true;
                    _productBatch.GetTextBox(0).Box.Focus();
                    return;
                } else {
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Product batch provided: {batchNum}, checking SCII batch configuration");
                    SciiBatchConfig sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
                    if (sciiBatchConfig.enabled == (int) YesOrNo.YES) {
                        logger.Debug($"[SCII:OpenBarCodePopUpForm] SCII batch configuration is enabled");
                        string? shiftTemp = _batchDropDownBox.Value;
                        if (string.IsNullOrEmpty(shiftTemp)) {
                            logger.Warn($"[SCII:OpenBarCodePopUpForm] Shift selection is empty, showing error");
                            WidgetUtils.ShowErrorPopUp("请选择班次");
                            if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                                _barCodePopUpForm.Hide();
                            }
                            _batchDropDownBox.SetError(true);
                            return;
                        } else {
                            logger.Debug($"[SCII:OpenBarCodePopUpForm] Shift selected: {shiftTemp}");
                            string[] shifts = shiftTemp.Split(",");
                            if (string.IsNullOrEmpty(shifts[0]) || shifts[0] != batchNum) {
                                logger.Warn($"[SCII:OpenBarCodePopUpForm] Product batch does not match shift, batch: {batchNum}, shift batch: {shifts[0]}");
                                WidgetUtils.ShowErrorPopUp("产品批次与班次不匹配");
                                if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                                    _barCodePopUpForm.Hide();
                                }
                                _productBatch.GetTextBox(0).IsError = true;
                                _batchDropDownBox.SetError(true);
                                return;
                            } else {
                                logger.Debug($"[SCII:OpenBarCodePopUpForm] Product batch matches shift, checking completion count limit");
                                List<MissionRecordDTO> missionRecordDTOs = GetRecoreds();
                                int okSum = missionRecordDTOs.Where(dto => dto.mission_result == (int) TighteningStatus.OK).Count();
                                logger.Debug($"[SCII:OpenBarCodePopUpForm] Current OK count: {okSum}, limit: {shifts[1]}");
                                if (okSum >= int.Parse(shifts[1])) {
                                    logger.Warn($"[SCII:OpenBarCodePopUpForm] Batch completion count has reached limit, requesting admin confirmation");
                                    bool confirmed = OpenAdminPasswordPopUpForm("当前批次完成数已达上限，需管理员确认。请输入管理员密码解锁", false);
                                    if (!confirmed) {
                                        logger.Debug($"[SCII:OpenBarCodePopUpForm] Admin confirmation failed or cancelled, batch limit reached");
                                        if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                                            _barCodePopUpForm.Hide();
                                        }
                                        _productBatch.GetTextBox(0).IsError = true;
                                        _batchDropDownBox.SetError(true);
                                        return;
                                    } else {
                                        logger.Info($"[SCII:OpenBarCodePopUpForm] Admin confirmed to exceed batch limit");
                                    }
                                }
                            }
                        }
                    } else {
                        logger.Debug($"[SCII:OpenBarCodePopUpForm] SCII batch configuration is disabled");
                    }
                }

                _batchDropDownBox?.SetError(false);
            } else {
                logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission is already activated");
            }

            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                logger.Info($"[SCII:OpenBarCodePopUpForm] Creating new barcode popup form");

                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission activated, getting excluded rules for current bolt");
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission not activated, getting general excluded rules");
                }

                _barCodePopUpForm = new BarCodeInputPopUpForm_SCII(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated,
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode, _rulesExcluded, CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode)) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                if (!_activated) {
                    logger.Debug($"[SCII:OpenBarCodePopUpForm] Adding 'Activate Mission' button");
                    _barCodePopUpForm.AddButton("激活任务").Click += (sender, eventArgs) => {
                        if (!_activated) {
                            if (!_barCodePopUpForm.CheckCanActivateMission()) {
                                logger.Debug($"[SCII:OpenBarCodePopUpForm] Cannot activate mission yet, barcode validation failed");
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
                                logger.Info($"[SCII:OpenBarCodePopUpForm] Activating mission");
                                ActivateMission();
                                _barCodePopUpForm.Dispose();
                            }
                        } else {
                            logger.Debug($"[SCII:OpenBarCodePopUpForm] Mission already activated, closing popup");
                            _barCodePopUpForm.Dispose();
                        }
                    };
                }
                _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.Dispose();
                _barCodePopUpForm.PretendToShowToCreateHandlesForChildren();
                _barCodePopUpForm.ResizeSelf();
                logger.Debug($"[SCII:OpenBarCodePopUpForm] Barcode popup form created and initialized");
            } else {
                logger.Debug($"[SCII:OpenBarCodePopUpForm] Using existing barcode popup form");
            }
            logger.Info($"[SCII:OpenBarCodePopUpForm] Showing barcode popup form");
            _barCodePopUpForm.Show();
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
        protected virtual void InitializeTopRightBottom() {
            _productSumPerDay.Ratio = 6.85;
            _ngRatePerDay.Ratio = 6.85;

            _missionSelectedName.Ratio = 8.45;
            _productBatch.Ratio = 8.45;

            _topRightBottom.Controls.Add(_missionDetailTitle);
            _topRightBottom.Controls.Add(_missionSelectedName);

            _topRightBottom.Controls.Add(_productBatch);
            // 特殊处理
            SciiBatchConfig sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            if (sciiBatchConfig.enabled == (int) YesOrNo.YES) {
                _productBatch.Ratio = 6.85;
                _productSumPerDay.TextName = "批次计数";
                _batchDropDownBox = new("早晚班") {
                    NameAlignment = HorizontalAlignment.Right,
                };
                _batchDropDownBox.ItemSelected += () => {
                    _batchDropDownBox.SetError(false);
                    _productBatch.GetTextBox(0).IsError = false;

                    ResetMissionDetails();
                };
                Dictionary<string, string>? dayShifts = sciiBatchConfig.GetDayShifts();
                if (dayShifts != null) {
                    foreach (var shift in dayShifts) {
                        _batchDropDownBox.AddItem(shift.Value, shift.Key);
                    }
                }
                Dictionary<string, string>? nightShifts = sciiBatchConfig.GetNightShifts();
                if (nightShifts != null) {
                    foreach (var shift in nightShifts) {
                        _batchDropDownBox.AddItem(shift.Value, shift.Key);
                    }
                }

                _topRightBottom.Controls.Add(_batchDropDownBox);
            }

            _topRightBottom.Controls.Add(_productSumPerDay);
            _topRightBottom.Controls.Add(_okSumPerDay);
            _topRightBottom.Controls.Add(_ngRatePerDay);
            _topRightBottom.Controls.Add(_pset);

            HandleScrewBitCounter();
        }

        protected virtual void HandleScrewBitCounter() {
            if (_topRightBottom == null) {
                return;
            }

            if (_screwBitCounterBoxes != null && _screwBitCounterBoxes.Count > 0) {
                foreach (var pair in _screwBitCounterBoxes) {
                    _topRightBottom.Controls.Remove(_screwBitCounterBoxes[pair.Key]);
                    _topRightBottom.Controls.Remove(_screwBitRemainingBoxes[pair.Key]);
                }
            }

            _screwBitCounterBoxes = new();
            _screwBitRemainingBoxes = new();
            _screwBitCounterDtos = new();

            screwBitCounterDTOsCached = _apis.FindScrewBitCounterByMissionId(new(_mission.id)).ScrewBitCounterDTOs;
            if (screwBitCounterDTOsCached.Count > 0) {
                for (int i = 0; i < screwBitCounterDTOsCached.Count; i++) {
                    ScrewBitCounterDTO dto = screwBitCounterDTOsCached[i];
                    if (_screwBitCounterDtos.ContainsKey(dto.bit_position)) {
                        continue;
                    }
                    if (i >= 4) { // More than 4 are not supported.
                        break;
                    }
                    _screwBitCounterDtos.Add(dto.bit_position, dto);

                    CustomTextBoxGroup boxGroup = new("批头计数" + dto.bit_position) {
                        ReadOnly = true,
                        Enabled = false,
                        NameAlignment = HorizontalAlignment.Right,
                        Ratio = 6.85,
                    };
                    boxGroup.GetTextBox(0).Box.Text = dto.current_counts > 0 ? dto.current_counts + "" : "0";

                    CustomTextBoxGroup boxGroup2 = new("剩余量") {
                        ReadOnly = true,
                        Enabled = false,
                        NameAlignment = HorizontalAlignment.Right,
                    };
                    boxGroup2.GetTextBox(0).Box.Text = (dto.max_num - dto.current_counts) + "";

                    _screwBitCounterBoxes.Add(dto.bit_position, boxGroup);
                    _screwBitRemainingBoxes.Add(dto.bit_position, boxGroup2);
                    _topRightBottom.Controls.Add(boxGroup);
                    _topRightBottom.Controls.Add(boxGroup2);
                }
            }
        }
        protected override void ActionAfterSwitchMission() {
            base.ActionAfterSwitchMission();
            ResetMissionDetails();
        }

        private void ResetMissionDetails() {
            logger.Debug($"[SCII:ResetMissionDetails] Resetting mission details");

            SetTodayData();
            SetPset();
            HandleScrewBitCounter();
            ResizeChildren();
        }
        private void SetTodayData() {
            logger.Debug($"[SCII:SetTodayData] Setting today's data");

            int sum = 0;
            int okSum = 0;
            double ngRate = 0;

            if (_mission.id > 0) {
                List<MissionRecordDTO> missionRecordDTOs = GetRecoreds();
                logger.Debug($"[SCII:SetTodayData] Retrieved {missionRecordDTOs.Count} mission records");

                IEnumerable<MissionRecordDTO> distinctData = missionRecordDTOs
                            .DistinctBy(dto => dto.product_bar_code);
                sum = distinctData.Count();
                okSum = distinctData
                            .Where(dto => dto.mission_result == (int) TighteningStatus.OK)
                            .Count();
                if (sum > 0) {
                    ngRate = (sum - okSum) / (double) sum * 100;
                }
                logger.Debug($"[SCII:SetTodayData] Calculated statistics - Total: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
            } else {
                logger.Debug($"[SCII:SetTodayData] Mission ID is 0, skipping data retrieval");
            }

            _productSumPerDay.SetValue(0, sum + "");
            _okSumPerDay.SetValue(0, okSum + "");
            _ngRatePerDay.SetValue(0, $"{ngRate.ToString("F2")}%");
            logger.Debug($"[SCII:SetTodayData] UI updated - Sum: {sum}, OK: {okSum}, NG Rate: {ngRate:F2}%");
        }
        private List<MissionRecordDTO> GetRecoreds() {
            logger.Debug($"[SCII:GetRecoreds] Getting mission records for mission ID: {_mission.id}");

            QueryMissionRecordListReq req = new() {
                MissionId = _mission.id,
            };

            // 如果打开了《班次配置》，则根据班次计算
            SciiBatchConfig sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            if (sciiBatchConfig.enabled == (int) YesOrNo.YES) {
                logger.Debug($"[SCII:GetRecoreds] SCII batch configuration is enabled, filtering by shift");

                string? shiftTemp = _batchDropDownBox?.Value;
                if (!string.IsNullOrEmpty(shiftTemp)) {
                    string[] shifts = shiftTemp.Split(",");
                    req.ProductBatch = shifts[0];
                    logger.Debug($"[SCII:GetRecoreds] Using product batch from shift: {req.ProductBatch}");
                } else {
                    logger.Warn($"[SCII:GetRecoreds] Shift selection is empty, returning empty list");
                    return new();
                }
            } else {
                req.Date = DateTime.Now;
                logger.Debug($"[SCII:GetRecoreds] Using current date: {req.Date}");
            }

            List<MissionRecordDTO> result = _apis.QueryMissionRecordList(req).MissionRecordDTOs;
            logger.Debug($"[SCII:GetRecoreds] Retrieved {result.Count} mission records");
            return result;
        }
        private void SetPset() => SetPset(null);
        private void SetPset(string? customMsg) {
            if (InvokeRequired) {
                Invoke(() => SetPset(customMsg));
                return;
            }
            if (!string.IsNullOrEmpty(customMsg)) {
                _pset.SetValue(0, customMsg);
                logger.Debug($"[SCII:SetPset] Set pset to custom message: {customMsg}");
            } else if (_currentWorkingBolt != null) {
                string psetValue = _currentWorkingBolt.CurrentParameterSet?.ToString() ?? "未配置程序号";
                _pset.SetValue(0, psetValue);
                logger.Debug($"[SCII:SetPset] Set pset to current bolt parameter set: {psetValue}");
            } else {
                _pset.SetValue(0, null);
                logger.Debug($"[SCII:SetPset] Cleared pset value");
            }
        }
        protected override void RefreshImageDisplayPanel() => ResizeTopLeftBottom();

        // 初始化中间
        private void InitializeMiddle() {
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
                Parent = _middle,
                HeaderHeight = WidgetUtils.WorkplaceGridViewHeaderHeight(),
                RowsHeight = WidgetUtils.WorkplaceGridViewContentRowHeight(),
                PageHeight = WidgetUtils.WorkplaceGridViewPageInfoHeight(),
                ColumnsPaddingRatio = WidgetUtils.WorkplaceGridViewColumnsPaddingRatio(),
                AutoDown = true,
            };
            _tighteningDataPanel.HandleCreated += (s, e) => {
                // 创建快照以避免UI初始化期间的竞态条件
                var snapshot = _tighteningDataVOs.ToList();
                _tighteningDataPanel.DataSource = snapshot;
            };
        }

        // 初始化底部
        private void InitializeBottom() {
            foreach (DeviceBlock block in _deviceBlocks) {
                block.BorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER;
                _bottom.Controls.Add(block);
            }
            _bottom.Controls.Add(_timeDisplayerOuter);
        }

        protected override async void SetMissionDetails() {
            logger.Debug($"[SCII:SetMissionDetails] Setting mission details for mission: {_mission.name}");

            _missionSelectedName.SetValue(0, _mission.name);
            logger.Debug($"[SCII:SetMissionDetails] Mission name set to: {_mission.name}");

            ResetMissionDetails();
        }

        protected override void ActionAfterArmDataReceived(int maxValue, Coordinates3D armCoordinates) {
            logger.Debug($"[SCII:ActionAfterArmDataReceived] Received arm data, maxValue: {maxValue}, coordinates: {armCoordinates}");

            Task.Run(() => {
                BeginInvoke(() => {
                    if (_activated && _currentWorkingBolt != null) {
                        logger.Debug($"[SCII:ActionAfterArmDataReceived] Mission activated and current bolt exists");

                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
                        if (toolId != null) {
                            logger.Debug($"[SCII:ActionAfterArmDataReceived] Tool ID found: {toolId}");

                            ToolTask toolTask = _toolTasks[toolId.Value];
                            Coordinates3D boltCoordinates = Coordinates3D.FromString(boltDTO.position);
                            _realTimeArmCoordinates = armCoordinates;

                            logger.Debug($"[SCII:ActionAfterArmDataReceived] Bolt coordinates: {boltCoordinates}, real-time arm coordinates: {_realTimeArmCoordinates}");

                            // Can't lock/unlock tools manually while arm is running (Only for SCII)
                            RemoveLockMsg(WorkingProcessPanel.UnlockedManually);
                            RemoveLockMsg(WorkingProcessPanel.LockedManually);
                            logger.Debug($"[SCII:ActionAfterArmDataReceived] Removed manual lock/unlock messages");

                            if (CheckArmPosition(maxValue, armCoordinates, boltCoordinates)) {
                                // Location ok, so remove locked reason of position
                                RemoveLockMsg(WorkingProcessPanel.LockedArmPosition);
                                logger.Debug($"[SCII:ActionAfterArmDataReceived] Arm position is OK, removed position lock message");
                            } else {
                                // Location because of position
                                AddLockMsg(WorkingProcessPanel.LockedArmPosition);
                                logger.Warn($"[SCII:ActionAfterArmDataReceived] Arm position is not OK, added position lock message");
                            }

                            // 需要管理员输入密码并确认
                            if (_adminConfirmed != null) {
                                logger.Debug($"[SCII:ActionAfterArmDataReceived] Admin confirmation status: {_adminConfirmed}");

                                // 管理员已确认
                                if (_adminConfirmed.Value) {
                                    RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
                                    _adminConfirmed = null;
                                    logger.Info($"[SCII:ActionAfterArmDataReceived] Admin confirmed, removed admin confirmation lock");
                                }
                                // 管理员未确认
                                else {
                                    AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                                    logger.Warn($"[SCII:ActionAfterArmDataReceived] Admin not confirmed, added admin confirmation lock");
                                    if (_adminPasswordPopUpForm == null || _adminPasswordPopUpForm.IsDisposed) {
                                        _adminConfirmed = false;
                                        logger.Info($"[SCII:ActionAfterArmDataReceived] Opening bolt NG confirmation popup");
                                        BoltNGConfirmPopUp();
                                    }
                                }
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
                                logger.Debug($"[SCII:ActionAfterArmDataRemoved] No admin confirmation needed, removed admin confirmation lock");
                            }

                            // 当前点位没有设置程序号
                            if (_currentWorkingBolt.CurrentParameterSet == null) {
                                logger.Warn($"[SCII:ActionAfterArmDataReceived] Current parameter set is null");
                                // 如果是没有配置就显示对应错误信息，否则可能是下发失败
                                if (_currentWorkingBolt.BoltDTO.parameters_set == null) {
                                    AddLockMsg(WorkingProcessPanel.LockedPsetNull);
                                    logger.Warn($"[SCII:ActionAfterArmDataReceived] Parameter set not configured, added pset null lock");
                                } else {
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                                    logger.Debug($"[SCII:ActionAfterArmDataReceived] Parameter set exists but not current, removed pset null lock");
                                }
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                                logger.Debug($"[SCII:ActionAfterArmDataReceived] Parameter set is configured: {_currentWorkingBolt.CurrentParameterSet}");
                            }
                        } else {
                            logger.Warn($"[SCII:ActionAfterArmDataReceived] No tool ID found for workstation");
                        }
                    } else {
                        logger.Debug($"[SCII:ActionAfterArmDataReceived] Mission not activated or no current bolt");
                    }
                });
            });
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
        protected virtual void ResizeOuters(int boxHeight, int titleHeight, int contentVPadding) {
            int extraHeightTopRightBottom = 0;
            if (_screwBitCounterBoxes.Count > 0) {
                extraHeightTopRightBottom += boxHeight * _screwBitCounterBoxes.Count + contentVPadding * _screwBitCounterBoxes.Count;
            }

            int padding = Padding.Left / 2;
            int workplaceWidth = Width - Padding.Left * 2;
            int workplaceHeight = Height - Padding.Top * 2;
            int barCodeHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceBarCodeHeightRatio());
            int imagePanelHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio());
            imagePanelHeight += extraHeightTopRightBottom;
            int topHeight = barCodeHeight + imagePanelHeight + padding;
            int bottomHeight = (int) (workplaceHeight * .045);
            int middleHeight = workplaceHeight - topHeight - bottomHeight - padding * 2; // 为了取整
            int topLeftWidth = (int) (workplaceWidth * WidgetUtils.WorkplaceLeftWidthRatio());
            int topRightWidth = workplaceWidth - topLeftWidth - padding;
            int topRightTopHeight = titleHeight + boxHeight + contentVPadding * 2;
            int topRightBottomHeight = titleHeight + boxHeight * 4 + contentVPadding * 5;
            topRightBottomHeight += extraHeightTopRightBottom;
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
            _bottom.Padding = new(0, 0, 1, 0);
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
        protected virtual void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _missionDetailTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _missionDetailTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
            int boxWidth2 = _operatorInfoTitle.Parent.Width - contentHPadding * 2;

            SciiBatchConfig sciiBatchConfig = ConfigUtils.LoadConfig<SciiBatchConfig>();
            if (sciiBatchConfig.enabled == (int) YesOrNo.YES) {
                _productBatch.Size = new(boxWidth, boxHeight);
                _productBatch.Margin = new(contentHPadding, contentVPadding, 0, 0);
                _batchDropDownBox.Size = new(boxWidth, boxHeight);
                _batchDropDownBox.Margin = new(contentHPadding, contentVPadding, 0, 0);
            } else {
                _productBatch.Size = new(boxWidth2, boxHeight);
                _productBatch.Margin = new(contentHPadding, contentVPadding, 0, 0);
            }

            foreach (KeyValuePair<int, CustomTextBoxGroup> pair in _screwBitCounterBoxes) {
                CustomTextBoxGroup boxGroup = pair.Value;
                boxGroup.Size = new(boxWidth, boxHeight);
                boxGroup.Margin = new(contentHPadding, contentVPadding, 0, 0);
            }
            if (_screwBitRemainingBoxes != null) {
                foreach (KeyValuePair<int, CustomTextBoxGroup> pair in _screwBitRemainingBoxes) {
                    CustomTextBoxGroup boxGroup = pair.Value;
                    boxGroup.Size = new(boxWidth, boxHeight);
                    boxGroup.Margin = new(contentHPadding, contentVPadding, 0, 0);
                }
            }

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
                    control.Size = new(_bottom.Height, _bottom.Height - 1);
                    blocksWidth += _bottom.Height;
                }
            }
            int timeDisplayerWidth = _bottom.Width - blocksWidth;
            _timeDisplayerOuter.Size = new(timeDisplayerWidth - 2, _bottom.Height - 2);
            _timeDisplayer.Font = new Font(WidgetsConfigs.SystemFontFamily, _bottom.Height * .4f, FontStyle.Regular, GraphicsUnit.Pixel);
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
        //     _last.ClicSk += (sender, eventArgs) => {
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

        protected override void OpenBoltPopUpForm(ProductBoltDTO boltDTO, BoltButton boltBtn) {
            List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id) { MissionId = _mission.id }).BarCodeMatchingRuleDTOs;
            _boltPopUpForm = new BoltPopUpForm_SCII(boltDTO, barCodeMatchingRuleDTOs) {
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

        protected override void ToolOperationPopUpFormExtraActions(ToolOperationPopUpForm popUpForm) {
            if (_activated) {
                popUpForm.BtnLock.Enabled = false;
                popUpForm.BtnUnlock.Enabled = false;
            }
        }

        protected override void AdminPopUpExtraActions() {
            if (_adminPasswordPopUpForm != null && !_adminPasswordPopUpForm.IsDisposed) {
                _adminPasswordPopUpForm.CloseButton.Enabled = false;
                _adminPasswordPopUpForm.Buttons[1].Enabled = false;
            }
        }

        protected void MissionNGConfirmPopUp(string msg) {
            logger.Info($"[SCII:MissionNGConfirmPopUp] Opening mission NG confirmation popup, message: {msg}");

            _missionNGAdminConfirmed = false;
            logger.Debug($"[SCII:MissionNGConfirmPopUp] Set admin confirmation flag to false");

            while (!_missionNGAdminConfirmed) {
                logger.Debug($"[SCII:MissionNGConfirmPopUp] Waiting for admin confirmation...");
                _missionNGAdminConfirmed = OpenAdminPasswordPopUpForm(msg, true);
                if (_missionNGAdminConfirmed) {
                    logger.Info($"[SCII:MissionNGConfirmPopUp] Admin confirmation received");
                } else {
                    logger.Warn($"[SCII:MissionNGConfirmPopUp] Admin confirmation failed or cancelled, retrying...");
                }
            }
            logger.Debug($"[SCII:MissionNGConfirmPopUp] Admin confirmation loop completed");
        }

        protected override async Task ActionAfterActivatingMission() {
            logger.Debug($"[SCII:ActionAfterActivatingMission] Action after activating mission started");

            await base.ActionAfterActivatingMission();

            // Clear data grid view
            _tighteningDataVOs.Clear();
            RefreshTighteningDataPanel(_tighteningDataVOs);
            logger.Debug($"[SCII:ActionAfterActivatingMission] Data grid view cleared");

            // Set product batch
            _missionRecord.product_batch = _productBatch.GetTextBox(0).Box.Text;
            logger.Debug($"[SCII:ActionAfterActivatingMission] Product batch set: {_missionRecord.product_batch}");
            _apis.AddOrUpdateMissionRecord(new(_missionRecord));
            logger.Info($"[SCII:ActionAfterActivatingMission] Mission record updated with product batch");
        }

        protected override async Task<bool> ValidationBeforeActivatingMission() {
            logger.Debug($"[SCII:ValidationBeforeActivatingMission] Validating before activating mission");

            if (await base.ValidationBeforeActivatingMission()) {
                if (await CheckScrewBitCount()) {
                    // 更新批头计数器 boxes
                    if (screwBitCounterDTOsCached.Count > 0) {
                        for (int i = 0; i < screwBitCounterDTOsCached.Count; i++) {
                            var dto = screwBitCounterDTOsCached[i];
                            int bit_position = dto.bit_position;
                            _screwBitCounterBoxes[bit_position].GetTextBox(0).Box.Text = dto.current_counts > 0 ? dto.current_counts + "" : "0";
                            _screwBitRemainingBoxes[bit_position].GetTextBox(0).Box.Text = (dto.max_num - dto.current_counts) + "";
                        }
                    }
                    return true;
                }
                return false;
            }
            return false;
        }

        protected virtual async Task<bool> CheckScrewBitCount() {
            // Count screw bit used time
            ScrewBitCounterDTO screwBitCounter;
            if (!CountScrewBitUsedTime(out screwBitCounter)) {
                _adminConfirmed = false;
                bool confirmed = OpenAdminPasswordPopUpForm(
                    $"({screwBitCounter.bit_position})号位批头将超过使用上限【{screwBitCounter.max_num}次】，需更换批头。更换批头后，请输入管理员密码",
                    false);
                if (confirmed) {
                    _adminConfirmed = null;
                    screwBitCounter.current_counts = 0;
                    _apis.AddOrUpdateScrewBitCounter(new(screwBitCounter));

                    // Check again to ensure no more screw bit needs to be replaced
                    return await ValidationBeforeActivatingMission();
                }
                return false;
            }
            return true;
        }

        private bool CountScrewBitUsedTime(out ScrewBitCounterDTO screwBitCounter) {
            // Check first
            foreach (ScrewBitCounterDTO sbc in screwBitCounterDTOsCached) {
                if (sbc.current_counts + sbc.count_each_time > sbc.max_num) {
                    screwBitCounter = sbc;
                    logger.Warn($"[SCII:CountScrewBitUsedTime] Screw bit at position {sbc.bit_position} will exceed usage limit");
                    return false;
                }
            }

            // Update
            foreach (ScrewBitCounterDTO sbc in screwBitCounterDTOsCached) {
                sbc.current_counts += sbc.count_each_time;
                _apis.AddOrUpdateScrewBitCounter(new(sbc));
                logger.Debug($"[SCII:CountScrewBitUsedTime] Updated screw bit at position {sbc.bit_position}, new count: {sbc.current_counts}");
            }

            screwBitCounter = new();
            logger.Info($"[SCII:CountScrewBitUsedTime] All screw bits within usage limits");
            return true;
        }

        protected override async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Received tightening data, device ID: {deviceId}, torque: {data.torque}, angle: {data.angle}, status: {data.tightening_status}");

            await Task.Run(() => {
                BeginInvoke(() => {
                    // Nonactivated or finished will not handle any received data
                    if (!_activated) {
                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Mission not activated, skipping data");
                        return;
                    }

                    try {
                        ToolTask toolTask = _toolTasks[deviceId];
                        // Lock first
                        toolTask.ForceSendLock();
                        if (toolTask.WorkstationId != null && _currentWorkingBolt != null) {
                            logger.Info($"[SCII:DoAfterRecevingTighteningDataAsync] Action running after received tightening data...");

                            int workstationId = toolTask.WorkstationId.Value;

                            List<int> workstationIds = new();
                            foreach (List<BoltButton> bolts in _allBolts.Values) {
                                workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                            }
                            workstationIds = workstationIds.Distinct().ToList();
                            List<WorkstationDTO> workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                            List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                            // Main display
                            _torque.Text = data.torque + "";
                            _angle.Text = data.angle + "";
                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Updated display - torque: {_torque.Text}, angle: {_angle.Text}");

                            // Get current bolt
                            BoltButton currentBolt = _currentWorkingBolt;
                            ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                            OperationDataDTO dataDTO = new();
                            CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);
                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Converted data to OperationDataDTO");

                            // Set pset manualy if tool type is sudong x7
                            if (toolTask.ToolType is ToolSudongX7 toolX7) {
                                dataDTO.parameter_set_number = currentBolt.CurrentParameterSet;
                                logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Set pset manually for Sudong X7 tool: {dataDTO.parameter_set_number}");
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
                            dataDTO.mission_record_id = _missionRecord.id;
                            dataDTO.vin_number = _missionRecord.product_bar_code;
                            if (_realTimeArmCoordinates != null) {
                                dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                            }
                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Populated OperationDataDTO with workstation and tool info");

                            // If result type is tightening
                            if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                                logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Processing tightening result");

                                bool tighteningOK = true;
                                string errorMsg = "";
                                // Initialize color to ok
                                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

                                // Check tightening status
                                if (data.tightening_status != (int) TighteningStatus.OK) {
                                    tighteningOK = false;
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Tightening status not OK: {data.tightening_status}");
                                }
                                if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                    tighteningOK = false;
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Torque status not OK: {data.torque_status}");
                                }
                                if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                    tighteningOK = false;
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Angle status not OK: {data.angle_status}");
                                }

                                // Check torque
                                if (boltDTO.torque_max > 0 && (data.torque < boltDTO.torque_min || data.torque > boltDTO.torque_max)) {
                                    tighteningOK = false;
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += "扭矩与配置范围不符";
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Torque out of range: {data.torque}, min: {boltDTO.torque_min}, max: {boltDTO.torque_max}");
                                }

                                // Check angle
                                if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                    tighteningOK = false;
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += "角度与配置范围不符";
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Angle out of range: {data.angle}, min: {boltDTO.angle_min}, max: {boltDTO.angle_max}");
                                }

                                // Switch to next bolt
                                if (tighteningOK) {
                                    logger.Info($"[SCII:DoAfterRecevingTighteningDataAsync] Tightening OK, switching to next bolt");

                                    DoAfterTighteningOk();

                                    // Reset tightening type to tightening in case somewhere did some changes
                                    _needLoosening = false;
                                    RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                    _workingProcessPanel.NGReasons = null;

                                    currentBolt.BoltStatus = BoltStatus.DONE;
                                    currentBolt.Label = data.torque.ToString("0.00");
                                    logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Current bolt status set to DONE with torque label: {currentBolt.Label}");

                                    // Check next index
                                    List<BoltButton> currentSideBolts = _allBolts[_sides[_currentSideIndex].id];
                                    int nextIndex = currentSideBolts.IndexOf(currentBolt) + 1;
                                    // 检查是否存在跳点的情况
                                    while (nextIndex < currentSideBolts.Count && currentSideBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                        nextIndex++;
                                    }

                                    // Store data
                                    dataDTO.tightening_status = (int) TighteningStatus.OK;
                                    StoreTighteningData(dataDTO);
                                    logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Stored tightening data with OK status");

                                    if (nextIndex < currentSideBolts.Count) {
                                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Switching to next bolt at index: {nextIndex}");
                                        _currentWorkingBolt = SwitchBolt(nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBolt);
                                    } else {
                                        logger.Info($"[SCII:DoAfterRecevingTighteningDataAsync] All bolts completed, mission finished successfully");

                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Mission record updated with OK result");

                                        // Checks for challenge mission
                                        if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                            AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                            logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Challenge mission result added");
                                        }

                                        // 重置任务信息
                                        ResetMissionDetails();

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
                                } else {
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Tightening NG, error: {errorMsg}");

                                    // Change bolt status
                                    currentBolt.BoltStatus = BoltStatus.ERROR;

                                    // Count ng times
                                    currentBolt.NgTimes++;
                                    logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] Bolt NG times: {currentBolt.NgTimes}, max allowed: {_mission.max_ng_num}");

                                    // Set custom error message
                                    _workingProcessPanel.NGReasons = errorMsg;
                                    AddInformationMsg(_workingProcessPanel.NGReasons);

                                    // Mission failed
                                    if (_mission.max_ng_num != 0 && currentBolt.NgTimes >= _mission.max_ng_num) {
                                        logger.Error($"[SCII:DoAfterRecevingTighteningDataAsync] Max NG count reached, terminating mission");

                                        // 重置任务信息
                                        ResetMissionDetails();

                                        // 记录数据
                                        StoreTighteningData(dataDTO);

                                        // Stop the mission
                                        TerminateMission(WorkplaceProcessStatus.FINISHED_NG);

                                        // 先记录数据再弹出提示
                                        // WidgetUtils.ShowErrorPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败");
                                        MissionNGConfirmPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败。请输入管理员密码");
                                    } else {
                                        _needLoosening = true;
                                        _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                        logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Setting mode to LOOSENING for retry");

                                        // 记录数据
                                        StoreTighteningData(dataDTO);

                                        // 需要管理员密码弹窗
                                        if (_mission.password_need_time != 0 && currentBolt.NgTimes >= _mission.password_need_time) {
                                            AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                                            _adminConfirmed = false;
                                            logger.Warn($"[SCII:DoAfterRecevingTighteningDataAsync] NG count reached password threshold, requesting admin confirmation");

                                            // 先记录数据再打开弹窗
                                            BoltNGConfirmPopUp();
                                        }
                                    }

                                    dataDTO.tightening_status = (int) TighteningStatus.NG;
                                }
                            } else {
                                logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Processing loosening result");

                                _needLoosening = false;

                                // 反松结束后把扭矩角度改回黑色
                                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                                // Remove error message
                                RemoveLockMsg(_workingProcessPanel.NGReasons);
                                _workingProcessPanel.NGReasons = null;

                                if (MainUtils.GetStoreLooseningData()) {
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                    logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Stored loosening data");
                                } else {
                                    logger.Debug($"[SCII:DoAfterRecevingTighteningDataAsync] Skipping loosening data storage based on configuration");
                                }
                            }
                        }
                    } catch (Exception e) {
                        logger.Error($"[SCII:DoAfterRecevingTighteningDataAsync] Error occurred while handling tightening data, e: {e}");
                    }
                });
            });
        }

        protected virtual void DoAfterTighteningOk() { }
        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            logger.Info($"[SCII:TerminateMission] Terminating mission with status: {status}");

            await base.TerminateMission(status);

            logger.Debug($"[SCII:TerminateMission] Base termination completed");

            // // If it's challenge mission, then switch mission automatically
            // if (_mission.is_challenge_mission == (int) YesOrNo.YES
            //         && _missionRecord != null
            //         && _missionRecord.mission_result == (int) TighteningStatus.OK) {
            //     _view.OpenWorkplaceView(_mission.challenge_mission_id);
            // }
            logger.Debug($"[SCII:TerminateMission] Mission termination completed");
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }

        public override void VisibleToTrue() {
            SetOperatorInfo();
        }
    }

}
