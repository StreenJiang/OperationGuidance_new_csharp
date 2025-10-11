using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class BatchAddOperationDataReq: ControlRequest {
        public List<OperationDataDTO> OperationDataDTOs { get; set; }

        public BatchAddOperationDataReq(List<OperationDataDTO> operationDataDTOs) {
            OperationDataDTOs = operationDataDTOs;
        }
    }
}
