using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateOperationDataRsp: ControlResponse {
        public OperationDataDTO OperationDataDTO { get; set; }

        public AddOrUpdateOperationDataRsp(OperationDataDTO curveDataDTO) {
            OperationDataDTO = curveDataDTO;
        }
    }
}
