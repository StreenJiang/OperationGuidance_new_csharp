using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("outer_database_config")]
    public class OuterDatabaseConfig: AEntityBase {
        public string? host { get; set; }
        public int? port { get; set; }
        public string? database_name { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public int? database_type { get; set; }
    }
}
