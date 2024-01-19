using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models {
    public class OperationDataDTO: ADTOBase {
        public int? procedure_set { get; set; }
        public string? tightened_status { get; set; }
        public string? torque_status { get; set; }
        public string? angle_status { get; set; }
        public float? torque { get; set; }
        public float? torque_max { get; set; }
        public float? angle { get; set; }
        public float? angle_max { get; set; }
        public float? angle_target { get; set; }
        public float? angle_min { get; set; }
        public int? batch_current { get; set; }
        public int? batch_sum { get; set; }
        public string? batch_status { get; set; }
    }
}
