using CustomLibrary.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

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
                // Wait for .5 seconds
                await Task.Delay(500);

                // If is self looping mode, then activate mission automatically
                if (MainUtils.IsMissionSelfLoopingModeEnabled()) {
                    ActivateMission();
                } else if (MainUtils.IsPLCBarCodeSelfLoopingEnabled()) {
                    if (ModBusServer != null) {
                        string barCode = ((ModBusServer_GLB) ModBusServer).BarCdoe.ASCIIStringValue;
                        logger.Info($"Get bar code[{barCode}] from plcs");
                        ActionAfterRecevingBarCode(barCode);
                    }
                }
            }
        }

        // Initialize mod bus server
        protected override void InitializeAfterHandelCreated() {
            if (_communicationTask != null) {
                // ModBusServer = new ModBusServer_GLB(MainUtils.(), MainUtils.GetPLCBarCodeLength());
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
