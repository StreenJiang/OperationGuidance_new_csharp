using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_model")]
    public class DeviceModel: AEntityBase {
        public string name { get; set; } = "device_type";
        public string? description { get; set; }
        public int category_id { get; set; }
        public int brand_id { get; set; }
    }
}
