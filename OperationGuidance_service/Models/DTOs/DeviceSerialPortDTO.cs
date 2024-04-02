using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceSerialPortDTO: ADTOBase {
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
        public int type { get; set; }
        public string port_name { get; set; } = string.Empty;
        public string port_full_name { get; set; } = string.Empty;
        public int baud_rate { get; set; }
        public int data_bit { get; set; }
        public int parity { get; set; }
        public int stop_bit { get; set; }
        public int data_type { get; set; }
        public string? invalid_char { get; set; }
        public int macs_id { get; set; } = SystemUtils.MacAddressesDTO.id;
    }
}
