using CustomLibrary.Configs;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_YMT: AWorkplaceMissionView<WorkplaceContentPanel_YMT, WorkplaceTopBar> {
        public WorkplaceMissionView_YMT() { }
        public WorkplaceMissionView_YMT(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_YMT GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_YMT: WorkplaceContentPanel {
        public WorkplaceContentPanel_YMT() { }
        public WorkplaceContentPanel_YMT(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) { }

        // YMT 路由到日聚合导出；导出开关及 BasePath/SortConfig 继承自 WorkplaceContentPanel（读 ExportConfig）
        internal override async Task ExportDataAsync(ExportRequest request) {
            await new YmtDataExportService().ExportAsync(request);
        }
    }
}
