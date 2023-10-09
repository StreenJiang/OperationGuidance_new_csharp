using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_category")]
    public class DeviceCategory: AEntityBase {
        public string name { get; set; } = "device_category";
        public string? description { get; set; }
        public int can_manipulate { get; set; } = 2;
        public string? icon_normal { get; set; }
        public string? icon_error { get; set; }
    }
}
