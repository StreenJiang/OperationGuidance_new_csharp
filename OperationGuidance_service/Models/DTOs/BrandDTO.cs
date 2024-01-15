using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class BrandDTO: ADTOBase {
        public string? name { get; set; }
        public string? short_name { get; set; }
        public string? english_name { get; set; }
        public string? english_short_name { get; set; }
        public string? description { get; set; }
    }
}
