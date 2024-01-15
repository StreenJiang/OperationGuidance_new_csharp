using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("brand")]
    public class Brand: AEntityBase {
        public string name { get; set; } = "brand_name";
        public string? short_name { get; set; }
        public string? english_name { get; set; }
        public string? english_short_name { get; set; }
        public string? description { get; set; }
    }
}
