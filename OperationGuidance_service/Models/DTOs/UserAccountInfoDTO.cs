using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class UserAccountInfoDTO: ADTOBase {
        public int? staff_id { get; set; }
        public string? name { get; set; }
        public string? position { get; set; }
        public string? account { get; set; }
        public string? password { get; set; }
    }
}
