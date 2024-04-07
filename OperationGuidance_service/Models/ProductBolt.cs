using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("product_bolt")]
    public class ProductBolt: AEntityBase {
        public int side_id { get; set;}
        public int serial_num { get; set;}
        public string? name { get; set; }
        public float? specification { get; set; }
        public int? workstation_id { get; set; }
        public string? position { get; set; }
        public float location_x_percent { get; set; }
        public float location_y_percent { get; set; }
        public float? bit_specification { get; set; }
        public int? parameters_set { get; set; }
        public float? torque_min { get; set; }
        public float? torque_max { get; set; }
        public float? angle_min { get; set; }
        public float? angle_max { get; set; }
        public int? enabled { get; set; }
    }
}
