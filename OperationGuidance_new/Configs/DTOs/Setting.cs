using System.Reflection;
using Newtonsoft.Json;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Configs.DTOs {
    public class Settings: ConfigBase {
        public string resolution { get; set; }

        public string data_storage_path { get; set; }
        public string data_storage_fields { get; set; }
        public string data_storage_fields_curr { get; set; }
        public string data_storage_name_format { get; set; }
        public int data_storage_store_loosening_data { get; set; }

        public int mission_arm_locating_enabled { get; set; }
        public int mission_arm_locating_accuracy { get; set; }
        public int mission_error_prompt_for_arm { get; set; }
        public int mission_self_looping_mode { get; set; }
        public int plc_bar_code_self_looping { get; set; }
        public int auto_lock_tool { get; set; }
        public string mat_code_api { get; set; }
        public string upload_data_api { get; set; }
        public int usb_scanner { get; set; }
        public string line_whyc { get; set; }
        public string operator_whyc { get; set; }
        public int auto_launch { get; set; }
        public int auto_login { get; set; }
        public string auto_login_info { get; set; }

        public int reverse_arranger { get; set; }
        public int hide_loosening_data_in_workplace { get; set; }
        public int screw_counter_max { get; set; }

        public Settings() {
            resolution = "";

            data_storage_path = GetDefaultStoragePath();
            data_storage_fields = GetDefaultSortConfigStr();
            data_storage_fields_curr = string.Empty;
            data_storage_name_format = MainUtils.DATETIME_FORMAT_YYYY_MM_DD;
            data_storage_store_loosening_data = YesOrNo.YES.ToInt();

            mission_arm_locating_enabled = YesOrNo.YES.ToInt();
            mission_arm_locating_accuracy = 100;
            mission_error_prompt_for_arm = YesOrNo.NO.ToInt();
            mission_self_looping_mode = YesOrNo.NO.ToInt();
            plc_bar_code_self_looping = YesOrNo.NO.ToInt();
            auto_lock_tool = YesOrNo.NO.ToInt();

            mat_code_api = "";
            upload_data_api = "";

            usb_scanner = YesOrNo.NO.ToInt();

            line_whyc = "";
            operator_whyc = "";

            auto_launch = YesOrNo.NO.ToInt();
            auto_login = YesOrNo.NO.ToInt();
            auto_login_info = "";

            reverse_arranger = YesOrNo.YES.ToInt();
            hide_loosening_data_in_workplace = YesOrNo.NO.ToInt();
            screw_counter_max = 4;
        }

        public string GetResolutionBySize(Size size) => $"{size.Width}, {size.Height}";

        public List<int> GetSortConfig() {
            List<int>? list = JsonConvert.DeserializeObject<List<int>>(data_storage_fields);
            if (list == null || list.Count == 0) {

            }
            return GetDefaultSortConfig();
        }

        private List<int> GetDefaultSortConfig() {
            return new List<int>() {
                33, 44, 14, 20, 18, 17, 15, 24, 22, 21, 16, 13, 11, 10, 47, 48
            };
        }

        private string GetDefaultSortConfigStr() {
            return JsonConvert.SerializeObject(GetDefaultSortConfig());
        }

        public List<OperationDataField> GetOperationDataFields() {
            List<PropertyInfo> props = typeof(OperationDataVO).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            List<OperationDataField> fields = new();
            int index = 1;
            props.ForEach(p => {
                IEnumerable<Attribute> enumerable = p.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute gridColumn) {
                        string fieldName;
                        if (gridColumn.ColumnName != null && gridColumn.ColumnName != string.Empty) {
                            fieldName = gridColumn.ColumnName;
                        } else {
                            fieldName = p.Name;
                        }
                        string propertyName = p.Name;
                        fields.Add(new(index++, fieldName, propertyName, false));
                    }
                }
            });
            // Get config
            List<int> sortConfig = GetSortConfig();
            fields = fields.OrderBy(f => {
                int indexTemp = sortConfig.IndexOf(f.Id);
                if (indexTemp == -1) {
                    indexTemp = fields.Count;
                }
                return indexTemp;
            }).ToList();
            fields.ForEach(f => {
                if (sortConfig.IndexOf(f.Id) != -1) {
                    f.Visible = true;
                }
            });
            return fields;
        }

        private string GetDefaultStoragePath() {
            string defaultPath = MainUtils.GetBaseDirectory() + "OperationDataStorage\\";
            // 如果文件夹不存在，则创建文件夹
            if (!Directory.Exists(defaultPath)) {
                Directory.CreateDirectory(defaultPath);
            }
            return defaultPath;
        }
    }
}
