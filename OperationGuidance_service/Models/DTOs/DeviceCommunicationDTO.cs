using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceCommunicationDTO: ADTOBase {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? model { get; set; }
    }
}
