using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("screw_bit_counter")]
    public class ScrewBitCounter: AEntityBase {
        public int mission_id { get; set; }
        public int bit_position { get; set; }
        public int max_num { get; set; }
        public int current_counts { get; set; }
        public int count_each_time { get; set; }
        public int clear_times { get; set; }
    }
}
