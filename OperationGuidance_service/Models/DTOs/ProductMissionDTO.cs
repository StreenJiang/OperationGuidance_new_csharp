using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ProductMissionDTO: ADTOBase {
        public string name { get; set; }
        public string? pn_code { get; set; }
        public int max_ng_num { get; set; }
        public int enabled { get; set; }
        public List<ProductSideDTO>? ProductSides { get; set; }
    }
}
