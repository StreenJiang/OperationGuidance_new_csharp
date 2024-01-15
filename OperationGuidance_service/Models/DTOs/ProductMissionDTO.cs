using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ProductMissionDTO: ADTOBase {
        public string? name { get; set; }
        public int? product_id { get; set; }
        public string? product_name { get; set; }
        public string? product_description { get; set; }
        public string? pn_code { get; set; }
        public int? enabled { get; set; }
        public List<ProductSideDTO>? ProductSides { get; set; }
    }
}
