using CustomLibrary.Configs;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using S7.Net;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_GLB: AWorkplaceMissionView<WorkplaceContentPanel_GLB, WorkplaceTopBar> {
        public WorkplaceMissionView_GLB() { }
        public WorkplaceMissionView_GLB(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_GLB GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_GLB: WorkplaceContentPanel {
        private List<OuterDatabaseConfigGlbDTO>? _outerDatabases = null;
        private List<OperationDataDTO> _operationDatasCached = new();

        public WorkplaceContentPanel_GLB() { }
        public WorkplaceContentPanel_GLB(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _outerDatabases = _apis.QueryOuterDatabaseConfigGlbList(new(SystemUtils.MacAddressesDTO.id)).OuterDatabaseConfigGlbDTOs;
        }

        private void StoreTighteningDataToOuterDatabase() {
            if (_outerDatabases != null && _outerDatabases.Count > 0 && _missionRecord != null && _operationDatasCached.Count > 0) {
                _outerDatabases.ForEach(database => {
                    _apis.AddDataToOuterDatabaseGlb(new(database, _missionRecord, _operationDatasCached));
                });
            }
            _operationDatasCached = new();
        }

        protected override async Task StoreTighteningData(OperationDataDTO operationDataDTO) {
            await base.StoreTighteningData(operationDataDTO);
            _operationDatasCached.Add(operationDataDTO);
        }

        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            StoreTighteningDataToOuterDatabase();

            // Send job finished signal to plc
            if (_communicationTask != null && _communicationTask.Connected
                && _communicationTask.CommunicationType is CommunicationSiemensPlc && _communicationTask.PlcServer != null
                && _communicationTask.PlcServer.Plc != null && _communicationTask.PlcServer.Plc.IsConnected) {
                PlcServer_GLB plcServer = (PlcServer_GLB) _communicationTask.PlcServer;
                bool result = _missionRecord.mission_result == (int) TighteningStatus.OK;
                plcServer.SendJobFinished(true);
                plcServer.SendJobResult(result);
            }

            await base.TerminateMission(status);
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            StoreTighteningDataToOuterDatabase();
            base.OnHandleDestroyed(e);
        }

        protected override async void ActivateMissionAutomatically() {
            if (_mission.id > 0) {
                // If is self looping mode, then activate mission automatically
                if (MainUtils.IsMissionSelfLoopingModeEnabled()) {
                    // Wait for .5 seconds
                    await Task.Delay(500);

                    // Activate mission
                    ActivateMission();
                } else if (MainUtils.IsPLCBarCodeSelfLoopingEnabled()) {
                    _ = StartReadFromPLC();
                }
            }
        }

        private async Task StartReadFromPLC() {
            if (WidgetUtils.ShowConfirmPopUp("是否立即读取PLC条码信息？")) {
                bool processCompleted = false;
                while (!processCompleted) {
                    // Wait for .5 seconds to ensure data is the latest
                    await Task.Delay(500);

                    try {
                        if (_communicationTask != null && _communicationTask.Connected
                            && _communicationTask.CommunicationType is CommunicationSiemensPlc && _communicationTask.PlcServer != null
                            && _communicationTask.PlcServer.Plc != null && _communicationTask.PlcServer.Plc.IsConnected) {
                            PlcServer_GLB plcServer = (PlcServer_GLB) _communicationTask.PlcServer;

                            int waitTime = 5000;
                            int waitTimeCount = 0;
                            int waitEach = 250;

                            // Fetching barCode
                            string barCode = "";
                            while (waitTimeCount < waitTime) {
                                string? barCodeTemp = plcServer.ReadBarCode();
                                if (string.IsNullOrEmpty(barCodeTemp)) {
                                    await Task.Delay(waitEach);
                                    waitTimeCount += waitEach;
                                    continue;
                                }

                                barCode = barCodeTemp.Trim();
                                logger.Info($"Get bar code[{barCode}] from plcs...");
                                break;
                            }

                            if (!string.IsNullOrEmpty(barCode)) {
                                try {
                                    // Send barCode read done
                                    plcServer.SendBarCodeReadDone();

                                    // Wait for start signal
                                    waitTimeCount = 0;
                                    bool startSignal = false;
                                    while (waitTimeCount < waitTime) {
                                        startSignal = plcServer.ReadStartSignal();
                                        if (!startSignal) {
                                            await Task.Delay(waitEach);
                                            waitTimeCount += waitEach;
                                            continue;
                                        }

                                        logger.Info($"Get start signal[{startSignal}] from plcs...");
                                        break;
                                    }

                                    if (startSignal) {
                                        processCompleted = true;

                                        // Analyze bar code
                                        ActionAfterRecevingBarCode(barCode);
                                    } else {
                                        logger.Warn($"Did not get any start signal from plcs...");
                                        WidgetUtils.ShowWarningPopUp($"任务启动失败，读取到条码信息【{barCode}】，但未读取到启动信号。已结束本次与 PLC 的通信。如需重新通信，请手动点击条码输入框。");
                                        break;
                                    }
                                } finally {
                                    // Reset barCode read done
                                    plcServer.ResetBarCodeReadDone();
                                }
                            } else {
                                logger.Warn($"Did not get any bar code from plcs...");
                            }
                        } else {
                            Load();
                            logger.Warn("PLC connection is unstable, get bar code from PLC failed, trying to reload...");
                        }
                    } catch (Exception ex) {
                        logger.Warn("Exception while communicating with PLC...", ex);
                    }

                    if (!processCompleted && !WidgetUtils.ShowConfirmPopUp("未读取到PLC条码信息，是否重新读取？")) {
                        logger.Warn("Confirm not to read bar code from PLC again, break the loop...");
                        break;
                    }
                }
            }
        }

        // Initialize mod bus server
        protected override void InitializeAfterHandelCreated() => Load();

        // Load Communication tasks
        private void Load() {
            if (MainUtils.IsPLCBarCodeSelfLoopingEnabled()) {
                _communicationTasks = MainUtils.CommunicationTasks;
                foreach (CommunicationTask task in _communicationTasks.Values) {
                    if (task.CommunicationType is CommunicationSiemensPlc) {
                        _communicationTask = task;
                        break;
                    }
                }

                if (_communicationTask != null && _communicationTask.Connected) {
                    // Close first if exists, because we need a new one each time
                    if (_communicationTask.PlcServer != null) {
                        _communicationTask.PlcServer.Dispose();
                        _communicationTask.PlcServer = null;
                    }

                    try {
                        PlcConfig_GLB plcConfig_GLB = MainUtils.PlcConfig_GLB;
                        CpuType cupType = plcConfig_GLB.GetCpuType();
                        _communicationTask.PlcServer = new PlcServer_GLB(cupType,
                                                                         _communicationTask.Ip,
                                                                         plcConfig_GLB);
                        _communicationTask.PlcServer.Connect();
                    } catch (InvalidOperationException ioe) {
                        logger.Error("Error while connecting to PLC", ioe);
                        WidgetUtils.ShowWarningPopUp($"连接 PLC 失败！{ioe.Message}");
                    } catch (Exception ex) {
                        logger.Error("Error while connecting to PLC", ex);
                        WidgetUtils.ShowWarningPopUp("连接 PLC 失败！请检查配置/网络。");
                    }
                }
            }
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

        protected override void OpenBarCodePopUpForm(string? barcode = null) {
            if (MainUtils.IsPLCBarCodeSelfLoopingEnabled() && barcode == null) {
                _ = StartReadFromPLC();
            } else {
                base.OpenBarCodePopUpForm(barcode);
            }
        }

        protected override void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            string taskName = _mission?.name ?? "Unknown";
            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Entry, deviceId={deviceId}, torque={data.torque}, angle={data.angle}, tightening_status={data.tightening_status}, result_type={data.result_type}");

            BeginInvoke(() => {
                // Nonactivated or finished will not handle any received data
                if (!_activated) {
                    logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Task not activated, ignoring data");
                    return;
                }

                try {
                    logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Getting ToolTask for deviceId={deviceId}");
                    ToolTask toolTask = _toolTasks[deviceId];
                    // Lock first
                    if (MainUtils.IsArmLocatingEnabled()) {
                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Arm locating enabled, sending ForceSendLock");
                        toolTask.ForceSendLock();
                    }
                    if (toolTask.WorkstationId != null) {
                        int workstationId = toolTask.WorkstationId.Value;
                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - WorkstationId={workstationId}");

                        List<WorkstationDTO> workstationDTOs;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            workstationDTOs = _workstationsDTOs.Where(dto => _currentWorkingBoltIndependence.Keys.Contains(dto.id)).ToList();
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Independence mode, workstationDTOs count={workstationDTOs.Count}");
                        } else {
                            List<int> workstationIds = new();
                            foreach (List<BoltButton> bolts in _allBolts.Values) {
                                workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                            }
                            workstationIds = workstationIds.Distinct().ToList();
                            workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Normal mode, workstationDTOs count={workstationDTOs.Count}");
                        }
                        List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                        // Main display
                        _torquePanel.Data = data.torque + "";
                        _anglePanel.Data = data.angle + "";
                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Displayed torque={data.torque}, angle={data.angle}");

                        // Get current bolt
                        BoltButton currentBolt;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            currentBolt = _currentWorkingBoltIndependence[workstationId];
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Independence mode, currentBolt serial_num={currentBolt.BoltDTO.serial_num}");
                        } else {
                            currentBolt = CommonUtils.CannotBeNull(_currentWorkingBolt);
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Normal mode, currentBolt serial_num={currentBolt.BoltDTO.serial_num}");
                        }

                        // Check if current showing side is equal to side of working bolt, if no then switch to the right side
                        if (currentBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Side mismatch, bolt side_id={currentBolt.BoltDTO.side_id}, current side_id={_sides[_currentSideIndex].id}");
                            ProductSideDTO? sideTemp = _sides.Find(s => s.id == currentBolt.BoltDTO.side_id);
                            if (sideTemp != null) {
                                _currentSideIndex = _sides.IndexOf(sideTemp);
                                ChangeSideAndInvalidate();
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Switched to side index={_currentSideIndex}");
                            }
                        }

                        ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                        OperationDataDTO dataDTO = new();
                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Converting TighteningData to OperationDataDTO");
                        CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);
                        // Set pset manualy if tool type is sudong x7
                        if (toolTask.ToolType is ToolSudongX7 toolX7) {
                            dataDTO.parameter_set_number = currentBolt.CurrentParameterSet;
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - ToolSudongX7 detected, pset={currentBolt.CurrentParameterSet}");
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
                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - DataDTO prepared: workstation={workstationDTO.name}, tool={toolDTO.name}, bolt_serial={boltDTO.serial_num}, vin={missionRecord.product_bar_code}");

                        // WHYC
                        // TZYX
                        _rundownTime = data.rundown_time;

                        // If result type is tightening
                        if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Result type is TIGHTENING");
                            bool tighteningOK = true;
                            string errorMsg = "";
                            // Initialize color to ok
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

                            // Check tightening status
                            if (data.tightening_status != (int) TighteningStatus.OK) {
                                tighteningOK = false;
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - tightening_status NG: {data.tightening_status}, error_status={data.tightening_error_status}");
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
                                    logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Tightening error: {errorMsgTemp}");
                                }
                                if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                    _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                    logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - torque_status NG: {data.torque_status}");
                                }
                                if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                    _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                    logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - angle_status NG: {data.angle_status}");
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
                                logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Torque out of range: value={data.torque}, min={boltDTO.torque_min}, max={boltDTO.torque_max}");
                            }

                            // Check angle
                            if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                tighteningOK = false;
                                _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "角度与配置范围不符";
                                logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Angle out of range: value={data.angle}, min={boltDTO.angle_min}, max={boltDTO.angle_max}");
                            }

                            // Switch to next bolt
                            if (tighteningOK) {
                                logger.Info($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Tightening OK, bolt_serial={boltDTO.serial_num}, torque={data.torque}, angle={data.angle}");
                                _errorMsg = null;

                                // Reset tightening type to tightening in case somewhere did some changes
                                _needLoosening = false;
                                RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                _workingProcessPanel.NGReasons = null;

                                currentBolt.BoltStatus = BoltStatus.DONE;
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Bolt status set to DONE");

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
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Next bolt index={nextIndex}, total bolts={currentSideBolts.Count}");

                                // Store data
                                dataDTO.tightening_status = (int) TighteningStatus.OK;
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Storing tightening data (OK)");
                                StoreTighteningData(dataDTO);

                                if (nextIndex < currentSideBolts.Count) {
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        _currentWorkingBoltIndependence[workstationId] = SwitchBolt(workstationId, nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBoltIndependence[workstationId]);
                                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Switched to next bolt (independence mode), index={nextIndex}");
                                    } else {
                                        _currentWorkingBolt = SwitchBolt(nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBolt);
                                        logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Switched to next bolt (normal mode), index={nextIndex}");
                                    }
                                } else {
                                    bool allDone = true;
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        foreach (int id in _allBoltsIndependence[_sides[_currentSideIndex].id].Keys) {
                                            if (id != workstationId) {
                                                BoltButton? boltButton = _allBoltsIndependence[_sides[_currentSideIndex].id][id].Find(b => b.BoltStatus != BoltStatus.DONE);
                                                if (boltButton != null) {
                                                    allDone = false;
                                                    logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Independence mode, workstation {id} still has pending bolts");
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
                                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Switched to next side, sideIndex={_currentSideIndex}");
                                        }
                                    }

                                    if (allDone) {
                                        logger.Info($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - All bolts completed, mission finished OK");
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
                                logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Tightening NG, bolt_serial={boltDTO.serial_num}, torque={data.torque}, angle={data.angle}, error={errorMsg}");
                                // Change bolt status
                                currentBolt.BoltStatus = BoltStatus.ERROR;

                                // Count ng times
                                currentBolt.NgTimes++;
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Bolt NG times={currentBolt.NgTimes}");

                                // Set error message
                                _workingProcessPanel.NGReasons = errorMsg;
                                AddInformationMsg(_workingProcessPanel.NGReasons);

                                // WHYC
                                // TZYX
                                _errorMsg = errorMsg;

                                // Mission failed
                                if (_mission.max_ng_num != 0 && currentBolt.NgTimes >= _mission.max_ng_num) {
                                    logger.Error($"[GLB:DoAfterRecevingTighteningDataAsync] Max NG count reached, terminating mission");

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
                                    logger.Debug($"[GLB:DoAfterRecevingTighteningDataAsync] Setting mode to LOOSENING for retry");

                                    // 记录数据
                                    StoreTighteningData(dataDTO);

                                    // 需要管理员密码弹窗
                                    if (_mission.password_need_time != 0 && currentBolt.NgTimes >= _mission.password_need_time) {
                                        AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                                        _adminConfirmed = false;
                                        logger.Warn($"[GLB:DoAfterRecevingTighteningDataAsync] NG count reached password threshold, requesting admin confirmation");

                                        // 先记录数据再打开弹窗
                                        BoltNGConfirmPopUp();
                                    }
                                }
                            }
                        } else {
                            logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Result type is LOOSENING");
                            _needLoosening = false;

                            // 反松结束后把扭矩角度改回黑色
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                            // Remove error message
                            RemoveLockMsg(_workingProcessPanel.NGReasons);
                            _workingProcessPanel.NGReasons = null;

                            if (MainUtils.GetStoreLooseningData()) {
                                logger.Debug($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Storing loosening data");
                                // 记录数据
                                StoreTighteningData(dataDTO);
                            }
                        }
                    } else {
                        logger.Warn($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - ToolTask.WorkstationId is null for deviceId={deviceId}");
                    }
                } catch (Exception e) {
                    logger.Error($"[Workplace:{taskName}] DoAfterRecevingTighteningDataAsync - Error occurred while handling tightening data, e: {e}");
                }
            });
        }
    }
}
