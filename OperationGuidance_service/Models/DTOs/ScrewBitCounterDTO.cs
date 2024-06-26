using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ScrewBitCounterDTO: ADTOBase {
        public int mission_id { get; set; }
        public int bit_position { get; set; }
        public int max_num { get; set; } = 0;
        public int current_counts { get; set; } = 0;
        public int count_each_time { get; set; } = 0;
        public int clear_times { get; set; } = 0;
    }
}
