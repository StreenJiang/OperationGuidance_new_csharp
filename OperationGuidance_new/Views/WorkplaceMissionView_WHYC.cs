using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Constants;
using OperationGuidance_new.HttpObjects.Requests;
using OperationGuidance_new.HttpObjects.Response;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using Timer = System.Windows.Forms.Timer;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_WHYC: AWorkplaceMissionView<WorkplaceContentPanel_WHYC, WorkplaceTopBar> {
        public WorkplaceMissionView_WHYC() { }
        public WorkplaceMissionView_WHYC(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_WHYC GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_WHYC: WorkplaceContentPanel {
        private CustomTextBoxGroup _lineBox;
        private CustomTextBoxGroup _operatorBox;

        private const int _delay = 1500;
        private Timer _lineTimer;
        private Timer _operatorTimer;

        public WorkplaceContentPanel_WHYC() { }
        public WorkplaceContentPanel_WHYC(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _lineBox = new("线体") {
                Ratio = 7.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _operatorBox = new("操作人员") {
                Ratio = 7.5,
                NameAlignment = HorizontalAlignment.Right,
            };

            _lineBox.SetValue(0, MainUtils.GetLine_WHYC());
            _operatorBox.SetValue(0, MainUtils.GetOperator_WHYC());

            _lineTimer = new();
            _lineTimer.Interval = _delay;
            _lineTimer.Tick += (s, e) => {
                string lineText = _lineBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(lineText)) {
                    MainUtils.SetLine_WHYC(lineText);
                }
                _lineTimer.Stop();
            };
            _operatorTimer = new();
            _operatorTimer.Interval = _delay;
            _operatorTimer.Tick += (s, e) => {
                string operatorText = _operatorBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(operatorText)) {
                    MainUtils.SetOperator_WHYC(operatorText);
                }
                _operatorTimer.Stop();
            };

            _lineBox.GetTextBox(0).Box.TextChanged += (s, e) => {
                // Debounce
                if (_lineTimer.Enabled) {
                    _lineTimer.Stop();
                }
                _lineTimer.Start();
            };
            _operatorBox.GetTextBox(0).Box.TextChanged += (s, e) => {
                // Debounce
                if (_operatorTimer.Enabled) {
                    _operatorTimer.Stop();
                }
                _operatorTimer.Start();
            };

            _topRightBottom.Controls.Add(_lineBox);
            _topRightBottom.Controls.Add(_operatorBox);

            _anglePanel.Hide();
        }

        protected override void ResizeTopRightMiddleRight(int panelPadding) {
            Size panelSize = new(_topRightMiddleBottom.Width, _topRightMiddleBottom.Height);
            _torquePanel.Size = panelSize;
        }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            if (!_activated) {
                string line = _lineBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(line)) {
                    WidgetUtils.ShowErrorPopUp("线体还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _lineBox.GetTextBox(0).IsError = true;
                    _lineBox.GetTextBox(0).Box.Focus();
                    return;
                }

                string operatorName = _operatorBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(operatorName)) {
                    WidgetUtils.ShowErrorPopUp("操作人员还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _operatorBox.GetTextBox(0).IsError = true;
                    _operatorBox.GetTextBox(0).Box.Focus();
                    return;
                }
            }
            _lineBox.GetTextBox(0).IsError = false;
            _operatorBox.GetTextBox(0).IsError = false;

            base.OpenBarCodePopUpForm();
        }

        protected override async Task<bool> ValidationBeforeActivatingMission() {
            if (!await base.ValidationBeforeActivatingMission()) {
                return false;
            } else {
                string getMatCodeUri = MainUtils.GetMatCodeApi();

                // Check uri
                if (!string.IsNullOrEmpty(getMatCodeUri)) {
                    // Send http request to get mat code
                    HttpRequestGetMatCode request = new(_barCodeObj.ProductBarCode);
                    HttpResponseGetMatCode response = await HttpUtils.SendPost<HttpRequestGetMatCode, HttpResponseGetMatCode>(getMatCodeUri, request);
                    logger.Info($"ucData = [{JsonConvert.SerializeObject(response.ucData)}]");

                    if (response.unStatus == HttpStatus_WHYC.FAILURE) {
                        WidgetUtils.ShowErrorPopUp($"请求MatCode失败，返回信息：{response.ucMsg}");
                        return false;
                    } else if (response.ucData == null || response.ucData.MatCode == null) {
                        WidgetUtils.ShowErrorPopUp($"获取到的MatCode为空！");
                        return false;
                    }

                    _matCode = response.ucData.MatCode;
                }
            }

            return true;
        }

        protected override void StartLockCheckingTask() {
            BeginInvoke(() => {
                Task.Run(async () => {
                    while (!IsDisposed && _activated) {
                        try {
                            CheckCurrentPSetForLockMsg();
                            CheckAdminConfirmationForLockMsg();

                            string statusDesc = string.Empty;
                            if (lockMsgs.Count > 0) {
                                statusDesc = string.Join("\r\n", lockMsgs);
                                statusDesc = string.Format(statusDesc, _workingProcessPanel.BoltSerialNum);
                                // Set status to working proccess panel
                                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                            } else {
                                if (_needLoosening) {
                                    statusDesc = string.Format(WorkingProcessPanel.LooseningDesc, _workingProcessPanel.BoltSerialNum);
                                    _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                } else {
                                    statusDesc = string.Format(WorkingProcessPanel.TighteningDesc, _workingProcessPanel.BoltSerialNum);
                                    _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                                }
                                // Set status to working proccess panel
                                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                            }

                            // Add information
                            if (informationMsgs.Count > 0 && statusDesc.Length > 0) {
                                statusDesc += "\r\n" + string.Join("\r\n", informationMsgs);
                            }

                            // Set description to working proccess panel
                            _workingProcessPanel.StatusDesc = statusDesc;
                        } catch (Exception e) {
                            // Sometimes will throw 'System.InvalidOperationException: cross-thread operation not valid' but don't know why
                            logger.Error($"StartLockCheckingTask: e = {e}");
                        } finally {
                            // Delay a little bit and check again
                            // Set 500ms for WHYC
                            await Task.Delay(500);
                        }
                    }
                });
            });
        }

        protected override async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    // Nonactivated or finished will not handle any received data
                    if (!_activated) {
                        logger.Info($"[WHYC][MISSION:{_mission?.id}|DEVICE:{deviceId}] 任务未激活，跳过拧紧数据处理 - torque={data.torque}, angle={data.angle}");
                        return;
                    }

                    try {
                        logger.Info($"[WHYC][MISSION:{_mission?.id}|DEVICE:{deviceId}] 开始处理拧紧数据 - torque={data.torque}, angle={data.angle}, " +
                                    $"status={data.tightening_status}, result_type={data.result_type}, rundown_time={data.rundown_time}");

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

                            // 参数集对比日志
                            ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                            logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 参数集对比 - " +
                                        $"currentBolt_parameter_set={currentBolt.CurrentParameterSet}, " +
                                        $"tighteningData_parameter_set={data.parameter_set_number}");

                            // Check if current showing side is equal to side of working bolt, if no then switch to the right side
                            if (currentBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                                ProductSideDTO? sideTemp = _sides.Find(s => s.id == currentBolt.BoltDTO.side_id);
                                if (sideTemp != null) {
                                    _currentSideIndex = _sides.IndexOf(sideTemp);
                                    ChangeSideAndInvalidate();
                                }
                            }

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
                            _rundownTime = data.rundown_time;

                            // 数据转换完成日志
                            logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}|WORKSTATION:{workstationDTO.name}] " +
                                        $"数据转换完成 - product_bar_code={missionRecord.product_bar_code}, " +
                                        $"parts_bar_code={missionRecord.parts_bar_code}, product_batch={missionRecord.product_batch}, " +
                                        $"tool={toolDTO.name}({toolDTO.ip}), rundown_time={data.rundown_time}");

                            // If result type is tightening
                            if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                                logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 开始拧紧结果验证 - torque={data.torque}, angle={data.angle}, " +
                                            $"tightening_status={data.tightening_status}, torque_status={data.torque_status}, angle_status={data.angle_status}");

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

                                // Check again to avoid actions which are too fast
                                if (!_activated) {
                                    return;
                                }

                                // Switch to next bolt
                                if (tighteningOK) {
                                    logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 拧紧验证结果 - OK, actual_torque={data.torque}, torque_range=[{boltDTO.torque_min}, {boltDTO.torque_max}], actual_angle={data.angle}, angle_range=[{boltDTO.angle_min}, {boltDTO.angle_max}]");

                                    _errorMsg = null;

                                    // Reset tightening type to tightening in case somewhere did some changes
                                    _needLoosening = false;
                                    RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                    _workingProcessPanel.NGReasons = null;

                                    currentBolt.BoltStatus = BoltStatus.DONE;
                                    logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 螺栓拧紧成功，状态更新为 DONE");

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

                                    // Check again to avoid actions which are too fast
                                    if (!_activated) {
                                        return;
                                    }

                                    // Store data
                                    dataDTO.tightening_status = (int) TighteningStatus.OK;
                                    StoreTighteningData(dataDTO);

                                    if (nextIndex < currentSideBolts.Count) {
                                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                                            _currentWorkingBoltIndependence[workstationId] = SwitchBolt(workstationId, nextIndex);
                                            ChangeBoltStatusToWorking(_currentWorkingBoltIndependence[workstationId]);
                                            logger.Info($"[WHYC][MISSION:{_mission?.id}|WORKSTATION:{workstationId}] 切换到下一个螺栓 (index={nextIndex})");
                                        } else {
                                            _currentWorkingBolt = SwitchBolt(nextIndex);
                                            ChangeBoltStatusToWorking(_currentWorkingBolt);
                                            logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 切换到下一个螺栓 (index={nextIndex})");
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
                                                logger.Info($"[WHYC][MISSION:{_mission?.id}] 当前产品面完成，切换到下一个产品面 (index={_currentSideIndex})");
                                            }
                                        }

                                        if (allDone) {
                                            // Update mission result to ok
                                            _missionRecord.mission_result = (int) TighteningStatus.OK;
                                            _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                            logger.Info($"[WHYC][MISSION:{_mission?.id}|RECORD_ID:{_missionRecord.id}] 任务完成 - mission_result=OK, " +
                                                        $"product_bar_code={_missionRecord.product_bar_code}, product_batch={_missionRecord.product_batch}, " +
                                                        $"is_redo={_missionRecord.is_redo}, rundown_time={_rundownTime}");

                                            // Checks for challenge mission
                                            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                                logger.Info($"[WHYC][MISSION:{_mission?.id}] 添加挑战任务完成记录");
                                                AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                            }

                                            TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                        }
                                    }
                                } else {
                                    logger.Warn($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 螺栓拧紧失败 - " +
                                                $"ng_times={currentBolt.NgTimes}, tightening_status=NG, error_msg={errorMsg}");

                                    // Change bolt status
                                    currentBolt.BoltStatus = BoltStatus.ERROR;

                                    // Count ng times
                                    currentBolt.NgTimes++;

                                    // Set error message
                                    _workingProcessPanel.NGReasons = errorMsg;
                                    AddInformationMsg(_workingProcessPanel.NGReasons);

                                    // WHYC
                                    _errorMsg = errorMsg;

                                    // 记录数据
                                    StoreTighteningData(dataDTO);

                                    // Set status of data to ng
                                    dataDTO.tightening_status = (int) TighteningStatus.NG;
                                }
                            } else {
                                logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 处理反松结果 - result_type=LOOSENING, " +
                                            $"torque={data.torque}, angle={data.angle}");

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
                                    logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 反松数据已存储");
                                } else {
                                    logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{boltDTO.serial_num}] 反松数据未存储 (配置禁用)");
                                }
                            }
                        }
                    } catch (Exception e) {
                        logger.Error($"[WHYC][MISSION:{_mission?.id}|DEVICE:{deviceId}] 处理拧紧数据时发生错误 - " +
                                    $"torque={data.torque}, angle={data.angle}, error={e.Message}, stack_trace={e.StackTrace}", e);
                    }
                });
            });
        }

        public override async Task TerminateMission(WorkplaceProcessStatus status) {
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

        private async void UploadDataToMES(OperationDataDTO operationDataDTO) {
            string uploadDataUri = MainUtils.GetUploadDataApi();

            logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 开始上传数据到MES - " +
                        $"workstation={operationDataDTO.workstation_name}, torque={operationDataDTO.torque}, " +
                        $"angle={operationDataDTO.angle}, result={((TighteningStatus) operationDataDTO.tightening_status.Value).ToString()}");

            // Check uri
            if (!string.IsNullOrEmpty(uploadDataUri)) {
                WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == operationDataDTO.workstation_id);

                // Send http request to upload data to MES
                HttpRequestUploadData request = new() {
                    QrCode = _barCodeObj.ProductBarCode,
                    Line = _lineBox.GetTextBox(0).Box.Text,
                    StationName = workstationDTO.name,
                    StaffName = _operatorBox.GetTextBox(0).Box.Text,
                    MatCode = _matCode,
                    Trosion = $"{_round_3(operationDataDTO.torque)}N.m",
                    TrosionStd = $"{_round_3(operationDataDTO.torque_final_target)}N.m",
                    TrosionUp = $"{_round_3(operationDataDTO.torque_max_limit)}N.m",
                    TrosionDow = $"{_round_3(operationDataDTO.torque_min_limit)}N.m",
                    Time = $"{_round_3((float) _rundownTime / 1000)}s",
                    Circle = $"{_round_3((float) operationDataDTO.rundown_angle / 360)}圈",
                    Angle = $"{operationDataDTO.rundown_angle}°",
                    Result = ((TighteningStatus) operationDataDTO.tightening_status.Value).ToString(),
                    Error = _errorMsg,
                    CreateTime = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS_FFF),
                    Seq = operationDataDTO.bolt_serial_num,
                    SumQty = _sumBoltDone,
                };

                HttpResponseUploadData response = await HttpUtils.SendPost<HttpRequestUploadData, HttpResponseUploadData>(uploadDataUri, request);
                if (response.unStatus == HttpStatus_WHYC.FAILURE) {
                    logger.Error($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 上传数据到MES失败 - " +
                                $"status={response.unStatus}, message={response.ucMsg}");
                    WidgetUtils.ShowErrorPopUp($"上传数据失败，返回信息：{response.ucMsg}");
                } else {
                    logger.Info($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 上传数据到MES成功");
                }
            } else {
                logger.Warn($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 未配置MES上传地址，跳过数据上传");
            }

            string _round_3<T>(T? num) {
                if (num != null && num is int numInt) {
                    return Math.Round((decimal) numInt, 3).ToString("0.000");
                }

                if (num != null && num is float numFloat) {
                    return Math.Round((decimal) numFloat, 3).ToString("0.000");
                }

                return "0.000";
            }
        }

        protected override async Task StoreTighteningData(OperationDataDTO operationDataDTO) {
            logger.Debug($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 开始存储拧紧数据");

            await base.StoreTighteningData(operationDataDTO);
            UploadDataToMES(operationDataDTO);

            logger.Debug($"[WHYC][MISSION:{_mission?.id}|BOLT:{operationDataDTO.bolt_serial_num}] 拧紧数据存储完成");
        }

        // Action after receving bar code msg
        private void ActionAfterRecevingBarCode(string msg) {
            if (!IsDisposed && !_activated) {
                // 交给弹窗处理
                if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                    OpenBarCodePopUpForm(msg);
                } else {
                    _barCodePopUpForm.ValidateBarCode(msg);
                }
            }
        }
    }
}
