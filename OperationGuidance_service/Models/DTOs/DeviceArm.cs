using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceArmDTO: ADTOBase {
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
        public string ip { get; set; } = string.Empty;
        public int port { get; set; }
        public int type { get; set; }
        public int macs_id { get; set; } = SystemUtils.MacAddressesDTO.id;
    }
}
