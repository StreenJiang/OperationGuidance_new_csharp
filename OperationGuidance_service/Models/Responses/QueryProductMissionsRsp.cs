using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryProductMissionsRsp: ControlResponse {
        public List<ProductMissionDTO> ProductMissionsDTOs {
            get; set;
        }
    }
}
