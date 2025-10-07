using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class PartsBarCodeDTO: ADTOBase {
        public int? mission_record_id { get; set; }
        public string parts_bar_code { get; set; } = string.Empty;
    }
}
