using System.ComponentModel.DataAnnotations.Schema;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models {
    [Table("parts_bar_code")]
    public class PartsBarCode: AEntityBase {
        public int? mission_record_id { get; set; }
        public string parts_bar_code { get; set; } = string.Empty;
    }
}
