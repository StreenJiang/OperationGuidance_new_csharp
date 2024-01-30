using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("device_tool")]
    public class DeviceTool: AEntityBase {
        public string name { get; set; } = "device_tool";
        public string? description { get; set; }
        public string? ip { get; set; }
        public int? port { get; set; }
        public int? model { get; set; }
    }
}
