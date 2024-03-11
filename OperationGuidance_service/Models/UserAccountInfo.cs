using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("user_account_info")]
    public class UserAccountInfo: AEntityBase {
        public int staff_id { get; set; } = -1;
        public string name { get; set; } = string.Empty;
        public string? position { get; set; }
        public string account { get; set; } = string.Empty;
        public string? password { get; set; }
        public int role_type { get; set; }
        public string? operation_password { get; set; }
    }
}
