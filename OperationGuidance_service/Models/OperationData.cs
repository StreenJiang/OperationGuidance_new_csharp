using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("operation_data")]
    public class OperationData: AEntityBase {
        public int? procedure_set { get; set; }
        public string tightened_status { get; set; } = string.Empty;
        public string torque_status { get; set; } = string.Empty;
        public string angle_status { get; set; } = string.Empty;
        public float torque { get; set; } = 0.0F;
        public float torque_max { get; set; } = 0.0F;
        public float angle { get; set; } = 0.0F;
        public float angle_max { get; set; } = 0.0F;
        public float angle_target { get; set; } = 0.0F;
        public float angle_min { get; set; } = 0.0F;
        public int? batch_current { get; set; }
        public int? batch_sum { get; set; }
        public string batch_status { get; set; } = string.Empty;
    }
}
