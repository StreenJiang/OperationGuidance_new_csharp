using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateOperationDataReq: ControlResponse {
        public OperationDataDTO OperationDataDTO { get; set; }

        public AddOrUpdateOperationDataReq(OperationDataDTO curveDataDTO) {
            OperationDataDTO = curveDataDTO;
        }
    }
}
