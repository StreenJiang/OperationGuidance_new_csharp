using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceModelDTO: ADTOBase {
        public string? name { get; set; }
        public string? description { get; set; }
        public int? category_id { get; set; }
        public string? device_category_name { get; set; }
        public int? brand_id { get; set; }
        public string? brand_name { get; set; }
    }
}
