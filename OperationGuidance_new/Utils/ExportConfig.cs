using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Utils {
    public class ExportConfig {
        private static readonly object _lock = new();
        private static ExportConfig? _instance;

        public static ExportConfig Instance {
            get {
                if (_instance == null) {
                    lock (_lock) {
                        _instance ??= new ExportConfig();
                    }
                }
                return _instance;
            }
        }

        private Settings _settings;
        private List<int> _cachedSortConfig;

        public bool ExcelExportEnabled =>
            _settings.data_storage_excel_export_enabled == (int)YesOrNo.YES;
        public bool TxtExportEnabled =>
            _settings.data_storage_txt_export_enabled == (int)YesOrNo.YES;
        public string StoragePath => _settings.data_storage_path;
        public List<int> SortConfig => _cachedSortConfig;

        private ExportConfig() { Reload(); }

        public void Reload() {
            _settings = ConfigUtils.LoadConfig<Settings>();
            _cachedSortConfig = JsonConvert.DeserializeObject<List<int>>(_settings.data_storage_fields)
                ?? MainUtils.GetDefaultSortConfig();
        }

        public void SetExcelExportEnabled(bool value) {
            _settings.data_storage_excel_export_enabled =
                value ? (int)YesOrNo.YES : (int)YesOrNo.NO;
            ConfigUtils.SaveConfig(_settings);
        }

        public void SetTxtExportEnabled(bool value) {
            _settings.data_storage_txt_export_enabled =
                value ? (int)YesOrNo.YES : (int)YesOrNo.NO;
            ConfigUtils.SaveConfig(_settings);
        }
    }
}
