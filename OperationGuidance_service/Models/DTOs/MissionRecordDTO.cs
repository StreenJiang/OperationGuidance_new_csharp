using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class MissionRecordDTO: AEntityBase {
        public int mission_id { get; set; }
        public string product_batch { get; set; } = string.Empty;
        public string? product_bar_code { get; set; }
        public string? parts_bar_code { get; set; }
        public int mission_result { get; set; }
        public int is_redo { get; set; }
    }
}
