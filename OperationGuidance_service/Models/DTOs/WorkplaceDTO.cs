using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models {
    public class WorkplaceDTO: ADTOBase {
        public string name { get; set; } = "workstation_name";
        public int? tool_id { get; set; }
        public string tool_name { get; set; } = "tool_name";
        public string? tool_description { get; set; }
        public int tool_device_type_id { get; set; }
        public string? tool_device_type_name { get; set; }
        public int tool_device_category_id { get; set; }
        public string? tool_device_category_name { get; set; }
        public int tool_can_manipulate { get; set; }
        public string? tool_icon_normal { get; set; }
        public string? tool_icon_error { get; set; }
        public int tool_brand_id { get; set; }
        public string? tool_brand_name { get; set; }
        public string tool_ip { get; set; } = "192.168.0.0";
        public int tool_port { get; set; }
        public int? arm_id { get; set; }
        public string arm_name { get; set; } = "arm_name";
        public string? arm_description { get; set; }
        public int arm_device_type_id { get; set; }
        public string? arm_device_type_name { get; set; }
        public int arm_device_category_id { get; set; }
        public string? arm_device_category_name { get; set; }
        public int arm_can_manipulate { get; set; }
        public string? arm_icon_normal { get; set; }
        public string? arm_icon_error { get; set; }
        public int arm_brand_id { get; set; }
        public string? arm_brand_name { get; set; }
        public string arm_ip { get; set; } = "192.168.0.1";
        public int arm_port { get; set; }
        public int enabled { get; set; } = (int) YesOrNo.YES;
    }
}
