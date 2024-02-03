using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceArmDTO: ADTOBase {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? type { get; set; }
    }
}
