using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("curve_data")]
    public class CurveData : AEntityBase {
        public int operation_data_id { get; set; }
        public string result_data_identifier { get; set; }
        public string time_stamp { get; set; }
        public int data_type { get; set; }
        public string data_samples { get; set; }
    }
}
