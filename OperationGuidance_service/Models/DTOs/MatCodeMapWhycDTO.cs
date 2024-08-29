using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.DTOs {
    public class MatCodeMapWhycDTO: ADTOBase {
        public string mat_code { get; set; }
        public int parameter_set { get; set; }
        public int macs_id { get; set; } = SystemUtils.MacAddressesDTO.id;
    }
}
