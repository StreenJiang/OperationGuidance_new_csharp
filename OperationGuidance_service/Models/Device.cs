using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device")]
    public class Device: AEntityBase {
        public string name { get; set; } = "device";
        public string? description { get; set; }
        public int model_id { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
    }
}
