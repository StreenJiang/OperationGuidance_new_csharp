using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class UserAccountInfoDTO: ADTOBase {
        public int staff_id { get; set; } = -1;
        public string name { get; set; } = string.Empty;
        public string? position { get; set; }
        public string account { get; set; } = string.Empty;
        public string? password { get; set; }
        public int role_type { get; set; }
        public string? operation_password { get; set; }
    }
}
