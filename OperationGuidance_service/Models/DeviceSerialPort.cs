using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_serial_port")]
    public class DeviceSerialPort: AEntityBase {
        public string name { get; set; } = "device_serial_port";
        public string? description { get; set; }
        public int? type { get; set; }
        public string? port_name { get; set; }
        public string? port_full_name { get; set; }
        public int? baud_rate { get; set; }
        public int? data_bit { get; set; }
        public int? parity { get; set; }
        public int? stop_bit { get; set; }
        public int? data_type { get; set; }
        public string? invalid_char { get; set; }
    }
}
