using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class DeviceDTO: ADTOBase {
        public string name { get; set; } = "device";
        public string? description {
            get; set;
        }
        public int device_type_id {
            get; set;
        }
        public string? device_type_name {
            get; set;
        }
        public int device_category_id {
            get; set;
        }
        public string? device_category_name {
            get; set;
        }
        public int can_manipulate {
            get; set;
        }
        public string? icon_normal {
            get; set;
        }
        public string? icon_error {
            get; set;
        }
        public int brand_id {
            get; set;
        }
        public string? brand_name {
            get; set;
        }
        public string ip {
            get; set;
        }
        public int port {
            get; set;
        }
    }
}
