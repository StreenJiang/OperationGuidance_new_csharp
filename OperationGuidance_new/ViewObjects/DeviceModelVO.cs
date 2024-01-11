using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class DeviceModelVO: AVOBase {
        [GridColumn("设备型号名称")]
        public string? name { get; set; }
        [GridColumn("设备型号描述")]
        public string? description { get; set; }
        [GridColumn("设备品牌")]
        public string? brand_name { get; set; }
        public int? brand_id { get; set; }
        [GridColumn("设备类型")]
        public string? category_name { get; set; }
        public int? category_id { get; set; }
    }
}
