using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_io")]
    public class DeviceIo: AEntityBase {
        public string name { get; set; } = "device_io";
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? type { get; set; }
        public int macs_id { get; set; }
    }
}
