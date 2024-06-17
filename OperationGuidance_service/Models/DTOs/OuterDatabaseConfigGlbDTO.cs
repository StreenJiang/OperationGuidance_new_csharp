using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class OuterDatabaseConfigGlbDTO : ADTOBase {
        public string? host { get; set; }
        public int? port { get; set; }
        public string? database_name { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public int? database_type { get; set; }
        public string? workstation_id { get; set; }
    }
}
