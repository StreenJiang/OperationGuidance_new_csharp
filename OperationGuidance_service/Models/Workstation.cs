using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("workstation")]
    public class Workstation: AEntityBase {
        public string name { get; set; } = "workstation_name";
        public int? tool_id { get; set; }
        public int? arm_id { get; set; }
        public int enabled { get; set; } = (int) YesOrNo.YES;
    }
}
