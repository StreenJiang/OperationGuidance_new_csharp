using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class BarCodeMatchingRuleDTO: ADTOBase {
        public int? length { get; set; }
        public string? end_char { get; set; }
        public string? key_position { get; set; }
        public string? key_char { get; set; }
        public int type { get; set; }
        public int mission_id { get; set; }
    }
}
