using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceCategoryDTO: ADTOBase {
        public string? name { get; set; }
        public string? description { get; set; }
        public int? can_manipulate { get; set; }
        public string? icon_normal { get; set; }
        public string? icon_normal_name { get; set; }
        public string? icon_error { get; set; }
        public string? icon_error_name { get; set; }
    }
}
