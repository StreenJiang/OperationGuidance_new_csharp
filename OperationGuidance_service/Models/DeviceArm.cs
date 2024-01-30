using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_arm")]
    public class DeviceArm: AEntityBase {
        public string name { get; set; } = "device_arm";
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? model { get; set; }
    }
}
