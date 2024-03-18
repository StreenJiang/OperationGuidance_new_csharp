using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("product_mission")]
    public class ProductMission: AEntityBase {
        public string name { get; set; } = "product_name";
        public string? pn_code { get; set; }
        public int max_ng_num { get; set; } = 0;
        public int enabled { get; set; } = (int) YesOrNo.YES;
    }
}
