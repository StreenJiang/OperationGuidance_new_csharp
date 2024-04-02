using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_communication")]
    public class DeviceCommunication: AEntityBase {
        public string name { get; set; } = "device_communication";
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? type { get; set; }
        public int macs_id { get; set; }
    }
}
