using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("mission_record")]
    public class MissionRecord: AEntityBase {
        public int mission_id { get; set; }
        public string product_batch { get; set; } = string.Empty;
        public string? product_bar_code { get; set; }
        public string? parts_bar_code { get; set; }
        public int mission_result { get; set; }
        public int is_redo { get; set; }
    }
}
