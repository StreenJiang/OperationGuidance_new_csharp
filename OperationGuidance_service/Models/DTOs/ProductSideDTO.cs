using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class ProductSideDTO: ADTOBase {
        public int? mission_id { get; set; }
        public string? name { get; set; }
        public string? image { get; set; }
        public int? max_rectangle_width { get; set; }
        public int? max_rectangle_height { get; set; }
        public string? max_rectangle_location { get; set; }
        public string? center_location { get; set; }
        public string? location_offset { get; set; }
        public string? location_offset_moving { get; set; }
        public float? zooming_ratio { get; set; }
        public float? zooming_ratio_extra { get; set; }
        public float? rotate_angle { get; set; }
        public int? cropped { get; set; }
        public List<ProductBoltDTO>? Bolts { get; set; }
    }
}
