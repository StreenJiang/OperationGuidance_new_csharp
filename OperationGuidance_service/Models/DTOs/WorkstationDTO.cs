using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class WorkstationDTO: ADTOBase {
        public string? name { get; set; }
        public int? tool_id { get; set; }
        public string? tool_name { get; set; }
        public string? tool_description { get; set; }
        public int? tool_type { get; set; }
        public string? tool_ip { get; set; }
        public int? tool_port { get; set; }
        public int? arm_id { get; set; }
        public string? arm_name { get; set; }
        public string? arm_description { get; set; }
        public int? arm_type { get; set; }
        public string? arm_ip { get; set; }
        public int? arm_port { get; set; }
        public int? serial_port_id { get; set; }
        public string? serial_port_name { get; set; }
        public string? serial_port_description { get; set; }
        public int? serial_port_type { get; set; }
        public string? serial_port_port_name { get; set; }
        public int? serial_port_baud_rate { get; set; }
        public int? serial_port_data_bit { get; set; }
        public int? serial_port_parity { get; set; }
        public int? serial_port_stop_bit { get; set; }
        public int? serial_port_data_type { get; set; }
        public int? communication_id { get; set; }
        public string? communication_name { get; set; }
        public string? communication_description { get; set; }
        public int? communication_type { get; set; }
        public string? communication_ip { get; set; }
        public int? communication_port { get; set; }
        public int? enabled { get; set; }
    }
}
