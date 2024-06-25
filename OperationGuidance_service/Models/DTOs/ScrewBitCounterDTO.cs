using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ScrewBitCounterDTO: ADTOBase {
        public int mission_id { get; set; }
        public int bit_position { get; set; }
        public int max_num { get; set; }
        public int current_counts { get; set; }
        public int clear_times { get; set; }
    }
}
