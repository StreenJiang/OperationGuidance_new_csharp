using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("product")]
    public class Product: AEntityBase {
        public string name { get; set; } = "product";
        public string? description { get; set; }
    }
}
