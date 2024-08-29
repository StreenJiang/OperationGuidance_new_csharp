using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("mat_code_map_whyc")]
    public class MatCodeMapWhyc: AEntityBase {
        public string mat_code { get; set; }
        public int parameter_set { get; set; }
        public int macs_id { get; set; }
    }
}
