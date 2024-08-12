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
using System.Text;

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
        }

        protected override void StoreTighteningData(OperationDataDTO operationDataDTO) {
            base.StoreTighteningData(operationDataDTO);
            _operationDatasCached.Add(operationDataDTO);
        }

        public override void TerminateMission(WorkplaceProcessStatus status) {
            StoreTighteningDataToOuterDatabase();
            base.TerminateMission(status);
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
                    if (_communicationTask != null && _communicationTask.Connected
                            && _communicationTask.CommunicationType is CommunicationSiemensPlc && _communicationTask.PlcServer != null
                            && _communicationTask.PlcServer.Plc != null && _communicationTask.PlcServer.Plc.IsConnected) {
                        _communicationTask.Reading = true;

                        // Wait for .5 seconds to ensure data is latest
                        await Task.Delay(500);

                        int waitTime = 5000;
                        int waitTimeCount = 0;
                        int waitEach = 250;

                        bool readOk = false;
                        while (waitTimeCount < waitTime) {
                            if (_communicationTask.PlcServer.DataBytes == null) {
                                await Task.Delay(waitEach);
                                waitTimeCount += waitEach;
                                continue;
                            }

                            string barCode = Encoding.ASCII.GetString(_communicationTask.PlcServer.DataBytes);
                            barCode = barCode.Trim();
                            logger.Info($"Get bar code[{barCode}] from plcs");
                            readOk = true;

                            // Analyze bar code
                            ActionAfterRecevingBarCode(barCode);
                            break;
                        }
                        if (!readOk) {
                            WidgetUtils.ShowWarningPopUp($"Can't get any bar code from PLC[{_communicationTask.Name}]");
                        }

                        _communicationTask.Reading = false;
                    }
                }
            }
        }

        // Initialize mod bus server
        protected override void InitializeAfterHandelCreated() {
            if (MainUtils.IsPLCBarCodeSelfLoopingEnabled()) {
                foreach (CommunicationTask task in _communicationTasks.Values) {
                    _communicationTask = task;
                    break;
                }

                if (_communicationTask != null && _communicationTask.Connected) {
                    CpuType cupType = Enum.Parse<CpuType>(MainUtils.GetPLCModel());
                    int db = MainUtils.GetPLCDBAddress();
                    string registerNo = MainUtils.GetPLCDBRegisterNo();
                    int bitAddress = MainUtils.GetPLCDBBitAddress();
                    int dataLength = MainUtils.GetPLCBarCodeLength();

                    if (_communicationTask.PlcServer != null) {
                        _communicationTask.PlcServer.Dispose();
                        _communicationTask.PlcServer = null;
                    }
                    _communicationTask.PlcServer = new PlcServer_GLB(cupType, _communicationTask.Ip, db, registerNo, bitAddress, dataLength);
                    _communicationTask.PlcServer.Connect();
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
    }
}
