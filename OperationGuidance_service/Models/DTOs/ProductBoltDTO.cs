using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ProductBoltDTO: ADTOBase {
        public int? side_id { get; set; }
        public int? serial_num { get; set; }
        public string? name { get; set; } = string.Empty;
        public string? description { get; set; }
        public float? specification { get; set; }
        public string? position { get; set; }
        public float? location_x_percent { get; set; }
        public float? location_y_percent { get; set; }
        public int? tool_id { get; set; }
        public string? tool_name { get; set; }
        public string? tool_description { get; set; }
        public string? tool_category_name { get; set; }
        public string? tool_category_icon_normal { get; set; }
        public string? tool_category_icon_error { get; set; }
        public string? tool_type_name { get; set; }
        public string? tool_brand_name { get; set; }
        public string? tool_ip { get; set; }
        public int? tool_port { get; set; }
        public float? bit_specification { get; set; }
        public int? procedure_set { get; set; }
        public float? torque_min { get; set; }
        public float? torque_max { get; set; }
        public float? angle_min { get; set; }
        public float? angle_max { get; set; }
        public int? enabled { get; set; }
    }
}
