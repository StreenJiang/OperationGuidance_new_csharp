using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class DeviceCommunicationVO: AVOBase {
        [GridColumn("设备名称")]
        public string? name { get; set; }
        [GridColumn("设备描述")]
        public string? description { get; set; }
        [GridColumn("IP地址")]
        public string? ip { get; set; }
        [GridColumn("端口号")]
        public int? port { get; set; }
        [GridColumn("设备型号")]
        public string? str_type { get; set; }
        private int? _type_id;
        public int? type { 
            get => _type_id;
            set {
                _type_id = value;
                if (value != null) {
                    str_type = DeviceType_Communication.GetNameById(value.Value);
                }
            } 
        }
    }
}
