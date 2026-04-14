using CustomLibrary.Configs;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
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
                plcServer.SendJobFinished(result);
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
    }
}
