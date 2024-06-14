using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class CurveDataDTO : ADTOBase {
        public int operation_data_id { get; set; }
        public string result_data_identifier { get; set; }
        public string time_stamp { get; set; }
        public int data_type { get; set; }
        public string data_samples { get; set; }
    }
}