using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("brand")]
    public class Brand: AEntityBase {
        public string name { get; set; }
        public string? description { get; set; }
    }
}
