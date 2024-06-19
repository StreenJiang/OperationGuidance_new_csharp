using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.DTOs {
    public class OuterDatabaseConfigGlbDTO: ADTOBase {
        public string? host { get; set; }
        public int? port { get; set; }
        public string? database_name { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public int? database_type { get; set; }
        public string? workstation_name { get; set; }
        public int macs_id { get; set; } = SystemUtils.MacAddressesDTO.id;
    }
}
