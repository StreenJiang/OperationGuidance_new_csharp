using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView_YMT: VariableSettingsView {
        protected override async Task ExportTestAsync(ExportRequest request) {
            await new YmtDataExportService().ExportAsync(request);
        }
    }
}
