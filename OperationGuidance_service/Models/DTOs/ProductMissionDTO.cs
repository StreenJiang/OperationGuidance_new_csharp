using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.DTOs {
    public class ProductMissionDTO: ADTOBase {
        public string name { get; set; }
        public string? pn_code { get; set; }
        public int max_ng_num { get; set; } = 3;
        public int password_need_time { get; set; } = 2;
        public int enabled { get; set; }
        public List<ProductSideDTO>? ProductSides { get; set; }
        public int macs_id { get; set; } = SystemUtils.MacAddressesDTO.id;
        public int? predecessor_mission_id { get; set; }
        public string? predecessor_part_mission_ids { get; set; }
        public int? multi_device_independence { get; set; }
    }
}
