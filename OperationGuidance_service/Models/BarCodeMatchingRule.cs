using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("bar_code_matching_rule")]
    public class BarCodeMatchingRule: AEntityBase {
        public string? name { get; set; }
        public int? length { get; set; }
        public string? end_char { get; set; }
        public string? key_position { get; set; }
        public string? key_char { get; set; }
        public int type { get; set; }
        public int mission_id { get; set; }
        public string? part_no { get; set; }
        public int macs_id { get; set; }
    }
}
