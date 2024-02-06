using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceSerialPortDTO: ADTOBase {
        public string? name { get; set; }
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
