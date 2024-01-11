using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class DeviceVO: AVOBase {
        [GridColumn("设备名称")]
        public string? name { get; set; }
        [GridColumn("设备描述")]
        public string? description { get; set; }
        [GridColumn("设备品牌")]
        public string? brand_name { get; set; }
        [GridColumn("设备类型")]
        public string? category_name { get; set; }
        [GridColumn("设备型号")]
        public string? model_name { get; set; }
        public int? model_id { get; set; }
        [GridColumn("IP地址")]
        public string? ip { get; set; }
        [GridColumn("端口号")]
        public int? port { get; set; }
    }
}
