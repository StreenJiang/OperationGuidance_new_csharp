using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("user_account_info")]
    public class UserAccountInfo: AEntityBase {
        public int staff_id { get; set; } = -1;
        public string name { get; set; } = "user";
        public string position { get; set; } = "position";
        public string account { get; set; } = "account";
        public string password { get; set; } = "password";
    }
}
